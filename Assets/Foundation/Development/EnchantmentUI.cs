using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static Idlebound.Foundation.Development.MobileUiKit;
namespace Idlebound.Foundation.Development
{
    public sealed partial class IdleboundMobileUI
    {
        private string EnchantName(EquipmentInstance item)
        {
            if(item.PermanentEnchantmentId==null)return "Unenchanted";
            var d=Session.Enchantments.Definitions.Single(x=>x.Id==item.PermanentEnchantmentId);
            return d.Activity().DisplayName+" · "+EffectAmount(d.Type,d.Potency)+" permanent";
        }
        private static string EffectAmount(EnchantmentType type,decimal value)=>type==EnchantmentType.BackgroundXp || type==EnchantmentType.CriticalChance?(value*100).ToString("0.#")+" pp":value.ToString("P1");
        private string EquipmentBonuses(string id) => "Matching four-piece set: "+(Session.HasMatchingSet(id)?"active":"inactive")+"\n"+
            string.Join(" · ",Enum.GetValues(typeof(EnchantmentType)).Cast<EnchantmentType>().Where(t=>Session.Bonus(id,t)>0).Select(t=>t+" "+EffectAmount(t,Session.Bonus(id,t))))+
            "\nBackground XP (current skill): "+Session.BackgroundEfficiency(id,Model.SkillFilter).ToString("P1")+" · Offline allowance: "+TimeSpan.FromTicks(Session.OfflineAllowance(id)).TotalHours.ToString("0.##")+"h";
        private void EnchantingActivity()
        {
            var card=Card(body);card.name="Enchant activity";
            Label(card,"Enchant · Magic",22,Gold);
            Button(card,"Choose enchantment",()=>EnchantMenu(0,false));
            string id=Model.InspectedId;
            if(Session.EnchantJobs.TryGetValue(id,out var job))
            {
                Label(card,job.Definition.Activity().DisplayName+" · "+job.InstanceId,19);
                BindProgress(Progress(card),()=>Model.Character(id).Progress);
                Bind(Label(card,"",18),()=>"Enchanting · "+Model.Character(id).Progress.ToString("P0"));
                Button(card,"Cancel enchant",()=>Act(()=>Model.Stop()),false,48);
            }
        }
        private void EnchantMenu(int tier,bool available)
        {
            Modal("Enchantments · permanent",content=>
            {
                Label(content,"One permanent, nonreplaceable enchantment per instance. All ten equipment slots are eligible. Target must be in the shared bank.",20,Muted);
                Button(content,"Tier: "+(tier==0?"All":tier.ToString())+" — next",()=>EnchantMenu((tier+1)%5,available));
                Button(content,available?"Showing available — show all":"Showing all — show available",()=>EnchantMenu(tier,!available));
                int level=Session.Characters.Single(c=>c.Id==Model.InspectedId).Skills.Single(s=>s.SkillId=="magic").Level;
                foreach(var d in Session.Enchantments.Definitions.Where(e=>(tier==0||e.Tier==tier) && (!available || level>=e.Level && e.Inputs.All(p=>Session.Quantity(p.Key)>=p.Value))))
                {
                    var e=d;var card=Card(content);Label(card,e.Activity().DisplayName+" · Magic "+e.Level,21,Gold);
                    Label(card,EffectAmount(e.Type,e.Potency)+" · "+GameUiModel.Seconds(e.DurationTicks)+"s · "+e.Xp+" Magic XP\n"+string.Join(" · ",e.Inputs.Select(p=>p.Value+" "+PlayerNames.Display(Session.BankItems[p.Key])+" (bank "+Session.Quantity(p.Key)+")")),18);
                    Button(card,level<e.Level?"Locked":"Choose bank equipment",()=>ChooseEnchantTarget(e)).interactable=level>=e.Level;
                }
            });
        }
        private void ChooseEnchantTarget(EnchantmentDefinition e)
        {
            Modal("Choose exact bank item",content=>
            {
                var candidates=Session.BankEquipmentInstances.Values.Where(i=>i.PermanentEnchantmentId==null&&!Session.IsItemLocked(i.InstanceId)).ToArray();
                foreach(var item in candidates)
                {var key=item.InstanceId;Button(content,PlayerNames.Display(item.Definition.Name)+" · "+key,()=>ConfirmEnchant(key,e));}
                foreach(var d in Session.Equipment.Definitions.Where(d=>Session.Quantity(d.Id)>Session.BankEquipmentInstances.Values.LongCount(i=>i.Definition.Id==d.Id)))
                {var key=d.Id;Button(content,PlayerNames.Display(d.Name)+" · new instance from bank",()=>{var instance=Model.Materialize(key);if(instance!=null)ConfirmEnchant(instance,e);});}
                if(candidates.Length==0)Label(content,"Only unenchanted, unlocked bank items qualify. Unequip or produce gear if needed.",19,Muted);
            });
        }
        private void ConfirmEnchant(string instance,EnchantmentDefinition e)
        {
            string occupant=Session.Assignments.FirstOrDefault(p=>p.Key!=Model.InspectedId && p.Value==e.Id).Key;
            Confirm("Permanent enchantment",e.Activity().DisplayName+" on "+instance+". This cannot be erased, replaced or upgraded.\n"+
                string.Join(" · ",e.Inputs.Select(p=>p.Value+" "+PlayerNames.Display(Session.BankItems[p.Key])))+"\n"+GameUiModel.Seconds(e.DurationTicks)+"s · "+e.Xp+" Magic XP (eligibility rules apply).\n"+
                "This replaces your current activity and refunds unfinished reserved work."+(occupant==null?"":" Displaces "+Model.Character(occupant).Name+" from this exact enchant activity."),
                ()=>Act(()=>Model.Enchant(instance,e.Id,true,occupant)),"Confirm permanent enchantment");
        }
    }
}
