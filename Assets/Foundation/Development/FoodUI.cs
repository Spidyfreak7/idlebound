using System;
using System.Linq;
using UnityEngine.UI;
using static Idlebound.Foundation.Development.MobileUiKit;
namespace Idlebound.Foundation.Development
{
    public sealed partial class IdleboundMobileUI
    {
        private string FoodSummary(string id)
        {
            var s=Session.FoodFor(id);
            var choices=Session.Foods.Definitions.Where(f=>s.Preferred=="auto" || f.ItemId==s.Preferred).ToArray();
            return "Auto-eat "+(s.Enabled?"ON":"OFF")+" · At/below "+s.Threshold+"% HP · "+(s.Preferred=="auto"?"Auto · ":"")+
                string.Join(" · ",choices.Select(f=>PlayerNames.Display(Session.BankItems[f.ItemId])+" ×"+Session.Quantity(f.ItemId)))+
                (choices.All(f=>Session.Quantity(f.ItemId)==0)?" · Food unavailable":"");
        }
        private void FoodControls(string id)
        {
            var card=Card(body);card.name="Food controls";
            Bind(Label(card,"",20,Gold),()=>"HP "+Session.Vitals[id].Hp+" / "+Session.Vitals[id].MaxHp);
            Bind(Label(card,"",18,Muted),()=>FoodSummary(id));
            var row=Row(card,64);
            var toggle=Button(row,"",()=>{var s=Session.FoodFor(id);Act(()=>Model.ConfigureFood(!s.Enabled,s.Threshold,s.Preferred));},false,64);
            Bind(toggle.GetComponentInChildren<Text>(),()=>"Auto-eat "+(Session.FoodFor(id).Enabled?"ON":"OFF"));
            Button(row,"Threshold",()=>Modal("Auto-eat HP threshold",content=>
            {
                for(int n=10;n<=90;n+=10){int threshold=n;Button(content,n+"% HP",()=>{var s=Session.FoodFor(id);CloseModal();Act(()=>Model.ConfigureFood(s.Enabled,threshold,s.Preferred));});}
            }),false,64);
            var options=Row(card,64);
            Button(options,"Food preference",()=>Modal("Preferred food",content=>
            {
                Button(content,"Auto — least adequate, otherwise strongest",()=>SetFoodPreference("auto"));
                foreach(var f in Session.Foods.Definitions){string key=f.ItemId;Button(content,PlayerNames.Display(Session.BankItems[key])+" · "+f.Healing+" HP",()=>SetFoodPreference(key));}
            }),false,64);
            var eat=Button(options,"Eat",()=>Act(()=>Model.Eat()),false,64);eat.name="Manual Eat";
            eat.interactable=Session.CanEat(id);bindings.Add(()=>{if(eat!=null)eat.interactable=Session.CanEat(id);});
            Bind(Label(card,"",17,Muted),()=>Session.Vitals[id].Hp<=0?"Defeated — food cannot revive this character.":Session.Vitals[id].Hp>=Session.Vitals[id].MaxHp?"Full HP":"Eating consumes one shared-bank serving; no XP.");
        }
        private void SetFoodPreference(string preference)
        {var s=Session.FoodFor(Model.InspectedId);CloseModal();Act(()=>Model.ConfigureFood(s.Enabled,s.Threshold,preference));}
    }
}
