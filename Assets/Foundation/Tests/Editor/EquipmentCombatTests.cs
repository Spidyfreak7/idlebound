using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Idlebound.Foundation.Tests
{
    public sealed class EquipmentCombatTests
    {
        private const string C = "character-1";
        private const string D = "character-2";
        private static long T(int seconds) => seconds * TimeSpan.TicksPerSecond;
        private static string Text(string name) => Resources.Load<TextAsset>("Idlebound/" + name).text;
        private static EquipmentCatalog Gear() => EquipmentCatalog.LoadDevelopment(Text("DevelopmentGear"));
        private static CombatCatalog Combat() => CombatCatalog.LoadDevelopment(Text("DevelopmentCombatRules"), Text("DevelopmentMonsters"), Text("DevelopmentLoot"));
        private static GatheringSession Session(CombatCatalog combat = null, IDeterministicLootRoller roller = null)
        {
            var gathering = GatheringCatalog.LoadProduction(Text("ResourceTiers"), Text("GatheringXp"));
            return new GatheringSession(gathering, XpCurve.Load(Text("ProductionXpCurve"), false),
                ProductionCatalog.Load(Text("StarterProductionRecipes"), gathering.Resources), equipment: Gear(), combat: combat ?? Combat(), random: roller);
        }
        private static decimal Xp(GatheringSession s, string skill = "melee", string character = C) =>
            s.Characters.First(c => c.Id == character).Skills.First(k => k.SkillId == skill).StoredXp;
        private static void Fight(GatheringSession s, string monster = "dev-chicken", string character = C, bool swap = false) =>
            Assert.That(s.StartCombatAt(s.LastSettledTicks, character, monster, s.Profile(character).Style, swap, out var error), Is.True, error);
        private static void Equip(GatheringSession s, string item, string character = C)
        {
            var slot = s.Equipment.Definitions.First(d => d.Id == item).Slot;
            Assert.That(s.EquipAt(s.LastSettledTicks, character, item, slot, out var error), Is.True, error);
        }
        private static CombatCatalog Simple(decimal hp = 3, decimal damage = 0, int enemySeconds = 3, int budget = 100000,
            IEnumerable<LootDefinition> drops = null, decimal playerHp = 50) => new CombatCatalog(
                new CombatRules(playerHp, 2, T(2), 1, 1, .5m, budget),
                new[] { new MonsterDefinition("dev-test", "[DEV] Test", hp, damage, T(enemySeconds), CombatStyle.Melee) },
                drops ?? Array.Empty<LootDefinition>());
        private sealed class FixedRoller : IDeterministicLootRoller
        {
            private readonly decimal value;
            public FixedRoller(decimal value) { this.value = value; }
            public decimal Roll(string character, string monster, long kill, string drop) => value;
        }
        private static IEnumerable<EquipmentSlot> AllSlots => Enum.GetValues(typeof(EquipmentSlot)).Cast<EquipmentSlot>();
        private static IEnumerable<int> Tiers => Enumerable.Range(1, 10);

        [TestCaseSource(nameof(AllSlots))]
        public void EverySlotEquipsAndReturnsExactlyOneItem(EquipmentSlot slot)
        {
            var s = Session(); s.GrantDevelopmentGearKitAt(0);
            var gear = s.Equipment.Definitions.First(d => d.Slot == slot && d.StandardArmorTier == 0);
            Equip(s, gear.Id);
            Assert.That(s.Quantity(gear.Id), Is.Zero);
            var instance = s.Loadouts[C].Slots[slot];
            Assert.That(s.UnequipAt(0, C, slot, out _), Is.True);
            Assert.That(s.Quantity(gear.Id), Is.EqualTo(1));
            Assert.That(s.BankEquipmentInstances[instance.InstanceId], Is.SameAs(instance));
            Assert.That(s.UnequipAt(0, C, slot, out _), Is.False);
            Assert.That(s.Quantity(gear.Id), Is.EqualTo(1));
        }

        [TestCaseSource(nameof(Tiers))]
        public void StandardArmorUsesApprovedLevels(int tier) =>
            Assert.That(EquipmentSlots.StandardArmorLevel(tier), Is.EqualTo(5 + 10 * (tier - 1)));

        [Test]
        public void EnchantmentHooksHaveNoFreeEffects()
        {
            Assert.That(EquipmentSlots.EnchantmentSetSlots, Is.EquivalentTo(new[] { EquipmentSlot.Head, EquipmentSlot.Body, EquipmentSlot.Legs, EquipmentSlot.Feet }));
            var s = Session(); s.GrantDevelopmentGearKitAt(0); Equip(s, "bronze-dagger-item");
            Assert.That(s.Loadouts[C].Slots[EquipmentSlot.Mainhand].PermanentEnchantmentId, Is.Null);
            Assert.That(s.Equipment.Definitions.All(d => d.InherentEffectIds.Count == 0), Is.True);
            Assert.That(s.BackgroundEfficiency(C, "melee"), Is.Zero);
        }

        [Test]
        public void CatalogRejectsDuplicatesAndUnlabelledData()
        {
            var d = Gear().Definitions[0];
            Assert.Throws<ArgumentException>(() => new EquipmentCatalog(new[] { d, d }));
            Assert.Throws<ArgumentException>(() => EquipmentCatalog.LoadDevelopment(Text("DevelopmentGear").Replace("development", "approved")));
            Assert.Throws<ArgumentException>(() => EquipmentCatalog.LoadDevelopment(Text("DevelopmentGear").Replace("[DEV]", "Live")));
        }

        [TestCase("melee:1", "unknown:1")]
        [TestCase("melee:1", "melee:100")]
        [TestCase("melee:1", "melee:0")]
        [TestCase("Mainhand", "InvalidSlot")]
        [TestCase(",2,false,development", ",0,false,development")]
        public void InvalidGearFieldsAreRejected(string from, string to) =>
            Assert.Catch<ArgumentException>(() => EquipmentCatalog.LoadDevelopment(Text("DevelopmentGear").Replace(from, to)));

        [Test]
        public void StandardArmorCannotBypassTierGate()
        {
            Assert.Throws<ArgumentException>(() => new EquipmentDefinition("bad", "[DEV] Bad", EquipmentSlot.Head,
                new Dictionary<string, int> { { "melee", 1 } }, 1, null, 0, 0, 0, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => EquipmentSlots.StandardArmorLevel(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => EquipmentSlots.StandardArmorLevel(11));
        }

        [Test]
        public void WrongSlotGateAndMissingStockPreserveBankAndLoadout()
        {
            var s = Session();
            Assert.That(s.EquipAt(0, C, "bronze-dagger-item", EquipmentSlot.Mainhand, out _), Is.False);
            s.GrantDevelopmentGearKitAt(0);
            Assert.That(s.EquipAt(0, C, "bronze-dagger-item", EquipmentSlot.Feet, out _), Is.False);
            Assert.That(s.EquipAt(0, C, "bronze-helm-item", EquipmentSlot.Head, out _), Is.False);
            Assert.That(s.EquipAt(0, "unknown", "bronze-dagger-item", EquipmentSlot.Mainhand, out _), Is.False);
            Assert.That(s.Loadouts[C].Slots, Is.Empty);
            Assert.That(s.Quantity("bronze-helm-item"), Is.EqualTo(1));
            Assert.That(s.Quantity("bronze-dagger-item"), Is.EqualTo(1));
        }

        [Test]
        public void ReplacementConservesBankAndInstanceIdentityAcrossCharacters()
        {
            var s = Session(); s.GrantDevelopmentGearKitAt(0); Equip(s, "bronze-dagger-item");
            var first = s.Loadouts[C].Slots[EquipmentSlot.Mainhand];
            Assert.That(s.EquipAt(0, D, "bronze-dagger-item", EquipmentSlot.Mainhand, out _), Is.False);
            Equip(s, "wood-wand-item");
            Assert.That(s.Quantity("bronze-dagger-item"), Is.EqualTo(1));
            Equip(s, "bronze-dagger-item", D);
            Assert.That(s.Loadouts[D].Slots[EquipmentSlot.Mainhand], Is.SameAs(first));
            Assert.That(s.BankEquipmentInstances.ContainsKey(first.InstanceId), Is.False);
            Assert.That(s.Quantity("bronze-dagger-item"), Is.Zero);
            Assert.That(s.Loadouts[C].Slots[EquipmentSlot.Mainhand].InstanceId, Is.Not.EqualTo(first.InstanceId));
        }

        [Test]
        public void TwoHandedReplacementReturnsOffhandAtomically()
        {
            var s = Session(); s.GrantDevelopmentGearKitAt(0);
            Equip(s, "dev-practice-board"); Equip(s, "bronze-dagger-item"); Equip(s, "dev-greatsword");
            Assert.That(s.Loadouts[C].Slots.ContainsKey(EquipmentSlot.Offhand), Is.False);
            Assert.That(s.Quantity("dev-practice-board"), Is.EqualTo(1));
            Assert.That(s.Quantity("bronze-dagger-item"), Is.EqualTo(1));
            Assert.That(s.EquipAt(0, C, "dev-practice-board", EquipmentSlot.Offhand, out _), Is.False);
            Assert.That(s.Quantity("dev-practice-board"), Is.EqualTo(1));
        }

        [TestCase(CombatStyle.Melee, CombatStyle.Melee, 1)]
        [TestCase(CombatStyle.Melee, CombatStyle.Ranged, 2)]
        [TestCase(CombatStyle.Melee, CombatStyle.Magic, .5)]
        [TestCase(CombatStyle.Ranged, CombatStyle.Melee, .5)]
        [TestCase(CombatStyle.Ranged, CombatStyle.Ranged, 1)]
        [TestCase(CombatStyle.Ranged, CombatStyle.Magic, 2)]
        [TestCase(CombatStyle.Magic, CombatStyle.Melee, 2)]
        [TestCase(CombatStyle.Magic, CombatStyle.Ranged, .5)]
        [TestCase(CombatStyle.Magic, CombatStyle.Magic, 1)]
        public void AllTriangleMatchups(CombatStyle attack, CombatStyle defense, double multiplier) =>
            Assert.That(CombatMath.Triangle(attack, defense), Is.EqualTo((decimal)multiplier));

        [Test]
        public void EveryDueAttackHitsAndOverkillNeverAwardsXp()
        {
            var s = Session(Simple()); Fight(s, "dev-test");
            s.SettleTo(T(2) - 1); Assert.That(Xp(s), Is.Zero);
            s.SettleTo(T(2)); Assert.That(Xp(s), Is.EqualTo(2));
            Assert.That(s.Encounters[C].EnemyHp, Is.EqualTo(1));
            s.SettleTo(T(4)); Assert.That(Xp(s), Is.EqualTo(3));
            Assert.That(s.KillsFor(C, "dev-test"), Is.EqualTo(1));
            Assert.That(s.Encounters[C].EnemyHp, Is.EqualTo(3));
            s.SettleTo(T(4)); Assert.That(Xp(s), Is.EqualTo(3));
        }

        [TestCase("bronze-dagger-item", "melee", 5)]
        [TestCase("wood-shortbow-item", "ranged", 2.5)]
        [TestCase("wood-wand-item", "magic", 5)]
        public void EquippedStyleDeterminesDamageAndXpSkill(string item, string skill, double expectedXp)
        {
            var s = Session(); s.GrantDevelopmentGearKitAt(0); Equip(s, item); Fight(s);
            s.SettleTo(T(2)); Assert.That(Xp(s, skill), Is.EqualTo((decimal)expectedXp));
            Assert.That(s.Characters[0].Skills.Where(k => k.SkillId != skill).All(k => k.StoredXp == 0), Is.True);
        }

        [Test]
        public void MagicCoefficientMustBeStrictlyBelowBothOtherStyles()
        {
            Assert.Throws<ArgumentException>(() => new CombatRules(50, 2, T(2), 1, 2, 1, 100));
            Assert.Throws<ArgumentException>(() => new CombatRules(50, 2, T(2), 2, 1, 1, 100));
            Assert.That(new CombatRules(50, 2, T(2), 2, 3, .25m, 100).MagicXpPerDamage, Is.EqualTo(.25m));
            Assert.Throws<ArgumentException>(() => Combat().Rules.XpPerDamage((CombatStyle)99));
        }

        [TestCase(false, false, 1)]
        [TestCase(true, false, 0)]
        [TestCase(false, true, 0)]
        [TestCase(true, true, 0)]
        public void SelectedBackgroundOfflineXpRules(bool background, bool offline, int efficiency)
        {
            var s = Session(Simple()); string id = background ? D : C;
            Fight(s, "dev-test", id); s.SettleTo(T(4), offline);
            Assert.That(Xp(s, "melee", id), Is.EqualTo(3 * efficiency));
            Assert.That(s.KillsFor(id, "dev-test"), Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BackgroundEffectsAreScopedAndCappedAtHalf(bool offline)
        {
            var s = Session(Simple());
            s.SetEffectsAt(0, D, new[] { new BackgroundXpEffect("a", "melee", .4m), new BackgroundXpEffect("b", "*", .4m) });
            Fight(s, "dev-test", D); s.SettleTo(T(4), offline);
            Assert.That(Xp(s, "melee", D), Is.EqualTo(1.5m));
        }

        [Test]
        public void SelectionChangeSettlesOldRoleBeforeFutureHits()
        {
            var s = Session(Simple(hp: 100)); Fight(s, "dev-test");
            s.SelectAt(T(2), D); s.SettleTo(T(4)); Assert.That(Xp(s), Is.EqualTo(2));
        }

        [Test]
        public void GuaranteedAndIndependentRandomDropsPayOnce()
        {
            var drops = new[] {
                new LootDefinition("a", "dev-test", "a", "A", 2, 1),
                new LootDefinition("b", "dev-test", "b", "B", 3, .5m),
                new LootDefinition("c", "dev-test", "c", "C", 4, .25m),
                new LootDefinition("d", "dev-test", "d", "D", 1, 0) };
            var s = Session(Simple(drops: drops), new FixedRoller(.3m)); Fight(s, "dev-test"); s.SettleTo(T(4));
            Assert.That(s.Quantity("a"), Is.EqualTo(2)); Assert.That(s.Quantity("b"), Is.EqualTo(3));
            Assert.That(s.Quantity("c"), Is.Zero); Assert.That(s.Quantity("d"), Is.Zero);
            s.SettleTo(T(4)); Assert.That(s.Quantity("a"), Is.EqualTo(2));
            s.SettleTo(T(8)); Assert.That(s.Quantity("a"), Is.EqualTo(4));
        }

        [Test]
        public void InvalidRandomRollRollsBackAllSettlementIncludingGathering()
        {
            var s = Session(Simple(drops: new[] { new LootDefinition("x", "dev-test", "x", "X", 1, .5m) }), new FixedRoller(1));
            Fight(s, "dev-test"); s.AssignAt(0, D, "mining-1", false, out _);
            Assert.Throws<InvalidOperationException>(() => s.SettleTo(T(10)));
            Assert.That(s.Inventory, Is.Empty); Assert.That(s.LastSettledTicks, Is.Zero);
            Assert.That(Xp(s), Is.Zero); Assert.That(s.Encounters[C].EnemyHp, Is.EqualTo(3));
            Assert.That(s.KillsFor(C, "dev-test"), Is.Zero);
        }

        [Test]
        public void SeededRollsAndSettlementArePartitionIndependent()
        {
            var a = Session(RandomCombat(), new SeededLootRoller(123));
            var b = Session(RandomCombat(), new SeededLootRoller(123));
            Fight(a, "dev-test"); Fight(b, "dev-test"); a.SettleTo(T(400));
            for (int i = 1; i <= 400; i++) b.SettleTo(T(i));
            Assert.That(a.Inventory, Is.EquivalentTo(b.Inventory));
            Assert.That(Xp(a), Is.EqualTo(Xp(b))); Assert.That(a.KillsFor(C, "dev-test"), Is.EqualTo(100));
            Assert.That(a.Encounters[C].EnemyHp, Is.EqualTo(b.Encounters[C].EnemyHp));
            Assert.That(a.Encounters[C].PlayerRemainingTicks, Is.EqualTo(b.Encounters[C].PlayerRemainingTicks));
        }
        private static CombatCatalog RandomCombat() => Simple(drops: new[] { new LootDefinition("r", "dev-test", "r", "R", 1, .5m) });

        [Test]
        public void KillingOnTieSuppressesRetaliationAndRespawnsWithoutFreeAttack()
        {
            var s = Session(Simple(hp: 2, damage: 100, enemySeconds: 2)); Fight(s, "dev-test"); s.SettleTo(T(2));
            Assert.That(s.Vitals[C].Hp, Is.EqualTo(50)); Assert.That(s.KillsFor(C, "dev-test"), Is.EqualTo(1));
            Assert.That(s.Encounters[C].PlayerRemainingTicks, Is.EqualTo(T(2)));
            Assert.That(s.Encounters[C].EnemyRemainingTicks, Is.EqualTo(T(2)));
            Assert.That(Xp(s), Is.EqualTo(2));
        }

        [Test]
        public void DeathStopsCombatWithoutLosingEquipmentOrEarnedXp()
        {
            var s = Session(Simple(hp: 100, damage: 100, enemySeconds: 3));
            s.GrantDevelopmentGearKitAt(0); Equip(s, "bronze-dagger-item");
            string instance = s.Loadouts[C].Slots[EquipmentSlot.Mainhand].InstanceId;
            Fight(s, "dev-test"); s.SettleTo(T(100));
            Assert.That(s.Vitals[C].Hp, Is.Zero); Assert.That(s.Encounters.ContainsKey(C), Is.False);
            Assert.That(s.Assignments.ContainsKey(C), Is.False); Assert.That(Xp(s), Is.EqualTo(5));
            Assert.That(s.Loadouts[C].Slots[EquipmentSlot.Mainhand].InstanceId, Is.EqualTo(instance));
            s.SettleTo(T(200)); Assert.That(Xp(s), Is.EqualTo(5));
            Assert.That(s.StartCombatAt(T(200), C, "dev-test", CombatStyle.Melee, false, out _), Is.False);
            Assert.That(s.RestoreDevelopmentHpAt(T(200), C, out _), Is.True);
            Fight(s, "dev-test"); Assert.That(s.Vitals[C].Hp, Is.EqualTo(50));
        }

        [Test]
        public void ArmorContributesFlatDevelopmentDefenseWithoutHealing()
        {
            var s = Session(Simple(hp: 100, damage: 2, enemySeconds: 1));
            s.GrantDevelopmentGearKitAt(0); Fight(s, "dev-test"); s.SettleTo(T(1));
            Assert.That(s.Vitals[C].Hp, Is.EqualTo(48)); Equip(s, "dev-practice-body");
            s.SettleTo(T(2)); Assert.That(s.Vitals[C].Hp, Is.EqualTo(47));
        }

        [Test]
        public void FailedCombatRequestsPreserveCurrentAssignment()
        {
            var s = Session(); s.AssignAt(0, C, "mining-1", false, out _);
            Assert.That(s.StartCombatAt(0, C, "dev-chicken", CombatStyle.Magic, false, out _), Is.False);
            Assert.That(s.Assignments[C], Is.EqualTo("mining-1"));
            Assert.That(s.AssignAt(0, C, "dev-chicken", false, out _), Is.False);
            Fight(s); Assert.That(s.Assignments[C], Is.EqualTo("dev-chicken"));
            s.SettleTo(T(1));
            Assert.That(s.StartCombatAt(T(1), C, "dev-chicken", CombatStyle.Melee, false, out _), Is.False);
            Assert.That(s.Encounters[C].PlayerRemainingTicks, Is.EqualTo(T(1)));
        }

        [Test]
        public void ExactMonsterExclusivityAndSwapCancelOldEncounter()
        {
            var s = Session(); Fight(s);
            Assert.That(s.StartCombatAt(0, D, "dev-chicken", CombatStyle.Melee, false, out _), Is.False);
            Fight(s, character: D, swap: true);
            Assert.That(s.Encounters.ContainsKey(C), Is.False); Assert.That(s.Assignments.ContainsKey(C), Is.False);
            Assert.That(s.Encounters.Keys, Is.EquivalentTo(new[] { D }));
            Assert.That(s.AssignAt(0, D, "mining-1", false, out _), Is.True);
            Assert.That(s.Encounters, Is.Empty);
        }

        [Test]
        public void ProductionCancellationRefundsBeforeCombatAndCannotDoublePay()
        {
            var s = Session(); Assert.That(s.AssignAt(0, C, "mining-1", false, out _), Is.True);
            s.SettleTo(T(10)); Assert.That(s.StartProductionAt(T(10), C, "bronze-dagger", false, out _), Is.True);
            Fight(s); Assert.That(s.Quantity("metal-1"), Is.EqualTo(2)); Assert.That(s.ProductionJobs, Is.Empty);
            s.UnassignAt(T(10), C); Assert.That(s.Quantity("metal-1"), Is.EqualTo(2));
            Assert.That(s.Quantity("bronze-dagger-item"), Is.Zero);
        }

        [Test]
        public void GatheringCraftingEquippingCombatLootIntegration()
        {
            var s = Session(); Assert.That(s.AssignAt(0, C, "mining-1", false, out _), Is.True);
            s.SettleTo(T(10)); Assert.That(s.Quantity("metal-1"), Is.EqualTo(2));
            Assert.That(s.StartProductionAt(T(10), C, "bronze-dagger", false, out _), Is.True);
            s.SettleTo(T(25)); Equip(s, "bronze-dagger-item"); Assert.That(s.Quantity("bronze-dagger-item"), Is.Zero);
            Fight(s); s.SettleTo(T(31));
            Assert.That(s.Quantity("dev-feather"), Is.EqualTo(1)); Assert.That(Xp(s), Is.EqualTo(12));
            Assert.That(s.KillsFor(C, "dev-chicken"), Is.EqualTo(1));
        }

        [Test]
        public void ChangingEquipmentRestartsPlayerClockButKeepsHpAndEnemyState()
        {
            var s = Session(); s.GrantDevelopmentGearKitAt(0); Fight(s); s.SettleTo(T(1));
            decimal hp = s.Vitals[C].Hp; Equip(s, "wood-wand-item");
            Assert.That(s.Encounters[C].PlayerRemainingTicks, Is.EqualTo(T(2)));
            Assert.That(s.Encounters[C].EnemyRemainingTicks, Is.EqualTo(T(2)));
            Assert.That(s.Encounters[C].EnemyHp, Is.EqualTo(12)); Assert.That(s.Vitals[C].Hp, Is.EqualTo(hp));
            s.SettleTo(T(3)); Assert.That(Xp(s, "magic"), Is.EqualTo(5));
            Assert.That(Xp(s), Is.Zero);
        }

        [Test]
        public void StoppingPreservesHpAndDiscardsEnemyProgressOnly()
        {
            var s = Session(); Fight(s); s.SettleTo(T(3)); s.UnassignAt(T(3), C);
            Assert.That(s.Vitals[C].Hp, Is.EqualTo(49)); Assert.That(Xp(s), Is.EqualTo(2));
            s.SettleTo(T(10)); Assert.That(s.Vitals[C].Hp, Is.EqualTo(49)); Fight(s);
            Assert.That(s.Encounters[C].EnemyHp, Is.EqualTo(12));
            Assert.That(s.RestoreDevelopmentHpAt(T(10), C, out _), Is.False);
        }

        [Test]
        public void EventBudgetRejectsWholeBatchWithoutAdvancingClockOrXp()
        {
            var s = Session(Simple(budget: 1)); Fight(s, "dev-test");
            Assert.That(s.AssignAt(0, D, "mining-1", false, out _), Is.True);
            Assert.Throws<InvalidOperationException>(() => s.SettleTo(T(10)));
            Assert.That(s.LastSettledTicks, Is.Zero); Assert.That(s.Inventory, Is.Empty);
            Assert.That(Xp(s), Is.Zero); Assert.That(s.KillsFor(C, "dev-test"), Is.Zero);
            Assert.That(s.Encounters[C].EnemyHp, Is.EqualTo(3));
            s.SettleTo(T(2)); Assert.That(Xp(s), Is.EqualTo(2));
        }

        [Test]
        public void LootOverflowRejectsAllRewardsWithoutPartialCommit()
        {
            var s = Session(Simple(hp: 2, drops: new[] { new LootDefinition("x", "dev-test", "x", "X", long.MaxValue, 1) }));
            Fight(s, "dev-test"); s.SettleTo(T(2));
            Assert.Throws<OverflowException>(() => s.SettleTo(T(4)));
            Assert.That(s.LastSettledTicks, Is.EqualTo(T(2))); Assert.That(s.Quantity("x"), Is.EqualTo(long.MaxValue));
            Assert.That(s.KillsFor(C, "dev-test"), Is.EqualTo(1)); Assert.That(Xp(s), Is.EqualTo(2));
        }

        [Test]
        public void BankOverflowCannotPartiallyReplaceOrUnequipGear()
        {
            var s = Session(Simple(hp: 2, drops: new[] { new LootDefinition("x", "dev-test", "bronze-dagger-item", "Dagger", long.MaxValue, 1) }));
            s.GrantDevelopmentGearKitAt(0); Equip(s, "bronze-dagger-item"); Fight(s, "dev-test"); s.SettleTo(T(2));
            var instance = s.Loadouts[C].Slots[EquipmentSlot.Mainhand];
            Assert.Throws<OverflowException>(() => s.EquipAt(T(2), C, "wood-wand-item", EquipmentSlot.Mainhand, out _));
            Assert.Throws<OverflowException>(() => s.UnequipAt(T(2), C, EquipmentSlot.Mainhand, out _));
            Assert.That(s.Loadouts[C].Slots[EquipmentSlot.Mainhand], Is.SameAs(instance));
            Assert.That(s.Quantity("wood-wand-item"), Is.EqualTo(1)); Assert.That(s.BankEquipmentInstances, Is.Empty);
        }

        [Test]
        public void OfflineCapAndRepeatedTimestampDoNotReplayExcessTime()
        {
            var s = Session(Simple(hp: 2)); Fight(s, "dev-test");
            s.SettleTo(T(8 * 3600), true); Assert.That(s.KillsFor(C, "dev-test"), Is.EqualTo(7200));
            Assert.That(Xp(s), Is.Zero); s.SettleTo(T(8 * 3600), true);
            Assert.That(s.KillsFor(C, "dev-test"), Is.EqualTo(7200));
            Assert.Throws<ArgumentOutOfRangeException>(() => s.SettleTo(0));
        }

        [Test]
        public void CatalogRejectsInvalidEnemyLootAndRuleData()
        {
            var m = Combat().Monsters[0];
            Assert.Throws<ArgumentException>(() => new CombatCatalog(Combat().Rules, new[] { m, m }, Array.Empty<LootDefinition>()));
            Assert.Throws<ArgumentException>(() => new CombatCatalog(Combat().Rules, new[] { m }, new[] { new LootDefinition("x", "missing", "x", "X", 1, 1) }));
            Assert.Throws<ArgumentException>(() => new MonsterDefinition("dev-x", "[DEV] X", 0, 1, T(1), CombatStyle.Melee));
            Assert.Throws<ArgumentException>(() => new LootDefinition("x", m.Id, "x", "X", 1, 1.1m));
            Assert.Throws<ArgumentException>(() => new LootDefinition("x", m.Id, "x", "X", 0, 1));
            Assert.Throws<ArgumentException>(() => CombatRules.LoadDevelopment(Text("DevelopmentCombatRules").Replace("development", "approved")));
        }
        [Test]
        public void StandardArmorBecomesEquippableAtTheRequiredCombatLevel()
        {
            var s = Session(Simple(hp: 2)); s.GrantDevelopmentGearKitAt(0);
            Assert.That(s.EquipAt(0, C, "bronze-helm-item", EquipmentSlot.Head, out _), Is.False);
            Fight(s, "dev-test"); s.SettleTo(T(9512));
            Assert.That(Xp(s), Is.EqualTo(9512)); Equip(s, "bronze-helm-item");
            Assert.That(s.Profile(C).Defense, Is.EqualTo(1));
        }

        [Test]
        public void EmptyStartHasNoFreeGearAndBankViewsCannotBeMutated()
        {
            var s = Session();
            Assert.That(s.Inventory, Is.Empty); Assert.That(s.Loadouts[C].Slots, Is.Empty);
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, long>)s.Inventory).Add("x", 1));
            Assert.Throws<NotSupportedException>(() => ((IDictionary<EquipmentSlot, EquipmentInstance>)s.Loadouts[C].Slots).Clear());
        }

        [Test]
        public void ProductionStartCancelsCombatWithoutGhostAttacks()
        {
            var s = Session(); s.AssignAt(0, C, "mining-1", false, out _); s.SettleTo(T(10));
            Fight(s); s.StartProductionAt(T(10), C, "bronze-dagger", false, out _);
            Assert.That(s.Encounters, Is.Empty); s.SettleTo(T(25));
            Assert.That(s.Quantity("bronze-dagger-item"), Is.EqualTo(1)); Assert.That(Xp(s), Is.Zero);
            Assert.That(s.Vitals[C].Hp, Is.EqualTo(50));
        }

        [Test]
        public void GearRecipeInputsRequireFutureInstanceAwareReservation()
        {
            var gathering = GatheringCatalog.LoadProduction(Text("ResourceTiers"), Text("GatheringXp"));
            var csv = Text("StarterProductionRecipes") + "\nrework,Rework,smithing,MetalWeapon,reworked-item,Reworked,1,bronze-dagger-item:1,1,15,0,new-assumption,Unit test only";
            var production = ProductionCatalog.Load(csv, gathering.Resources);
            Assert.Throws<ArgumentException>(() => new GatheringSession(gathering, XpCurve.Load(Text("ProductionXpCurve"), false), production, equipment: Gear()));
        }

        [Test]
        public void StatOverflowCannotPartiallyEquipOrConsumeBankItem()
        {
            var gear = new EquipmentCatalog(new[] {
                new EquipmentDefinition("huge", "[DEV] Huge", EquipmentSlot.Ring, new Dictionary<string, int> { { "melee", 1 } }, 0, null, decimal.MaxValue, 0, 0, false) });
            var gathering = GatheringCatalog.LoadProduction(Text("ResourceTiers"), Text("GatheringXp"));
            var s = new GatheringSession(gathering, XpCurve.Load(Text("ProductionXpCurve"), false), equipment: gear);
            s.GrantDevelopmentGearKitAt(0);
            Assert.Throws<OverflowException>(() => s.EquipAt(0, C, "huge", EquipmentSlot.Ring, out _));
            Assert.That(s.Loadouts[C].Slots, Is.Empty); Assert.That(s.Quantity("huge"), Is.EqualTo(1));
            Assert.That(s.BankEquipmentInstances, Is.Empty);
        }

        [Test]
        public void SeededRandomnessIsPureAndWithinRange()
        {
            var roller = new SeededLootRoller(42);
            var values = Enumerable.Range(1, 100).Select(i => roller.Roll(C, "dev-test", i, "drop")).ToArray();
            Assert.That(values.All(v => v >= 0 && v < 1), Is.True);
            Assert.That(values, Is.EqualTo(Enumerable.Range(1, 100).Select(i => roller.Roll(C, "dev-test", i, "drop")).ToArray()));
            Assert.That(values.Distinct().Count(), Is.GreaterThan(1));
        }
    }
}
