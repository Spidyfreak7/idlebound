using System;
using System.Collections.Generic;
using System.Linq;

namespace Idlebound.Foundation
{
    public sealed partial class GatheringSession
    {
        // Preflight only: all bank/progress/XP changes stay detached until SettleTo commits.
        private List<Settlement> PlanActivities(long eligible, bool offline, Dictionary<string,long> bank, OfflineReport receipt)
        {
            var plans = new List<Settlement>();
            foreach (var assignment in Assignments.OrderBy(p => p.Key,StringComparer.Ordinal))
            {
                if (Combat.Monsters.Any(m => m.Id == assignment.Value)) continue;
                var recipe = Production.Recipes.FirstOrDefault(r => r.Id == assignment.Value);
                if (recipe != null && !productionJobs.ContainsKey(assignment.Key)) continue; // Stopped means stopped, even after stock arrives.
                var activity = Activities.First(a => a.Id == assignment.Value);
                plans.Add(new Settlement { CharacterId = assignment.Key, Activity = activity, Recipe = recipe,
                    Skill = Characters.First(c => c.Id == assignment.Key).Skills.First(s => s.SkillId == activity.SkillId),
                    Remaining = PartialTicks(assignment.Key), TotalCycles = CompletedCycles(assignment.Key) });
            }
            long remaining = eligible;
            while (remaining > 0)
            {
                var running = plans.Where(p => p.Recipe != null && !p.StoppedProduction).ToArray();
                long step = running.Length == 0 ? remaining : Math.Min(remaining,running.Min(p => p.Activity.DurationTicks - p.Remaining));
                // Pure production can batch in O(inputs), including the reservation at an exact boundary.
                if (running.Length == 1 && plans.All(p => p.Recipe != null))
                {
                    var p = running[0];
                    long affordable = p.Recipe.Inputs.Min(i => (bank.TryGetValue(i.Key,out var n) ? n : 0) / i.Value);
                    decimal ticks = p.Remaining + (decimal)remaining;
                    long possible = checked((long)decimal.Floor(ticks / p.Activity.DurationTicks));
                    long cycles = possible <= affordable ? possible : checked(affordable + 1);
                    bool stopped = possible > affordable;
                    long reservations = stopped ? cycles - 1 : cycles;
                    Consume(p.Recipe,bank,reservations);
                    AddCycles(p,cycles,offline,bank,receipt);
                    p.Remaining = stopped ? 0 : (long)(ticks % p.Activity.DurationTicks);
                    p.StoppedProduction = stopped;
                    remaining = 0;
                    break;
                }
                foreach (var p in plans.Where(p => p.Recipe == null))
                {
                    decimal ticks = p.Remaining + (decimal)step;
                    long cycles = checked((long)decimal.Floor(ticks / p.Activity.DurationTicks));
                    p.Remaining = (long)(ticks % p.Activity.DurationTicks);
                    AddCycles(p,cycles,offline,bank,receipt);
                }
                foreach (var p in running) p.Remaining = checked(p.Remaining + step);
                // Same-timestamp policy: gathering, then all completed outputs, then new reservations
                // in ordinal character-ID order. Future gathered resources are never borrowed.
                var completed = running.Where(p => p.Remaining == p.Activity.DurationTicks).ToArray();
                foreach (var p in completed) { p.Remaining = 0; AddCycles(p,1,offline,bank,receipt); }
                foreach (var p in completed)
                {
                    if (p.Recipe.Inputs.All(i => bank.TryGetValue(i.Key,out var n) && n >= i.Value)) Consume(p.Recipe,bank,1);
                    else p.StoppedProduction = true;
                }
                remaining -= step;
            }
            foreach (var p in plans)
            {
                decimal nextXp = curve.ApplyAward(p.Skill.StoredXp,p.Xp); curve.KnownLevelFloor(nextXp);
                if (receipt == null) continue;
                var outcome = receipt.For(p.CharacterId,p.Activity.Id);
                outcome.Award(p.Activity.SkillId,nextXp - p.Skill.StoredXp);
                if (p.StoppedProduction) outcome.StopReason = "Out of materials";
            }
            return plans;
        }
        private static void Consume(ProductionRecipe recipe, Dictionary<string,long> bank, long cycles)
        {
            foreach (var input in recipe.Inputs)
            {
                long quantity = checked((bank.TryGetValue(input.Key,out var n) ? n : 0) - checked(input.Value * cycles));
                if (quantity == 0) bank.Remove(input.Key); else bank[input.Key] = quantity;
            }
        }
        private void AddCycles(Settlement plan, long cycles, bool offline, Dictionary<string,long> bank, OfflineReport receipt)
        {
            if (cycles == 0) return;
            long quantity = checked(cycles * (plan.Recipe?.OutputQuantity ?? 1));
            bank[plan.Activity.ResourceId] = checked((bank.TryGetValue(plan.Activity.ResourceId,out var n) ? n : 0) + quantity);
            plan.TotalCycles = checked(plan.TotalCycles + cycles);
            decimal efficiency = XpPolicy.Efficiency(IsActive(plan.CharacterId),offline,plan.Activity.SkillId,EffectsFor(plan.CharacterId));
            plan.Xp = checked(plan.Xp + checked(cycles * (plan.Activity.XpPerCycle ?? 0m) * efficiency));
            receipt?.For(plan.CharacterId,plan.Activity.Id).Item(plan.Activity.ResourceId,quantity);
        }
    }
}
