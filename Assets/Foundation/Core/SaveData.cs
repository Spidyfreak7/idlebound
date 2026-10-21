using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Idlebound.Foundation
{
    [DataContract] public sealed class SaveDocument
    {
        [DataMember(IsRequired = true)] public string Catalog;
        [DataMember(IsRequired = true)] public long UtcTicks;
        [DataMember(IsRequired = true)] public SessionSnapshot State;
        [DataMember(IsRequired = true)] public OfflineReport Pending;
    }
    [DataContract] public sealed class SessionSnapshot
    {
        [DataMember(IsRequired = true)] public int Slots;
        [DataMember(IsRequired = true)] public string Active;
        [DataMember(IsRequired = true)] public long Ticks, EquipmentSerial;
        [DataMember(IsRequired = true)] public ulong LootSeed;
        [DataMember(IsRequired = true)] public bool RefundInputs;
        [DataMember(IsRequired = true)] public SavedCharacter[] Characters;
        [DataMember(IsRequired = true)] public ItemAmount[] Bank;
        [DataMember(IsRequired = true)] public SavedEquipment[] Equipment;
        [DataMember(IsRequired = true)] public SavedKills[] Kills;
    }
    [DataContract] public sealed class SavedCharacter
    {
        [DataMember(IsRequired = true)] public string Id, Name, Assignment, Job;
        [DataMember(IsRequired = true)] public SavedSkill[] Skills;
        [DataMember(IsRequired = true)] public long Partial, Cycles;
        [DataMember(IsRequired = true)] public decimal Hp;
        [DataMember(IsRequired = true)] public SavedEncounter Encounter;
        [DataMember(IsRequired = true)] public SavedEffect[] Effects;
    }
    [DataContract] public sealed class SavedSkill
    {
        [DataMember(IsRequired = true)] public string Id;
        [DataMember(IsRequired = true)] public decimal Xp;
        [DataMember(IsRequired = true)] public int Level;
    }
    [DataContract] public sealed class SavedEquipment
    {
        // Null owner means a bank instance. Slot is reconstructed from the item definition.
        [DataMember(IsRequired = true)] public string Instance, Item, Owner;
    }
    [DataContract] public sealed class SavedEncounter
    {
        [DataMember(IsRequired = true)] public string Monster;
        [DataMember(IsRequired = true)] public decimal Hp;
        [DataMember(IsRequired = true)] public long PlayerTicks, EnemyTicks;
    }
    [DataContract] public sealed class SavedEffect
    {
        [DataMember(IsRequired = true)] public string Source, Skill;
        [DataMember(IsRequired = true)] public decimal Efficiency;
    }
    [DataContract] public sealed class SavedKills
    {
        [DataMember(IsRequired = true)] public string Character, Monster;
        [DataMember(IsRequired = true)] public long Count;
    }
    [DataContract] public sealed class ItemAmount
    {
        [DataMember(IsRequired = true)] public string Id;
        [DataMember(IsRequired = true)] public long Quantity;
    }
    [DataContract] public sealed class XpAmount
    {
        [DataMember(IsRequired = true)] public string Skill;
        [DataMember(IsRequired = true)] public decimal Quantity;
    }
    [DataContract] public sealed class CharacterOutcome
    {
        [DataMember(IsRequired = true)] public string Character, Activity, StopReason;
        [DataMember(IsRequired = true)] public List<ItemAmount> Items = new List<ItemAmount>();
        [DataMember(IsRequired = true)] public List<XpAmount> Xp = new List<XpAmount>();
        public void Item(string id, long count)
        {
            if (count <= 0) return;
            var item = Items.Find(x => x.Id == id);
            if (item == null) Items.Add(new ItemAmount { Id = id, Quantity = count });
            else item.Quantity = checked(item.Quantity + count);
        }
        public void Award(string skill, decimal count)
        {
            if (count <= 0) return;
            var xp = Xp.Find(x => x.Skill == skill);
            if (xp == null) Xp.Add(new XpAmount { Skill = skill, Quantity = count });
            else xp.Quantity = checked(xp.Quantity + count);
        }
    }
    [DataContract] public sealed class OfflineReport
    {
        [DataMember(IsRequired = true)] public string Id = Guid.NewGuid().ToString("N");
        [DataMember(IsRequired = true)] public long AwayTicks, EligibleTicks;
        [DataMember(IsRequired = true)] public bool Capped;
        [DataMember(IsRequired = true)] public int Intervals = 1;
        [DataMember(IsRequired = true)] public List<CharacterOutcome> Characters = new List<CharacterOutcome>();
        public CharacterOutcome For(string character, string activity)
        {
            var entry = Characters.Find(c => c.Character == character && c.Activity == activity);
            if (entry == null) { entry = new CharacterOutcome { Character = character, Activity = activity }; Characters.Add(entry); }
            return entry;
        }
        public void Merge(OfflineReport next)
        {
            Intervals = checked(Intervals + next.Intervals);
            AwayTicks = checked(AwayTicks + next.AwayTicks); EligibleTicks = checked(EligibleTicks + next.EligibleTicks); Capped |= next.Capped;
            foreach (var c in next.Characters)
            {
                var target = For(c.Character,c.Activity);
                foreach (var item in c.Items) target.Item(item.Id,item.Quantity);
                foreach (var xp in c.Xp) target.Award(xp.Skill,xp.Quantity);
                if (c.StopReason != null) target.StopReason = c.StopReason;
            }
        }
    }
}
