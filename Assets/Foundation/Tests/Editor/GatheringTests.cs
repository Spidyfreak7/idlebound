using System;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Idlebound.Foundation.Tests
{
    public sealed class GatheringTests
    {
        private static string Tiers => Resources.Load<TextAsset>("Idlebound/ResourceTiers").text;
        private static string Milestones => Resources.Load<TextAsset>("Idlebound/KnownXpMilestones").text;
        private static long Seconds(decimal seconds) => (long)(seconds * TimeSpan.TicksPerSecond);
        private static GatheringSession Session(bool fixture = false) =>
            new GatheringSession(GatheringCatalog.Load(Tiers, fixture), XpCurve.Load(Milestones, true));
        private static SkillState Skill(GatheringSession s, string character, string skill) =>
            s.Characters.First(c => c.Id == character).Skills.First(k => k.SkillId == skill);
        private static void Assign(GatheringSession s, string c, string a, bool swap = false) =>
            Assert.That(s.AssignAt(s.LastSettledTicks, c, a, swap, out var error), Is.True, error);

        [Test]
        public void ApprovedSkillNamesReplaceNumberedPlaceholders()
        {
            Assert.That(SkillCatalog.Create().Select(s => s.DisplayName), Is.EqualTo(new[]
            { "Mining", "Smithing", "Crafting", "Woodcutting", "Fishing", "Cooking", "Melee", "Ranged", "Magic" }));
            Assert.That(SkillCatalog.Create().Select(s => s.Id).Distinct().Count(), Is.EqualTo(9));
        }

        [Test]
        public void SourceDataHasTenTiersThirtyGatheringTargetsAndFiftyMaterials()
        {
            var c = GatheringCatalog.Load(Tiers);
            Assert.That(c.Activities.Count, Is.EqualTo(30));
            Assert.That(c.Resources.Count, Is.EqualTo(50));
            var durations = new decimal[] { 5, 7.8m, 10.6m, 13.3m, 16.1m, 18.9m, 21.7m, 24.4m, 27.2m, 30 };
            var levels = new[] { 1, 10, 20, 30, 40, 50, 60, 70, 80, 90 };
            foreach (var skill in new[] { "mining", "woodcutting", "fishing" })
            {
                var activities = c.Activities.Where(a => a.SkillId == skill).ToArray();
                Assert.That(activities.Select(a => a.RequiredLevel), Is.EqualTo(levels));
                Assert.That(activities.Select(a => a.DurationTicks), Is.EqualTo(durations.Select(Seconds)));
                Assert.That(activities.All(a => a.XpPerCycle == null), Is.True);
            }
            Assert.That(c.Resources.First(r => r.Id == "metal-90").DisplayName, Is.EqualTo("Astralium"));
            Assert.That(c.Resources.First(r => r.Id == "wood-1").DisplayName, Is.EqualTo("Common Wood"));
        }

        [Test]
        public void CatalogRejectsMissingDuplicateTierAndInvalidDuration()
        {
            Assert.Throws<ArgumentException>(() => GatheringCatalog.Load("1,Bronze,Common Wood,Shrimp,Worn Leather,Linen,5"));
            Assert.Throws<ArgumentException>(() => GatheringCatalog.Load(Tiers.Replace("10,Iron", "1,Iron")));
            Assert.Throws<ArgumentException>(() => GatheringCatalog.Load(Tiers.Replace("Linen,5", "Linen,0")));
            Assert.Throws<ArgumentException>(() => GatheringCatalog.Load(Tiers.Replace("Linen,5", "Linen,-5")));
        }

        [Test]
        public void CatalogParsingIsCultureIndependent()
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                Assert.That(GatheringCatalog.Load(Tiers).Activities.First(a => a.Id == "mining-10").DurationTicks,
                    Is.EqualTo(Seconds(7.8m)));
                Assert.That(XpCurve.Load(Milestones, true).Thresholds[99], Is.EqualTo(10000000m));
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }

        [TestCase(30, 500000)]
        [TestCase(60, 2000000)]
        [TestCase(80, 4500000)]
        [TestCase(90, 6500000)]
        [TestCase(99, 10000000)]
        public void MilestoneValuesAreRequired(int level, int xp)
        {
            var curve = XpCurve.Load(Milestones, true);
            Assert.That(curve.Thresholds[level], Is.EqualTo((decimal)xp));
            string line = level + "," + xp;
            Assert.Throws<ArgumentException>(() => XpCurve.Load(Milestones.Replace(line, level + "," + (xp + 1)), true));
        }

        [Test]
        public void SparseCurveCannotMasqueradeAsProductionOrInterpolate()
        {
            Assert.Throws<ArgumentException>(() => XpCurve.Load(Milestones, false));
            var c = XpCurve.Load(Milestones, true);
            Assert.That(c.KnownLevelFloor(499999), Is.EqualTo(1));
            Assert.That(c.KnownLevelFloor(500000), Is.EqualTo(30));
            Assert.That(c.KnownLevelFloor(1999999), Is.EqualTo(30));
            Assert.That(c.KnownLevelFloor(decimal.MaxValue), Is.EqualTo(99));
            Assert.That(c.Thresholds.ContainsKey(2), Is.False);
        }

        [Test]
        public void CurveRejectsDuplicateMissingNonmonotonicAndFractionalRows()
        {
            Assert.Throws<ArgumentException>(() => XpCurve.Load(Milestones + "99,10000000\n", true));
            Assert.Throws<ArgumentException>(() => XpCurve.Load(Milestones.Replace("30,500000", ""), true));
            Assert.Throws<ArgumentException>(() => XpCurve.Load(Milestones.Replace("60,2000000", "60,499999"), true));
            Assert.Throws<ArgumentException>(() => XpCurve.Load(Milestones.Replace("30,500000", "30,500000.1"), true));
        }

        [Test]
        public void CompleteExplicitCurveCanLoadWithoutGeneratingProductionData()
        {
            // Validation-only invented rows; never exported as production balancing.
            var anchors = XpCurve.Load(Milestones, true).Thresholds;
            decimal previous = -1;
            var lines = Enumerable.Range(1, 99).Select(level =>
            {
                decimal xp = anchors.TryGetValue(level, out var value) ? value : previous + 1;
                previous = xp;
                return level + "," + xp.ToString(CultureInfo.InvariantCulture);
            }).ToArray();
            var curve = XpCurve.Load(string.Join("\n", lines), false);
            Assert.That(curve.IsDevelopmentFixture, Is.False);
            Assert.That(curve.Thresholds.Count, Is.EqualTo(99));
            Assert.That(curve.KnownLevelFloor(1), Is.EqualTo(2));
        }

        [Test]
        public void CompletedCyclesOnlyAndSameTimestampCannotDuplicate()
        {
            var s = Session(); Assign(s, "character-1", "mining-1");
            s.SettleTo(Seconds(4.9m));
            Assert.That(s.Inventory, Is.Empty);
            s.SettleTo(Seconds(5));
            Assert.That(s.Quantity("metal-1"), Is.EqualTo(1));
            s.SettleTo(Seconds(5));
            Assert.That(s.Quantity("metal-1"), Is.EqualTo(1));
            s.SettleTo(Seconds(12));
            Assert.That(s.Quantity("metal-1"), Is.EqualTo(2));
            Assert.That(s.PartialTicks("character-1"), Is.EqualTo(Seconds(2)));
            Assert.That(Skill(s, "character-1", "mining").StoredXp, Is.Zero);
        }

        [Test]
        public void PartitionedAndBatchedSettlementAgreeWithRemainder()
        {
            var a = Session(true); var b = Session(true);
            Assign(a, "character-1", "mining-1"); Assign(b, "character-1", "mining-1");
            for (int i = 1; i <= 123; i++) a.SettleTo(Seconds(i / 10m));
            b.SettleTo(Seconds(12.3m));
            Assert.That(a.Quantity("metal-1"), Is.EqualTo(b.Quantity("metal-1")));
            Assert.That(a.PartialTicks("character-1"), Is.EqualTo(b.PartialTicks("character-1")));
            Assert.That(Skill(a, "character-1", "mining").StoredXp, Is.EqualTo(Skill(b, "character-1", "mining").StoredXp));
        }

        [Test]
        public void RewindRejectedWithoutChangingInventoryOrCursor()
        {
            var s = Session(); Assign(s, "character-1", "mining-1"); s.SettleTo(Seconds(5));
            Assert.Throws<ArgumentOutOfRangeException>(() => s.SettleTo(Seconds(4)));
            Assert.That(s.Quantity("metal-1"), Is.EqualTo(1));
            Assert.That(s.LastSettledTicks, Is.EqualTo(Seconds(5)));
        }

        [Test]
        public void ParallelActivitiesProduceSharedResourcesDespiteBackgroundZeroXp()
        {
            var s = Session(true);
            Assign(s, "character-1", "mining-1"); Assign(s, "character-2", "fishing-1");
            Assign(s, "character-3", "woodcutting-1"); s.SettleTo(Seconds(10));
            Assert.That(s.Quantity("metal-1"), Is.EqualTo(2));
            Assert.That(s.Quantity("fish-1"), Is.EqualTo(2));
            Assert.That(s.Quantity("wood-1"), Is.EqualTo(2));
            Assert.That(Skill(s, "character-1", "mining").StoredXp, Is.EqualTo(200000m));
            Assert.That(Skill(s, "character-2", "fishing").StoredXp, Is.Zero);
            Assert.That(Skill(s, "character-3", "woodcutting").StoredXp, Is.Zero);
        }

        [Test]
        public void SelectionSettlesOldActiveFirstAndDoesNotResetPartialCycles()
        {
            var s = Session(true); Assign(s, "character-1", "mining-1"); Assign(s, "character-2", "fishing-1");
            Assert.That(s.SelectAt(Seconds(6), "character-2"), Is.True);
            Assert.That(s.PartialTicks("character-1"), Is.EqualTo(Seconds(1)));
            s.SettleTo(Seconds(10));
            Assert.That(Skill(s, "character-1", "mining").StoredXp, Is.EqualTo(100000m));
            Assert.That(Skill(s, "character-2", "fishing").StoredXp, Is.EqualTo(100000m));
            Assert.That(s.Assignments.Count, Is.EqualTo(2));
            Assert.That(s.SelectAt(Seconds(10), "unknown"), Is.False);
            Assert.That(s.ActiveCharacterId, Is.EqualTo("character-2"));
        }

        [Test]
        public void CompletionTimePolicyIsExplicitForMidcycleSelectionChange()
        {
            var s = Session(true); Assign(s, "character-1", "mining-1"); Assign(s, "character-2", "fishing-1");
            s.SelectAt(Seconds(4), "character-2"); s.SettleTo(Seconds(5));
            Assert.That(Skill(s, "character-1", "mining").StoredXp, Is.Zero);
            Assert.That(Skill(s, "character-2", "fishing").StoredXp, Is.EqualTo(100000m));
        }

        [Test]
        public void UnknownUnavailableAndLockedAssignmentsRejected()
        {
            var s = Session(); Assign(s, "character-1", "mining-1"); s.SettleTo(Seconds(2));
            Assert.That(s.AssignAt(Seconds(2), "character-1", "mining-10", true, out _), Is.False);
            Assert.That(s.AssignAt(Seconds(2), "character-1", "unknown", true, out _), Is.False);
            Assert.That(s.AssignAt(Seconds(2), "unknown", "mining-1", true, out _), Is.False);
            Assert.That(s.Assignments["character-1"], Is.EqualTo("mining-1"));
            Assert.That(s.PartialTicks("character-1"), Is.EqualTo(Seconds(2)));
        }

        [Test]
        public void OccupancyRejectsOrdinaryAssignmentButExplicitSwapDisplacesAtomically()
        {
            var s = Session(); Assign(s, "character-1", "mining-1"); Assign(s, "character-2", "fishing-1");
            s.SettleTo(Seconds(3));
            Assert.That(s.AssignAt(Seconds(3), "character-2", "mining-1", false, out _), Is.False);
            Assert.That(s.PartialTicks("character-1"), Is.EqualTo(Seconds(3)));
            Assign(s, "character-2", "mining-1", true);
            Assert.That(s.Assignments.ContainsKey("character-1"), Is.False);
            Assert.That(s.Assignments["character-2"], Is.EqualTo("mining-1"));
            Assert.That(s.PartialTicks("character-1"), Is.Zero);
            Assert.That(s.PartialTicks("character-2"), Is.Zero);
            s.SettleTo(Seconds(8));
            Assert.That(s.Quantity("metal-1"), Is.EqualTo(1));
            Assert.That(s.Quantity("fish-1"), Is.Zero);
        }

        [Test]
        public void ReassignUnassignAndRestartDiscardPartialButSameAssignmentPreservesIt()
        {
            var s = Session(); Assign(s, "character-1", "mining-1"); s.SettleTo(Seconds(4));
            Assign(s, "character-1", "mining-1");
            Assert.That(s.PartialTicks("character-1"), Is.EqualTo(Seconds(4)));
            Assign(s, "character-1", "fishing-1");
            Assert.That(s.PartialTicks("character-1"), Is.Zero);
            s.UnassignAt(Seconds(8), "character-1");
            Assign(s, "character-1", "fishing-1"); s.SettleTo(Seconds(9));
            Assert.That(s.Quantity("fish-1"), Is.Zero);
            Assert.That(s.Quantity("metal-1"), Is.Zero);
        }

        [Test]
        public void FractionalBackgroundXpIsNotRoundedUpOrLost()
        {
            var s = Session(true); Assign(s, "character-2", "fishing-1");
            s.SetEffectsAt(0, "character-2", new[] { new BackgroundXpEffect("test-only", "fishing", 0.000001m) });
            s.SettleTo(Seconds(15));
            Assert.That(Skill(s, "character-2", "fishing").StoredXp, Is.EqualTo(0.3m));
            Assert.That(s.Quantity("fish-1"), Is.EqualTo(3));
        }

        [TestCase(false, false, 0)]
        [TestCase(true, false, 1)]
        [TestCase(false, true, 0)]
        [TestCase(true, true, 0)]
        public void ZeroBaselineAndSelectedOnlineInvariant(bool selected, bool offline, int expected) =>
            Assert.That(XpPolicy.Efficiency(selected, offline, "mining", Array.Empty<BackgroundXpEffect>()),
                Is.EqualTo((decimal)expected));

        [Test]
        public void EffectsAreScopedCappedAndNeverMakeActiveXpExceedFull()
        {
            var effects = Enumerable.Range(1, 10).Select(i => new BackgroundXpEffect("slot-" + i, "mining", 0.1m)).ToArray();
            Assert.That(XpPolicy.Efficiency(false, false, "mining", effects), Is.EqualTo(0.5m));
            Assert.That(XpPolicy.Efficiency(false, true, "mining", effects), Is.EqualTo(0.5m));
            Assert.That(XpPolicy.Efficiency(false, false, "fishing", effects), Is.Zero);
            Assert.That(XpPolicy.Efficiency(true, false, "mining", effects), Is.EqualTo(1m));
            Assert.That(XpPolicy.Efficiency(false, false, "mining", effects.Take(4)), Is.EqualTo(0.4m));
        }

        [Test]
        public void EffectChangeSettlesOldRateAndRejectsDuplicateOrInvalidSources()
        {
            var s = Session(true); Assign(s, "character-2", "fishing-1");
            var effect = new BackgroundXpEffect("test-slot", "fishing", 0.1m);
            s.SetEffectsAt(Seconds(5), "character-2", new[] { effect });
            Assert.That(Skill(s, "character-2", "fishing").StoredXp, Is.Zero);
            s.SettleTo(Seconds(10));
            Assert.That(Skill(s, "character-2", "fishing").StoredXp, Is.EqualTo(10000m));
            Assert.Throws<ArgumentException>(() => s.SetEffectsAt(Seconds(15), "character-2", new[] { effect, effect }));
            Assert.That(s.LastSettledTicks, Is.EqualTo(Seconds(10)));
            Assert.Throws<ArgumentException>(() => new BackgroundXpEffect("x", "mining", -1));
            Assert.Throws<ArgumentException>(() => s.SetEffectsAt(Seconds(10), "unknown", new[] { effect }));
            Assert.Throws<ArgumentException>(() => s.SetEffectsAt(Seconds(10), "character-2",
                new[] { new BackgroundXpEffect("x", "unknown", 0.1m) }));
        }

        [Test]
        public void OfflineNeverGrantsSelectedFullXpAndCapsAllCharactersAtBaseFourHours()
        {
            var s = Session(true); Assign(s, "character-1", "mining-1"); Assign(s, "character-2", "fishing-1");
            s.SetEffectsAt(0, "character-2", new[] { new BackgroundXpEffect("test-only", "*", 0.9m) });
            s.SettleTo(Seconds(5 * 3600), true);
            Assert.That(s.Quantity("metal-1"), Is.EqualTo(2880));
            Assert.That(s.Quantity("fish-1"), Is.EqualTo(2880));
            Assert.That(Skill(s, "character-1", "mining").StoredXp, Is.Zero);
            Assert.That(Skill(s, "character-2", "fishing").StoredXp, Is.EqualTo(144000000m));
            s.SettleTo(Seconds(5 * 3600), true);
            Assert.That(s.Quantity("fish-1"), Is.EqualTo(2880));
        }

        [Test]
        public void FixtureProgressionUnlocksOnlyKnownFloorAndKeepsGatheringAt99()
        {
            var s = Session(true); Assign(s, "character-1", "mining-1");
            s.SettleTo(Seconds(25));
            Assert.That(Skill(s, "character-1", "mining").Level, Is.EqualTo(30));
            Assign(s, "character-1", "mining-10"); s.SettleTo(Seconds(32.8m));
            Assert.That(s.Quantity("metal-10"), Is.EqualTo(1));
            s.SettleTo(Seconds(10000));
            Assert.That(Skill(s, "character-1", "mining").Level, Is.EqualTo(99));
            long before = s.Quantity("metal-10"); s.SettleTo(Seconds(10007.8m));
            Assert.That(s.Quantity("metal-10"), Is.EqualTo(before + 1));
        }

        [Test]
        public void MultipleCharactersCanContributeSameResourceSequentiallyWithoutLoss()
        {
            var s = Session(); Assign(s, "character-1", "mining-1"); s.SettleTo(Seconds(5));
            Assign(s, "character-2", "mining-1", true); s.SettleTo(Seconds(10));
            Assert.That(s.Quantity("metal-1"), Is.EqualTo(2));
            Assert.That(s.Inventory.Count, Is.EqualTo(1));
            Assert.That(s.CompletedCycles("character-1"), Is.EqualTo(1));
            Assert.That(s.CompletedCycles("character-2"), Is.EqualTo(1));
        }

        [Test]
        public void AssignmentAtBoundaryPaysOldActivityOnceBeforeChanging()
        {
            var s = Session(true); Assign(s, "character-1", "mining-1");
            Assert.That(s.AssignAt(Seconds(5), "character-1", "fishing-1", false, out _), Is.True);
            Assert.That(s.Quantity("metal-1"), Is.EqualTo(1));
            Assert.That(Skill(s, "character-1", "mining").StoredXp, Is.EqualTo(100000m));
            s.AssignAt(Seconds(5), "character-1", "fishing-1", false, out _);
            s.SettleTo(Seconds(10));
            Assert.That(s.Quantity("metal-1"), Is.EqualTo(1));
            Assert.That(s.Quantity("fish-1"), Is.EqualTo(1));
        }

        [Test]
        public void InventoryCannotBeMutatedThroughItsPublicView()
        {
            var s = Session(); Assign(s, "character-1", "mining-1"); s.SettleTo(Seconds(5));
            var view = (System.Collections.Generic.IDictionary<string, long>)s.Inventory;
            Assert.Throws<NotSupportedException>(() => view["metal-1"] = 999);
            Assert.That(s.Quantity("metal-1"), Is.EqualTo(1));
        }

        [Test]
        public void OfflinePartialCycleCarriesTicksButNotSelectedXpPrivilege()
        {
            var s = Session(true); Assign(s, "character-1", "mining-1");
            s.SettleTo(Seconds(4)); s.SettleTo(Seconds(5), true);
            Assert.That(s.Quantity("metal-1"), Is.EqualTo(1));
            Assert.That(Skill(s, "character-1", "mining").StoredXp, Is.Zero);
            Assert.That(s.PartialTicks("character-1"), Is.Zero);
        }

        [Test]
        public void AvailableEffectsCanBeRemovedWithoutPersistingFreeBonus()
        {
            var s = Session(true); Assign(s, "character-2", "fishing-1");
            s.SetEffectsAt(0, "character-2", new[] { new BackgroundXpEffect("test-only", "fishing", 0.1m) });
            s.SetEffectsAt(Seconds(5), "character-2", Array.Empty<BackgroundXpEffect>());
            s.SettleTo(Seconds(10));
            Assert.That(Skill(s, "character-2", "fishing").StoredXp, Is.EqualTo(10000m));
            Assert.That(s.BackgroundEfficiency("character-2", "fishing"), Is.Zero);
        }
    }
}
