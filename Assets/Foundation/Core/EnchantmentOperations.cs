using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace Idlebound.Foundation
{
    public sealed class EnchantJob
    {
        public string InstanceId { get; }
        public EnchantmentDefinition Definition { get; }
        internal EnchantJob(string instance,EnchantmentDefinition definition) { InstanceId=instance; Definition=definition; }
    }
    public sealed partial class GatheringSession
    {
        public EnchantmentCatalog Enchantments { get; }
        private readonly Dictionary<string,EnchantJob> enchantJobs=new Dictionary<string,EnchantJob>();
        public IReadOnlyDictionary<string,EnchantJob> EnchantJobs => new ReadOnlyDictionary<string,EnchantJob>(enchantJobs);
        public bool IsItemLocked(string instance) => enchantJobs.Values.Any(j=>j.InstanceId==instance);
        public decimal Bonus(string character, EnchantmentType type) => Aggregate(loadouts[character].Items.Values,type);
        private decimal Aggregate(IEnumerable<EquipmentInstance> items, EnchantmentType type)
        {
            var a=items.ToArray(); var defs=a.Where(i=>i.PermanentEnchantmentId!=null).Select(i=>Enchantments.Definitions.Single(d=>d.Id==i.PermanentEnchantmentId)).Where(d=>d.Type==type).ToArray();
            if(defs.Length==0) return 0;
            decimal total=defs.Sum(d=>d.Potency);
            var set=a.Where(i=>EquipmentSlots.EnchantmentSetSlots.Contains(i.Definition.Slot)).ToArray();
            if(set.Length==4 && set.All(i=>i.PermanentEnchantmentId!=null) && set.Select(i=>i.PermanentEnchantmentId).Distinct().Count()==1)
            {
                var d=Enchantments.Definitions.Single(x=>x.Id==set[0].PermanentEnchantmentId);
                if(d.Type==type) total+=4*d.Potency*Enchantments.SetMultiplier;
            }
            return Math.Min(defs[0].Cap,total);
        }
        public bool HasMatchingSet(string character) => EquipmentSlots.EnchantmentSetSlots.All(s=>loadouts[character].Items.ContainsKey(s)) &&
            EquipmentSlots.EnchantmentSetSlots.Select(s=>loadouts[character].Items[s].PermanentEnchantmentId).Distinct().Count()==1 && loadouts[character].Items[EquipmentSlot.Head].PermanentEnchantmentId!=null;
        public long OfflineAllowance(string character) => Math.Min(7L*TimeSpan.TicksPerHour,checked((long)(BaseOfflineTicks*(1+Bonus(character,EnchantmentType.OfflineDuration)))));
        private long Eligible(string character,long elapsed,bool offline) => offline?Math.Min(elapsed,OfflineAllowance(character)):elapsed;
        public long EffectiveDuration(string character,ActivityDefinition a)
        {
            if(!Enum.TryParse(a.SkillId+"Speed",true,out EnchantmentType type) || !Enum.IsDefined(typeof(EnchantmentType),type)) return a.DurationTicks;
            return Math.Max(1,checked((long)decimal.Ceiling(a.DurationTicks/(1+Bonus(character,type)))));
        }
        // Explicitly materialize an existing anonymous bank unit, never grant a new item.
        public string MaterializeBankEquipmentAt(long ticks,string itemId)
        {
            SettleTo(ticks); var def=Equipment.Definitions.FirstOrDefault(d=>d.Id==itemId);
            if(def==null || Quantity(itemId)<=bankInstances.Values.LongCount(i=>i.Definition.Id==itemId)) return null;
            long serial=checked(equipmentSerial+1); var instance=new EquipmentInstance("gear-"+serial.ToString("D10",System.Globalization.CultureInfo.InvariantCulture),def);
            bankInstances.Add(instance.InstanceId,instance); equipmentSerial=serial; return instance.InstanceId;
        }
        public bool StartEnchantAt(long ticks,string character,string instanceId,string enchantmentId,bool swap,out string error)
        {
            SettleTo(ticks); var d=Enchantments.Definitions.FirstOrDefault(x=>x.Id==enchantmentId);
            if(d==null || !bankInstances.TryGetValue(instanceId??"",out var item) || item.PermanentEnchantmentId!=null || IsItemLocked(instanceId))
            { error="Choose an unlocked, unenchanted bank equipment instance."; return false; }
            if(!TryPlanChange(character,d.Id,swap,out var cancelled,out error)) return false;
            if(enchantJobs.ContainsKey(character))
            {
                if(!swap){error="Confirm replacement of the current enchantment job.";return false;}
                if(!cancelled.Contains(character))cancelled.Add(character);
            }
            var bank=PreviewRefunds(cancelled);
            if(d.Inputs.Any(p=>!bank.TryGetValue(p.Key,out var n)||n<p.Value)) {error="Insufficient enchantment materials.";return false;}
            foreach(var p in d.Inputs) {bank[p.Key]-=p.Value;if(bank[p.Key]==0)bank.Remove(p.Key);}
            var before=Assignments.ToDictionary(p=>p.Key,p=>p.Value);
            bool ok=swap?state.TrySwapAssignment(character,d.Id,out error):state.TryAssign(character,d.Id,out error);
            if(!ok)return false;
            ReplaceInventory(bank);DiscardChangedJobs(before);enchantJobs.Remove(character);partialTicks.Remove(character);enchantJobs.Add(character,new EnchantJob(instanceId,d));error=null;return true;
        }
        private sealed class EnchantPlan { public string Character; public long Partial; public decimal Xp; public bool Complete; }
        private List<EnchantPlan> PlanEnchants(long elapsed,bool offline,OfflineReport receipt)
        {
            var list=new List<EnchantPlan>();
            foreach(var p in enchantJobs)
            {
                var job=p.Value; long remaining=job.Definition.DurationTicks-PartialTicks(p.Key);
                long eligible=Eligible(p.Key,elapsed,offline); bool complete=eligible>=remaining;
                decimal xp=complete?job.Definition.Xp*XpPolicy.Efficiency(IsActive(p.Key),offline,"magic",EffectsFor(p.Key)):0;
                var skill=Characters.Single(c=>c.Id==p.Key).Skills.Single(s=>s.SkillId=="magic"); curve.ApplyAward(skill.StoredXp,xp);
                if(complete && receipt!=null) {var r=receipt.For(p.Key,job.Definition.Id);r.Award("magic",curve.ApplyAward(skill.StoredXp,xp)-skill.StoredXp);r.StopReason="Job completed";}
                list.Add(new EnchantPlan{Character=p.Key,Partial=complete?0:checked(PartialTicks(p.Key)+eligible),Xp=xp,Complete=complete});
            }
            return list;
        }
        private void CommitEnchants(IEnumerable<EnchantPlan> plans)
        {
            foreach(var p in plans)
            {
                partialTicks[p.Character]=p.Partial;
                if(!p.Complete)continue;
                var job=enchantJobs[p.Character];bankInstances[job.InstanceId].PermanentEnchantmentId=job.Definition.Id;
                Characters.Single(c=>c.Id==p.Character).Skills.Single(s=>s.SkillId=="magic").Award(p.Xp,curve);
                enchantJobs.Remove(p.Character);state.Unassign(p.Character);partialTicks.Remove(p.Character);
            }
        }
    }
}
