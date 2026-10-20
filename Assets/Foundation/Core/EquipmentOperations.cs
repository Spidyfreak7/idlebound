using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace Idlebound.Foundation
{
    public sealed class CharacterEquipment
    {
        internal readonly Dictionary<EquipmentSlot, EquipmentInstance> Items = new Dictionary<EquipmentSlot, EquipmentInstance>();
        public ReadOnlyDictionary<EquipmentSlot, EquipmentInstance> Slots { get; }
        internal CharacterEquipment() { Slots = new ReadOnlyDictionary<EquipmentSlot, EquipmentInstance>(Items); }
    }

    public sealed class CombatProfile
    {
        public CombatStyle Style { get; }
        public decimal Damage { get; }
        public decimal Defense { get; }
        public long IntervalTicks { get; }
        internal CombatProfile(CombatStyle style, decimal damage, decimal defense, long interval)
        { Style = style; Damage = damage; Defense = defense; IntervalTicks = interval; }
    }

    public sealed partial class GatheringSession
    {
        private readonly Dictionary<string, CharacterEquipment> loadouts = new Dictionary<string, CharacterEquipment>();
        private readonly Dictionary<string, EquipmentInstance> bankInstances = new Dictionary<string, EquipmentInstance>();
        private long equipmentSerial;
        public EquipmentCatalog Equipment { get; }
        public ReadOnlyDictionary<string, CharacterEquipment> Loadouts { get; private set; }
        public ReadOnlyDictionary<string, EquipmentInstance> BankEquipmentInstances { get; private set; }
        public ReadOnlyDictionary<string, string> BankItems { get; private set; }

        public CombatProfile Profile(string characterId)
        {
            if (!loadouts.TryGetValue(characterId, out var loadout)) throw new ArgumentException("Unknown character.");
            return BuildProfile(loadout.Items.Values);
        }

        private CombatProfile BuildProfile(IEnumerable<EquipmentInstance> items)
        {
            var list = items.ToArray();
            var weapon = list.FirstOrDefault(i => i.Definition.Slot == EquipmentSlot.Mainhand);
            decimal damage = Combat.Rules.BaseDamage;
            decimal defense = 0;
            foreach (var item in list)
            { damage = checked(damage + item.Definition.DamageBonus); defense = checked(defense + item.Definition.DefenseBonus); }
            return new CombatProfile(weapon?.Definition.Style ?? CombatStyle.Melee, damage, defense,
                weapon?.Definition.AttackIntervalTicks ?? Combat.Rules.BaseIntervalTicks);
        }

        public bool EquipAt(long ticks, string characterId, string itemId, EquipmentSlot slot, out string error)
        {
            SettleTo(ticks);
            var definition = Equipment.Definitions.FirstOrDefault(d => d.Id == itemId);
            var character = Characters.FirstOrDefault(c => c.Id == characterId);
            if (definition == null || character == null || definition.Slot != slot)
            { error = "Unknown item/character or incompatible slot."; return false; }
            if (definition.Requirements.Any(r => character.Skills.First(s => s.SkillId == r.Key).Level < r.Value))
            { error = "Equip skill requirements not met."; return false; }
            var current = loadouts[characterId].Items;
            if (slot == EquipmentSlot.Offhand && current.TryGetValue(EquipmentSlot.Mainhand, out var main) && main.Definition.TwoHanded)
            { error = "A two-handed weapon blocks offhand."; return false; }
            if (Quantity(itemId) < 1) { error = "Item is not available in the shared bank."; return false; }
            var nextBank = new Dictionary<string, long>(inventory);
            var nextInstances = new Dictionary<string, EquipmentInstance>(bankInstances);
            var nextSlots = new Dictionary<EquipmentSlot, EquipmentInstance>(current);
            var candidate = nextInstances.Values.Where(i => i.Definition.Id == itemId).OrderBy(i => i.InstanceId, StringComparer.Ordinal).FirstOrDefault();
            long serial = equipmentSerial;
            if (candidate == null)
            {
                serial = checked(serial + 1);
                candidate = new EquipmentInstance("gear-" + serial.ToString("D10", CultureInfo.InvariantCulture), definition);
            }
            else nextInstances.Remove(candidate.InstanceId);
            if (--nextBank[itemId] == 0) nextBank.Remove(itemId);
            var returnedSlots = new List<EquipmentSlot> { slot };
            if (slot == EquipmentSlot.Mainhand && definition.TwoHanded) returnedSlots.Add(EquipmentSlot.Offhand);
            foreach (var returnedSlot in returnedSlots)
                if (nextSlots.TryGetValue(returnedSlot, out var old))
                {
                    ReturnEquipment(nextBank, nextInstances, old);
                    nextSlots.Remove(returnedSlot);
                }
            nextSlots[slot] = candidate;
            var profile = BuildProfile(nextSlots.Values);
            // Everything, including bank overflow and instance collisions, was preflighted above.
            ReplaceInventory(nextBank);
            bankInstances.Clear(); foreach (var pair in nextInstances) bankInstances.Add(pair.Key, pair.Value);
            current.Clear(); foreach (var pair in nextSlots) current.Add(pair.Key, pair.Value);
            equipmentSerial = serial;
            OnEquipmentChanged(characterId, profile);
            error = null;
            return true;
        }

        public bool UnequipAt(long ticks, string characterId, EquipmentSlot slot, out string error)
        {
            SettleTo(ticks);
            if (!loadouts.TryGetValue(characterId, out var loadout) || !loadout.Items.TryGetValue(slot, out var instance))
            { error = "Unknown character or empty slot."; return false; }
            var nextBank = new Dictionary<string, long>(inventory);
            var nextInstances = new Dictionary<string, EquipmentInstance>(bankInstances);
            ReturnEquipment(nextBank, nextInstances, instance);
            var profile = BuildProfile(loadout.Items.Where(p => p.Key != slot).Select(p => p.Value));
            ReplaceInventory(nextBank);
            bankInstances.Clear(); foreach (var pair in nextInstances) bankInstances.Add(pair.Key, pair.Value);
            loadout.Items.Remove(slot);
            OnEquipmentChanged(characterId, profile);
            error = null;
            return true;
        }

        private static void ReturnEquipment(Dictionary<string, long> bank, Dictionary<string, EquipmentInstance> instances, EquipmentInstance item)
        {
            long quantity = bank.TryGetValue(item.Definition.Id, out var count) ? count : 0;
            bank[item.Definition.Id] = checked(quantity + 1);
            instances.Add(item.InstanceId, item);
        }

        // Explicit testing command, never called automatically and never a production reward.
        public void GrantDevelopmentGearKitAt(long ticks)
        {
            SettleTo(ticks);
            var next = new Dictionary<string, long>(inventory);
            foreach (var item in Equipment.Definitions)
                next[item.Id] = checked((next.TryGetValue(item.Id, out var count) ? count : 0) + 1);
            ReplaceInventory(next);
        }

        private void OnEquipmentChanged(string characterId, CombatProfile profile)
        {
            if (encounters.TryGetValue(characterId, out var fight))
                fight.PlayerRemainingTicks = profile.IntervalTicks;
        }
    }
}
