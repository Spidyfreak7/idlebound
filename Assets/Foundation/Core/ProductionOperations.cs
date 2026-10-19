using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Idlebound.Foundation
{
    public sealed partial class GatheringSession
    {
        private readonly Dictionary<string, ProductionRecipe> productionJobs = new Dictionary<string, ProductionRecipe>();
        public ProductionCatalog Production { get; }
        public ReadOnlyDictionary<string, ProductionRecipe> ProductionJobs { get; private set; }
        public ReadOnlyCollection<ActivityDefinition> Activities => state.Activities;

        public bool CanAfford(ProductionRecipe recipe) => recipe != null &&
            recipe.Inputs.All(input => Quantity(input.Key) >= input.Value);

        public bool StartProductionAt(long ticks, string characterId, string recipeId, bool swap, out string error)
        {
            SettleTo(ticks);
            var recipe = Production.Recipes.FirstOrDefault(r => r.Id == recipeId);
            if (recipe == null || !MeetsRequirement(characterId, recipe.Activity()))
            { error = "Unknown recipe/character or required level not reached."; return false; }
            if (productionJobs.TryGetValue(characterId, out var running) && running.Id == recipeId)
            { error = "This job is already running; ingredients were not consumed again."; return false; }
            if (!CanAfford(recipe))
            { error = "Insufficient ingredients; existing assignment/job preserved."; return false; }
            var before = Assignments.ToDictionary(p => p.Key, p => p.Value);
            bool assigned = swap ? state.TrySwapAssignment(characterId, recipeId, out error)
                : state.TryAssign(characterId, recipeId, out error);
            if (!assigned) return false;
            // All requirements and every ingredient checked before any inventory deduction.
            foreach (var input in recipe.Inputs)
            {
                long after = inventory[input.Key] - input.Value;
                if (after == 0) inventory.Remove(input.Key);
                else inventory[input.Key] = after;
            }
            DiscardChangedJobs(before);
            partialTicks.Remove(characterId);
            productionJobs[characterId] = recipe;
            error = null;
            return true;
        }

        private void DiscardChangedJobs(Dictionary<string, string> before)
        {
            foreach (var pair in before)
                if (!Assignments.TryGetValue(pair.Key, out var now) || now != pair.Value)
                {
                    partialTicks.Remove(pair.Key);
                    // Development policy: already-consumed ingredients are not refunded.
                    productionJobs.Remove(pair.Key);
                }
        }
    }
}
