using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Idlebound.Foundation.Tests
{
    public sealed class ProductionTests
    {
        private static string Text(string name) => Resources.Load<TextAsset>("Idlebound/" + name).text;
        private static GatheringCatalog Gathering() => GatheringCatalog.Load(Text("ResourceTiers"));
        private static long T(int seconds) => seconds * TimeSpan.TicksPerSecond;
        private static string Row(string id = "dev-test", string skill = "smithing", string category = "MetalWeapon",
            string output = "dev-output", string quantity = "1", string inputs = "metal-1:2", string level = "1",
            string seconds = "15", string xp = "100000", string status = "development") =>
            string.Join(",", id, "[DEV] Test recipe", skill, category, output, "[DEV] Test item", quantity,
                inputs, level, seconds, xp, status, "Unit test fixture ONLY");
        private static ProductionCatalog Load(string csv) => ProductionCatalog.Load(csv, Gathering().Resources, true);
        private static GatheringSession Session(string csv = null) => new GatheringSession(Gathering(),
            XpCurve.Load(Text("KnownXpMilestones"), true), Load(csv ?? Text("DevelopmentProductionRecipes")));
        private static SkillState Skill(GatheringSession s, string id, string skill) =>
            s.Characters.First(c => c.Id == id).Skills.First(k => k.SkillId == skill);
        private static void Gather(GatheringSession s, string activity, int seconds, string id = "character-1")
        {
            Assert.That(s.AssignAt(s.LastSettledTicks, id, activity, false, out var error), Is.True, error);
            s.SettleTo(s.LastSettledTicks + T(seconds));
        }
        private static void Start(GatheringSession s, string recipe, string id = "character-1", bool swap = false) =>
            Assert.That(s.StartProductionAt(s.LastSettledTicks, id, recipe, swap, out var error), Is.True, error);

        [Test]
        public void ApprovedCatalogIsHonestlyEmptyAndFixturesRequireOptIn()
        {
            var approved = ProductionCatalog.Load(Text("ProductionRecipes"), Gathering().Resources);
            Assert.That(approved.Recipes, Is.Empty);
            Assert.That(approved.HasDevelopmentRecipes, Is.False);
            Assert.Throws<ArgumentException>(() => ProductionCatalog.Load(Text("DevelopmentProductionRecipes"), Gathering().Resources));
            var fixtures = Load(Text("DevelopmentProductionRecipes"));
            Assert.That(fixtures.Recipes.Count, Is.EqualTo(5));
            Assert.That(fixtures.Recipes.All(r => r.IsDevelopmentOnly && r.DisplayName.StartsWith("[DEV]")), Is.True);
        }

        [TestCase("smithing", "MetalWeapon")]
        [TestCase("smithing", "MetalArmor")]
        [TestCase("smithing", "Shield")]
        [TestCase("crafting", "RangedWeapon")]
        [TestCase("crafting", "MagicWeapon")]
        [TestCase("crafting", "LeatherArmor")]
        [TestCase("crafting", "ClothArmor")]
        [TestCase("crafting", "Offhand")]
        [TestCase("cooking", "Food")]
        public void SupportedCategorySkillPairsLoad(string skill, string category) =>
            Assert.That(Load(Row(skill: skill, category: category)).Recipes.Single().SkillId, Is.EqualTo(skill));

        [TestCase("mining", "MetalWeapon")]
        [TestCase("smithing", "Food")]
        [TestCase("crafting", "MetalArmor")]
        [TestCase("cooking", "MagicWeapon")]
        public void WrongCategorySkillPairsRejected(string skill, string category) =>
            Assert.Throws<ArgumentException>(() => Load(Row(skill: skill, category: category)));

        [TestCase("quantity", "0")]
        [TestCase("quantity", "-1")]
        [TestCase("inputs", "metal-1:0")]
        [TestCase("inputs", "metal-1:-1")]
        [TestCase("level", "0")]
        [TestCase("level", "100")]
        [TestCase("seconds", "0")]
        [TestCase("seconds", "-1")]
        [TestCase("seconds", "0.00000001")]
        [TestCase("xp", "-1")]
        public void InvalidNumericDefinitionsRejected(string field, string value)
        {
            string row = Row(quantity: field == "quantity" ? value : "1", inputs: field == "inputs" ? value : "metal-1:2",
                level: field == "level" ? value : "1", seconds: field == "seconds" ? value : "15", xp: field == "xp" ? value : "100000");
            Assert.Catch<ArgumentException>(() => Load(row));
        }

        [Test]
        public void UnknownDuplicateAndAmbiguousDefinitionsRejected()
        {
            Assert.Throws<ArgumentException>(() => Load(Row() + "\n" + Row()));
            Assert.Throws<ArgumentException>(() => Load(Row() + "\n" + Row(id: "dev-second")));
            Assert.Throws<ArgumentException>(() => Load(Row(inputs: "unknown:1")));
            Assert.Throws<ArgumentException>(() => Load(Row(inputs: "metal-1:1;metal-1:1")));
            Assert.Throws<ArgumentException>(() => Load(Row(inputs: "dev-output:1")));
            Assert.Throws<ArgumentException>(() => Load(Row(output: "metal-1")));
            Assert.Throws<ArgumentException>(() => Load(Row(category: "Unknown")));
            Assert.Throws<ArgumentException>(() => Load(Row(status: "proposed")));
            Assert.Throws<ArgumentException>(() => Load("not,enough,columns"));
            Assert.Throws<ArgumentException>(() => Load(Row(id: "Not a stable id")));
            Assert.Throws<ArgumentException>(() => Load(Row(status: "approved")));
        }

        [Test]
        public void NullDefinitionsAndCatalogMismatchRejected()
        {
            Assert.Throws<ArgumentException>(() => new ProductionCatalog(new ProductionRecipe[] { null }, Gathering().Resources));
            var fake = new ProductionCatalog(Array.Empty<ProductionRecipe>(), Array.Empty<ResourceDefinition>());
            Assert.Throws<ArgumentException>(() => new GatheringSession(Gathering(), XpCurve.Load(Text("KnownXpMilestones"), true), fake));
        }

        [Test]
        public void CsvParsingDoesNotDependOnMachineCulture()
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                var r = Load(Row(seconds: "1.5", xp: "0.25")).Recipes.Single();
                Assert.That(r.DurationTicks, Is.EqualTo(T(1) + T(1) / 2));
                Assert.That(r.XpReward, Is.EqualTo(0.25m));
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }

        [Test]
        public void GatheringToSmithingConsumesAtStartAndPaysOnlyOnceAtCompletion()
        {
            var s = Session(); Gather(s, "mining-1", 10);
            Start(s, "dev-bronze-blade");
            Assert.That(s.Quantity("metal-1"), Is.Zero);
            Assert.That(s.Quantity("dev-blade"), Is.Zero);
            Assert.That(Skill(s, "character-1", "smithing").StoredXp, Is.Zero);
            s.SettleTo(T(24));
            Assert.That(s.PartialTicks("character-1"), Is.EqualTo(T(14)));
            s.SettleTo(T(25)); s.SettleTo(T(25)); s.SettleTo(T(100));
            Assert.That(s.Quantity("dev-blade"), Is.EqualTo(1));
            Assert.That(Skill(s, "character-1", "smithing").StoredXp, Is.EqualTo(100000m));
            Assert.That(s.ProductionJobs, Is.Empty);
            Assert.That(s.Assignments["character-1"], Is.EqualTo("dev-bronze-blade"));
        }

        [TestCase("dev-wood-bow", "dev-bow", "crafting", "woodcutting-1", 10, 15)]
        [TestCase("dev-wood-wand", "dev-wand", "crafting", "woodcutting-1", 10, 15)]
        [TestCase("dev-shrimp-meal", "dev-meal", "cooking", "fishing-1", 5, 5)]
        public void GatheringToCraftingAndCookingProduceBankedProducts(string recipe, string output,
            string skill, string activity, int gatherSeconds, int duration)
        {
            var s = Session(); Gather(s, activity, gatherSeconds); Start(s, recipe);
            s.SettleTo(s.LastSettledTicks + T(duration));
            Assert.That(s.Quantity(output), Is.EqualTo(1));
            Assert.That(Skill(s, "character-1", skill).StoredXp, Is.EqualTo(100000m));
            Assert.That(s.Production.ItemNames[output], Does.StartWith("[DEV]"));
        }

        [Test]
        public void InsufficientMultiInputRecipeDoesNotPartlyConsumeOrReplaceAssignment()
        {
            var s = Session(); Gather(s, "woodcutting-1", 5);
            Assert.That(s.StartProductionAt(T(5), "character-1", "dev-crossbow", false, out _), Is.False);
            Assert.That(s.Quantity("wood-1"), Is.EqualTo(1));
            Assert.That(s.Assignments["character-1"], Is.EqualTo("woodcutting-1"));
            Assert.That(s.ProductionJobs, Is.Empty);
            Gather(s, "mining-1", 5); Start(s, "dev-crossbow");
            Assert.That(s.Quantity("wood-1"), Is.Zero);
            Assert.That(s.Quantity("metal-1"), Is.Zero);
            s.SettleTo(T(25));
            Assert.That(s.Quantity("dev-crossbow-item"), Is.EqualTo(1));
        }

        [Test]
        public void DuplicateStartCannotChargeTwiceAndGenericAssignmentCannotBypassPayment()
        {
            var s = Session(); Gather(s, "mining-1", 20); Start(s, "dev-bronze-blade");
            Assert.That(s.StartProductionAt(T(20), "character-1", "dev-bronze-blade", false, out _), Is.False);
            Assert.That(s.AssignAt(T(20), "character-2", "dev-wood-bow", true, out _), Is.False);
            Assert.That(s.Quantity("metal-1"), Is.EqualTo(2));
            s.SettleTo(T(35)); Start(s, "dev-bronze-blade");
            Assert.That(s.Quantity("metal-1"), Is.Zero);
            s.SettleTo(T(50));
            Assert.That(s.Quantity("dev-blade"), Is.EqualTo(2));
        }

        [Test]
        public void CancellationLosesPaidInputsButNeverGrantsOutputOrXp()
        {
            var s = Session(); Gather(s, "mining-1", 10); Start(s, "dev-bronze-blade");
            s.UnassignAt(T(24), "character-1"); s.SettleTo(T(100));
            Assert.That(s.Quantity("metal-1"), Is.Zero);
            Assert.That(s.Quantity("dev-blade"), Is.Zero);
            Assert.That(Skill(s, "character-1", "smithing").StoredXp, Is.Zero);
            Assert.That(s.ProductionJobs, Is.Empty);
            Assert.That(s.PartialTicks("character-1"), Is.Zero);
        }

        [Test]
        public void ReassignmentToGatheringCancelsJobWithoutGhostCompletion()
        {
            var s = Session(); Gather(s, "mining-1", 10); Start(s, "dev-bronze-blade");
            Assert.That(s.AssignAt(T(14), "character-1", "fishing-1", false, out _), Is.True);
            s.SettleTo(T(29));
            Assert.That(s.Quantity("fish-1"), Is.EqualTo(3));
            Assert.That(s.Quantity("dev-blade"), Is.Zero);
            Assert.That(s.ProductionJobs, Is.Empty);
        }

        [Test]
        public void FailedStartPreservesOldPaidJobAndPartialProgress()
        {
            var s = Session(); Gather(s, "mining-1", 10); Start(s, "dev-bronze-blade");
            Assert.That(s.StartProductionAt(T(14), "character-1", "dev-wood-bow", false, out _), Is.False);
            Assert.That(s.ProductionJobs["character-1"].Id, Is.EqualTo("dev-bronze-blade"));
            Assert.That(s.PartialTicks("character-1"), Is.EqualTo(T(4)));
            s.SettleTo(T(25)); Assert.That(s.Quantity("dev-blade"), Is.EqualTo(1));
        }

        [Test]
        public void ExplicitSwapDiscardsOldPaidJobAndChargesNewAssigneeExactlyOnce()
        {
            var s = Session(); Gather(s, "mining-1", 20); Start(s, "dev-bronze-blade");
            Assert.That(s.StartProductionAt(T(24), "character-2", "dev-bronze-blade", false, out _), Is.False);
            Assert.That(s.Quantity("metal-1"), Is.EqualTo(2));
            Start(s, "dev-bronze-blade", "character-2", true);
            Assert.That(s.Assignments.ContainsKey("character-1"), Is.False);
            Assert.That(s.PartialTicks("character-2"), Is.Zero);
            Assert.That(s.Quantity("metal-1"), Is.Zero);
            s.SettleTo(T(35)); Assert.That(s.Quantity("dev-blade"), Is.Zero);
            s.SettleTo(T(39)); Assert.That(s.Quantity("dev-blade"), Is.EqualTo(1));
            Assert.That(Skill(s, "character-2", "smithing").StoredXp, Is.Zero);
        }

        [Test]
        public void InsufficientStockSwapCannotDisplaceExistingJob()
        {
            var s = Session(); Gather(s, "mining-1", 10); Start(s, "dev-bronze-blade");
            Assert.That(s.StartProductionAt(T(14), "character-2", "dev-bronze-blade", true, out _), Is.False);
            Assert.That(s.ProductionJobs.ContainsKey("character-1"), Is.True);
            Assert.That(s.Assignments.ContainsKey("character-2"), Is.False);
            s.SettleTo(T(25)); Assert.That(s.Quantity("dev-blade"), Is.EqualTo(1));
        }

        [Test]
        public void UnknownCharactersRecipesAndLevelGatesDoNotConsumeStock()
        {
            var s = Session(Row(level: "30")); Gather(s, "mining-1", 10);
            Assert.That(s.StartProductionAt(T(10), "character-1", "dev-test", false, out _), Is.False);
            Assert.That(s.StartProductionAt(T(10), "unknown", "dev-test", false, out _), Is.False);
            Assert.That(s.StartProductionAt(T(10), "character-1", "unknown", true, out _), Is.False);
            Assert.That(s.Quantity("metal-1"), Is.EqualTo(2));
            Assert.That(s.Assignments["character-1"], Is.EqualTo("mining-1"));
        }

        [Test]
        public void BatchedAndPartitionedProductionSettlementAgree()
        {
            var a = Session(); var b = Session();
            Gather(a, "mining-1", 10); Gather(b, "mining-1", 10);
            Start(a, "dev-bronze-blade"); Start(b, "dev-bronze-blade");
            for (int i = 11; i <= 100; i++) a.SettleTo(T(i));
            b.SettleTo(T(100));
            Assert.That(a.Quantity("dev-blade"), Is.EqualTo(b.Quantity("dev-blade")));
            Assert.That(Skill(a, "character-1", "smithing").StoredXp, Is.EqualTo(Skill(b, "character-1", "smithing").StoredXp));
            Assert.That(a.CompletedCycles("character-1"), Is.EqualTo(b.CompletedCycles("character-1")));
        }

        [Test]
        public void SelectionChangesPreserveJobButUseCompletionTimeXpEligibility()
        {
            var s = Session(); Gather(s, "mining-1", 10); Start(s, "dev-bronze-blade");
            s.SelectAt(T(24), "character-2");
            Assert.That(s.PartialTicks("character-1"), Is.EqualTo(T(14)));
            s.SettleTo(T(25));
            Assert.That(s.Quantity("dev-blade"), Is.EqualTo(1));
            Assert.That(Skill(s, "character-1", "smithing").StoredXp, Is.Zero);
        }

        [Test]
        public void OfflineProductionNeverUsesSelectedFullXp()
        {
            var s = Session(); Gather(s, "fishing-1", 5); Start(s, "dev-shrimp-meal");
            s.SettleTo(T(10), true); s.SettleTo(T(10), true);
            Assert.That(s.Quantity("dev-meal"), Is.EqualTo(1));
            Assert.That(Skill(s, "character-1", "cooking").StoredXp, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BackgroundAndOfflineProductionEffectsAreCappedAtFiftyPercent(bool offline)
        {
            var s = Session(); Gather(s, "fishing-1", 5);
            s.SetEffectsAt(T(5), "character-2", new[] { new BackgroundXpEffect("test-only", "cooking", 0.9m) });
            Start(s, "dev-shrimp-meal", "character-2"); s.SettleTo(T(10), offline);
            Assert.That(s.Quantity("dev-meal"), Is.EqualTo(1));
            Assert.That(Skill(s, "character-2", "cooking").StoredXp, Is.EqualTo(50000m));
        }

        [Test]
        public void UnknownXpPaysOnlyOutputWhileFractionalConfiguredXpIsPreserved()
        {
            var s = Session(Row(xp: "TBD")); Gather(s, "mining-1", 10); Start(s, "dev-test"); s.SettleTo(T(25));
            Assert.That(s.Quantity("dev-output"), Is.EqualTo(1));
            Assert.That(Skill(s, "character-1", "smithing").StoredXp, Is.Zero);
            var fractional = Session(Row(xp: "0.25")); Gather(fractional, "mining-1", 10);
            fractional.SetEffectsAt(T(10), "character-2", new[] { new BackgroundXpEffect("fixture", "smithing", 0.1m) });
            Start(fractional, "dev-test", "character-2"); fractional.SettleTo(T(25));
            Assert.That(Skill(fractional, "character-2", "smithing").StoredXp, Is.EqualTo(0.025m));
        }

        [Test]
        public void ProductionXpReachesKnownMilestoneWithoutInventingIntermediateLevels()
        {
            var s = Session(); Gather(s, "fishing-1", 25);
            for (int i = 0; i < 5; i++)
            {
                Start(s, "dev-shrimp-meal"); s.SettleTo(s.LastSettledTicks + T(5));
                Assert.That(Skill(s, "character-1", "cooking").Level, Is.EqualTo(i == 4 ? 30 : 1));
            }
            Assert.That(s.Quantity("dev-meal"), Is.EqualTo(5));
            Assert.That(Skill(s, "character-1", "cooking").StoredXp, Is.EqualTo(500000m));
        }

        [Test]
        public void LongOfflineJobStopsAtBaseCapAndRepeatedReplayCannotFinishIt()
        {
            var s = Session(Row(seconds: "18000")); Gather(s, "mining-1", 10); Start(s, "dev-test");
            s.SettleTo(T(18010), true); s.SettleTo(T(18010), true);
            Assert.That(s.PartialTicks("character-1"), Is.EqualTo(T(14400)));
            Assert.That(s.Quantity("dev-output"), Is.Zero);
            s.SettleTo(T(21610), true);
            Assert.That(s.Quantity("dev-output"), Is.EqualTo(1));
            Assert.That(Skill(s, "character-1", "smithing").StoredXp, Is.Zero);
        }

        [Test]
        public void ConcurrentJobsShareStockWithoutDoubleConsumptionAndProduceIndependently()
        {
            var s = Session(); Gather(s, "woodcutting-1", 20);
            Start(s, "dev-wood-bow"); Start(s, "dev-wood-wand", "character-2");
            Assert.That(s.Quantity("wood-1"), Is.Zero);
            s.SettleTo(T(35));
            Assert.That(s.Quantity("dev-bow"), Is.EqualTo(1));
            Assert.That(s.Quantity("dev-wand"), Is.EqualTo(1));
            Assert.That(Skill(s, "character-1", "crafting").StoredXp, Is.EqualTo(100000m));
            Assert.That(Skill(s, "character-2", "crafting").StoredXp, Is.Zero);
        }

        [Test]
        public void SimultaneousGatheringAndProductionDoNotOverwriteSharedBankChanges()
        {
            var s = Session(); Gather(s, "fishing-1", 5);
            Start(s, "dev-shrimp-meal", "character-2"); s.SettleTo(T(10));
            Assert.That(s.Quantity("fish-1"), Is.EqualTo(1));
            Assert.That(s.Quantity("dev-meal"), Is.EqualTo(1));
        }

        [Test]
        public void OverflowAndRewindDoNotPartlyPayCompletion()
        {
            var s = Session(Row(quantity: long.MaxValue.ToString(CultureInfo.InvariantCulture)));
            Gather(s, "mining-1", 20); Start(s, "dev-test"); s.SettleTo(T(35));
            Start(s, "dev-test");
            Assert.Throws<OverflowException>(() => s.SettleTo(T(50)));
            Assert.That(s.LastSettledTicks, Is.EqualTo(T(35)));
            Assert.That(s.Quantity("dev-output"), Is.EqualTo(long.MaxValue));
            Assert.That(s.ProductionJobs.Count, Is.EqualTo(1));
            Assert.That(Skill(s, "character-1", "smithing").StoredXp, Is.EqualTo(100000m));
            Assert.Throws<ArgumentOutOfRangeException>(() => s.SettleTo(T(34)));
        }

        [Test]
        public void OutputQuantityAndPublicReadOnlyViewsAreEnforced()
        {
            var s = Session(Row(quantity: "3")); Gather(s, "mining-1", 10); Start(s, "dev-test");
            var jobs = (IDictionary<string, ProductionRecipe>)s.ProductionJobs;
            Assert.Throws<NotSupportedException>(() => jobs.Clear());
            var inputs = (IDictionary<string, long>)s.Production.Recipes[0].Inputs;
            Assert.Throws<NotSupportedException>(() => inputs["metal-1"] = 0);
            s.SettleTo(T(25)); Assert.That(s.Quantity("dev-output"), Is.EqualTo(3));
        }

        [Test]
        public void FinishedProductCanBeConsumedAsAnotherRecipesIngredient()
        {
            // Test-only chain; not an approved production recipe.
            var s = Session(Row(id: "dev-first", output: "dev-base") + "\n" +
                Row(id: "dev-second", skill: "crafting", category: "Offhand", output: "dev-finished", inputs: "dev-base:1", seconds: "5"));
            Gather(s, "mining-1", 10); Start(s, "dev-first"); s.SettleTo(T(25));
            Start(s, "dev-second");
            Assert.That(s.Quantity("dev-base"), Is.Zero);
            s.SettleTo(T(30)); Assert.That(s.Quantity("dev-finished"), Is.EqualTo(1));
        }

        [Test]
        public void KnownMilestoneCanUnlockHigherRequirementRecipeWithoutInventedLevels()
        {
            var s = Session(Row(skill: "cooking", category: "Food", inputs: "fish-1:1", seconds: "5") + "\n" +
                Row(id: "dev-advanced", output: "dev-advanced-item", skill: "cooking", category: "Food",
                    inputs: "fish-1:1", seconds: "5", level: "30"));
            Gather(s, "fishing-1", 30);
            Assert.That(s.StartProductionAt(s.LastSettledTicks, "character-1", "dev-advanced", false, out _), Is.False);
            for (int i = 0; i < 5; i++)
            {
                Start(s, "dev-test"); s.SettleTo(s.LastSettledTicks + T(5));
            }
            Start(s, "dev-advanced"); s.SettleTo(s.LastSettledTicks + T(5));
            Assert.That(s.Quantity("dev-advanced-item"), Is.EqualTo(1));
            Assert.That(Skill(s, "character-1", "cooking").Level, Is.EqualTo(30));
        }
    }
}
