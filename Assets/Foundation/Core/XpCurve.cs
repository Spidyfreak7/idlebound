using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace Idlebound.Foundation
{
    public sealed class XpCurve
    {
        private readonly SortedDictionary<int, decimal> thresholds;
        public bool IsDevelopmentFixture { get; }
        public IReadOnlyDictionary<int, decimal> Thresholds { get; }
        private XpCurve(SortedDictionary<int, decimal> values, bool fixture)
        {
            thresholds = values;
            IsDevelopmentFixture = fixture;
            Thresholds = new ReadOnlyDictionary<int, decimal>(thresholds);
        }

        public static XpCurve Load(string csv, bool developmentFixture)
        {
            var values = new SortedDictionary<int, decimal>();
            int previousLevel = 0;
            decimal previousXp = -1;
            foreach (var line in GatheringCatalog.DataRows(csv))
            {
                var row = line.Split(',');
                if (row.Length != 2) throw new ArgumentException("XP rows require level,XP.");
                int level = int.Parse(row[0], CultureInfo.InvariantCulture);
                decimal xp = decimal.Parse(row[1], CultureInfo.InvariantCulture);
                if (level <= previousLevel || level > 99 || xp < 0 || xp <= previousXp ||
                    xp != decimal.Truncate(xp)) throw new ArgumentException("XP thresholds must strictly increase.");
                values.Add(level, xp);
                previousLevel = level;
                previousXp = xp;
            }
            var milestones = new Dictionary<int, decimal>
            {
                { 1, 0 }, { 30, 500000 }, { 60, 2000000 }, { 80, 4500000 }, { 90, 6500000 }, { 99, 10000000 }
            };
            foreach (var pair in milestones)
                if (!values.TryGetValue(pair.Key, out var xp) || xp != pair.Value)
                    throw new ArgumentException("Missing or incorrect XP milestone: " + pair.Key);
            if (!developmentFixture && (values.Count != 99 || Enumerable.Range(1, 99).Any(i => !values.ContainsKey(i))))
                throw new ArgumentException("Production curve requires all 99 levels; interpolation is forbidden.");
            return new XpCurve(values, developmentFixture);
        }

        // Sparse fixture yields a known lower bound only; it never invents intermediate levels.
        public int KnownLevelFloor(decimal xp)
        {
            if (xp < 0) throw new ArgumentOutOfRangeException(nameof(xp));
            return thresholds.Last(p => p.Value <= xp).Key;
        }
    }

    public sealed class BackgroundXpEffect
    {
        public string SourceId { get; }
        public string SkillId { get; }
        public decimal Efficiency { get; }
        public BackgroundXpEffect(string sourceId, string skillId, decimal efficiency)
        {
            if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(skillId) || efficiency < 0)
                throw new ArgumentException("Effect requires a source, skill, and nonnegative efficiency.");
            SourceId = sourceId;
            SkillId = skillId;
            Efficiency = efficiency;
        }
    }

    public static class XpPolicy
    {
        public const decimal BackgroundCap = 0.5m;
        public static decimal Efficiency(bool selected, bool offline, string skillId,
            IEnumerable<BackgroundXpEffect> effects)
        {
            if (selected && !offline) return 1m;
            // Additive pathway is an engineering adapter, not an equipment stacking implementation.
            decimal total = 0;
            var sources = new HashSet<string>();
            foreach (var effect in effects ?? throw new ArgumentNullException(nameof(effects)))
            {
                if (effect == null || !sources.Add(effect.SourceId)) throw new ArgumentException("Duplicate/null XP effect.");
                if (effect.SkillId == skillId || effect.SkillId == "*")
                    total = Math.Min(BackgroundCap, total + Math.Min(effect.Efficiency, BackgroundCap));
            }
            return total;
        }
    }
}
