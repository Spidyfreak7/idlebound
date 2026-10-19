using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Idlebound.Foundation
{
    public sealed class SkillDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public SkillDefinition(string id, string displayName)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Skill ID and label are required.");
            Id = id;
            DisplayName = displayName;
        }
    }

    // XP is stored only. No curve, level calculation, or XP generation is defined.
    public sealed class SkillState
    {
        public const int MaximumLevel = 99;
        public string SkillId { get; }
        public int Level { get; }
        public long StoredXp { get; }
        public SkillState(string skillId, int level, long storedXp)
        {
            if (string.IsNullOrWhiteSpace(skillId)) throw new ArgumentException("Skill ID required.");
            if (level < 1 || level > MaximumLevel) throw new ArgumentOutOfRangeException(nameof(level));
            if (storedXp < 0) throw new ArgumentOutOfRangeException(nameof(storedXp));
            SkillId = skillId;
            Level = level;
            StoredXp = storedXp;
        }
    }

    public sealed class CharacterState
    {
        public const int SkillCount = 9;
        public string Id { get; }
        public string DisplayName { get; }
        public ReadOnlyCollection<SkillState> Skills { get; }
        public CharacterState(string id, string displayName, IEnumerable<SkillState> skills)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Character ID and label required.");
            var copy = skills?.ToList() ?? throw new ArgumentNullException(nameof(skills));
            if (copy.Count != SkillCount || copy.Any(s => s == null) ||
                copy.Select(s => s.SkillId).Distinct().Count() != SkillCount)
                throw new ArgumentException("Exactly nine distinct skills required.");
            Id = id;
            DisplayName = displayName;
            Skills = copy.AsReadOnly();
        }
    }

    public sealed class ActivityDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string SkillId { get; }
        public bool IsAvailable { get; }
        public ActivityDefinition(string id, string displayName, string skillId, bool isAvailable)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(displayName) ||
                string.IsNullOrWhiteSpace(skillId)) throw new ArgumentException("Activity fields required.");
            Id = id;
            DisplayName = displayName;
            SkillId = skillId;
            IsAvailable = isAvailable;
        }
    }

    // Access entitlement is separate from characters. No membership purchase/unlock rules.
    public sealed class RosterAccess
    {
        public const int Capacity = 9;
        public const int InitialAvailableSlots = 3;
        public int AvailableSlots { get; }
        public RosterAccess(int availableSlots = InitialAvailableSlots)
        {
            if (availableSlots < InitialAvailableSlots || availableSlots > Capacity)
                throw new ArgumentOutOfRangeException(nameof(availableSlots));
            AvailableSlots = availableSlots;
        }
    }

    public sealed class GameState
    {
        private readonly List<CharacterState> characters = new List<CharacterState>();
        private readonly Dictionary<string, string> assignments = new Dictionary<string, string>();
        public RosterAccess Access { get; }
        public ReadOnlyCollection<SkillDefinition> Skills { get; }
        public ReadOnlyCollection<ActivityDefinition> Activities { get; }
        public ReadOnlyCollection<CharacterState> Characters { get; }
        // Character ID -> exact activity ID. One assignment per character and per activity.
        public ReadOnlyDictionary<string, string> Assignments { get; }
        public string ActiveCharacterId { get; private set; }

        public GameState(RosterAccess access, IEnumerable<SkillDefinition> skills,
            IEnumerable<ActivityDefinition> activities)
        {
            Access = access ?? throw new ArgumentNullException(nameof(access));
            var skillList = skills?.ToList() ?? throw new ArgumentNullException(nameof(skills));
            if (skillList.Count != CharacterState.SkillCount || skillList.Any(s => s == null) ||
                skillList.Select(s => s.Id).Distinct().Count() != CharacterState.SkillCount)
                throw new ArgumentException("Exactly nine skill definitions required.");
            var activityList = activities?.ToList() ?? throw new ArgumentNullException(nameof(activities));
            if (activityList.Any(a => a == null || !skillList.Any(s => s.Id == a.SkillId)) ||
                activityList.Select(a => a.Id).Distinct().Count() != activityList.Count)
                throw new ArgumentException("Activities need unique IDs and defined skills.");
            Skills = skillList.AsReadOnly();
            Activities = activityList.AsReadOnly();
            Characters = characters.AsReadOnly();
            Assignments = new ReadOnlyDictionary<string, string>(assignments);
        }

        public bool TryAddCharacter(CharacterState character)
        {
            if (character == null || characters.Count >= Access.AvailableSlots ||
                characters.Count >= RosterAccess.Capacity || characters.Any(c => c.Id == character.Id) ||
                character.Skills.Any(s => !Skills.Any(d => d.Id == s.SkillId))) return false;
            characters.Add(character);
            if (ActiveCharacterId == null) ActiveCharacterId = character.Id;
            return true;
        }

        public bool TrySelectActive(string characterId)
        {
            if (!characters.Any(c => c.Id == characterId)) return false;
            ActiveCharacterId = characterId;
            return true;
        }

        public bool IsActiveCharacter(string characterId) =>
            characterId != null && ActiveCharacterId == characterId;

        public bool TryAssign(string characterId, string activityId, out string error)
        {
            if (!characters.Any(c => c.Id == characterId)) { error = "Unknown character."; return false; }
            var activity = Activities.FirstOrDefault(a => a.Id == activityId);
            if (activity == null || !activity.IsAvailable) { error = "Activity unavailable."; return false; }
            if (assignments.Any(a => a.Key != characterId && a.Value == activityId))
            { error = "Activity already assigned to another character."; return false; }
            assignments[characterId] = activityId;
            error = null;
            return true;
        }

        public bool Unassign(string characterId) => characterId != null && assignments.Remove(characterId);
    }
}
