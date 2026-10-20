using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Idlebound.Foundation.Tests
{
    public sealed class GameUiTests
    {
        private const string C = "character-1", D = "character-2";
        private readonly object owner = new object();
        private static string Data(string name) => Resources.Load<TextAsset>("Idlebound/" + name).text;
        private static long T(int seconds) => seconds * TimeSpan.TicksPerSecond;
        private GameUiModel Model()
        {
            var g = GatheringCatalog.LoadProduction(Data("ResourceTiers"),Data("GatheringXp"));
            var s = new GatheringSession(g,XpCurve.Load(Data("ProductionXpCurve"),false),ProductionCatalog.Load(Data("StarterProductionRecipes"),g.Resources),
                equipment: EquipmentCatalog.LoadDevelopment(Data("DevelopmentGear")),combat: CombatCatalog.LoadDevelopment(Data("DevelopmentCombatRules"),Data("DevelopmentMonsters"),Data("DevelopmentLoot")));
            return new GameUiModel(new SessionRuntime(s,owner));
        }
        private static void Assign(GameUiModel m,string skill = "mining",string activity = "mining-1")
        { m.Inspect(C); m.ShowSkill(skill); Assert.That(m.Start(activity),Is.True,m.Feedback); }
        private static void Gear(GameUiModel m,string item = "bronze-dagger-item")
        { m.Session.GrantDevelopmentGearKitAt(m.Session.LastSettledTicks); Assert.That(m.Equip(item),Is.True,m.Feedback); }

        [Test]
        public void InspectionDoesNotSelectBackgroundCharacter()
        {
            var m = Model(); m.Inspect(D);
            Assert.That(m.InspectedId,Is.EqualTo(D)); Assert.That(m.Session.ActiveCharacterId,Is.EqualTo(C));
            Assert.That(m.Character(D).Active,Is.False);
        }
        [Test]
        public void MakeActivePreservesAssignmentsEquipmentAndProgress()
        {
            var m = Model(); Assign(m); Gear(m); m.Runtime.Tick(owner,2); m.Inspect(D);
            Assert.That(m.MakeActive(),Is.True);
            Assert.That(m.Session.Assignments[C],Is.EqualTo("mining-1"));
            Assert.That(m.Session.Loadouts[C].Slots.Count,Is.EqualTo(1)); Assert.That(m.Session.PartialTicks(C),Is.EqualTo(T(2)));
            Assert.That(m.Session.ActiveCharacterId,Is.EqualTo(D));
        }
        [Test]
        public void NineSkillsAreOrderedAndUseActualCurveThresholds()
        {
            var m = Model(); var skills = m.Skills(C);
            Assert.That(skills.Select(s => s.Name),Is.EqualTo(new[] { "Mining","Smithing","Crafting","Woodcutting","Fishing","Cooking","Melee","Ranged","Magic" }));
            Assert.That(skills.All(s => s.Level == 1 && s.Xp == 0 && s.NextXp == 595),Is.True);
            foreach (var skill in skills) { m.ShowSkill(skill.Id); Assert.That(m.SkillFilter,Is.EqualTo(skill.Id)); Assert.That(m.CharacterTab,Is.EqualTo(CharacterScreen.Activities)); }
        }
        [Test]
        public void GatheringProgressAndResourceStockUseRealValues()
        {
            var m = Model(); Assign(m); m.Runtime.Tick(owner,2);
            Assert.That(m.Character(C).Progress,Is.EqualTo(.4).Within(.00001)); Assert.That(m.Character(C).Status,Is.EqualTo(WorkStatus.Working));
            m.Runtime.Tick(owner,5); Assert.That(m.Session.Quantity("metal-1"),Is.EqualTo(1));
            Assert.That(m.Skills(C).First(s => s.Id == "mining").Xp,Is.EqualTo(56));
        }
        [Test]
        public void LockedActivitiesRemainVisible()
        {
            var m = Model(); m.Inspect(C); m.ShowSkill("mining"); var activities = m.Activities();
            Assert.That(activities.Length,Is.EqualTo(10)); Assert.That(activities.First(a => a.Id == "mining-10").Locked,Is.True);
            Assert.That(activities.First(a => a.Id == "mining-10").CanStart,Is.False);
        }
        [Test]
        public void OccupantIsIdentifiedAndSwapRequiresExplicitConfirmation()
        {
            var m = Model(); Assign(m); m.Inspect(D); m.ShowSkill("mining");
            Assert.That(m.Activities().First(a => a.Id == "mining-1").Occupant,Is.EqualTo(C));
            Assert.That(m.Start("mining-1"),Is.False); Assert.That(m.Feedback,Does.Contain("Character 1"));
            Assert.That(m.Session.Assignments[C],Is.EqualTo("mining-1"));
            Assert.That(m.Start("mining-1",true),Is.True);
            Assert.That(m.Session.Assignments.ContainsKey(C),Is.False); Assert.That(m.Session.Assignments[D],Is.EqualTo("mining-1"));
            Assert.That(m.Session.ActiveCharacterId,Is.EqualTo(C));
            m.Inspect(C); m.ShowSkill("mining");
            Assert.That(m.Start("mining-1",true,C),Is.False,"Stale occupant confirmation must not displace someone else.");
            Assert.That(m.Session.Assignments[D],Is.EqualTo("mining-1"));
        }
        [Test]
        public void ProductionInputsStockReservationAndCompletionAreProjected()
        {
            var m = Model(); Assign(m); m.Runtime.Tick(owner,10); m.ShowSkill("smithing");
            var recipe = m.Activities().First(a => a.Id == "bronze-dagger");
            Assert.That(recipe.Inputs.Single().Required,Is.EqualTo(2)); Assert.That(recipe.Inputs.Single().Available,Is.EqualTo(2));
            Assert.That(recipe.CanStart,Is.True); Assert.That(m.Start(recipe.Id),Is.True); Assert.That(m.Session.Quantity("metal-1"),Is.Zero);
            m.Runtime.Tick(owner,25); Assert.That(m.Character(C).Status,Is.EqualTo(WorkStatus.NeedsAttention));
            Assert.That(m.Session.Quantity("bronze-dagger-item"),Is.EqualTo(1));
        }
        [Test]
        public void ProductionCancelReturnsInputsOnceThroughUi()
        {
            var m = Model(); Assign(m); m.Runtime.Tick(owner,10); m.ShowSkill("smithing"); m.Start("bronze-dagger");
            m.Runtime.Tick(owner,14); Assert.That(m.Stop(),Is.True); Assert.That(m.Stop(),Is.False);
            Assert.That(m.Session.Quantity("metal-1"),Is.EqualTo(2)); Assert.That(m.Session.Quantity("bronze-dagger-item"),Is.Zero);
        }
        [Test]
        public void RepeatedUiProductionStartsCannotDoubleReserveOrReward()
        {
            var m = Model(); Assign(m); m.Runtime.Tick(owner,10); m.ShowSkill("smithing"); m.Start("bronze-dagger");
            Assert.That(m.Start("bronze-dagger"),Is.False); m.Runtime.Tick(owner,25); m.Runtime.Tick(owner,25);
            Assert.That(m.Session.Quantity("bronze-dagger-item"),Is.EqualTo(1));
            Assert.That(m.Skills(C).First(s => s.Id == "smithing").Xp,Is.EqualTo(250));
        }
        [Test]
        public void EquipReplaceUnequipConserveStockAndIdentityThroughUi()
        {
            var m = Model(); Gear(m); var first = m.Session.Loadouts[C].Slots[EquipmentSlot.Mainhand].InstanceId;
            Assert.That(m.Equip("wood-wand-item"),Is.True); Assert.That(m.Session.Quantity("bronze-dagger-item"),Is.EqualTo(1));
            Assert.That(m.Unequip(EquipmentSlot.Mainhand),Is.True); Assert.That(m.Session.Quantity("wood-wand-item"),Is.EqualTo(1));
            m.Inspect(D); Assert.That(m.Equip("bronze-dagger-item"),Is.True);
            Assert.That(m.Session.Loadouts[D].Slots[EquipmentSlot.Mainhand].InstanceId,Is.EqualTo(first));
        }
        [Test]
        public void EquipmentBrowserDoesNotOfferAnotherCharactersItem()
        {
            var m = Model(); Gear(m); m.Inspect(D);
            Assert.That(m.CompatibleStock(EquipmentSlot.Mainhand).Any(d => d.Id == "bronze-dagger-item"),Is.False);
            Assert.That(m.Equip("bronze-dagger-item"),Is.False); Assert.That(m.Session.Loadouts[D].Slots,Is.Empty);
        }
        [Test]
        public void ComparisonsOmitUnchangedStatsAndAccountForReturnedOffhand()
        {
            var m = Model(); Gear(m); Assert.That(m.Compare("wood-wand-item"),Is.Empty);
            var sword = m.Compare("dev-greatsword"); Assert.That(sword.Select(s => s.Name),Is.EquivalentTo(new[] { "Damage","Attack seconds" }));
            Assert.That(sword.First(s => s.Name == "Damage").Improvement,Is.True);
            Assert.That(sword.First(s => s.Name == "Attack seconds").Improvement,Is.False);
            m.Equip("dev-practice-board"); Assert.That(m.Compare("dev-greatsword").Single(s => s.Name == "Defense").Improvement,Is.False);
        }
        [TestCase(BankFilter.All)]
        [TestCase(BankFilter.Resources)]
        [TestCase(BankFilter.Equipment)]
        [TestCase(BankFilter.Products)]
        [TestCase(BankFilter.Loot)]
        public void BankFilteringNeverMutatesInventory(BankFilter filter)
        {
            var m = Model(); Assign(m); m.Runtime.Tick(owner,10); Gear(m);
            var before = m.Session.Inventory.ToArray(); m.Filter = filter; var items = m.Bank();
            Assert.That(items.All(i => i.Quantity > 0 && (filter == BankFilter.All || i.Type == filter)),Is.True);
            Assert.That(m.Session.Inventory,Is.EquivalentTo(before));
        }
        [Test]
        public void BankSearchIsCaseInsensitiveAndUsesNamesAndIds()
        {
            var m = Model(); Gear(m); m.Search = "BRONZE";
            Assert.That(m.Bank().Any(i => i.Id == "bronze-helm-item"),Is.True);
            Assert.That(m.Bank().All(i => i.Name.IndexOf("bronze",StringComparison.OrdinalIgnoreCase) >= 0 || i.Id.Contains("bronze")),Is.True);
            m.Search = "nothing-matches"; Assert.That(m.Bank(),Is.Empty);
        }
        [Test]
        public void CombatPresentationUsesActualHpXpDefeatsAndLoot()
        {
            var m = Model(); m.Inspect(C); Gear(m); m.ShowSkill("melee"); Assert.That(m.Start("dev-chicken"),Is.True);
            m.Runtime.Tick(owner,6);
            Assert.That(m.Character(C).Hp,Is.EqualTo(49)); Assert.That(m.Session.Encounters[C].EnemyHp,Is.EqualTo(12));
            Assert.That(m.Skills(C).First(s => s.Id == "melee").Xp,Is.EqualTo(36));
            Assert.That(m.Session.KillsFor(C,"dev-chicken"),Is.EqualTo(1)); Assert.That(m.Bank().First(i => i.Id == "dev-feather").Quantity,Is.EqualTo(1));
        }
        [Test]
        public void IncompatibleStyleCannotStartAndDoesNotUnequipWeapon()
        {
            var m = Model(); Gear(m); m.ShowSkill("magic"); Assert.That(m.Activities().All(a => a.Locked && !a.CanStart),Is.True);
            Assert.That(m.Start("dev-chicken"),Is.False); Assert.That(m.Session.Profile(C).Style,Is.EqualTo(CombatStyle.Melee));
        }
        [Test]
        public void DeadCharacterIsNeedsAttentionAndCannotRestartCombat()
        {
            var m = Model(); m.Inspect(C); m.ShowSkill("melee"); m.Start("dev-mage"); m.Runtime.Tick(owner,40);
            Assert.That(m.Character(C).Status,Is.EqualTo(WorkStatus.NeedsAttention)); Assert.That(m.Character(C).Hp,Is.Zero);
            Assert.That(m.Start("dev-mage"),Is.False);
        }
        [Test]
        public void EveryNavigationDestinationPreservesSessionAssignmentsAndTime()
        {
            var m = Model(); Assign(m); m.Runtime.Tick(owner,2); var session = m.Session;
            foreach (MainScreen screen in Enum.GetValues(typeof(MainScreen)))
            {
                m.Navigate(screen); m.Bank(); m.Skills(D); m.Character(C); m.SceneCollapsed = !m.SceneCollapsed;
                Assert.That(m.Session,Is.SameAs(session)); Assert.That(m.Session.LastSettledTicks,Is.EqualTo(T(2)));
                Assert.That(m.Session.Assignments[C],Is.EqualTo("mining-1")); Assert.That(m.Session.PartialTicks(C),Is.EqualTo(T(2)));
            }
        }
        [Test]
        public void RefreshesAndRepeatedTimestampsCannotCreateRewards()
        {
            var m = Model(); Assign(m); m.Runtime.Tick(owner,5);
            for (int i = 0; i < 20; i++) { m.Character(C); m.Skills(C); m.Activities(); m.Bank(); m.Runtime.Tick(owner,5); }
            Assert.That(m.Session.Quantity("metal-1"),Is.EqualTo(1)); Assert.That(m.Skills(C).First(s => s.Id == "mining").Xp,Is.EqualTo(56));
        }
        [Test]
        public void SessionCanHaveOnlyOneRuntimeAndOneClockCaller()
        {
            var m = Model(); Assert.Throws<InvalidOperationException>(() => new SessionRuntime(m.Session,new object()));
            Assert.Throws<InvalidOperationException>(() => m.Runtime.Tick(new object(),10));
            Assert.Throws<InvalidOperationException>(() => m.Runtime.SetPresence(new object(),10,false,false));
            Assert.That(m.Session.LastSettledTicks,Is.Zero);
        }
        [Test]
        public void FractionalClockAdvancementIsPartitionIndependent()
        {
            var a = Model(); var b = Model(); Assign(a); Assign(b);
            a.Runtime.Tick(owner,10); for (int i = 1; i <= 100; i++) b.Runtime.Tick(owner,i / 10d);
            Assert.That(a.Session.LastSettledTicks,Is.EqualTo(b.Session.LastSettledTicks));
            Assert.That(a.Session.Quantity("metal-1"),Is.EqualTo(b.Session.Quantity("metal-1")));
        }
        [Test]
        public void FocusAndPauseRepresentOneContiguousOfflineInterval()
        {
            var m = Model(); Assign(m); m.Runtime.Tick(owner,5);
            m.Runtime.SetPresence(owner,5,true,true); m.Runtime.SetPresence(owner,5,true,false);
            m.Runtime.Tick(owner,100); Assert.That(m.Session.LastSettledTicks,Is.EqualTo(T(5)));
            m.Runtime.SetPresence(owner,305,false,false); m.Runtime.SetPresence(owner,305,false,true);
            Assert.That(m.Session.Quantity("metal-1"),Is.EqualTo(61));
            Assert.That(m.Skills(C).First(s => s.Id == "mining").Xp,Is.EqualTo(56));
            m.Runtime.SetPresence(owner,305,false,true); m.Runtime.Tick(owner,305);
            Assert.That(m.Session.Quantity("metal-1"),Is.EqualTo(61));
        }
        [Test]
        public void PausedDevelopmentSteppingUsesTheSameSessionExactlyOnce()
        {
            var m = Model(); Assign(m); Assert.Throws<InvalidOperationException>(() => m.Runtime.Step(owner,T(5)));
            m.Runtime.SetRunning(owner,0,false); m.Runtime.Tick(owner,100); m.Runtime.Step(owner,T(5));
            Assert.That(m.Session.Quantity("metal-1"),Is.EqualTo(1)); m.Runtime.SetRunning(owner,100,true); m.Runtime.Tick(owner,105);
            Assert.That(m.Session.Quantity("metal-1"),Is.EqualTo(2));
        }
        [Test]
        public void ResponsiveAndNavigationStateHaveNoGameplaySideEffects()
        {
            var m = Model(); Assign(m); m.SceneCollapsed = true; m.Search = "ore"; m.Filter = BankFilter.Resources;
            m.ShowCharacterTab(CharacterScreen.Equipment); m.Navigate(MainScreen.Shop); m.Navigate(MainScreen.CollectionLog); m.Inspect(D);
            Assert.That(m.Session.Assignments[C],Is.EqualTo("mining-1")); Assert.That(m.Session.ActiveCharacterId,Is.EqualTo(C));
            Assert.That(m.Session.LastSettledTicks,Is.Zero); Assert.That(m.Session.Inventory,Is.Empty);
        }
        [Test]
        public void ClockRejectsRewindNonFiniteTimeAndInvalidUiDestinations()
        {
            var m = Model(); m.Runtime.Tick(owner,1);
            Assert.Throws<ArgumentOutOfRangeException>(() => m.Runtime.Tick(owner,0));
            Assert.Throws<ArgumentOutOfRangeException>(() => m.Runtime.Tick(owner,double.NaN));
            Assert.Throws<ArgumentException>(() => m.Navigate((MainScreen)100));
            Assert.Throws<ArgumentException>(() => m.Inspect("missing")); Assert.Throws<ArgumentException>(() => m.ShowSkill("missing"));
        }
        [Test]
        public void BankNamesComeFromCatalogAndRefreshWithoutMutatingInventory()
        {
            var m = Model(); Assign(m); m.Runtime.Tick(owner,5);
            var first = m.Bank().Single(i => i.Id == "metal-1");
            Assert.That(first.Name,Is.EqualTo(m.Session.Catalog.Resources.First(r => r.Id == first.Id).DisplayName));
            Assert.That(first.Quantity,Is.EqualTo(1));
            m.Navigate(MainScreen.Bank); m.Runtime.Tick(owner,10);
            var second = m.Bank().Single(i => i.Id == first.Id);
            Assert.That(second.Name,Is.EqualTo(first.Name)); Assert.That(second.Quantity,Is.EqualTo(2));
            var before = m.Session.Inventory.ToArray(); m.Search = second.Name.ToUpperInvariant(); m.Filter = BankFilter.Resources;
            Assert.That(m.Bank().Any(i => i.Id == second.Id),Is.True);
            m.Search = second.Id; Assert.That(m.Bank().Single().Name,Is.EqualTo(second.Name));
            Assert.That(m.Session.Inventory,Is.EquivalentTo(before)); Assert.That(m.Session.Assignments[C],Is.EqualTo("mining-1"));
        }
    }
}
