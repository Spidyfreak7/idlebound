using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
namespace Idlebound.Foundation
{
    public sealed class FoodDefinition
    {
        public string ItemId { get; }
        public decimal Healing { get; }
        public FoodDefinition(string itemId,decimal healing)
        {
            if(!DevelopmentIds.IsValid(itemId) || healing<=0)throw new ArgumentException("Invalid food definition.");
            ItemId=itemId;Healing=healing;
        }
    }
    public sealed class FoodCatalog
    {
        public ReadOnlyCollection<FoodDefinition> Definitions { get; }
        public FoodCatalog(IEnumerable<FoodDefinition> definitions,ProductionCatalog production)
        {
            var a=definitions?.ToArray()??throw new ArgumentNullException(nameof(definitions));
            if(a.Any(x=>x==null) || a.Select(x=>x.ItemId).Distinct().Count()!=a.Length || a.Any(x=>!production.Recipes.Any(p=>p.SkillId=="cooking" && p.OutputId==x.ItemId)))throw new ArgumentException("Foods require unique existing Cooking outputs.");
            Definitions=Array.AsReadOnly(a);
        }
        public static FoodCatalog Load(string csv,ProductionCatalog production)
        {
            if(!GatheringCatalog.DataRows(csv).Any())throw new ArgumentException("Live food catalog cannot be empty.");
            return new FoodCatalog(GatheringCatalog.DataRows(csv).Select(line=>
        {var a=line.Split(',');if(a.Length!=2)throw new ArgumentException("Food CSV needs two fields.");return new FoodDefinition(a[0],decimal.Parse(a[1],CultureInfo.InvariantCulture));}),production);
        }
    }
    public sealed class FoodSettings
    {
        // Retained for v3/source compatibility; combat auto-eating is always active.
        public bool Enabled => true;
        public int Threshold { get; }
        public string Preferred { get; }
        public FoodSettings(bool enabled=true,int threshold=50,string preferred="auto")
        {
            if(threshold<10 || threshold>90 || threshold%10!=0 || string.IsNullOrWhiteSpace(preferred))throw new ArgumentException("Food threshold must be 10–90 in ten-point steps.");
            Threshold=threshold;Preferred=preferred;
        }
    }
    public sealed partial class GatheringSession
    {
        public FoodCatalog Foods { get; }
        private readonly Dictionary<string,FoodSettings> foodSettings=new Dictionary<string,FoodSettings>();
        private readonly Dictionary<string,long> manualEatTicks=new Dictionary<string,long>();
        public FoodSettings FoodFor(string character)=>foodSettings.TryGetValue(character??"",out var s)?s:throw new ArgumentException("Unknown character.");
        public bool ConfigureFoodAt(long ticks,string character,bool enabled,int threshold,string preferred,out string error)
        {
            var settings=new FoodSettings(enabled,threshold,preferred);
            if(!foodSettings.ContainsKey(character??"") || preferred!="auto" && !Foods.Definitions.Any(f=>f.ItemId==preferred)){error="Unknown character or food preference.";return false;}
            SettleTo(ticks);foodSettings[character]=settings;error=null;return true;
        }
        // Lowest adequate serving minimizes unavoidable overheal; otherwise strongest, stable ID tie.
        private FoodDefinition ChooseFood(string preferred,decimal missing,IDictionary<string,long> bank)
        {
            var available=Foods.Definitions.Where(f=>bank.TryGetValue(f.ItemId,out var n) && n>0 && (preferred=="auto" || preferred==f.ItemId)).ToArray();
            return available.Where(f=>f.Healing>=missing).OrderBy(f=>f.Healing).ThenBy(f=>f.ItemId,StringComparer.Ordinal).FirstOrDefault()
                ?? available.OrderByDescending(f=>f.Healing).ThenBy(f=>f.ItemId,StringComparer.Ordinal).FirstOrDefault();
        }
        public bool CanEat(string character,string foodId=null)
        {
            if(!vitals.TryGetValue(character??"",out var hp) || hp.Hp<=0 || hp.Hp>=hp.MaxHp || manualEatTicks.TryGetValue(character,out var t)&&t==LastSettledTicks)return false;
            return ChooseFood(foodId??FoodFor(character).Preferred,hp.MaxHp-hp.Hp,inventory)!=null;
        }
        public bool EatAt(long ticks,string character,out string error,string foodId=null)
        {
            SettleTo(ticks);
            if(!CanEat(character,foodId)){error="Cannot eat: no available preferred food, full HP, dead character or repeated request.";return false;}
            var hp=vitals[character];var food=ChooseFood(foodId??FoodFor(character).Preferred,hp.MaxHp-hp.Hp,inventory);
            UseFood(food,hp,inventory);manualEatTicks[character]=LastSettledTicks;error=null;return true;
        }
        private static void UseFood(FoodDefinition food,CharacterVitals hp,IDictionary<string,long> bank)
        {
            decimal restored=food.Healing>=hp.MaxHp-hp.Hp?hp.MaxHp:hp.Hp+food.Healing;
            long next=bank[food.ItemId]-1;if(next==0)bank.Remove(food.ItemId);else bank[food.ItemId]=next;
            hp.Hp=restored;
        }
        private void AutoEat(string character,CharacterVitals hp,IDictionary<string,long> bank,OfflineReport receipt)
        {
            var settings=FoodFor(character);if(hp.Hp<=0)return;
            while(hp.Hp<=hp.MaxHp*settings.Threshold/100m)
            {
                var f=ChooseFood(settings.Preferred,hp.MaxHp-hp.Hp,bank);if(f==null)break;
                // Bounded by missing HP, not bank size; one positive-healing serving each iteration.
                UseFood(f,hp,bank);receipt?.For(character,Assignments.TryGetValue(character,out var a)?a:null).Consume(f.ItemId,1);
            }
        }
    }
}
