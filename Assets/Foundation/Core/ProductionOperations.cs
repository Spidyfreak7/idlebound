using System;
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
            if (!TryPlanChange(characterId, recipeId, swap, out var cancelled, out error)) return false;
            var nextInventory = PreviewRefunds(cancelled);
            if (!recipe.Inputs.All(input => nextInventory.TryGetValue(input.Key, out var count) && count >= input.Value))
            { error = "Insufficient ingredients; existing assignment/job preserved."; return false; }
            // Prepare both refunds and the replacement reservation before changing assignments.
            foreach (var input in recipe.Inputs)
            {
                long after = nextInventory[input.Key] - input.Value;
                if (after == 0) nextInventory.Remove(input.Key);
                else nextInventory[input.Key] = after;
            }
            var before = Assignments.ToDictionary(p => p.Key, p => p.Value);
            bool assigned = swap ? state.TrySwapAssignment(characterId, recipeId, out error)
                : state.TryAssign(characterId, recipeId, out error);
            if (!assigned) return false;
            ReplaceInventory(nextInventory);
            DiscardChangedJobs(before);
            partialTicks.Remove(characterId);
            productionJobs[characterId] = recipe;
            error = null;
            return true;
        }

        public bool CanStartProduction(string characterId, ProductionRecipe recipe, bool swap)
        {
            if (string.IsNullOrWhiteSpace(characterId) || recipe == null || !Production.Recipes.Contains(recipe) ||
                productionJobs.TryGetValue(characterId, out var running) && running.Id == recipe.Id ||
                !TryPlanChange(characterId, recipe.Id, swap, out var cancelled, out _)) return false;
            try
            {
                var available = PreviewRefunds(cancelled);
                return recipe.Inputs.All(p => available.TryGetValue(p.Key, out var count) && count >= p.Value);
            }
            catch (OverflowException) { return false; }
        }

        private bool TryPlanChange(string characterId, string activityId, bool swap,
            out List<string> cancelled, out string error)
        {
            cancelled = new List<string>();
            var activity = Activities.FirstOrDefault(a => a.Id == activityId);
            if (!MeetsRequirement(characterId, activity))
            { error = "Unknown character/activity or level requirement not met."; return false; }
            var occupant = Assignments.FirstOrDefault(a => a.Key != characterId && a.Value == activityId).Key;
            if (occupant != null && !swap)
            { error = "Activity already assigned to another character."; return false; }
            if (Assignments.TryGetValue(characterId, out var old) && old != activityId) cancelled.Add(characterId);
            if (occupant != null) cancelled.Add(occupant);
            error = null;
            return true;
        }

        private Dictionary<string, long> PreviewRefunds(IEnumerable<string> characters)
        {
            var result = new Dictionary<string, long>(inventory);
            if (!RefundCancelledInputs) return result;
            foreach (var id in characters.Where(id => id != null).Distinct())
                if (productionJobs.TryGetValue(id, out var job))
                    foreach (var input in job.Inputs)
                    {
                        long before = result.TryGetValue(input.Key, out var quantity) ? quantity : 0;
                        result[input.Key] = checked(before + input.Value);
                    }
            return result;
        }

        private void ReplaceInventory(Dictionary<string, long> next)
        {
            inventory.Clear();
            foreach (var pair in next) inventory.Add(pair.Key, pair.Value);
        }

        private void DiscardChangedJobs(Dictionary<string, string> before)
        {
            foreach (var pair in before)
                if (!Assignments.TryGetValue(pair.Key, out var now) || now != pair.Value)
                {
                    partialTicks.Remove(pair.Key);
                    // Refunds were preflighted/committed before discarding this reservation.
                    productionJobs.Remove(pair.Key);
                    encounters.Remove(pair.Key);
                }
        }
    }
}
