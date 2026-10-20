using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace Idlebound.Foundation
{
    internal static class DevelopmentIds
    {
        internal static bool IsValid(string id) => !string.IsNullOrWhiteSpace(id) &&
            id.All(c => c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '-');
    }

    public sealed class CombatRules
    {
        public decimal PlayerMaxHp { get; }
        public decimal BaseDamage { get; }
        public long BaseIntervalTicks { get; }
        public decimal MeleeXpPerDamage { get; }
        public decimal RangedXpPerDamage { get; }
        public decimal MagicXpPerDamage { get; }
        public int EventBudget { get; }
        public CombatRules(decimal hp, decimal damage, long interval, decimal meleeXp, decimal rangedXp, decimal magicXp, int eventBudget)
        {
            if (hp <= 0 || damage <= 0 || interval <= 0 || meleeXp <= 0 || rangedXp <= 0 || magicXp < 0 ||
                magicXp >= meleeXp || magicXp >= rangedXp || eventBudget <= 0)
                throw new ArgumentException("Invalid development combat rules; Magic XP must be lower.");
            PlayerMaxHp = hp; BaseDamage = damage; BaseIntervalTicks = interval;
            MeleeXpPerDamage = meleeXp; RangedXpPerDamage = rangedXp; MagicXpPerDamage = magicXp; EventBudget = eventBudget;
        }
        public decimal XpPerDamage(CombatStyle style)
        {
            if (!Enum.IsDefined(typeof(CombatStyle), style)) throw new ArgumentException("Unknown combat style.");
            return style == CombatStyle.Melee ? MeleeXpPerDamage : style == CombatStyle.Ranged ? RangedXpPerDamage : MagicXpPerDamage;
        }
        public static CombatRules LoadDevelopment(string csv)
        {
            var lines = GatheringCatalog.DataRows(csv).ToArray();
            if (lines.Length != 1) throw new ArgumentException("Exactly one development rules row required.");
            var r = lines[0].Split(',');
            if (r.Length != 8 || r[7] != "development") throw new ArgumentException("Rules must be labelled development.");
            decimal ticks = decimal.Parse(r[2], CultureInfo.InvariantCulture) * TimeSpan.TicksPerSecond;
            if (ticks != decimal.Truncate(ticks) || ticks > long.MaxValue) throw new ArgumentException("Invalid player interval.");
            return new CombatRules(decimal.Parse(r[0], CultureInfo.InvariantCulture), decimal.Parse(r[1], CultureInfo.InvariantCulture),
                (long)ticks, decimal.Parse(r[3], CultureInfo.InvariantCulture), decimal.Parse(r[4], CultureInfo.InvariantCulture),
                decimal.Parse(r[5], CultureInfo.InvariantCulture), int.Parse(r[6], CultureInfo.InvariantCulture));
        }
    }

    public sealed class MonsterDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public decimal MaxHp { get; }
        public decimal Damage { get; }
        public long AttackIntervalTicks { get; }
        public CombatStyle Style { get; }
        public MonsterDefinition(string id, string name, decimal hp, decimal damage, long interval, CombatStyle style)
        {
            if (!DevelopmentIds.IsValid(id) || !id.StartsWith("dev-", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(name) ||
                !name.StartsWith("[DEV]", StringComparison.Ordinal) || hp <= 0 || damage < 0 || interval <= 0 || !Enum.IsDefined(typeof(CombatStyle), style))
                throw new ArgumentException("Invalid development monster.");
            Id = id; Name = name; MaxHp = hp; Damage = damage; AttackIntervalTicks = interval; Style = style;
        }
        internal ActivityDefinition Activity() => new ActivityDefinition(Id, Name, "melee", true);
    }

    public sealed class LootDefinition
    {
        public string Id { get; }
        public string MonsterId { get; }
        public string ItemId { get; }
        public string ItemName { get; }
        public long Quantity { get; }
        public decimal Chance { get; }
        public LootDefinition(string id, string monster, string item, string name, long quantity, decimal chance)
        {
            if (!DevelopmentIds.IsValid(id) || !DevelopmentIds.IsValid(monster) || !DevelopmentIds.IsValid(item) ||
                string.IsNullOrWhiteSpace(name) || quantity <= 0 || chance < 0 || chance > 1) throw new ArgumentException("Invalid loot definition.");
            Id = id; MonsterId = monster; ItemId = item; ItemName = name; Quantity = quantity; Chance = chance;
        }
    }

    public sealed class CombatCatalog
    {
        public CombatRules Rules { get; }
        public ReadOnlyCollection<MonsterDefinition> Monsters { get; }
        public ReadOnlyCollection<LootDefinition> Drops { get; }
        public CombatCatalog(CombatRules rules, IEnumerable<MonsterDefinition> monsters, IEnumerable<LootDefinition> drops)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            var m = monsters?.ToList() ?? throw new ArgumentNullException(nameof(monsters));
            var d = drops?.ToList() ?? throw new ArgumentNullException(nameof(drops));
            if (m.Any(x => x == null) || m.Select(x => x.Id).Distinct().Count() != m.Count || d.Any(x => x == null) ||
                d.Select(x => x.Id).Distinct().Count() != d.Count || d.Any(x => !m.Any(monster => monster.Id == x.MonsterId)))
                throw new ArgumentException("Duplicate/null/unknown monster or drop definition.");
            if (d.GroupBy(x => x.ItemId).Any(group => group.Select(x => x.ItemName).Distinct().Count() != 1))
                throw new ArgumentException("Conflicting loot item names.");
            Monsters = m.AsReadOnly(); Drops = d.AsReadOnly();
        }
        public static CombatCatalog Empty => new CombatCatalog(new CombatRules(50, 2, 2 * TimeSpan.TicksPerSecond, 1, 1, 0.5m, 100000),
            Array.Empty<MonsterDefinition>(), Array.Empty<LootDefinition>());
        public static CombatCatalog LoadDevelopment(string rules, string monsters, string drops)
        {
            var m = new List<MonsterDefinition>(); var d = new List<LootDefinition>();
            foreach (string line in GatheringCatalog.DataRows(monsters))
            {
                var r = line.Split(',');
                if (r.Length != 7 || r[6] != "development" || !Enum.TryParse(r[5], out CombatStyle style) || style.ToString() != r[5])
                    throw new ArgumentException("Invalid development monster row.");
                decimal ticks = decimal.Parse(r[4], CultureInfo.InvariantCulture) * TimeSpan.TicksPerSecond;
                if (ticks != decimal.Truncate(ticks) || ticks > long.MaxValue) throw new ArgumentException("Invalid enemy interval.");
                m.Add(new MonsterDefinition(r[0], r[1], decimal.Parse(r[2], CultureInfo.InvariantCulture),
                    decimal.Parse(r[3], CultureInfo.InvariantCulture), (long)ticks, style));
            }
            foreach (string line in GatheringCatalog.DataRows(drops))
            {
                var r = line.Split(',');
                if (r.Length != 6) throw new ArgumentException("Loot rows need six fields.");
                d.Add(new LootDefinition(r[0], r[1], r[2], r[3], long.Parse(r[4], CultureInfo.InvariantCulture), decimal.Parse(r[5], CultureInfo.InvariantCulture)));
            }
            return new CombatCatalog(CombatRules.LoadDevelopment(rules), m, d);
        }
    }

    // Must be pure in these arguments: counter-based rolls support rollback/replay without RNG mutation.
    public interface IDeterministicLootRoller { decimal Roll(string characterId, string monsterId, long killIndex, string dropId); }
    public sealed class SeededLootRoller : IDeterministicLootRoller
    {
        private readonly ulong seed;
        public SeededLootRoller(ulong seed) { this.seed = seed; }
        public decimal Roll(string characterId, string monsterId, long killIndex, string dropId)
        {
            ulong hash = 14695981039346656037UL ^ seed;
            string key = characterId + "|" + monsterId + "|" + killIndex.ToString(CultureInfo.InvariantCulture) + "|" + dropId;
            unchecked { foreach (char c in key) { hash ^= c; hash *= 1099511628211UL; } }
            return (hash % 1000000UL) / 1000000m;
        }
    }
}
