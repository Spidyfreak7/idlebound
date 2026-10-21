using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Idlebound.Foundation.Tests
{
    public sealed class ContinuousProductionTests
    {
        private static long T(decimal seconds) => (long)(seconds * TimeSpan.TicksPerSecond);
        private static string Data(string id) => Resources.Load<TextAsset>("Idlebound/" + id).text;
        private static GatheringSession Empty()
        {
            var g = GatheringCatalog.LoadProduction(Data("ResourceTiers"),Data("GatheringXp"));
            return new GatheringSession(g,XpCurve.Load(Data("ProductionXpCurve"),false),ProductionCatalog.Load(Data("StarterProductionRecipes"),g.Resources));
        }
        private static GatheringSession Stock(string input,long quantity)
        {
            var d = Empty().Capture(); d.Bank = quantity == 0 ? Array.Empty<ItemAmount>() : new[] { new ItemAmount { Id = input, Quantity = quantity } };
            return GatheringSession.Restore(d,Empty());
        }
        private static void Start(GatheringSession s,string recipe,string character = "character-1",bool swap = false)
        { Assert.That(s.StartProductionAt(s.LastSettledTicks,character,recipe,swap,out var error),Is.True,error); }
        private static void Gather(GatheringSession s,string activity,string character = "character-1")
        { Assert.That(s.AssignAt(s.LastSettledTicks,character,activity,false,out var error),Is.True,error); }
        private static decimal Xp(GatheringSession s,string skill,string character = "character-1") => s.Characters.First(c => c.Id == character).Skills.First(k => k.SkillId == skill).StoredXp;

        [TestCase("bronze-dagger","metal-1",2,15,250)]
        [TestCase("wood-shortbow","wood-1",2,15,250)]
        [TestCase("cooked-shrimp","fish-1",1,5,100)]
        public void AllProductionSkillsRepeatAndCancelOnlyTheUnfinishedReservation(string recipe,string input,int cost,int seconds,int xp)
        {
            var s = Stock(input,9 * cost); var r = s.Production.Recipes.Single(x => x.Id == recipe); Start(s,recipe);
            s.SettleTo(T(3.5m * seconds));
            Assert.That(s.Quantity(r.OutputId),Is.EqualTo(3)); Assert.That(Xp(s,r.SkillId),Is.EqualTo(3 * xp));
            Assert.That(s.Quantity(input),Is.EqualTo(5 * cost)); Assert.That(s.PartialTicks("character-1"),Is.EqualTo(T(.5m * seconds)));
            s.UnassignAt(s.LastSettledTicks,"character-1"); Assert.That(s.Quantity(input),Is.EqualTo(6 * cost));
            s.UnassignAt(s.LastSettledTicks,"character-1"); Assert.That(s.Quantity(input),Is.EqualTo(6 * cost));
            Assert.That(s.Quantity(r.OutputId),Is.EqualTo(3)); Assert.That(s.ProductionJobs,Is.Empty);
        }
        [TestCase("bronze-dagger","metal-1",2,15)]
        [TestCase("wood-shortbow","wood-1",2,15)]
        [TestCase("cooked-shrimp","fish-1",1,5)]
        public void ExhaustionIsDurableAndNewMaterialsNeverAutomaticallyRestart(string recipe,string input,int cost,int seconds)
        {
            var s = Stock(input,3 * cost); Start(s,recipe); s.SettleTo(T(10 * seconds));
            var r = s.Production.Recipes.Single(x => x.Id == recipe);
            Assert.That(s.Quantity(r.OutputId),Is.EqualTo(3)); Assert.That(s.ProductionJobs,Is.Empty);
            Assert.That(s.Assignments["character-1"],Is.EqualTo(recipe)); Assert.That(s.PartialTicks("character-1"),Is.Zero);
            var d = s.Capture(); d.Bank = d.Bank.Concat(new[] { new ItemAmount { Id = input, Quantity = 2 * cost } }).ToArray();
            s = GatheringSession.Restore(d,Empty()); s.SettleTo(T(20 * seconds));
            Assert.That(s.Quantity(r.OutputId),Is.EqualTo(3)); Assert.That(s.Quantity(input),Is.EqualTo(2 * cost));
            Start(s,recipe); s.SettleTo(T(22 * seconds)); Assert.That(s.Quantity(r.OutputId),Is.EqualTo(5)); Assert.That(s.ProductionJobs,Is.Empty);
        }
        [TestCase("bronze-dagger","metal-1",2,15)]
        [TestCase("wood-shortbow","wood-1",2,15)]
        [TestCase("cooked-shrimp","fish-1",1,5)]
        public void DiskRestoreMidCycleAndOfflineMultipleCompletionsAreExactlyOnce(string recipe,string input,int cost,int seconds)
        {
            string directory = Path.Combine(Path.GetTempPath(),"Idlebound-continuous-" + Guid.NewGuid().ToString("N"));
            string path = Path.Combine(directory,"save.json"); long epoch = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc).Ticks;
            Func<LocalGame> open = () => new LocalGame(new AtomicLocalSaveStore(path),() => Stock(input,20 * cost),"continuous-test");
            try
            {
                var g = open(); g.Open(epoch); Start(g.Session,recipe); g.Session.SettleTo(T(seconds / 2m)); g.Save(epoch + T(seconds / 2m));
                var h = open(); h.Open(epoch + T(3.5m * seconds)); var r = h.Session.Production.Recipes.Single(x => x.Id == recipe);
                Assert.That(h.Session.Quantity(r.OutputId),Is.EqualTo(3)); Assert.That(h.Session.Quantity(input),Is.EqualTo(16 * cost));
                Assert.That(h.Session.PartialTicks("character-1"),Is.EqualTo(T(seconds / 2m))); Assert.That(h.Session.ProductionJobs.Count,Is.EqualTo(1));
                Assert.That(Xp(h.Session,r.SkillId),Is.Zero);
                var again = open(); again.Open(epoch + T(3.5m * seconds)); Assert.That(SaveCodec.Json(again.Session.Capture()),Is.EqualTo(SaveCodec.Json(h.Session.Capture())));
                again.Session.UnassignAt(again.Session.LastSettledTicks,"character-1"); Assert.That(again.Session.Quantity(input),Is.EqualTo(17 * cost));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory,true); }
        }
        [TestCase(false,true,0,1)][TestCase(false,false,0,0)][TestCase(true,true,0,0)]
        [TestCase(false,false,.9,.5)][TestCase(true,true,.9,.5)]
        public void RepeatedCyclesKeepActiveBackgroundAndOfflineXpRules(bool offline,bool selected,double effect,double expected)
        {
            var s = Stock("fish-1",10); Start(s,"cooked-shrimp");
            s.SetEffectsAt(0,"character-1",new[] { new BackgroundXpEffect("test-only","cooking",(decimal)effect) });
            if (!selected) s.SelectAt(0,"character-2"); s.SettleTo(T(15),offline);
            Assert.That(s.Quantity("cooked-shrimp-item"),Is.EqualTo(3)); Assert.That(Xp(s,"cooking"),Is.EqualTo(300m * (decimal)expected));
        }
        [Test] public void GatheringAndProductionShareStockChronologicallyAndPartitionInvariantly()
        {
            Func<GatheringSession> make = () => { var s = Stock("metal-1",2); Gather(s,"mining-1"); Start(s,"bronze-dagger","character-2"); return s; };
            var whole = make(); var split = make(); whole.SettleTo(T(31),true);
            for (int i = 1; i <= 31; i++) split.SettleTo(T(i),true);
            Assert.That(SaveCodec.Json(split.Capture()),Is.EqualTo(SaveCodec.Json(whole.Capture())));
            Assert.That(whole.Quantity("bronze-dagger-item"),Is.EqualTo(2)); Assert.That(whole.Quantity("metal-1"),Is.EqualTo(2));
            Assert.That(whole.LastOfflineReport.Characters.Single(c => c.Character == "character-1").Items.Single().Quantity,Is.EqualTo(6));
        }
        [Test] public void FutureGatheringCannotFundAnEarlierCycleAndStoppedJobsStayStopped()
        {
            Func<GatheringSession> make = () =>
            {
                var d = Stock("fish-10",1).Capture();
                foreach (var c in d.Characters) foreach (var skill in c.Skills.Where(k => k.Id == "fishing" || k.Id == "cooking"))
                { skill.Level = 10; skill.Xp = Empty().Curve.Thresholds[10]; }
                var s = GatheringSession.Restore(d,Empty()); Gather(s,"fishing-10"); Start(s,"cooked-sardine","character-2"); return s;
            };
            var whole = make(); var split = make(); whole.SettleTo(T(30),true);
            for (int i = 1; i <= 30; i++) split.SettleTo(T(i),true);
            Assert.That(SaveCodec.Json(split.Capture()),Is.EqualTo(SaveCodec.Json(whole.Capture())));
            Assert.That(whole.Quantity("cooked-sardine-item"),Is.EqualTo(1)); Assert.That(whole.Quantity("fish-10"),Is.EqualTo(3));
            Assert.That(whole.ProductionJobs,Is.Empty);
            Assert.That(whole.LastOfflineReport.Characters.Single(c => c.Character == "character-2").StopReason,Is.EqualTo("Out of materials"));
        }
        [Test] public void SimultaneousRecipesCompeteForStockInStableCharacterOrder()
        {
            var s = Stock("wood-1",6); Start(s,"wood-shortbow"); Start(s,"wood-wand","character-2"); s.SettleTo(T(15));
            Assert.That(s.ProductionJobs.Keys,Is.EquivalentTo(new[] { "character-1" })); s.SettleTo(T(30));
            Assert.That(s.Quantity("wood-shortbow-item"),Is.EqualTo(2)); Assert.That(s.Quantity("wood-wand-item"),Is.EqualTo(1)); Assert.That(s.Quantity("wood-1"),Is.Zero);
        }
        [Test] public void ExactCompletionImmediatelyReservesTheNextCycleAndCancelRefundsIt()
        {
            var s = Stock("metal-1",6); Start(s,"bronze-dagger"); s.SettleTo(T(15));
            Assert.That(s.Quantity("metal-1"),Is.EqualTo(2)); Assert.That(s.PartialTicks("character-1"),Is.Zero);
            s.SettleTo(T(15)); Assert.That(s.Quantity("bronze-dagger-item"),Is.EqualTo(1));
            s.UnassignAt(T(15),"character-1"); Assert.That(s.Quantity("metal-1"),Is.EqualTo(4));
        }
        [Test] public void SwappingAnAutomaticReservationStillRequiresExplicitDisplacement()
        {
            var s = Stock("metal-1",8); Start(s,"bronze-dagger"); s.SettleTo(T(15));
            string before = SaveCodec.Json(s.Capture());
            Assert.That(s.StartProductionAt(T(15),"character-2","bronze-dagger",false,out _),Is.False);
            Assert.That(SaveCodec.Json(s.Capture()),Is.EqualTo(before)); Start(s,"bronze-dagger","character-2",true);
            Assert.That(s.Assignments.ContainsKey("character-1"),Is.False); Assert.That(s.Quantity("metal-1"),Is.EqualTo(4));
            s.SettleTo(T(30)); s.UnassignAt(T(30),"character-2"); Assert.That(s.Quantity("metal-1"),Is.EqualTo(4)); Assert.That(s.Quantity("bronze-dagger-item"),Is.EqualTo(2));
        }
        [Test] public void ContinuousOfflineCookingRespectsFourHourCapWithoutDoubleReservations()
        {
            var s = Stock("fish-1",10000); Start(s,"cooked-shrimp"); s.SettleTo(T(36000),true);
            Assert.That(s.Quantity("cooked-shrimp-item"),Is.EqualTo(2880)); Assert.That(s.Quantity("fish-1"),Is.EqualTo(7119));
            Assert.That(s.ProductionJobs.Count,Is.EqualTo(1)); Assert.That(Xp(s,"cooking"),Is.Zero);
            string before = SaveCodec.Json(s.Capture()); s.SettleTo(T(36000),true); Assert.That(SaveCodec.Json(s.Capture()),Is.EqualTo(before));
            Assert.That(s.LastOfflineReport.EligibleTicks,Is.EqualTo(GatheringSession.BaseOfflineTicks));
        }
    }
}
