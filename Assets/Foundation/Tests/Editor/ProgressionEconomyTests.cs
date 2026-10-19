using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Idlebound.Foundation.Tests
{
    public sealed class ProgressionEconomyTests
    {
        private static string Data(string name) => Resources.Load<TextAsset>("Idlebound/" + name).text;
        private static XpCurve Curve() => XpCurve.Load(Data("ProductionXpCurve"), false);
        private static GatheringCatalog Catalog() => GatheringCatalog.LoadProduction(Data("ResourceTiers"), Data("GatheringXp"));
        private static GatheringSession Session() => new GatheringSession(Catalog(), Curve(),
            ProductionCatalog.Load(Data("StarterProductionRecipes"), Catalog().Resources));
        private static long T(decimal seconds) => (long)(seconds * TimeSpan.TicksPerSecond);
        private static SkillState Skill(GatheringSession s, string id, string skill) =>
            s.Characters.First(c => c.Id == id).Skills.First(k => k.SkillId == skill);
        private static void Gather(GatheringSession s, string activity, decimal seconds, string id = "character-1")
        {
            Assert.That(s.AssignAt(s.LastSettledTicks, id, activity, false, out var error), Is.True, error);
            s.SettleTo(s.LastSettledTicks + T(seconds));
        }
        private static void Start(GatheringSession s, string recipe, string id = "character-1", bool swap = false) =>
            Assert.That(s.StartProductionAt(s.LastSettledTicks, id, recipe, swap, out var error), Is.True, error);

        public static IEnumerable<int> AllLevels => Enumerable.Range(1, 99);
        public static IEnumerable<TestCaseData> GatheringCases
        {
            get
            {
                foreach (string skill in new[] { "mining", "woodcutting", "fishing" })
                    for (int tier = 1; tier <= 10; tier++) yield return new TestCaseData(skill, tier);
            }
        }
        public static IEnumerable<string> StarterRecipes => new[]
        { "bronze-dagger", "bronze-helm", "wood-shortbow", "wood-wand", "wood-crossbow", "cooked-shrimp", "iron-dagger", "cooked-sardine" };

        [TestCaseSource(nameof(AllLevels))]
        public void All99LevelsMatchReconstructionAndHaveExactBoundaries(int level)
        {
            int[] levels = { 1, 30, 60, 80, 90, 99 };
            decimal[] xp = { 0, 500000, 2000000, 4500000, 6500000, 10000000 };
            decimal value;
            if (level <= 30) value = 500000m * (level - 1) * (level - 1) / (29 * 29);
            else
            {
                int upper = Array.FindIndex(levels, l => l >= level);
                value = xp[upper - 1] + (xp[upper] - xp[upper - 1]) *
                    (level - levels[upper - 1]) / (levels[upper] - levels[upper - 1]);
            }
            decimal expected = Math.Round(value, 0, MidpointRounding.AwayFromZero);
            var curve = Curve();
            Assert.That(curve.Thresholds[level], Is.EqualTo(expected));
            Assert.That(curve.KnownLevelFloor(expected), Is.EqualTo(level));
            if (level > 1) Assert.That(curve.KnownLevelFloor(expected - 1), Is.EqualTo(level - 1));
            Assert.That(curve.NextLevelXp(expected), Is.EqualTo(level < 99 ? (decimal?)curve.Thresholds[level + 1] : null));
        }

        [TestCaseSource(nameof(GatheringCases))]
        public void AllThirtyGatheringActivitiesUseApprovedRewardsAndDurations(string skill, int tier)
        {
            int[] requirements = { 1, 10, 20, 30, 40, 50, 60, 70, 80, 90 };
            int[] rewards = { 56, 89, 124, 159, 197, 236, 277, 318, 362, 407 };
            decimal[] durations = { 5, 7.8m, 10.6m, 13.3m, 16.1m, 18.9m, 21.7m, 24.4m, 27.2m, 30 };
            var activity = Catalog().Activities.Single(a => a.Id == skill + "-" + requirements[tier - 1]);
            Assert.That(activity.XpPerCycle, Is.EqualTo((decimal)rewards[tier - 1]));
            Assert.That(activity.DurationTicks, Is.EqualTo(T(durations[tier - 1])));
            Assert.That(Catalog().UsesDevelopmentXp, Is.False);
        }

        [TestCaseSource(nameof(StarterRecipes))]
        public void StarterAssumptionsAreTraceableAndHaveBalancedSupplyAdjustedXp(string id)
        {
            var s = Session(); var recipe = s.Production.Recipes.Single(r => r.Id == id);
            Assert.That(recipe.IsBalanceAssumption, Is.True);
            Assert.That(recipe.IsDevelopmentOnly, Is.False);
            Assert.That(recipe.Provenance, Is.EqualTo("NEW STAGE 8B-03B BALANCE ASSUMPTIONS"));
            decimal inputSeconds = 0;
            decimal referenceRate = 0;
            foreach (var input in recipe.Inputs)
            {
                var activity = Catalog().Activities.Single(a => a.ResourceId == input.Key);
                decimal seconds = activity.DurationTicks / (decimal)TimeSpan.TicksPerSecond;
                inputSeconds += seconds * input.Value;
                referenceRate = activity.XpPerCycle.Value / seconds;
            }
            decimal duration = recipe.DurationTicks / (decimal)TimeSpan.TicksPerSecond;
            decimal proposed = Math.Min(0.9m * referenceRate * (inputSeconds + duration), 2m * referenceRate * duration);
            Assert.That(recipe.XpReward, Is.EqualTo(Math.Round(proposed / 10m, 0, MidpointRounding.AwayFromZero) * 10m));
            Assert.That(recipe.XpReward.Value * 3600 / (inputSeconds + duration), Is.LessThan(referenceRate * 3600));
        }

        [Test]
        public void EarlyLevelsAdvanceAndGateIronAtCorrectXp()
        {
            var s = Session(); Gather(s, "mining-1", 50);
            Assert.That(Skill(s, "character-1", "mining").StoredXp, Is.EqualTo(560m));
            Assert.That(Skill(s, "character-1", "mining").Level, Is.EqualTo(1));
            s.SettleTo(T(55));
            Assert.That(Skill(s, "character-1", "mining").Level, Is.EqualTo(2));
            Assert.That(s.AssignAt(T(55), "character-1", "mining-10", false, out _), Is.False);
            s.SettleTo(T(4300));
            Assert.That(Skill(s, "character-1", "mining").StoredXp, Is.EqualTo(48160m));
            Assert.That(Skill(s, "character-1", "mining").Level, Is.EqualTo(10));
            Gather(s, "mining-10", 7.8m);
            Assert.That(s.Quantity("metal-10"), Is.EqualTo(1));
            Assert.That(Skill(s, "character-1", "mining").StoredXp, Is.EqualTo(48249m));
        }

        [Test]
        public void MissingOrMismatchedGatheringXpDataRejected()
        {
            string data = Data("GatheringXp");
            Assert.Throws<ArgumentException>(() => GatheringCatalog.LoadProduction(Data("ResourceTiers"), "1,5,56"));
            Assert.Throws<ArgumentException>(() => GatheringCatalog.LoadProduction(Data("ResourceTiers"), data.Replace("2,7.8,89", "2,8,89")));
            Assert.Throws<ArgumentException>(() => GatheringCatalog.LoadProduction(Data("ResourceTiers"), data.Replace("1,5.0,56", "1,5.0,-1")));
        }

        [Test]
        public void RealRewardLoopConsumesStockAndPaysCompletionXpOnly()
        {
            var s = Session(); Gather(s, "mining-1", 10); Start(s, "bronze-dagger");
            Assert.That(s.Quantity("metal-1"), Is.Zero);
            Assert.That(Skill(s, "character-1", "mining").StoredXp, Is.EqualTo(112m));
            Assert.That(Skill(s, "character-1", "smithing").StoredXp, Is.Zero);
            s.SettleTo(T(24)); Assert.That(s.Quantity("bronze-dagger-item"), Is.Zero);
            s.SettleTo(T(25)); s.SettleTo(T(25));
            Assert.That(s.Quantity("bronze-dagger-item"), Is.EqualTo(1));
            Assert.That(Skill(s, "character-1", "smithing").StoredXp, Is.EqualTo(250m));
        }

        [Test]
        public void ProductionRefundsAreDefaultWhileLegacyTestsRemainExplicitSparseFixtures()
        {
            Assert.That(Session().RefundCancelledInputs, Is.True);
            var legacy = new GatheringSession(GatheringCatalog.Load(Data("ResourceTiers")), XpCurve.Load(Data("KnownXpMilestones"), true));
            Assert.That(legacy.RefundCancelledInputs, Is.False);
            var upgraded = new GatheringSession(legacy.Catalog, legacy.Curve, refundCancelledInputs: true);
            Assert.That(upgraded.RefundCancelledInputs, Is.True);
        }

        [Test]
        public void CancelRefundsEveryInputExactlyOnceAndNeverPaysXpOrProduct()
        {
            var s = Session(); Gather(s, "mining-1", 10); Start(s, "bronze-dagger");
            s.UnassignAt(T(24), "character-1");
            Assert.That(s.Quantity("metal-1"), Is.EqualTo(2));
            Assert.That(s.UnassignAt(T(24), "character-1"), Is.False);
            s.SettleTo(T(100));
            Assert.That(s.Quantity("metal-1"), Is.EqualTo(2));
            Assert.That(s.Quantity("bronze-dagger-item"), Is.Zero);
            Assert.That(Skill(s, "character-1", "smithing").StoredXp, Is.Zero);
        }

        [Test]
        public void CancelExactlyAtCompletionPaysProductNotRefund()
        {
            var s = Session(); Gather(s, "mining-1", 10); Start(s, "bronze-dagger");
            s.UnassignAt(T(25), "character-1"); s.UnassignAt(T(25), "character-1");
            Assert.That(s.Quantity("metal-1"), Is.Zero);
            Assert.That(s.Quantity("bronze-dagger-item"), Is.EqualTo(1));
            Assert.That(Skill(s, "character-1", "smithing").StoredXp, Is.EqualTo(250m));
        }

        [Test]
        public void ReassignmentToGatheringRefundsOldJobBeforeNewProduction()
        {
            var s = Session(); Gather(s, "mining-1", 10); Start(s, "bronze-dagger");
            Assert.That(s.AssignAt(T(14), "character-1", "fishing-1", false, out _), Is.True);
            Assert.That(s.Quantity("metal-1"), Is.EqualTo(2));
            s.SettleTo(T(30)); Assert.That(s.Quantity("bronze-dagger-item"), Is.Zero);
            Assert.That(s.Quantity("fish-1"), Is.EqualTo(3));
        }

        [Test]
        public void SwapTransfersReservationWithoutDuplicatingOrLosingInputs()
        {
            var s = Session(); Gather(s, "mining-1", 10); Start(s, "bronze-dagger");
            s.SettleTo(T(14));
            Assert.That(s.CanStartProduction("character-2", s.Production.Recipes.First(r => r.Id == "bronze-dagger"), true), Is.True);
            Start(s, "bronze-dagger", "character-2", true);
            Assert.That(s.Assignments.ContainsKey("character-1"), Is.False);
            Assert.That(s.Quantity("metal-1"), Is.Zero);
            Assert.That(s.PartialTicks("character-2"), Is.Zero);
            s.UnassignAt(T(14), "character-2");
            Assert.That(s.Quantity("metal-1"), Is.EqualTo(2));
            s.UnassignAt(T(14), "character-1"); Assert.That(s.Quantity("metal-1"), Is.EqualTo(2));
        }

        [Test]
        public void FailedReplacementDoesNotRefundOrCancelExistingPaidJob()
        {
            var s = Session(); Gather(s, "mining-1", 10); Start(s, "bronze-dagger");
            Assert.That(s.StartProductionAt(T(14), "character-1", "wood-shortbow", false, out _), Is.False);
            Assert.That(s.Quantity("metal-1"), Is.Zero);
            Assert.That(s.ProductionJobs["character-1"].Id, Is.EqualTo("bronze-dagger"));
            Assert.That(s.PartialTicks("character-1"), Is.EqualTo(T(4)));
        }

        [Test]
        public void ReplacementJobCanUseRefundedMaterialsButDoesNotRefundTwice()
        {
            var s = Session(); Gather(s, "woodcutting-1", 10); Start(s, "wood-shortbow");
            Start(s, "wood-wand");
            Assert.That(s.Quantity("wood-1"), Is.Zero);
            Assert.That(s.StartProductionAt(s.LastSettledTicks, "character-1", "wood-wand", false, out _), Is.False);
            s.UnassignAt(s.LastSettledTicks, "character-1");
            Assert.That(s.Quantity("wood-1"), Is.EqualTo(2));
        }

        [Test]
        public void ProductionCapIsTenMillionAndResourcesContinueWithoutDuplicateRewards()
        {
            var s = Session(); Gather(s, "mining-1", 1000000);
            Assert.That(Skill(s, "character-1", "mining").StoredXp, Is.EqualTo(10000000m));
            Assert.That(Skill(s, "character-1", "mining").Level, Is.EqualTo(99));
            long before = s.Quantity("metal-1"); s.SettleTo(s.LastSettledTicks + T(5));
            Assert.That(s.Quantity("metal-1"), Is.EqualTo(before + 1));
            Assert.That(Skill(s, "character-1", "mining").StoredXp, Is.EqualTo(10000000m));
            Assert.That(Curve().ApplyAward(9999999.75m, decimal.MaxValue), Is.EqualTo(10000000m));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RealBackgroundAndOfflineRewardsHaveZeroBaselineAndFiftyPercentCap(bool offline)
        {
            var s = Session(); Gather(s, "mining-1", 5, "character-2");
            Assert.That(Skill(s, "character-2", "mining").StoredXp, Is.Zero);
            s.SetEffectsAt(s.LastSettledTicks, "character-2", new[] { new BackgroundXpEffect("test-only", "mining", 0.9m) });
            s.SettleTo(s.LastSettledTicks + T(5), offline);
            Assert.That(Skill(s, "character-2", "mining").StoredXp, Is.EqualTo(28m));
            if (offline)
            {
                Gather(s, "fishing-1", 0); s.SettleTo(s.LastSettledTicks + T(5), true);
                Assert.That(Skill(s, "character-1", "fishing").StoredXp, Is.Zero);
            }
        }

        [Test]
        public void RecipeLevelGateUsesExactProductionCurveAndCookingReachesLevelTwo()
        {
            var s = Session(); Gather(s, "fishing-1", 30);
            for (int i = 0; i < 6; i++) { Start(s, "cooked-shrimp"); s.SettleTo(s.LastSettledTicks + T(5)); }
            Assert.That(Skill(s, "character-1", "cooking").StoredXp, Is.EqualTo(600m));
            Assert.That(Skill(s, "character-1", "cooking").Level, Is.EqualTo(2));
            Assert.That(s.StartProductionAt(s.LastSettledTicks, "character-1", "cooked-sardine", false, out _), Is.False);
        }

        [Test]
        public void MultiInputFixtureRefundsEveryIngredientUnderNewLivePolicy()
        {
            var catalog = Catalog();
            var s = new GatheringSession(catalog, Curve(),
                ProductionCatalog.Load(Data("DevelopmentProductionRecipes"), catalog.Resources, true));
            Gather(s, "woodcutting-1", 10); Gather(s, "mining-1", 10);
            Start(s, "dev-crossbow"); s.UnassignAt(s.LastSettledTicks + T(1), "character-1");
            Assert.That(s.Quantity("wood-1"), Is.EqualTo(2));
            Assert.That(s.Quantity("metal-1"), Is.EqualTo(2));
            Assert.That(s.Quantity("dev-crossbow-item"), Is.Zero);
        }

        [Test]
        public void RefundOverflowPreservesJobBankAndAssignmentInsteadOfPartlyRefunding()
        {
            // Deliberately extreme test-only quantities, never exported as balance data.
            string rows = "refund-stock,Test stock,smithing,MetalWeapon,refund-stock-item,Test stock," +
                (long.MaxValue - 1) + ",metal-1:1,1,5,0,new-assumption,Unit test ONLY\n" +
                "refund-job,Test consumer,crafting,Offhand,refund-output,Test output,1,refund-stock-item:" +
                (long.MaxValue - 2) + ",1,15,0,new-assumption,Unit test ONLY";
            var catalog = Catalog(); var s = new GatheringSession(catalog, Curve(), ProductionCatalog.Load(rows, catalog.Resources));
            Gather(s, "mining-1", 10); Start(s, "refund-stock"); s.SettleTo(T(15));
            Start(s, "refund-job"); Start(s, "refund-stock", "character-2"); s.SettleTo(T(20));
            Assert.That(s.Quantity("refund-stock-item"), Is.EqualTo(long.MaxValue));
            Assert.Throws<OverflowException>(() => s.UnassignAt(T(20), "character-1"));
            Assert.That(s.ProductionJobs["character-1"].Id, Is.EqualTo("refund-job"));
            Assert.That(s.Assignments["character-1"], Is.EqualTo("refund-job"));
            Assert.That(s.Quantity("refund-stock-item"), Is.EqualTo(long.MaxValue));
            Assert.That(s.Quantity("refund-output"), Is.Zero);
        }
    }
}
