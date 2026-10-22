using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace Idlebound.Foundation
{
    public enum EquipmentSlot { Head, Body, Legs, Hands, Feet, Mainhand, Offhand, Necklace, Ring, Cape }
    public enum CombatStyle { Melee, Ranged, Magic }

    public static class EquipmentSlots
    {
        public static readonly ReadOnlyCollection<EquipmentSlot> EnchantmentSetSlots =
            new[] { EquipmentSlot.Head, EquipmentSlot.Body, EquipmentSlot.Legs, EquipmentSlot.Feet }.ToList().AsReadOnly();
        public static int StandardArmorLevel(int tier)
        {
            if (tier < 1 || tier > 10) throw new ArgumentOutOfRangeException(nameof(tier));
            return 5 + (tier - 1) * 10;
        }
    }

    public sealed class EquipmentDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public EquipmentSlot Slot { get; }
        public ReadOnlyDictionary<string, int> Requirements { get; }
        public int StandardArmorTier { get; }
        public CombatStyle? Style { get; }
        public decimal DamageBonus { get; }
        public decimal DefenseBonus { get; }
        public long AttackIntervalTicks { get; }
        public bool TwoHanded { get; }
        public bool IsDevelopmentOnly => true;
        // Boss inherent powers remain deferred; permanent enchantments are instance-specific.
        public IReadOnlyList<string> InherentEffectIds { get; } = Array.AsReadOnly(Array.Empty<string>());

        public EquipmentDefinition(string id, string name, EquipmentSlot slot, IDictionary<string, int> requirements,
            int standardArmorTier, CombatStyle? style, decimal damage, decimal defense, long intervalTicks, bool twoHanded)
        {
            if (!DevelopmentIds.IsValid(id) || string.IsNullOrWhiteSpace(name) || !name.StartsWith("[DEV]", StringComparison.Ordinal) ||
                !Enum.IsDefined(typeof(EquipmentSlot), slot) || standardArmorTier < 0 || standardArmorTier > 10 ||
                damage < 0 || defense < 0 || intervalTicks < 0 || style.HasValue && !Enum.IsDefined(typeof(CombatStyle), style.Value))
                throw new ArgumentException("Invalid development equipment definition.");
            var copy = requirements == null ? throw new ArgumentNullException(nameof(requirements)) : new Dictionary<string, int>(requirements);
            if (copy.Any(r => !SkillCatalog.Create().Any(s => s.Id == r.Key) || r.Value < 1 || r.Value > 99))
                throw new ArgumentException("Invalid equip requirements.");
            if (standardArmorTier > 0 && (!(EquipmentSlots.EnchantmentSetSlots.Contains(slot) || slot == EquipmentSlot.Hands) ||
                !copy.Values.Contains(EquipmentSlots.StandardArmorLevel(standardArmorTier))))
                throw new ArgumentException("Standard armor must enforce its approved equip level.");
            if (style.HasValue != (slot == EquipmentSlot.Mainhand) ||
                (style.HasValue ? intervalTicks <= 0 : intervalTicks != 0) || twoHanded && slot != EquipmentSlot.Mainhand)
                throw new ArgumentException("Only mainhand weapons define a style/interval or block offhand.");
            Id = id; Name = name; Slot = slot; Requirements = new ReadOnlyDictionary<string, int>(copy);
            StandardArmorTier = standardArmorTier; Style = style; DamageBonus = damage;
            DefenseBonus = defense; AttackIntervalTicks = intervalTicks; TwoHanded = twoHanded;
        }
    }

    public sealed class EquipmentInstance
    {
        public string InstanceId { get; }
        public EquipmentDefinition Definition { get; }
        // Applied only by the session completion transaction; no replacement/removal command.
        public string PermanentEnchantmentId { get; internal set; }
        internal EquipmentInstance(string id, EquipmentDefinition definition)
        { InstanceId = id; Definition = definition; PermanentEnchantmentId = null; }
    }

    public sealed class EquipmentCatalog
    {
        public ReadOnlyCollection<EquipmentDefinition> Definitions { get; }
        public EquipmentCatalog(IEnumerable<EquipmentDefinition> definitions)
        {
            var list = definitions?.ToList() ?? throw new ArgumentNullException(nameof(definitions));
            if (list.Any(d => d == null) || list.Select(d => d.Id).Distinct().Count() != list.Count)
                throw new ArgumentException("Duplicate/null equipment definitions.");
            Definitions = list.AsReadOnly();
        }
        public static EquipmentCatalog Empty => new EquipmentCatalog(Array.Empty<EquipmentDefinition>());
        public static EquipmentCatalog LoadDevelopment(string csv)
        {
            var items = new List<EquipmentDefinition>();
            foreach (string line in GatheringCatalog.DataRows(csv))
            {
                var row = line.Split(',').Select(x => x.Trim()).ToArray();
                if (row.Length != 11) throw new ArgumentException("Gear CSV needs eleven fields.");
                if (!Enum.TryParse(row[2], out EquipmentSlot slot) || slot.ToString() != row[2])
                    throw new ArgumentException("Unknown equipment slot.");
                CombatStyle? style = null;
                if (row[5] != "none")
                {
                    if (!Enum.TryParse(row[5], out CombatStyle parsed) || parsed.ToString() != row[5]) throw new ArgumentException("Unknown style.");
                    style = parsed;
                }
                var requirements = new Dictionary<string, int>();
                foreach (string field in row[3].Split(';'))
                {
                    var pair = field.Split(':');
                    if (pair.Length != 2 || requirements.ContainsKey(pair[0])) throw new ArgumentException("Invalid/duplicate equip requirement.");
                    requirements.Add(pair[0], int.Parse(pair[1], CultureInfo.InvariantCulture));
                }
                decimal interval = decimal.Parse(row[8], CultureInfo.InvariantCulture) * TimeSpan.TicksPerSecond;
                if (interval != decimal.Truncate(interval) || interval < 0 || interval > long.MaxValue) throw new ArgumentException("Invalid interval.");
                if (row[9] != "true" && row[9] != "false" || row[10] != "development") throw new ArgumentException("Gear must be explicit development data.");
                items.Add(new EquipmentDefinition(row[0], row[1], slot, requirements,
                    int.Parse(row[4], CultureInfo.InvariantCulture), style,
                    decimal.Parse(row[6], CultureInfo.InvariantCulture), decimal.Parse(row[7], CultureInfo.InvariantCulture), (long)interval, row[9] == "true"));
            }
            return new EquipmentCatalog(items);
        }
    }
}
