using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace Idlebound.Foundation
{
    public static class SkillCatalog
    {
        public static ReadOnlyCollection<SkillDefinition> Create() => new[]
        {
            "Mining", "Smithing", "Crafting", "Woodcutting", "Fishing", "Cooking", "Melee", "Ranged", "Magic"
        }.Select(name => new SkillDefinition(name.ToLowerInvariant(), name)).ToList().AsReadOnly();
    }

    public sealed class ResourceDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public int Tier { get; }
        public ResourceDefinition(string id, string displayName, int tier)
        {
            Id = id;
            DisplayName = displayName;
            Tier = tier;
        }
    }

    public sealed class GatheringCatalog
    {
        public ReadOnlyCollection<ResourceDefinition> Resources { get; }
        public ReadOnlyCollection<ActivityDefinition> Activities { get; }
        public bool UsesDevelopmentXp { get; }

        // This is explicitly a small unquoted CSV schema. No arbitrary CSV dialect is claimed.
        public static GatheringCatalog Load(string csv, bool developmentXp = false)
        {
            var rows = DataRows(csv).ToArray();
            var levels = new[] { 1, 10, 20, 30, 40, 50, 60, 70, 80, 90 };
            if (rows.Length != levels.Length) throw new ArgumentException("Exactly ten resource tiers required.");
            var resources = new List<ResourceDefinition>();
            var activities = new List<ActivityDefinition>();
            var skills = new[] { "mining", "woodcutting", "fishing" };
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i].Split(',').Select(x => x.Trim()).ToArray();
                if (row.Length != 7 || !int.TryParse(row[0], out int level) || level != levels[i])
                    throw new ArgumentException("Invalid or unordered tier row.");
                if (row.Skip(1).Take(5).Any(string.IsNullOrWhiteSpace))
                    throw new ArgumentException("All material names required.");
                decimal seconds = decimal.Parse(row[6], CultureInfo.InvariantCulture);
                decimal ticks = seconds * TimeSpan.TicksPerSecond;
                if (ticks <= 0 || ticks != decimal.Truncate(ticks) || ticks > long.MaxValue)
                    throw new ArgumentException("Duration must be positive integral ticks.");
                var families = new[] { "metal", "wood", "fish", "leather", "cloth" };
                for (int family = 0; family < 5; family++)
                {
                    string id = families[family] + "-" + level;
                    resources.Add(new ResourceDefinition(id, row[family + 1], level));
                    if (family < 3)
                        activities.Add(new ActivityDefinition(skills[family] + "-" + level,
                            row[family + 1], skills[family], true, level, (long)ticks, id,
                            developmentXp ? (decimal?)100000m : null));
                }
            }
            return new GatheringCatalog(resources, activities, developmentXp);
        }

        private GatheringCatalog(List<ResourceDefinition> resources, List<ActivityDefinition> activities, bool fixture)
        {
            Resources = resources.AsReadOnly();
            Activities = activities.AsReadOnly();
            UsesDevelopmentXp = fixture;
        }

        internal static IEnumerable<string> DataRows(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            return text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim()).Where(x => x.Length > 0 && !x.StartsWith("#", StringComparison.Ordinal));
        }
    }
}
