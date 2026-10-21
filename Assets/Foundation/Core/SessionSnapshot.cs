using System;
using System.Globalization;
using System.Linq;

namespace Idlebound.Foundation
{
    public sealed partial class GatheringSession
    {
        public SessionSnapshot Capture()
        {
            if (!(lootRoller is SeededLootRoller seeded)) throw new InvalidOperationException("Custom loot rollers require a persistence adapter.");
            return new SessionSnapshot
            {
                Slots = state.Access.AvailableSlots, Active = ActiveCharacterId, Ticks = LastSettledTicks,
                EquipmentSerial = equipmentSerial, LootSeed = seeded.Seed, RefundInputs = RefundCancelledInputs,
                Characters = Characters.Select(c => new SavedCharacter
                {
                    Id = c.Id, Name = c.DisplayName, Assignment = Assignments.TryGetValue(c.Id,out var a) ? a : null,
                    Job = productionJobs.TryGetValue(c.Id,out var j) ? j.Id : null,
                    Skills = c.Skills.Select(s => new SavedSkill { Id = s.SkillId, Xp = s.StoredXp, Level = s.Level }).ToArray(),
                    Hp = vitals[c.Id].Hp, Partial = PartialTicks(c.Id), Cycles = CompletedCycles(c.Id),
                    Encounter = encounters.TryGetValue(c.Id,out var f) ? new SavedEncounter
                        { Monster = f.MonsterId, Hp = f.EnemyHp, PlayerTicks = f.PlayerRemainingTicks, EnemyTicks = f.EnemyRemainingTicks } : null,
                    Effects = EffectsFor(c.Id).Select(e => new SavedEffect { Source = e.SourceId, Skill = e.SkillId, Efficiency = e.Efficiency }).ToArray()
                }).ToArray(),
                Bank = inventory.OrderBy(p => p.Key,StringComparer.Ordinal).Select(p => new ItemAmount { Id = p.Key, Quantity = p.Value }).ToArray(),
                Equipment = loadouts.SelectMany(p => p.Value.Items.Values.Select(i => SaveGear(i,p.Key)))
                    .Concat(bankInstances.Values.Select(i => SaveGear(i,null))).OrderBy(i => i.Instance,StringComparer.Ordinal).ToArray(),
                Kills = killCounts.OrderBy(p => p.Key,StringComparer.Ordinal).Select(p => new SavedKills
                    { Character = p.Key.Split('|')[0], Monster = p.Key.Split('|')[1], Count = p.Value }).ToArray()
            };
        }
        private static SavedEquipment SaveGear(EquipmentInstance instance, string owner)
        {
            if (instance.PermanentEnchantmentId != null) throw new InvalidOperationException("Unimplemented enchantment cannot be saved.");
            return new SavedEquipment { Instance = instance.InstanceId, Item = instance.Definition.Id, Owner = owner };
        }
        private static void Require(bool condition, string message)
        { if (!condition) throw new ArgumentException("Invalid save: " + message); }

        // Restores a new, unowned session only. Failed validation cannot mutate the live session.
        public static GatheringSession Restore(SessionSnapshot data, GatheringSession catalogs)
        {
            Require(data != null && catalogs != null,"missing snapshot/catalogs");
            Require(data.Ticks >= 0 && data.Ticks <= long.MaxValue - DateTime.MaxValue.Ticks,"invalid simulation clock");
            Require(data.Slots >= RosterAccess.InitialAvailableSlots && data.Slots <= RosterAccess.Capacity,"roster slots");
            Require(data.EquipmentSerial >= 0 && data.EquipmentSerial < long.MaxValue,"equipment serial");
            Require(data.RefundInputs == catalogs.RefundCancelledInputs,"cancellation policy changed");
            Require(data.Characters != null && data.Characters.Length >= 1 && data.Characters.Length <= data.Slots,"roster");
            Require(data.Bank != null && data.Equipment != null && data.Kills != null,"missing collections");
            var result = new GatheringSession(catalogs.Catalog,catalogs.curve,catalogs.Production,data.RefundInputs,catalogs.Equipment,catalogs.Combat,new SeededLootRoller(data.LootSeed));
            result.state = new GameState(new RosterAccess(data.Slots),result.Skills,result.Activities);
            result.loadouts.Clear(); result.vitals.Clear();
            foreach (var c in data.Characters)
            {
                Require(c != null && c.Skills != null && c.Skills.All(s => s != null) && c.Effects != null,"character fields");
                Require(c.Id != null && !c.Id.Contains("|") && c.Partial >= 0 && c.Cycles >= 0,"character identity/counters");
                var skills = c.Skills.Select(s =>
                {
                    Require(s.Xp >= 0 && s.Xp <= result.curve.Thresholds[99] && s.Level == result.curve.KnownLevelFloor(s.Xp),"XP/level mismatch");
                    return new SkillState(s.Id,s.Level,s.Xp);
                });
                Require(result.state.TryAddCharacter(new CharacterState(c.Id,c.Name,skills)),"duplicate/unknown character or skill");
                result.loadouts.Add(c.Id,new CharacterEquipment());
                Require(c.Hp >= 0 && c.Hp <= result.Combat.Rules.PlayerMaxHp,"HP");
                result.vitals.Add(c.Id,new CharacterVitals(result.Combat.Rules.PlayerMaxHp,c.Hp));
                result.completedCycles.Add(c.Id,c.Cycles);
                Require(c.Effects.All(e => e != null),"null effect");
                result.SetEffectsAt(0,c.Id,c.Effects.Select(e => new BackgroundXpEffect(e.Source,e.Skill,e.Efficiency)));
            }
            Require(result.state.TrySelectActive(data.Active),"active character");
            foreach (var item in data.Bank)
            {
                Require(item != null && item.Id != null && result.BankItems.ContainsKey(item.Id) && item.Quantity > 0,"bank item/quantity");
                result.inventory.Add(item.Id,item.Quantity);
            }
            var instanceIds = new System.Collections.Generic.HashSet<string>();
            foreach (var item in data.Equipment)
            {
                Require(item != null && item.Instance != null && item.Instance.StartsWith("gear-",StringComparison.Ordinal) && instanceIds.Add(item.Instance),"duplicate/invalid instance");
                Require(long.TryParse(item.Instance.Substring(5),NumberStyles.None,CultureInfo.InvariantCulture,out var serial) && serial > 0 && serial <= data.EquipmentSerial,"instance serial");
                var definition = result.Equipment.Definitions.FirstOrDefault(d => d.Id == item.Item);
                Require(definition != null,"equipment catalog reference");
                var gear = new EquipmentInstance(item.Instance,definition);
                if (item.Owner == null) result.bankInstances.Add(item.Instance,gear);
                else
                {
                    Require(result.loadouts.ContainsKey(item.Owner),"equipment owner");
                    Require(definition.Requirements.All(p => result.Characters.First(c => c.Id == item.Owner).Skills.First(s => s.SkillId == p.Key).Level >= p.Value),"equip level gate");
                    result.loadouts[item.Owner].Items.Add(definition.Slot,gear);
                }
            }
            foreach (var group in result.bankInstances.Values.GroupBy(i => i.Definition.Id))
                Require(group.LongCount() <= result.Quantity(group.Key),"bank instances exceed quantity");
            foreach (var loadout in result.loadouts.Values)
                Require(!loadout.Items.ContainsKey(EquipmentSlot.Offhand) || !loadout.Items.TryGetValue(EquipmentSlot.Mainhand,out var main) || !main.Definition.TwoHanded,"two-handed conflict");
            foreach (var c in data.Characters)
            {
                if (c.Assignment != null) Require(result.state.TryAssign(c.Id,c.Assignment,out _),"assignment reference/gate/exclusivity");
                var activity = result.Activities.FirstOrDefault(a => a.Id == c.Assignment);
                if (c.Job != null)
                {
                    var recipe = result.Production.Recipes.FirstOrDefault(r => r.Id == c.Job);
                    Require(recipe != null && c.Assignment == c.Job,"reserved job mismatch");
                    result.productionJobs.Add(c.Id,recipe);
                }
                bool combat = result.Combat.Monsters.Any(m => m.Id == c.Assignment);
                Require(combat == (c.Encounter != null),"missing/orphan encounter");
                if (c.Encounter != null)
                {
                    var f = c.Encounter; var monster = result.Combat.Monsters.FirstOrDefault(m => m.Id == f.Monster);
                    Require(monster != null && f.Monster == c.Assignment && c.Hp > 0 && f.Hp > 0 && f.Hp <= monster.MaxHp,"encounter target/HP");
                    Require(f.PlayerTicks > 0 && f.PlayerTicks <= result.Profile(c.Id).IntervalTicks && f.EnemyTicks > 0 && f.EnemyTicks <= monster.AttackIntervalTicks,"encounter clocks");
                    result.encounters.Add(c.Id,new CombatEncounter(f.Monster,f.Hp,f.PlayerTicks,f.EnemyTicks));
                }
                Require(c.Partial == 0 || activity != null && !combat && c.Partial < activity.DurationTicks &&
                    (!result.Production.Recipes.Any(r => r.Id == c.Assignment) || c.Job != null),"cycle progress");
                if (activity != null && !combat) result.partialTicks[c.Id] = c.Partial;
            }
            foreach (var k in data.Kills)
            {
                Require(k != null && result.loadouts.ContainsKey(k.Character ?? "") && result.Combat.Monsters.Any(m => m.Id == k.Monster) && k.Count >= 0,"kill counter reference");
                result.killCounts.Add(k.Character + "|" + k.Monster,k.Count);
            }
            result.equipmentSerial = data.EquipmentSerial; result.LastSettledTicks = data.Ticks;
            return result;
        }
    }
}
