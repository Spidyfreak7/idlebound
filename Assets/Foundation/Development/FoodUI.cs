using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static Idlebound.Foundation.Development.MobileUiKit;
namespace Idlebound.Foundation.Development
{
    public sealed partial class IdleboundMobileUI
    {
        private string FoodSummary(string id)
        {
            var s=Session.FoodFor(id);
            var choices=Session.Foods.Definitions.Where(f=>s.Preferred=="auto" || f.ItemId==s.Preferred);
            return "Auto-eat always active · At/below "+s.Threshold+"% HP · "+
                string.Join(" · ",choices.Select(f=>PlayerNames.Display(Session.BankItems[f.ItemId])+" ×"+Session.Quantity(f.ItemId)));
        }
        private void FoodModalText(Transform parent,Func<string> value)
        {
            var text=Label(parent,value(),20);
            modalBindings.Add(()=>{if(text!=null)text.text=value();});
        }
        private string FoodPreferenceName(string id)=>Session.FoodFor(id).Preferred=="auto"?"Auto":PlayerNames.Display(Session.BankItems[Session.FoodFor(id).Preferred]);
        private void FoodSettingsModal()
        {
            string id=Model.InspectedId;
            Modal("Food settings · "+Model.Character(id).Name,content=>
            {
                Label(content,"Auto-eating is always active in combat.",20,Gold);
                FoodModalText(content,()=>"Preferred food: "+FoodPreferenceName(id)+" · Threshold: "+Session.FoodFor(id).Threshold+"% HP");
                Button(content,"Change HP threshold",()=>Modal("HP threshold · "+Model.Character(id).Name,options=>
                {
                    for(int n=10;n<=90;n+=10)
                    {int threshold=n;Button(options,n+"% HP",()=>UpdateFoodSettings(()=>Model.ConfigureFood(true,threshold,Session.FoodFor(id).Preferred)));}
                }));
                Button(content,"Auto",()=>SetBankFoodPreference("auto"));
                foreach(var food in Session.Foods.Definitions)
                {
                    string key=food.ItemId;
                    FoodModalText(content,()=>PlayerNames.Display(Session.BankItems[key])+" ×"+Session.Quantity(key)+" · "+food.Healing+" HP");
                    Button(content,PlayerNames.Display(Session.BankItems[key]),()=>SetBankFoodPreference(key));
                }
            });
        }
        private void SetBankFoodPreference(string preference)
        {
            var s=Session.FoodFor(Model.InspectedId);
            UpdateFoodSettings(()=>Model.ConfigureFood(true,s.Threshold,preference));
        }
        private void UpdateFoodSettings(Func<bool> change)
        {
            if(host.IsLoading || host.PersistenceError!=null)return;
            try
            {
                if(!change())return;
                localNotice="";
                // Replace only the modal contents. Do not deactivate its root or rebuild Bank
                // while its threshold/preference button callback is still executing.
                FoodSettingsModal();
            }
            catch(Exception error){localNotice=error.Message;}
        }
        private void BankFoodActions(Transform content,FoodDefinition food)
        {
            string id=Model.InspectedId,key=food.ItemId;
            Label(content,"Restores "+food.Healing+" HP",20,Gold);
            FoodModalText(content,()=>"For "+Model.Character(id).Name+" · HP "+Session.Vitals[id].Hp+" / "+Session.Vitals[id].MaxHp);
            Button eat=null;
            eat=Button(content,"Eat",()=>{Act(()=>Model.Eat(key));eat.interactable=Session.CanEat(id,key);});
            eat.interactable=Session.CanEat(id,key);
            modalBindings.Add(()=>{if(eat!=null)eat.interactable=Session.CanEat(id,key);});
            var prefer=Button(content,"Set as preferred food",()=>Act(()=>Model.ConfigureFood(true,Session.FoodFor(id).Threshold,key)));
            Action refresh=()=>
            {
                if(prefer==null)return;bool selected=Session.FoodFor(id).Preferred==key;
                prefer.GetComponentInChildren<Text>().text=selected?"Already preferred":"Set as preferred food";
                prefer.interactable=!selected;
            };
            refresh();modalBindings.Add(refresh);
            Label(content,"Auto-eating is always active. Eating uses one shared-bank serving and grants no XP.",18,Muted);
        }
    }
}
