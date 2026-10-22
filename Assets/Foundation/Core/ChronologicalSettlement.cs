using System;
using System.Linq;
using System.Collections.Generic;
namespace Idlebound.Foundation
{
    public sealed partial class GatheringSession
    {
        private long NextFoodEvent()
        {
            long next=long.MaxValue;
            foreach(var c in Characters)
            {
                if(encounters.TryGetValue(c.Id,out var fight))next=Math.Min(next,Math.Min(fight.PlayerRemainingTicks,fight.EnemyRemainingTicks));
                else if(Assignments.TryGetValue(c.Id,out var id) && (!Production.Recipes.Any(p=>p.Id==id) || productionJobs.ContainsKey(c.Id)))
                    next=Math.Min(next,EffectiveDuration(c.Id,Activities.Single(a=>a.Id==id))-PartialTicks(c.Id));
                if(Bonus(c.Id,EnchantmentType.HpRegeneration)>0 && vitals[c.Id].Hp>0)next=Math.Min(next,Enchantments.RegenInterval-(regenTicks.TryGetValue(c.Id,out var phase)?phase:0));
            }
            return next;
        }
        // A detached transaction uses the existing batch simulator at the next actual event.
        // There is still one session clock; no per-frame food timer or second owner.
        private void SettleChronologically(long absoluteTicks,bool offline)
        {
            long elapsed=absoluteTicks-LastSettledTicks;
            var next=Restore(Capture(),this);
            long maximum=offline?Characters.Max(c=>OfflineAllowance(c.Id)):elapsed;
            long window=Math.Min(elapsed,maximum),cursor=0;
            var report=offline?new OfflineReport{AwayTicks=elapsed,EligibleTicks=window,Capped=elapsed>window}:null;
            if(report!=null)foreach(var c in Characters)report.For(c.Id,Assignments.TryGetValue(c.Id,out var a)?a:null);
            foreach(var c in next.encounters.Keys.OrderBy(x=>x,StringComparer.Ordinal))next.AutoEat(c,next.vitals[c],next.inventory,report);
            long events=0;
            while(cursor<window)
            {
                next.eligibilityOverride=next.Characters.ToDictionary(c=>c.Id,c=>offline?Math.Max(0,Math.Min(window-cursor,next.OfflineAllowance(c.Id)-cursor)):window-cursor);
                long step=window-cursor;
                foreach(var c in next.Characters)
                {
                    long available=next.eligibilityOverride[c.Id];if(available<=0)continue;
                    step=Math.Min(step,available);
                    if(next.encounters.TryGetValue(c.Id,out var fight))step=Math.Min(step,Math.Min(fight.PlayerRemainingTicks,fight.EnemyRemainingTicks));
                    else if(next.Assignments.TryGetValue(c.Id,out var id))
                    {
                        bool production=next.Production.Recipes.Any(p=>p.Id==id);
                        if(!production || next.productionJobs.ContainsKey(c.Id))
                        {var a=next.Activities.Single(x=>x.Id==id);step=Math.Min(step,next.EffectiveDuration(c.Id,a)-next.PartialTicks(c.Id));}
                    }
                    if(next.Bonus(c.Id,EnchantmentType.HpRegeneration)>0 && next.vitals[c.Id].Hp>0)
                        step=Math.Min(step,next.Enchantments.RegenInterval-(next.regenTicks.TryGetValue(c.Id,out var phase)?phase:0));
                }
                if(step<=0)throw new InvalidOperationException("Invalid event boundary; transaction was not committed.");
                if(++events>Combat.Rules.EventBudget)throw new InvalidOperationException("Settlement event budget exceeded; transaction was not committed.");
                next.SettleBatch(checked(next.LastSettledTicks+step),offline);
                if(report!=null)
                {
                    var chunk=next.LastOfflineReport;
                    chunk.Characters.RemoveAll(c=>c.Items.Count==0 && c.Xp.Count==0 && (c.Consumed==null || c.Consumed.Count==0) && c.StopReason==null);
                    report.Merge(chunk);
                }
                cursor+=step;
            }
            next.eligibilityOverride=null;
            if(report!=null){report.AwayTicks=elapsed;report.EligibleTicks=window;report.Capped=elapsed>window;report.Intervals=1;}
            // Publish only after every event, inventory operation and XP award has succeeded.
            state=next.state;ReplaceInventory(next.inventory);
            CopyMap(partialTicks,next.partialTicks);CopyMap(completedCycles,next.completedCycles);CopyMap(productionJobs,next.productionJobs);
            CopyMap(enchantJobs,next.enchantJobs);CopyMap(loadouts,next.loadouts);CopyMap(bankInstances,next.bankInstances);
            CopyMap(vitals,next.vitals);CopyMap(encounters,next.encounters);CopyMap(killCounts,next.killCounts);
            CopyMap(attackCounts,next.attackCounts);CopyMap(regenTicks,next.regenTicks);CopyMap(effects,next.effects);
            CopyMap(foodSettings,next.foodSettings);CopyMap(manualEatTicks,next.manualEatTicks);
            equipmentSerial=next.equipmentSerial;LastSettledTicks=absoluteTicks;LastOfflineReport=report;
        }
        private static void CopyMap<K,V>(Dictionary<K,V> destination,Dictionary<K,V> source)
        {destination.Clear();foreach(var p in source)destination.Add(p.Key,p.Value);}
    }
}
