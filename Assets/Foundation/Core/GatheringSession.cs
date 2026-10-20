using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Idlebound.Foundation
{
    // Single owner of assignment/selection/effect mutation and time settlement.
    // In-memory only: no persistence, wall-clock authority, or production offline replay.
    public sealed partial class GatheringSession
    {
        public const long BaseOfflineTicks = 4L * TimeSpan.TicksPerHour;
        private readonly GameState state;
        private readonly XpCurve curve;
        private readonly Dictionary<string, long> inventory = new Dictionary<string, long>();
        private readonly Dictionary<string, long> partialTicks = new Dictionary<string, long>();
        private readonly Dictionary<string, long> completedCycles = new Dictionary<string, long>();
        private readonly Dictionary<string, BackgroundXpEffect[]> effects = new Dictionary<string, BackgroundXpEffect[]>();
        public GatheringCatalog Catalog { get; }
        public ReadOnlyCollection<CharacterState> Characters => state.Characters;
        public ReadOnlyCollection<SkillDefinition> Skills => state.Skills;
        public ReadOnlyDictionary<string, string> Assignments => state.Assignments;
        public ReadOnlyDictionary<string, long> Inventory { get; }
        public string ActiveCharacterId => state.ActiveCharacterId;
        public long LastSettledTicks { get; private set; }
        public bool UsesDevelopmentXp => Catalog.UsesDevelopmentXp;
        public bool UsesSparseCurve => curve.IsDevelopmentFixture;
        public XpCurve Curve => curve;
        public bool RefundCancelledInputs { get; }

        public GatheringSession(GatheringCatalog catalog, XpCurve curve, ProductionCatalog production = null, bool? refundCancelledInputs = null,
            EquipmentCatalog equipment = null, CombatCatalog combat = null, IDeterministicLootRoller random = null)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.curve = curve ?? throw new ArgumentNullException(nameof(curve));
            // Full-curve production defaults to refunds. Sparse historical fixtures preserve their
            // old test contract unless explicitly opted into the new reversible policy.
            RefundCancelledInputs = refundCancelledInputs ?? !curve.IsDevelopmentFixture;
            Production = production ?? new ProductionCatalog(Array.Empty<ProductionRecipe>(), catalog.Resources);
            var knownItems = new HashSet<string>(catalog.Resources.Select(r => r.Id)
                .Concat(Production.Recipes.Select(r => r.OutputId)));
            if (Production.ItemNames.Keys.Any(id => !knownItems.Contains(id)) ||
                catalog.Resources.Any(r => !Production.ItemNames.TryGetValue(r.Id, out var name) || name != r.DisplayName) ||
                Production.Recipes.Any(r => catalog.Resources.Any(raw => raw.Id == r.OutputId)))
                throw new ArgumentException("Production catalog does not match session resource definitions.");
            Equipment = equipment ?? EquipmentCatalog.Empty;
            Combat = combat ?? CombatCatalog.Empty;
            lootRoller = random ?? new SeededLootRoller(804);
            if (Production.Recipes.Any(recipe => recipe.Inputs.Keys.Any(id => Equipment.Definitions.Any(d => d.Id == id))))
                throw new ArgumentException("Equipment-item recipe inputs need future instance-aware reservations; not supported yet.");
            var names = new Dictionary<string, string>(Production.ItemNames);
            foreach (var gear in Equipment.Definitions) if (!names.ContainsKey(gear.Id)) names.Add(gear.Id, gear.Name);
            foreach (var drop in Combat.Drops) if (!names.ContainsKey(drop.ItemId)) names.Add(drop.ItemId, drop.ItemName);
            BankItems = new ReadOnlyDictionary<string, string>(names);
            ProductionJobs = new ReadOnlyDictionary<string, ProductionRecipe>(productionJobs);
            state = new GameState(new RosterAccess(), SkillCatalog.Create(),
                catalog.Activities.Concat(Production.Recipes.Select(r => r.Activity())).Concat(Combat.Monsters.Select(m => m.Activity())));
            for (int i = 1; i <= RosterAccess.InitialAvailableSlots; i++)
                state.TryAddCharacter(new CharacterState("character-" + i, "Character " + i,
                    state.Skills.Select(s => new SkillState(s.Id, 1, 0))));
            foreach (var character in Characters)
            {
                loadouts.Add(character.Id, new CharacterEquipment());
                vitals.Add(character.Id, new CharacterVitals(Combat.Rules.PlayerMaxHp, Combat.Rules.PlayerMaxHp));
            }
            Loadouts = new ReadOnlyDictionary<string, CharacterEquipment>(loadouts);
            BankEquipmentInstances = new ReadOnlyDictionary<string, EquipmentInstance>(bankInstances);
            Vitals = new ReadOnlyDictionary<string, CharacterVitals>(vitals);
            Encounters = new ReadOnlyDictionary<string, CombatEncounter>(encounters);
            Inventory = new ReadOnlyDictionary<string, long>(inventory);
        }

        public long Quantity(string resourceId) => inventory.TryGetValue(resourceId, out var count) ? count : 0;
        public long PartialTicks(string characterId) => partialTicks.TryGetValue(characterId, out var ticks) ? ticks : 0;
        public long CompletedCycles(string characterId) => completedCycles.TryGetValue(characterId, out var count) ? count : 0;
        public bool IsActive(string characterId) => state.IsActiveCharacter(characterId);
        public bool MeetsRequirement(string characterId, ActivityDefinition activity) =>
            activity != null && activity.IsAvailable && Characters.Any(c => c.Id == characterId &&
                c.Skills.Any(s => s.SkillId == activity.SkillId && s.Level >= activity.RequiredLevel));

        public decimal BackgroundEfficiency(string characterId, string skillId) =>
            XpPolicy.Efficiency(false, true, skillId, EffectsFor(characterId));

        private IEnumerable<BackgroundXpEffect> EffectsFor(string id) =>
            effects.TryGetValue(id, out var list) ? list : Array.Empty<BackgroundXpEffect>();

        private sealed class Settlement
        {
            public string CharacterId;
            public SkillState Skill;
            public long Remaining;
            public long TotalCycles;
            public decimal Xp;
            public bool FinishedProduction;
        }

        public void SettleTo(long absoluteTicks, bool offline = false)
        {
            if (absoluteTicks < LastSettledTicks) throw new ArgumentOutOfRangeException(nameof(absoluteTicks), "Clock rewind rejected.");
            long elapsed = absoluteTicks - LastSettledTicks;
            if (elapsed == 0) return;
            long eligible = offline ? Math.Min(elapsed, BaseOfflineTicks) : elapsed;
            var plans = new List<Settlement>();
            var nextInventory = new Dictionary<string, long>(inventory);
            // Preflight all rewards before mutating anything; checked overflow cannot partly pay a batch.
            foreach (var assignment in Assignments.OrderBy(a => a.Key, StringComparer.Ordinal))
            {
                if (Combat.Monsters.Any(m => m.Id == assignment.Value)) continue;
                var activity = Activities.First(a => a.Id == assignment.Value);
                var recipe = Production.Recipes.FirstOrDefault(r => r.Id == assignment.Value);
                if (recipe != null && !productionJobs.ContainsKey(assignment.Key)) continue;
                decimal ticks = PartialTicks(assignment.Key) + (decimal)eligible;
                long cycles = recipe == null ? checked((long)decimal.Floor(ticks / activity.DurationTicks))
                    : ticks >= activity.DurationTicks ? 1 : 0;
                long remainder = recipe == null ? (long)(ticks % activity.DurationTicks)
                    : cycles == 1 ? 0 : (long)ticks;
                var skill = Characters.First(c => c.Id == assignment.Key).Skills.First(s => s.SkillId == activity.SkillId);
                decimal rate = XpPolicy.Efficiency(IsActive(assignment.Key), offline, activity.SkillId, EffectsFor(assignment.Key));
                decimal xp = checked(cycles * (activity.XpPerCycle ?? 0m) * rate);
                decimal nextXp = curve.ApplyAward(skill.StoredXp, xp);
                curve.KnownLevelFloor(nextXp);
                long totalCycles = checked(CompletedCycles(assignment.Key) + cycles);
                if (cycles > 0)
                {
                    long before = nextInventory.TryGetValue(activity.ResourceId, out var count) ? count : 0;
                    nextInventory[activity.ResourceId] = checked(before + checked(cycles * (recipe?.OutputQuantity ?? 1)));
                }
                plans.Add(new Settlement { CharacterId = assignment.Key, Skill = skill,
                    Remaining = remainder, TotalCycles = totalCycles, Xp = xp, FinishedProduction = recipe != null && cycles == 1 });
            }
            var combatBatch = PlanCombat(eligible, offline, nextInventory);
            inventory.Clear();
            foreach (var pair in nextInventory) inventory.Add(pair.Key, pair.Value);
            foreach (var plan in plans)
            {
                partialTicks[plan.CharacterId] = plan.Remaining;
                completedCycles[plan.CharacterId] = plan.TotalCycles;
                plan.Skill.Award(plan.Xp, curve);
                if (plan.FinishedProduction) productionJobs.Remove(plan.CharacterId);
            }
            CommitCombat(combatBatch);
            // Advance across ineligible offline excess too, so a repeated call cannot reclaim it.
            LastSettledTicks = absoluteTicks;
        }

        public bool SelectAt(long ticks, string characterId)
        {
            SettleTo(ticks);
            return state.TrySelectActive(characterId);
        }

        public bool AssignAt(long ticks, string characterId, string activityId, bool swap, out string error)
        {
            SettleTo(ticks);
            if (Combat.Monsters.Any(m => m.Id == activityId))
            { error = "Use StartCombatAt so encounter state is initialized."; return false; }
            if (Production.Recipes.Any(r => r.Id == activityId))
            { error = "Use StartProductionAt so ingredients are consumed."; return false; }
            if (!TryPlanChange(characterId, activityId, swap, out var cancelled, out error)) return false;
            var refundedInventory = PreviewRefunds(cancelled);
            var before = Assignments.ToDictionary(p => p.Key, p => p.Value);
            bool success = swap ? state.TrySwapAssignment(characterId, activityId, out error)
                : state.TryAssign(characterId, activityId, out error);
            if (!success) return false;
            ReplaceInventory(refundedInventory);
            DiscardChangedJobs(before);
            // Selecting the same exact assignment is idempotent and keeps its partial cycle.
            if (!before.TryGetValue(characterId, out var old) || old != activityId) partialTicks.Remove(characterId);
            return true;
        }

        public bool UnassignAt(long ticks, string characterId)
        {
            SettleTo(ticks);
            var refundedInventory = PreviewRefunds(new[] { characterId });
            bool removed = state.Unassign(characterId);
            if (removed)
            {
                ReplaceInventory(refundedInventory);
                partialTicks.Remove(characterId);
                productionJobs.Remove(characterId);
                encounters.Remove(characterId);
            }
            return removed;
        }

        public void SetEffectsAt(long ticks, string characterId, IEnumerable<BackgroundXpEffect> applicableEffects)
        {
            if (!Characters.Any(c => c.Id == characterId)) throw new ArgumentException("Unknown character.");
            var copy = applicableEffects?.ToArray() ?? throw new ArgumentNullException(nameof(applicableEffects));
            // Validate source identity and scope before changing time or effects.
            XpPolicy.Efficiency(false, true, "*", copy);
            if (copy.Any(e => e.SkillId != "*" && !Skills.Any(s => s.Id == e.SkillId)))
                throw new ArgumentException("Unknown effect skill.");
            SettleTo(ticks);
            effects[characterId] = copy;
        }
    }
}
