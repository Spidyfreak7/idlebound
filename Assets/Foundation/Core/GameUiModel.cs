using System;
using System.Collections.Generic;
using System.Linq;

namespace Idlebound.Foundation
{
    public enum MainScreen { Roster, Bank, Shop, CollectionLog }
    public enum CharacterScreen { Overview, Equipment, Activities }
    public enum BankFilter { All, Resources, Equipment, Products, Loot }
    public enum WorkStatus { Idle, Working, NeedsAttention }

    public sealed class CharacterView
    {
        public string Id, Name, Assignment, SkillId;
        public bool Active;
        public WorkStatus Status;
        public decimal Hp, MaxHp;
        public double Progress;
    }
    public sealed class SkillView
    {
        public string Id, Name;
        public int Level;
        public decimal Xp, Progress;
        public decimal? NextXp;
    }
    public sealed class StockView
    {
        public string Id, Name;
        public long Required, Available;
    }
    public sealed class ActivityView
    {
        public string Id, Name, Skill, Kind, Occupant, Description;
        public int Level;
        public bool Locked, Assigned, CanStart;
        public long Duration;
        public decimal? Xp;
        public StockView[] Inputs = Array.Empty<StockView>();
    }
    public sealed class BankItemView
    {
        public string Id, Name;
        public long Quantity;
        public BankFilter Type;
    }
    public sealed class StatChange
    {
        public string Name, Before, After;
        public bool Improvement;
    }

    // Navigation/projection never advances time, resets a session, or changes the active character.
    public sealed class GameUiModel
    {
        public SessionRuntime Runtime { get; }
        public GatheringSession Session => Runtime.Session;
        public MainScreen Screen { get; private set; }
        public CharacterScreen CharacterTab { get; private set; }
        public string InspectedId { get; private set; }
        public string SkillFilter { get; private set; } = "mining";
        public bool CharacterOpen { get; private set; }
        private readonly Dictionary<CharacterScreen, bool> sceneCollapsed = new Dictionary<CharacterScreen, bool>
        {
            { CharacterScreen.Overview, false }, { CharacterScreen.Equipment, true }, { CharacterScreen.Activities, true }
        };
        public bool SceneCollapsed { get => sceneCollapsed[CharacterTab]; set => sceneCollapsed[CharacterTab] = value; }
        public string Search { get; set; } = "";
        public BankFilter Filter { get; set; }
        public string Feedback { get; private set; } = "";
        public GameUiModel(SessionRuntime runtime)
        { Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime)); InspectedId = Session.Characters[0].Id; }
        public void Navigate(MainScreen screen)
        {
            if (!Enum.IsDefined(typeof(MainScreen), screen)) throw new ArgumentException("Unknown screen.");
            Screen = screen; CharacterOpen = false;
        }
        public void Inspect(string id)
        {
            if (!Session.Characters.Any(c => c.Id == id)) throw new ArgumentException("Unknown character.");
            InspectedId = id; CharacterOpen = true; Screen = MainScreen.Roster; CharacterTab = CharacterScreen.Overview;
        }
        public void ShowCharacterTab(CharacterScreen tab)
        {
            if (!Enum.IsDefined(typeof(CharacterScreen), tab)) throw new ArgumentException("Unknown character tab.");
            CharacterTab = tab;
        }
        public void ShowSkill(string id)
        {
            if (!Session.Skills.Any(s => s.Id == id)) throw new ArgumentException("Unknown skill.");
            SkillFilter = id; CharacterTab = CharacterScreen.Activities;
        }
        public bool MakeActive() => Result(Session.SelectAt(Session.LastSettledTicks, InspectedId), null, "Active character changed.");
        private bool Result(bool ok, string error, string success)
        { Feedback = ok ? success : error ?? "Action unavailable."; return ok; }
        public CharacterView Character(string id)
        {
            var c = Session.Characters.First(x => x.Id == id);
            Session.Assignments.TryGetValue(id, out var assignment);
            var activity = Session.Activities.FirstOrDefault(a => a.Id == assignment);
            bool production = Session.Production.Recipes.Any(r => r.Id == assignment);
            var hp = Session.Vitals[id];
            var result = new CharacterView { Id = id, Name = c.DisplayName, Active = Session.IsActive(id), Hp = hp.Hp, MaxHp = hp.MaxHp,
                Assignment = activity?.DisplayName ?? "No assignment", SkillId = activity?.SkillId,
                Status = hp.Hp == 0 || production && !Session.ProductionJobs.ContainsKey(id) ? WorkStatus.NeedsAttention : activity == null ? WorkStatus.Idle : WorkStatus.Working };
            if (Session.Encounters.TryGetValue(id, out var fight))
            {
                result.SkillId = Session.Profile(id).Style.ToString().ToLowerInvariant();
                result.Progress = 1 - fight.PlayerRemainingTicks / (double)Session.Profile(id).IntervalTicks;
            }
            else if (activity != null && activity.DurationTicks > 0)
                result.Progress = production && !Session.ProductionJobs.ContainsKey(id) ? 1 : Session.PartialTicks(id) / (double)activity.DurationTicks;
            return result;
        }
        public SkillView[] Skills(string id) => Session.Characters.First(c => c.Id == id).Skills.Select(s =>
        {
            decimal? next = Session.Curve.NextLevelXp(s.StoredXp);
            decimal start = Session.Curve.Thresholds[s.Level];
            return new SkillView { Id = s.SkillId, Name = Session.Skills.First(k => k.Id == s.SkillId).DisplayName,
                Level = s.Level, Xp = s.StoredXp, NextXp = next, Progress = next.HasValue ? (s.StoredXp - start) / (next.Value - start) : 1 };
        }).ToArray();
        public ActivityView[] Activities()
        {
            string id = InspectedId;
            var result = new List<ActivityView>();
            foreach (var a in Session.Catalog.Activities.Where(a => a.SkillId == SkillFilter))
                result.Add(new ActivityView { Id = a.Id, Name = a.DisplayName, Skill = a.SkillId, Kind = "Gathering", Level = a.RequiredLevel,
                    Duration = a.DurationTicks, Xp = a.XpPerCycle, Description = "+1 " + Session.BankItems[a.ResourceId] + " per completed cycle • bank " + Session.Quantity(a.ResourceId),
                    Locked = !Session.MeetsRequirement(id, a), CanStart = Session.MeetsRequirement(id, a) });
            foreach (var r in Session.Production.Recipes.Where(r => r.SkillId == SkillFilter))
                result.Add(new ActivityView { Id = r.Id, Name = r.DisplayName, Skill = r.SkillId, Kind = "Production", Level = r.RequiredLevel,
                    Duration = r.DurationTicks, Xp = r.XpReward, Description = r.OutputQuantity + " × " + r.OutputName +
                        " • one job per Start; unfinished inputs refunded on cancellation." + (r.IsBalanceAssumption ? " Starter balance assumption." : " Development recipe."),
                    Locked = !Session.MeetsRequirement(id, Session.Activities.First(a => a.Id == r.Id)),
                    CanStart = Session.CanStartProduction(id, r, true),
                    Inputs = r.Inputs.Select(p => new StockView { Id = p.Key, Name = Session.BankItems[p.Key], Required = p.Value, Available = Session.Quantity(p.Key) }).ToArray() });
            if (new[] { "melee", "ranged", "magic" }.Contains(SkillFilter))
                foreach (var m in Session.Combat.Monsters)
                {
                    bool compatible = Session.Profile(id).Style.ToString().ToLowerInvariant() == SkillFilter;
                    result.Add(new ActivityView { Id = m.Id, Name = m.Name, Skill = SkillFilter, Kind = "Combat", Level = 1,
                        Duration = Session.Profile(id).IntervalTicks, Xp = null, Locked = !compatible || Session.Vitals[id].Hp <= 0, CanStart = compatible && Session.Vitals[id].Hp > 0,
                        Description = m.Style + " • " + m.MaxHp + " HP • " + m.Damage + " damage / " + Seconds(m.AttackIntervalTicks) +
                            "s. XP per actual damage: " + Session.Combat.Rules.XpPerDamage((CombatStyle)Enum.Parse(typeof(CombatStyle), SkillFilter, true)) +
                            ". " + (compatible ? "" : "Equip a matching mainhand first. ") + "Development-only combat tuning.\n" +
                            string.Join("\n", Session.Combat.Drops.Where(d => d.MonsterId == m.Id).Select(d => d.ItemName + " × " + d.Quantity + " @ " + d.Chance.ToString("P0"))) });
                }
            foreach (var a in result)
            {
                a.Occupant = Session.Assignments.FirstOrDefault(p => p.Key != id && p.Value == a.Id).Key;
                a.Assigned = Session.Assignments.TryGetValue(id, out var current) && current == a.Id;
                if (a.Locked) a.CanStart = false;
            }
            return result.ToArray();
        }
        public bool Start(string activityId, bool confirmedSwap = false, string expectedOccupant = null)
        {
            var view = Activities().FirstOrDefault(a => a.Id == activityId);
            if (view == null) return Result(false, "Choose an activity in the current skill.", "");
            if (view.Occupant != null && !confirmedSwap)
                return Result(false, "Assigned to " + Character(view.Occupant).Name + ". Confirm Swap Assignment to displace them.", "");
            if (confirmedSwap && expectedOccupant != null && view.Occupant != expectedOccupant)
                return Result(false, "Assignment changed. Reopen the swap confirmation.", "");
            bool ok; string error;
            if (view.Kind == "Production") ok = Session.StartProductionAt(Session.LastSettledTicks, InspectedId, activityId, confirmedSwap, out error);
            else if (view.Kind == "Combat") ok = Session.StartCombatAt(Session.LastSettledTicks, InspectedId, activityId,
                (CombatStyle)Enum.Parse(typeof(CombatStyle), SkillFilter, true), confirmedSwap, out error);
            else ok = Session.AssignAt(Session.LastSettledTicks, InspectedId, activityId, confirmedSwap, out error);
            return Result(ok, error, "Started " + view.Name + ".");
        }
        public bool Stop() => Result(Session.UnassignAt(Session.LastSettledTicks, InspectedId), "No current assignment.", "Stopped. Unfinished production inputs returned.");
        public bool Equip(string itemId)
        {
            var gear = Session.Equipment.Definitions.FirstOrDefault(d => d.Id == itemId);
            if (gear == null) return Result(false, "This item cannot be equipped.", "");
            bool ok = Session.EquipAt(Session.LastSettledTicks, InspectedId, itemId, gear.Slot, out var error);
            return Result(ok, error, "Equipped " + gear.Name + ".");
        }
        public bool Unequip(EquipmentSlot slot)
        { bool ok = Session.UnequipAt(Session.LastSettledTicks, InspectedId, slot, out var error); return Result(ok, error, "Returned item to the shared bank."); }
        public BankItemView[] Bank() => Session.BankItems.Select(p => new BankItemView { Id = p.Key, Name = p.Value, Quantity = Session.Quantity(p.Key),
            Type = Session.Equipment.Definitions.Any(d => d.Id == p.Key) ? BankFilter.Equipment : Session.Catalog.Resources.Any(r => r.Id == p.Key) ? BankFilter.Resources :
                Session.Production.Recipes.Any(r => r.OutputId == p.Key) ? BankFilter.Products : BankFilter.Loot })
            .Where(i => i.Quantity > 0 && (Filter == BankFilter.All || Filter == i.Type) &&
                (i.Name.IndexOf(Search ?? "", StringComparison.OrdinalIgnoreCase) >= 0 || i.Id.IndexOf(Search ?? "", StringComparison.OrdinalIgnoreCase) >= 0))
            .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        public EquipmentDefinition[] CompatibleStock(EquipmentSlot slot) => Session.Equipment.Definitions.Where(d => d.Slot == slot && Session.Quantity(d.Id) > 0).ToArray();
        public StatChange[] Compare(string itemId)
        {
            var gear = Session.Equipment.Definitions.First(d => d.Id == itemId);
            var slots = Session.Loadouts[InspectedId].Slots;
            var next = slots.Where(p => p.Key != gear.Slot && !(gear.TwoHanded && p.Key == EquipmentSlot.Offhand)).Select(p => p.Value.Definition).Concat(new[] { gear }).ToArray();
            var current = Session.Profile(InspectedId); var weapon = next.FirstOrDefault(d => d.Slot == EquipmentSlot.Mainhand);
            decimal damage = Session.Combat.Rules.BaseDamage + next.Sum(d => d.DamageBonus), defense = next.Sum(d => d.DefenseBonus);
            long interval = weapon?.AttackIntervalTicks ?? Session.Combat.Rules.BaseIntervalTicks;
            var changes = new List<StatChange>();
            AddChange(changes, "Damage", current.Damage, damage, false); AddChange(changes, "Defense", current.Defense, defense, false);
            AddChange(changes, "Attack seconds", (decimal)current.IntervalTicks / TimeSpan.TicksPerSecond, (decimal)interval / TimeSpan.TicksPerSecond, true);
            return changes.ToArray();
        }
        private static void AddChange(List<StatChange> result, string name, decimal before, decimal after, bool lowerBetter)
        { if (before != after) result.Add(new StatChange { Name = name, Before = before.ToString("0.###"), After = after.ToString("0.###"), Improvement = lowerBetter ? after < before : after > before }); }
        public static string Seconds(long ticks) => (ticks / (double)TimeSpan.TicksPerSecond).ToString("0.#");
    }
}
