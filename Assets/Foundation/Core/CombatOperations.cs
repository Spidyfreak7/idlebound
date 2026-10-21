using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Idlebound.Foundation
{
    public sealed class CharacterVitals
    {
        public decimal MaxHp { get; }
        public decimal Hp { get; internal set; }
        internal CharacterVitals(decimal maxHp, decimal hp) { MaxHp = maxHp; Hp = hp; }
    }

    public sealed class CombatEncounter
    {
        public string MonsterId { get; }
        public decimal EnemyHp { get; internal set; }
        public long PlayerRemainingTicks { get; internal set; }
        public long EnemyRemainingTicks { get; internal set; }
        internal CombatEncounter(string monster, decimal hp, long playerTicks, long enemyTicks)
        { MonsterId = monster; EnemyHp = hp; PlayerRemainingTicks = playerTicks; EnemyRemainingTicks = enemyTicks; }
        internal CombatEncounter Copy() => new CombatEncounter(MonsterId, EnemyHp, PlayerRemainingTicks, EnemyRemainingTicks);
    }

    public static class CombatMath
    {
        public static decimal Triangle(CombatStyle attacker, CombatStyle defender)
        {
            if (!Enum.IsDefined(typeof(CombatStyle), attacker) || !Enum.IsDefined(typeof(CombatStyle), defender)) throw new ArgumentException("Unknown style.");
            if (attacker == defender) return 1;
            return attacker == CombatStyle.Melee && defender == CombatStyle.Ranged ||
                attacker == CombatStyle.Ranged && defender == CombatStyle.Magic ||
                attacker == CombatStyle.Magic && defender == CombatStyle.Melee ? 2m : 0.5m;
        }
        public static decimal ActualDamage(decimal damage, decimal remainingHp)
        {
            if (damage < 0 || remainingHp < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            return Math.Min(damage, remainingHp);
        }
    }

    public sealed partial class GatheringSession
    {
        private readonly Dictionary<string, CharacterVitals> vitals = new Dictionary<string, CharacterVitals>();
        private readonly Dictionary<string, CombatEncounter> encounters = new Dictionary<string, CombatEncounter>();
        private readonly Dictionary<string, long> killCounts = new Dictionary<string, long>();
        private readonly IDeterministicLootRoller lootRoller;
        public CombatCatalog Combat { get; }
        public ReadOnlyDictionary<string, CharacterVitals> Vitals { get; private set; }
        public ReadOnlyDictionary<string, CombatEncounter> Encounters { get; private set; }
        public long KillsFor(string characterId, string monsterId) => killCounts.TryGetValue(characterId + "|" + monsterId, out var count) ? count : 0;

        public bool StartCombatAt(long ticks, string characterId, string monsterId, CombatStyle style, bool swap, out string error)
        {
            SettleTo(ticks);
            var monster = Combat.Monsters.FirstOrDefault(m => m.Id == monsterId);
            if (monster == null || !vitals.TryGetValue(characterId ?? "", out var health) || health.Hp <= 0)
            { error = "Unknown monster/character or character is dead. No automatic recovery is implemented."; return false; }
            var profile = Profile(characterId);
            if (profile.Style != style) { error = "Requested style must match the equipped mainhand (unarmed is Melee)."; return false; }
            if (encounters.TryGetValue(characterId, out var oldFight) && oldFight.MonsterId == monsterId)
            { error = "Already fighting this monster; progress was preserved."; return false; }
            if (!TryPlanChange(characterId, monsterId, swap, out var cancelled, out error)) return false;
            var refunded = PreviewRefunds(cancelled);
            var before = Assignments.ToDictionary(p => p.Key, p => p.Value);
            bool assigned = swap ? state.TrySwapAssignment(characterId, monsterId, out error) : state.TryAssign(characterId, monsterId, out error);
            if (!assigned) return false;
            ReplaceInventory(refunded);
            DiscardChangedJobs(before);
            partialTicks.Remove(characterId);
            encounters[characterId] = new CombatEncounter(monsterId, monster.MaxHp, profile.IntervalTicks, monster.AttackIntervalTicks);
            error = null;
            return true;
        }

        // Explicit development tool; never passive regen, food or automatic death recovery.
        public bool RestoreDevelopmentHpAt(long ticks, string characterId, out string error)
        {
            SettleTo(ticks);
            if (!vitals.TryGetValue(characterId ?? "", out var hp) || encounters.ContainsKey(characterId))
            { error = "Stop combat first; unknown character or still fighting."; return false; }
            hp.Hp = hp.MaxHp;
            error = null;
            return true;
        }

        private sealed class CombatPlan
        {
            public string CharacterId;
            public CharacterVitals Vitals;
            public CombatEncounter Fight;
            public SkillState Skill;
            public decimal Xp;
        }
        private sealed class CombatBatch
        {
            public readonly List<CombatPlan> Plans = new List<CombatPlan>();
            public Dictionary<string, long> Kills;
        }

        private CombatBatch PlanCombat(long eligibleTicks, bool offline, Dictionary<string, long> nextInventory, OfflineReport receipt)
        {
            var batch = new CombatBatch { Kills = new Dictionary<string, long>(killCounts) };
            long events = 0;
            foreach (var pair in encounters.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                string id = pair.Key;
                var fight = pair.Value.Copy();
                var hp = new CharacterVitals(vitals[id].MaxHp, vitals[id].Hp);
                var monster = Combat.Monsters.First(m => m.Id == fight.MonsterId);
                var profile = Profile(id);
                string skillId = profile.Style.ToString().ToLowerInvariant();
                decimal rate = XpPolicy.Efficiency(IsActive(id), offline, skillId, EffectsFor(id));
                decimal xp = 0;
                long remaining = eligibleTicks;
                while (remaining > 0 && hp.Hp > 0)
                {
                    long next = Math.Min(fight.PlayerRemainingTicks, fight.EnemyRemainingTicks);
                    if (next > remaining)
                    {
                        fight.PlayerRemainingTicks -= remaining; fight.EnemyRemainingTicks -= remaining;
                        break;
                    }
                    remaining -= next;
                    fight.PlayerRemainingTicks -= next; fight.EnemyRemainingTicks -= next;
                    if (++events > Combat.Rules.EventBudget) throw new InvalidOperationException("Development combat event budget exceeded. Settle smaller time chunks; no state was committed.");
                    // Deterministic tie policy: player first; a defeated monster cannot retaliate.
                    if (fight.PlayerRemainingTicks == 0)
                    {
                        decimal actual = CombatMath.ActualDamage(checked(profile.Damage * CombatMath.Triangle(profile.Style, monster.Style)), fight.EnemyHp);
                        fight.EnemyHp -= actual;
                        xp = checked(xp + checked(actual * Combat.Rules.XpPerDamage(profile.Style) * rate));
                        fight.PlayerRemainingTicks = profile.IntervalTicks;
                        if (fight.EnemyHp == 0)
                        {
                            string key = id + "|" + monster.Id;
                            long kill = checked((batch.Kills.TryGetValue(key, out var count) ? count : 0) + 1);
                            batch.Kills[key] = kill;
                            foreach (var drop in Combat.Drops.Where(d => d.MonsterId == monster.Id))
                            {
                                bool granted = drop.Chance == 1;
                                if (drop.Chance > 0 && drop.Chance < 1)
                                {
                                    decimal roll = lootRoller.Roll(id, monster.Id, kill, drop.Id);
                                    if (roll < 0 || roll >= 1) throw new InvalidOperationException("Deterministic loot roller must return [0,1).");
                                    granted = roll < drop.Chance;
                                }
                                if (granted)
                                {
                                    nextInventory[drop.ItemId] = checked((nextInventory.TryGetValue(drop.ItemId, out var banked) ? banked : 0) + drop.Quantity);
                                    receipt?.For(id,monster.Id).Item(drop.ItemId,drop.Quantity);
                                }
                            }
                            // No travel delay or free immediate hit: both clocks restart for the new enemy.
                            fight.EnemyHp = monster.MaxHp;
                            fight.EnemyRemainingTicks = monster.AttackIntervalTicks;
                            continue;
                        }
                    }
                    if (fight.EnemyRemainingTicks == 0)
                    {
                        decimal outgoing = checked(monster.Damage * CombatMath.Triangle(monster.Style, profile.Style));
                        hp.Hp -= CombatMath.ActualDamage(Math.Max(0, outgoing - profile.Defense), hp.Hp);
                        fight.EnemyRemainingTicks = monster.AttackIntervalTicks;
                    }
                }
                var skill = Characters.First(c => c.Id == id).Skills.First(s => s.SkillId == skillId);
                curve.ApplyAward(skill.StoredXp, xp); // Preflight XP before any bank/HP/counter changes.
                if (receipt != null)
                {
                    var outcome = receipt.For(id,monster.Id); outcome.Award(skillId,curve.ApplyAward(skill.StoredXp,xp) - skill.StoredXp);
                    if (hp.Hp == 0) outcome.StopReason = "Defeated";
                }
                batch.Plans.Add(new CombatPlan { CharacterId = id, Vitals = hp, Fight = fight, Skill = skill, Xp = xp });
            }
            return batch;
        }

        private void CommitCombat(CombatBatch batch)
        {
            killCounts.Clear(); foreach (var pair in batch.Kills) killCounts.Add(pair.Key, pair.Value);
            foreach (var plan in batch.Plans)
            {
                vitals[plan.CharacterId].Hp = plan.Vitals.Hp;
                plan.Skill.Award(plan.Xp, curve);
                if (plan.Vitals.Hp == 0)
                {
                    state.Unassign(plan.CharacterId);
                    encounters.Remove(plan.CharacterId);
                    partialTicks.Remove(plan.CharacterId);
                }
                else encounters[plan.CharacterId] = plan.Fight;
            }
        }
    }
}
