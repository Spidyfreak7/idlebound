using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
namespace Idlebound.Foundation
{
    public enum EnchantmentType { MiningSpeed, FishingSpeed, WoodcuttingSpeed, SmithingSpeed, CookingSpeed, CraftingSpeed, Defense, AttackSpeed, CriticalChance, OfflineDuration, BackgroundXp, HpRegeneration }
    public sealed class EnchantmentDefinition
    {
        public string Id { get; }
        public EnchantmentType Type { get; }
        public int Tier { get; }
        public int Level { get; }
        public decimal Potency { get; }
        public decimal Cap { get; }
        public long DurationTicks { get; }
        public decimal Xp { get; }
        public ReadOnlyDictionary<string,long> Inputs { get; }
        public EnchantmentDefinition(string id, EnchantmentType type, int tier, int level, decimal potency, decimal cap,
            IDictionary<string,long> inputs, long duration, decimal xp)
        {
            if (!DevelopmentIds.IsValid(id) || !Enum.IsDefined(typeof(EnchantmentType),type) || tier < 1 || tier > 4 || level < 15 || level > 99 ||
                potency <= 0 || cap <= 0 || cap > MaximumCap(type) || potency > cap || duration <= 0 || xp <= 0 || inputs == null || inputs.Count != 2 || inputs.Any(p=>!DevelopmentIds.IsValid(p.Key) || p.Value <= 0))
                throw new ArgumentException("Invalid enchantment definition.");
            Id=id; Type=type; Tier=tier; Level=level; Potency=potency; Cap=cap; DurationTicks=duration; Xp=xp;
            Inputs=new ReadOnlyDictionary<string,long>(new Dictionary<string,long>(inputs));
        }
        public static decimal MaximumCap(EnchantmentType type)
        {
            switch(type){case EnchantmentType.Defense:return .25m;case EnchantmentType.AttackSpeed:return .3m;case EnchantmentType.CriticalChance:return .15m;case EnchantmentType.OfflineDuration:return .75m;default:return .5m;}
        }
        public ActivityDefinition Activity() => new ActivityDefinition(Id,System.Text.RegularExpressions.Regex.Replace(Type.ToString(),"(?<!^)([A-Z])"," $1").Replace("Hp ","HP ")+" "+new[]{"I","II","III","IV"}[Tier-1],"magic",true,Level,DurationTicks,Id,Xp);
    }
    public sealed class EnchantmentCatalog
    {
        public ReadOnlyCollection<EnchantmentDefinition> Definitions { get; }
        public decimal SetMultiplier { get; }
        public decimal CriticalMultiplier { get; }
        public decimal RegenFraction { get; }
        public long RegenInterval { get; }
        public EnchantmentCatalog(IEnumerable<EnchantmentDefinition> definitions, decimal setMultiplier=.25m, decimal criticalMultiplier=1.5m, decimal regenFraction=.01m, long regenInterval=60L*TimeSpan.TicksPerSecond)
        {
            var a=definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            if(a.Any(x=>x==null) || a.Select(x=>x.Id).Distinct().Count()!=a.Length || a.Select(x=>x.Type+":"+x.Tier).Distinct().Count()!=a.Length ||
                a.GroupBy(x=>x.Type).Any(g=>g.Select(x=>x.Cap).Distinct().Count()!=1) || setMultiplier<0 || setMultiplier>1 || criticalMultiplier<1 || criticalMultiplier>2 || regenFraction<=0 || regenFraction>1 || regenInterval<=0)
                throw new ArgumentException("Invalid/duplicate enchantment catalog or rules.");
            Definitions=Array.AsReadOnly(a); SetMultiplier=setMultiplier; CriticalMultiplier=criticalMultiplier; RegenFraction=regenFraction; RegenInterval=regenInterval;
        }
        public static EnchantmentCatalog Empty => new EnchantmentCatalog(Array.Empty<EnchantmentDefinition>());
        public static EnchantmentCatalog Load(string csv,string rules,IEnumerable<ResourceDefinition> resources)
        {
            var known=new HashSet<string>(resources.Select(x=>x.Id)); var list=new List<EnchantmentDefinition>();
            foreach(var line in GatheringCatalog.DataRows(csv))
            {
                var r=line.Split(','); if(r.Length!=12 || !Enum.TryParse(r[1],out EnchantmentType type) || type.ToString()!=r[1]) throw new ArgumentException("Invalid enchantment CSV.");
                decimal N(int i)=>decimal.Parse(r[i],CultureInfo.InvariantCulture);
                if(!known.Contains(r[6]) || !known.Contains(r[8])) throw new ArgumentException("Unknown enchantment material.");
                if(N(10)*TimeSpan.TicksPerSecond!=decimal.Truncate(N(10)*TimeSpan.TicksPerSecond))throw new ArgumentException("Fractional tick duration.");
                list.Add(new EnchantmentDefinition(r[0],type,int.Parse(r[2]),int.Parse(r[3]),N(4),N(5),new Dictionary<string,long>{{r[6],long.Parse(r[7])},{r[8],long.Parse(r[9])}},checked((long)(N(10)*TimeSpan.TicksPerSecond)),N(11)));
            }
            if(list.Count!=48 || Enum.GetValues(typeof(EnchantmentType)).Cast<EnchantmentType>().Any(t=>list.Count(e=>e.Type==t)!=4))throw new ArgumentException("Live enchantment catalog requires all 48 type/tier entries.");
            var c=GatheringCatalog.DataRows(rules).Single().Split(','); if(c.Length!=4) throw new ArgumentException("Invalid enchantment rules.");
            decimal D(int i)=>decimal.Parse(c[i],CultureInfo.InvariantCulture);
            return new EnchantmentCatalog(list,D(0),D(1),D(2),checked((long)(D(3)*TimeSpan.TicksPerSecond)));
        }
    }
}
