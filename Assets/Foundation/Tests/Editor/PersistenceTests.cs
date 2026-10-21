using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Idlebound.Foundation.Tests
{
    public sealed class PersistenceTests
    {
        private string directory, path;
        private static readonly long Epoch = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc).Ticks;
        private static long T(long seconds) => checked(seconds * TimeSpan.TicksPerSecond);
        private static string Data(string name) => Resources.Load<TextAsset>("Idlebound/" + name).text;
        private static GatheringSession Factory()
        {
            var g = GatheringCatalog.LoadProduction(Data("ResourceTiers"),Data("GatheringXp"));
            return new GatheringSession(g,XpCurve.Load(Data("ProductionXpCurve"),false),ProductionCatalog.Load(Data("StarterProductionRecipes"),g.Resources),true,
                EquipmentCatalog.LoadDevelopment(Data("DevelopmentGear")),CombatCatalog.LoadDevelopment(Data("DevelopmentCombatRules"),Data("DevelopmentMonsters"),Data("DevelopmentLoot")));
        }
        [SetUp] public void Setup() { directory = Path.Combine(Path.GetTempPath(),"Idlebound-test-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory); path = Path.Combine(directory,"player.json"); }
        [TearDown] public void Cleanup() { Directory.Delete(directory,true); }
        private LocalGame Open(long? utc = null, ILocalSaveStore store = null)
        { var game = new LocalGame(store ?? new AtomicLocalSaveStore(path),Factory,"catalog-v1"); game.Open(utc ?? Epoch); return game; }
        private static void Assign(GatheringSession s, string id = "character-1", string activity = "mining-1")
        { Assert.That(s.AssignAt(s.LastSettledTicks,id,activity,false,out var error),Is.True,error); }
        private static void Produce(GatheringSession s, string id = "character-1")
        { Assert.That(s.StartProductionAt(s.LastSettledTicks,id,"bronze-dagger",false,out var error),Is.True,error); }
        private static decimal Xp(GatheringSession s, string id = "character-1", string skill = "mining") => s.Characters.First(c => c.Id == id).Skills.First(k => k.SkillId == skill).StoredXp;
        private static void Equip(GatheringSession s,string id)
        { Assert.That(s.EquipAt(s.LastSettledTicks,"character-1",id,s.Equipment.Definitions.First(d => d.Id == id).Slot,out var error),Is.True,error); }

        [Test] public void FirstLaunchWritesVersionedStarterAndBackup()
        {
            var game = Open(); Assert.That(game.Session.Characters.Count,Is.EqualTo(3)); Assert.That(game.Session.Inventory,Is.Empty);
            Assert.That(File.Exists(path),Is.True); Assert.That(File.Exists(path + ".bak"),Is.True);
            Assert.That(SaveCodec.Decode(File.ReadAllText(path)).UtcTicks,Is.EqualTo(Epoch));
        }
        [Test] public void SaveDoesNotSettleOrMutateLiveGameplay()
        {
            var g = Open(); Assign(g.Session); g.Session.SettleTo(T(2)); string before = SaveCodec.Json(g.Session.Capture());
            g.Save(Epoch + T(2)); Assert.That(SaveCodec.Json(g.Session.Capture()),Is.EqualTo(before));
        }
        [Test] public void ExactDecimalXpAllNineSkillsAndAllCharactersRoundTrip()
        {
            var snapshot = Factory().Capture(); snapshot.Slots = 9;
            snapshot.Characters = Enumerable.Range(1,9).Select(i =>
            {
                var c = SaveCodec.Copy(snapshot.Characters[0]); c.Id = "character-" + i; c.Name = "Saved " + i;
                foreach (var skill in c.Skills) { skill.Xp = 595.1234567890123456789m; skill.Level = 2; } return c;
            }).ToArray(); snapshot.Active = "character-9";
            var restored = GatheringSession.Restore(SaveCodec.Copy(snapshot),Factory());
            Assert.That(restored.Characters.Count,Is.EqualTo(9)); Assert.That(restored.ActiveCharacterId,Is.EqualTo("character-9"));
            Assert.That(SaveCodec.Json(restored.Capture()),Is.EqualTo(SaveCodec.Json(snapshot)));
        }
        [Test] public void GatheringPartialAssignmentsSelectionAndStockSurviveColdLoad()
        {
            var g = Open(); Assign(g.Session); g.Session.SettleTo(T(7)); g.Session.SelectAt(T(7),"character-2"); g.Save(Epoch + T(7));
            var h = Open(Epoch + T(7)); Assert.That(h.Session.ActiveCharacterId,Is.EqualTo("character-2"));
            Assert.That(h.Session.Quantity("metal-1"),Is.EqualTo(1)); Assert.That(h.Session.PartialTicks("character-1"),Is.EqualTo(T(2)));
            Assert.That(h.Session.Assignments["character-1"],Is.EqualTo("mining-1")); Assert.That(Xp(h.Session),Is.EqualTo(56));
        }
        [Test] public void ReservedProductionInputsAndProgressSurviveAndCancelRefundsOnce()
        {
            var g = Open(); Assign(g.Session); g.Session.SettleTo(T(10)); Produce(g.Session); g.Session.SettleTo(T(17)); g.Save(Epoch + T(17));
            var h = Open(Epoch + T(17)); Assert.That(h.Session.Quantity("metal-1"),Is.Zero);
            Assert.That(h.Session.PartialTicks("character-1"),Is.EqualTo(T(7))); Assert.That(h.Session.ProductionJobs.Count,Is.EqualTo(1));
            Assert.That(h.Session.UnassignAt(T(17),"character-1"),Is.True); Assert.That(h.Session.UnassignAt(T(17),"character-1"),Is.False);
            Assert.That(h.Session.Quantity("metal-1"),Is.EqualTo(2)); Assert.That(h.Session.Quantity("bronze-dagger-item"),Is.Zero);
        }
        [Test] public void EquipmentBankInstancesAndSerialSurviveWithoutDuplication()
        {
            var g = Open(); g.Session.GrantDevelopmentGearKitAt(0); Equip(g.Session,"bronze-dagger-item");
            string id = g.Session.Loadouts["character-1"].Slots[EquipmentSlot.Mainhand].InstanceId;
            g.Session.UnequipAt(0,"character-1",EquipmentSlot.Mainhand,out _); g.Save(Epoch);
            var h = Open(); Assert.That(h.Session.BankEquipmentInstances.ContainsKey(id),Is.True); Equip(h.Session,"bronze-dagger-item");
            Assert.That(h.Session.Loadouts["character-1"].Slots[EquipmentSlot.Mainhand].InstanceId,Is.EqualTo(id));
            Assert.That(h.Session.Quantity("bronze-dagger-item"),Is.Zero); Assert.That(h.Session.BankEquipmentInstances,Is.Empty);
        }
        [Test] public void CombatHpEnemyPartialClocksKillsAndSeedRoundTrip()
        {
            var s = Factory(); s.GrantDevelopmentGearKitAt(0); Equip(s,"bronze-dagger-item");
            Assert.That(s.StartCombatAt(0,"character-1","dev-chicken",CombatStyle.Melee,false,out _),Is.True);
            s.SettleTo(T(7)); var data = s.Capture(); data.LootSeed = 123456789;
            var restored = GatheringSession.Restore(SaveCodec.Copy(data),Factory());
            Assert.That(SaveCodec.Json(restored.Capture()),Is.EqualTo(SaveCodec.Json(data)));
            s = GatheringSession.Restore(data,Factory()); s.SettleTo(T(40),true);
            restored.SettleTo(T(20),true); restored = GatheringSession.Restore(restored.Capture(),Factory()); restored.SettleTo(T(40),true);
            Assert.That(SaveCodec.Json(restored.Capture()),Is.EqualTo(SaveCodec.Json(s.Capture())));
        }
        [TestCase("active")][TestCase("slots")][TestCase("xp")][TestCase("level")][TestCase("skill")]
        [TestCase("item")][TestCase("quantity")][TestCase("character")][TestCase("assignment")][TestCase("partial")]
        [TestCase("job")][TestCase("hp")][TestCase("encounter")][TestCase("clock")][TestCase("instance")]
        [TestCase("effect")][TestCase("kills")][TestCase("missing")][TestCase("policy")]
        [TestCase("assignment-duplicate")][TestCase("bank-duplicate")][TestCase("instance-duplicate")]
        public void InvalidSnapshotsAreRejectedWithoutChangingCatalogTemplate(string invalid)
        {
            var template = Factory(); var d = template.Capture(); string original = SaveCodec.Json(template.Capture());
            switch (invalid)
            {
                case "assignment-duplicate": d.Characters[0].Assignment = d.Characters[1].Assignment = "mining-1"; break;
                case "bank-duplicate": d.Bank = new[] { new ItemAmount { Id = "metal-1", Quantity = 1 }, new ItemAmount { Id = "metal-1", Quantity = 1 } }; break;
                case "instance-duplicate": d.EquipmentSerial = 1; d.Bank = new[] { new ItemAmount { Id = "bronze-dagger-item", Quantity = 2 } }; d.Equipment = new[] { new SavedEquipment { Instance = "gear-0000000001", Item = "bronze-dagger-item" }, new SavedEquipment { Instance = "gear-0000000001", Item = "bronze-dagger-item" } }; break;
                case "active": d.Active = "missing"; break;
                case "slots": d.Slots = 10; break;
                case "xp": d.Characters[0].Skills[0].Xp = -1; break;
                case "level": d.Characters[0].Skills[0].Level = 99; break;
                case "skill": d.Characters[0].Skills[0].Id = "missing"; break;
                case "item": d.Bank = new[] { new ItemAmount { Id = "missing", Quantity = 1 } }; break;
                case "quantity": d.Bank = new[] { new ItemAmount { Id = "metal-1", Quantity = -1 } }; break;
                case "character": d.Characters[1].Id = d.Characters[0].Id; break;
                case "assignment": d.Characters[0].Assignment = "missing"; break;
                case "partial": d.Characters[0].Partial = 1; break;
                case "job": d.Characters[0].Job = "bronze-dagger"; break;
                case "hp": d.Characters[0].Hp = 999; break;
                case "encounter": d.Characters[0].Encounter = new SavedEncounter { Monster = "dev-chicken", Hp = 12, PlayerTicks = 1, EnemyTicks = 1 }; break;
                case "clock": d.Ticks = -1; break;
                case "instance": d.Equipment = new[] { new SavedEquipment { Instance = "gear-0000000001", Item = "bronze-dagger-item" } }; break;
                case "effect": d.Characters[0].Effects = new[] { new SavedEffect { Source = "x", Skill = "missing", Efficiency = .5m } }; break;
                case "kills": d.Kills = new[] { new SavedKills { Character = "missing", Monster = "dev-chicken", Count = 1 } }; break;
                case "missing": d.Characters = null; break;
                case "policy": d.RefundInputs = false; break;
            }
            Assert.Throws<ArgumentException>(() => GatheringSession.Restore(d,template));
            Assert.That(SaveCodec.Json(template.Capture()),Is.EqualTo(original));
        }
        [Test] public void CatalogChangesBlockLoadAndPreserveBothFiles()
        {
            Open(); string primary = File.ReadAllText(path), backup = File.ReadAllText(path + ".bak");
            var g = new LocalGame(new AtomicLocalSaveStore(path),Factory,"changed-catalog"); Assert.Throws<SaveCompatibilityException>(() => g.Open(Epoch));
            Assert.That(g.Session,Is.Null); Assert.That(File.ReadAllText(path),Is.EqualTo(primary)); Assert.That(File.ReadAllText(path + ".bak"),Is.EqualTo(backup));
        }
        [Test] public void FutureVersionIsNotDowngradedOrReplacedWithBackup()
        {
            Open(); string future = File.ReadAllText(path).Replace("\"Version\":1","\"Version\":99"); File.WriteAllText(path,future);
            Assert.Throws<SaveCompatibilityException>(() => Open()); Assert.That(File.ReadAllText(path),Is.EqualTo(future));
        }
        [Test] public void ChecksumDetectsChangedPayload()
        {
            var g = Open(); string json = File.ReadAllText(path).Replace("catalog-v1","catalog-xx");
            Assert.Throws<System.Runtime.Serialization.SerializationException>(() => SaveCodec.Decode(json));
        }
        [TestCase("corrupt original")][TestCase("{}")][TestCase("null")] public void CorruptionRecoversBackupAndPreservesOriginalEvidence(string corrupt)
        {
            var g = Open(); Assign(g.Session); g.Session.SettleTo(T(5)); g.Save(Epoch + T(5)); File.WriteAllText(path,corrupt);
            var h = Open(Epoch + T(5)); Assert.That(h.Session.Inventory,Is.Empty,"The backup predates gathering, not fictitious fresh rewards.");
            Assert.That(Directory.GetFiles(directory,"*.corrupt-*").Length,Is.EqualTo(1));
            Assert.That(File.ReadAllText(Directory.GetFiles(directory,"*.corrupt-*")[0]),Is.EqualTo(corrupt));
        }
        [Test] public void InvalidSaveWithoutBackupNeverCreatesStarterOrOverwritesFile()
        {
            File.WriteAllText(path,"bad"); var g = new LocalGame(new AtomicLocalSaveStore(path),Factory,"catalog-v1");
            Assert.Throws<System.Runtime.Serialization.SerializationException>(() => g.Open(Epoch)); Assert.That(g.Session,Is.Null); Assert.That(File.ReadAllText(path),Is.EqualTo("bad"));
        }
        [Test] public void InterruptedWriteLeavesPreviousPrimaryAndBackupUsable()
        {
            Open(); string primary = File.ReadAllText(path), backup = File.ReadAllText(path + ".bak");
            var store = new AtomicLocalSaveStore(path,() => throw new IOException("interrupted before replacement"));
            Assert.Throws<IOException>(() => store.Write(SaveCodec.Decode(primary)));
            Assert.That(File.ReadAllText(path),Is.EqualTo(primary)); Assert.That(File.ReadAllText(path + ".bak"),Is.EqualTo(backup));
            Assert.That(Open().Session.Characters.Count,Is.EqualTo(3));
        }
        [Test] public void OrphanedFirstWriteDoesNotSilentlyCreateStarter()
        { File.WriteAllText(path + ".tmp","interrupted"); Assert.Throws<System.Runtime.Serialization.SerializationException>(() => Open()); }
        [TestCase(0,0)][TestCase(299,59)][TestCase(300,60)][TestCase(2700,540)][TestCase(14400,2880)][TestCase(36000,2880)][TestCase(int.MaxValue,2880)]
        public void OfflineDurationsUseRealTimeAndOneFourHourCap(int seconds,int resources)
        {
            var g = Open(); Assign(g.Session); g.Save(Epoch);
            var h = Open(Epoch + T(seconds)); Assert.That(h.Session.Quantity("metal-1"),Is.EqualTo(resources)); Assert.That(Xp(h.Session),Is.Zero);
            Assert.That(h.Session.LastSettledTicks,Is.EqualTo(T(seconds)));
            var again = Open(Epoch + T(seconds)); Assert.That(again.Session.Quantity("metal-1"),Is.EqualTo(resources));
            if (seconds >= 300) { Assert.That(h.Pending.EligibleTicks,Is.EqualTo(Math.Min(T(seconds),GatheringSession.BaseOfflineTicks))); Assert.That(h.Pending.Capped,Is.EqualTo(seconds > 14400)); }
            else Assert.That(h.Pending,Is.Null);
        }
        [Test] public void RollbackAwardsNothingAndRetainsHighWaterBoundary()
        {
            var g = Open(); Assign(g.Session); g.Save(Epoch);
            var h = Open(Epoch - T(300)); Assert.That(h.Session.Inventory,Is.Empty); Assert.That(h.BoundaryUtcTicks,Is.EqualTo(Epoch));
            h.Resume(Epoch + T(5)); Assert.That(h.Session.Quantity("metal-1"),Is.EqualTo(1)); h.Resume(Epoch + T(5)); Assert.That(h.Session.Quantity("metal-1"),Is.EqualTo(1));
        }
        [TestCase(0,0)][TestCase(.25,14)][TestCase(.75,28)]
        public void SavedEffectsApplyOfflineBackgroundRulesAndFiftyPercentCap(double effect,int xpPerCycle)
        {
            var g = Open(); Assign(g.Session); g.Session.SetEffectsAt(0,"character-1",new[] { new BackgroundXpEffect("test-effect","mining",(decimal)effect) }); g.Save(Epoch);
            var h = Open(Epoch + T(300)); Assert.That(Xp(h.Session),Is.EqualTo(60 * xpPerCycle)); Assert.That(h.Session.Quantity("metal-1"),Is.EqualTo(60));
        }
        [Test] public void SelectedAndOtherAssignedCharactersHaveZeroBaselineOfflineXp()
        {
            var g = Open(); Assign(g.Session); Assign(g.Session,"character-2","woodcutting-1"); g.Save(Epoch);
            var h = Open(Epoch + T(300)); Assert.That(Xp(h.Session),Is.Zero); Assert.That(Xp(h.Session,"character-2","woodcutting"),Is.Zero);
            Assert.That(h.Session.Quantity("metal-1"),Is.EqualTo(60)); Assert.That(h.Session.Quantity("wood-1"),Is.EqualTo(60));
        }
        [Test] public void OfflineProductionRemainsOneJobAndNeverRefundsCompletedInputs()
        {
            var g = Open(); Assign(g.Session); g.Session.SettleTo(T(20)); Produce(g.Session); g.Session.SettleTo(T(27)); g.Save(Epoch + T(27));
            var h = Open(Epoch + T(1827)); Assert.That(h.Session.Quantity("bronze-dagger-item"),Is.EqualTo(1)); Assert.That(h.Session.Quantity("metal-1"),Is.EqualTo(2));
            Assert.That(h.Session.ProductionJobs,Is.Empty); Assert.That(Xp(h.Session,skill:"smithing"),Is.Zero);
            h.Session.UnassignAt(h.Session.LastSettledTicks,"character-1"); Assert.That(h.Session.Quantity("metal-1"),Is.EqualTo(2));
            Assert.That(h.Pending.Characters.Single(c => c.Character == "character-1").StopReason,Is.EqualTo("Job completed"));
        }
        [Test] public void PerCharacterReceiptsExcludeInitialStockAndReservations()
        {
            var g = Open(); Assign(g.Session); g.Session.SettleTo(T(20)); Produce(g.Session,"character-2"); g.Save(Epoch + T(20));
            var h = Open(Epoch + T(320)); var report = h.Pending;
            Assert.That(report.Characters.Single(c => c.Character == "character-1").Items.Single().Quantity,Is.EqualTo(60));
            Assert.That(report.Characters.Single(c => c.Character == "character-2").Items.Single().Id,Is.EqualTo("bronze-dagger-item"));
            Assert.That(h.Session.Quantity("metal-1"),Is.EqualTo(62)); Assert.That(report.Characters.SelectMany(c => c.Xp),Is.Empty);
        }
        [Test] public void CombatOfflineLootHpDeathAndNoDuplicateReplay()
        {
            var g = Open(); Assert.That(g.Session.StartCombatAt(0,"character-1","dev-chicken",CombatStyle.Melee,false,out _),Is.True); g.Save(Epoch);
            var h = Open(Epoch + T(1800)); Assert.That(h.Session.Vitals["character-1"].Hp,Is.Zero); Assert.That(h.Session.Assignments.ContainsKey("character-1"),Is.False);
            Assert.That(h.Session.KillsFor("character-1","dev-chicken"),Is.GreaterThan(0)); Assert.That(Xp(h.Session,skill:"melee"),Is.Zero);
            var outcome = h.Pending.Characters.Single(c => c.Character == "character-1"); Assert.That(outcome.StopReason,Is.EqualTo("Defeated"));
            foreach (var item in outcome.Items) Assert.That(h.Session.Quantity(item.Id),Is.EqualTo(item.Quantity));
            string captured = SaveCodec.Json(h.Session.Capture()); h.Resume(Epoch + T(1800)); Assert.That(SaveCodec.Json(h.Session.Capture()),Is.EqualTo(captured));
        }
        [Test] public void FourHourCombatUsesEventEngineNotFrameReplay()
        {
            var g = Open(); g.Session.GrantDevelopmentGearKitAt(0); Equip(g.Session,"bronze-dagger-item"); Equip(g.Session,"dev-practice-hood");
            g.Session.StartCombatAt(0,"character-1","dev-chicken",CombatStyle.Melee,false,out _); g.Save(Epoch);
            var h = Open(Epoch + T(36000)); Assert.That(h.Session.Vitals["character-1"].Hp,Is.EqualTo(50));
            Assert.That(h.Session.KillsFor("character-1","dev-chicken"),Is.EqualTo(2400)); Assert.That(h.Pending.Capped,Is.True);
        }
        [Test] public void WelcomePersistsUntilAcknowledgedAndDoesNotReplayAfterRestart()
        {
            var g = Open(); Assign(g.Session); g.Save(Epoch); var h = Open(Epoch + T(300)); string id = h.Pending.Id;
            var again = Open(Epoch + T(300)); Assert.That(again.Pending.Id,Is.EqualTo(id)); Assert.That(again.Session.Quantity("metal-1"),Is.EqualTo(60));
            again.Acknowledge(id); Assert.That(again.Pending,Is.Null); Assert.That(Open(Epoch + T(300)).Pending,Is.Null);
        }
        [Test] public void UnacknowledgedIntervalsMergeWithoutInventingRewards()
        {
            var g = Open(); Assign(g.Session); g.Save(Epoch); var h = Open(Epoch + T(300)); string id = h.Pending.Id;
            h.Resume(Epoch + T(305)); Assert.That(h.Pending.Id,Is.EqualTo(id)); Assert.That(h.Pending.Characters.First().Items.Single().Quantity,Is.EqualTo(61));
            Assert.That(h.Pending.EligibleTicks,Is.EqualTo(T(305))); Assert.That(h.Pending.AwayTicks,Is.EqualTo(T(305)));
        }
        [Test] public void PendingReportCopiesCannotBeMutatedByUi()
        {
            var g = Open(); Assign(g.Session); g.Save(Epoch); var h = Open(Epoch + T(300)); var copy = h.Pending; copy.Characters.Clear();
            Assert.That(h.Pending.Characters.Count,Is.EqualTo(3));
        }
        [Test] public void PublicSettlementReceiptCannotMutateTheDurablePendingReport()
        {
            var g = Open(); Assign(g.Session); g.Save(Epoch); g.Resume(Epoch + T(300));
            g.Session.LastOfflineReport.Characters.Clear(); Assert.That(g.Pending.Characters.Count,Is.EqualTo(3));
            g.Save(Epoch + T(300)); Assert.That(Open(Epoch + T(300)).Pending.Characters.Count,Is.EqualTo(3));
        }
        [Test] public void ReportContainsActualXpAfterTenMillionSaturation()
        {
            var s = Factory().Capture(); s.Characters[0].Skills[0].Xp = 9999999; s.Characters[0].Skills[0].Level = 98;
            var g = GatheringSession.Restore(s,Factory()); Assign(g); g.SetEffectsAt(0,"character-1",new[] { new BackgroundXpEffect("test","mining",.5m) }); g.SettleTo(T(5),true);
            Assert.That(g.LastOfflineReport.Characters.First().Xp.Single().Quantity,Is.EqualTo(1));
        }
        private sealed class FailingStore : ILocalSaveStore
        {
            private readonly AtomicLocalSaveStore inner;
            public bool Fail;
            public FailingStore(string path) { inner = new AtomicLocalSaveStore(path); }
            public SaveDocument Load(Action<SaveDocument> validate) => inner.Load(validate);
            public void Write(SaveDocument data) { if (Fail) throw new IOException("injected commit failure"); inner.Write(data); }
        }
        [Test] public void FailedOfflineCommitPreservesAuthoritativeSessionAndCanRetryOnce()
        {
            var store = new FailingStore(path); var g = Open(store:store); Assign(g.Session); g.Save(Epoch);
            var original = g.Session; string disk = File.ReadAllText(path); store.Fail = true;
            Assert.Throws<IOException>(() => g.Resume(Epoch + T(300)));
            Assert.That(g.Session,Is.SameAs(original)); Assert.That(g.Session.Inventory,Is.Empty); Assert.That(File.ReadAllText(path),Is.EqualTo(disk));
            store.Fail = false; g.Resume(Epoch + T(300)); Assert.That(g.Session.Quantity("metal-1"),Is.EqualTo(60));
            g.Resume(Epoch + T(300)); Assert.That(g.Session.Quantity("metal-1"),Is.EqualTo(60));
        }
        [Test] public void FailedAcknowledgmentRetainsPendingReportOnDiskAndInMemory()
        {
            var store = new FailingStore(path); var g = Open(store:store); Assign(g.Session); g.Save(Epoch); g.Resume(Epoch + T(300));
            string id = g.Pending.Id; store.Fail = true; Assert.Throws<IOException>(() => g.Acknowledge(id)); Assert.That(g.Pending.Id,Is.EqualTo(id));
            Assert.That(Open(Epoch + T(300)).Pending.Id,Is.EqualTo(id));
            store.Fail = false; g.Acknowledge(id); Assert.That(Open(Epoch + T(300)).Pending,Is.Null);
        }
        [Test] public void PartialGatheringCompletesAfterOnlyItsRemainingTime()
        {
            var g = Open(); Assign(g.Session); g.Session.SettleTo(T(2)); g.Save(Epoch + T(2));
            var h = Open(Epoch + T(5)); Assert.That(h.Session.Quantity("metal-1"),Is.EqualTo(1)); Assert.That(h.Session.PartialTicks("character-1"),Is.Zero);
            Assert.That(Xp(h.Session),Is.Zero); Assert.That(Open(Epoch + T(5)).Session.Quantity("metal-1"),Is.EqualTo(1));
        }
        [Test] public void PartialOfflineProductionKeepsReservationUntilExactlyCompleted()
        {
            var g = Open(); Assign(g.Session); g.Session.SettleTo(T(10)); Produce(g.Session); g.Session.SettleTo(T(14)); g.Save(Epoch + T(14));
            var h = Open(Epoch + T(19)); Assert.That(h.Session.PartialTicks("character-1"),Is.EqualTo(T(9))); Assert.That(h.Session.ProductionJobs.Count,Is.EqualTo(1));
            Assert.That(h.Session.Quantity("metal-1"),Is.Zero); Assert.That(h.Session.Quantity("bronze-dagger-item"),Is.Zero);
            var again = Open(Epoch + T(25)); Assert.That(again.Session.ProductionJobs,Is.Empty); Assert.That(again.Session.Quantity("bronze-dagger-item"),Is.EqualTo(1));
            Assert.That(again.Session.Quantity("metal-1"),Is.Zero);
        }
        [Test] public void FutureBackupAlsoBlocksDowngradeWrites()
        {
            Open(); string primary = File.ReadAllText(path); string future = File.ReadAllText(path + ".bak").Replace("\"Version\":1","\"Version\":99");
            File.WriteAllText(path + ".bak",future);
            Assert.Throws<SaveCompatibilityException>(() => new AtomicLocalSaveStore(path).Write(SaveCodec.Decode(primary)));
            Assert.That(File.ReadAllText(path),Is.EqualTo(primary)); Assert.That(File.ReadAllText(path + ".bak"),Is.EqualTo(future));
        }
        [Test] public void AcknowledgmentCannotCheckpointNewStateWithAnOldUtcBoundary()
        {
            var g = Open(); Assign(g.Session); g.Save(Epoch); g.Resume(Epoch + T(300)); string id = g.Pending.Id;
            g.Session.SettleTo(T(305)); Assert.Throws<InvalidOperationException>(() => g.Acknowledge(id));
            g.Save(Epoch + T(305)); g.Acknowledge(id);
            var h = Open(Epoch + T(305)); Assert.That(h.Session.Quantity("metal-1"),Is.EqualTo(61)); Assert.That(h.Pending,Is.Null);
        }
        [Test] public void RuntimeAdoptionDoesNotSettleTwiceAndKeepsOneOwner()
        {
            var g = Open(); Assign(g.Session); g.Save(Epoch); object owner = new object(); var runtime = new SessionRuntime(g.Session,owner);
            runtime.SetPresence(owner,0,true,false); g.Resume(Epoch + T(300)); runtime.AdoptRestored(owner,g.Session,300,false);
            runtime.Tick(owner,300); Assert.That(runtime.Session.Quantity("metal-1"),Is.EqualTo(60));
            Assert.Throws<InvalidOperationException>(() => new SessionRuntime(g.Session,new object()));
            Assert.Throws<InvalidOperationException>(() => runtime.AdoptRestored(new object(),Factory(),301,false));
            var model = new GameUiModel(runtime); model.Inspect("character-1"); model.ShowSkill("mining"); model.Navigate(MainScreen.Bank); model.ReturnFromBank();
            Assert.That(runtime.Session.Quantity("metal-1"),Is.EqualTo(60)); Assert.That(runtime.Session.LastSettledTicks,Is.EqualTo(T(300)));
        }
    }
}
