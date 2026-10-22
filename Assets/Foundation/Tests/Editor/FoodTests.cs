using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
namespace Idlebound.Foundation.Tests
{
    public sealed class FoodTests
    {
        private static string Data(string n)=>Resources.Load<TextAsset>("Idlebound/"+n).text;
        private static long T(long n)=>n*TimeSpan.TicksPerSecond;
        private static long Used(OfflineReport report)
        {long total=0;foreach(var c in report.Characters)if(c.Consumed!=null)foreach(var i in c.Consumed)total+=i.Quantity;return total;}
        private static readonly long Epoch=new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc).Ticks;
        public static GatheringSession Factory(bool food=true,bool enchant=true)
        {
            var g=GatheringCatalog.LoadProduction(Data("ResourceTiers"),Data("GatheringXp"));var p=ProductionCatalog.Load(Data("StarterProductionRecipes"),g.Resources);
            return new GatheringSession(g,XpCurve.Load(Data("ProductionXpCurve"),false),p,true,EquipmentCatalog.LoadDevelopment(Data("DevelopmentGear")),CombatCatalog.LoadDevelopment(Data("DevelopmentCombatRules"),Data("DevelopmentMonsters"),Data("DevelopmentLoot")),enchantments:enchant?EnchantmentCatalog.Load(Data("Enchantments"),Data("EnchantmentRules"),g.Resources):null,foods:food?FoodCatalog.Load(Data("Foods"),p):null);
        }
        private static GatheringSession Stock(decimal hp=20,long shrimp=10,long sardine=10,long raw=0)
        {
            var s=Factory();var d=s.Capture();foreach(var c in d.Characters)c.Hp=hp;
            d.Bank=new[]{new ItemAmount{Id="cooked-shrimp-item",Quantity=shrimp},new ItemAmount{Id="cooked-sardine-item",Quantity=sardine},new ItemAmount{Id="fish-1",Quantity=raw}}.Where(i=>i.Quantity>0).ToArray();
            return GatheringSession.Restore(d,s);
        }
        private static void Start(GatheringSession s,string character="character-1",string monster="dev-chicken")=>Assert.That(s.StartCombatAt(s.LastSettledTicks,character,monster,CombatStyle.Melee,false,out var error),Is.True,error);
        private static void Config(GatheringSession s,bool enabled=true,int threshold=50,string preferred="auto",string c="character-1")=>Assert.That(s.ConfigureFoodAt(s.LastSettledTicks,c,enabled,threshold,preferred,out var error),Is.True,error);
        [Test] public void OnlyExistingCookingProductsHaveExactProvisionalHealing()
        {
            var s=Factory();Assert.That(s.Foods.Definitions.Count,Is.EqualTo(2));Assert.That(s.Foods.Definitions.Single(f=>f.ItemId=="cooked-shrimp-item").Healing,Is.EqualTo(8));Assert.That(s.Foods.Definitions.Single(f=>f.ItemId=="cooked-sardine-item").Healing,Is.EqualTo(12));
        }
        [TestCase("duplicate")][TestCase("unknown")][TestCase("zero")][TestCase("negative")][TestCase("raw")][TestCase("invalid-id")]
        public void CatalogRejectsInvalidFood(string kind)
        {
            var s=Factory();Assert.That(()=>{var f=new FoodDefinition(kind=="unknown"?"absent":kind=="raw"?"fish-1":kind=="invalid-id"?"BAD ID":"cooked-shrimp-item",kind=="zero"?0:kind=="negative"?-1:8);new FoodCatalog(kind=="duplicate"?new[]{f,f}:new[]{f},s.Production);},Throws.ArgumentException);
        }
        [TestCase(10)][TestCase(20)][TestCase(30)][TestCase(40)][TestCase(50)][TestCase(60)][TestCase(70)][TestCase(80)][TestCase(90)]
        public void AllThresholdsAndSettingsAreIndependentAndPersist(int threshold)
        {
            var s=Stock();Assert.That(s.FoodFor("character-1").Enabled,Is.True);Assert.That(s.FoodFor("character-1").Threshold,Is.EqualTo(50));Assert.That(s.FoodFor("character-1").Preferred,Is.EqualTo("auto"));Config(s,false,threshold,"cooked-sardine-item");s=GatheringSession.Restore(s.Capture(),s);Assert.That(s.FoodFor("character-1").Threshold,Is.EqualTo(threshold));Assert.That(s.FoodFor("character-1").Enabled,Is.True);Assert.That(s.FoodFor("character-2").Threshold,Is.EqualTo(50));
        }
        [TestCase(0)][TestCase(5)][TestCase(15)][TestCase(100)] public void InvalidThresholdRejectedBeforeMutation(int threshold)
        {var s=Stock();string before=SaveCodec.Json(s.Capture());Assert.Throws<ArgumentException>(()=>s.ConfigureFoodAt(0,"character-1",true,threshold,"auto",out _));Assert.That(SaveCodec.Json(s.Capture()),Is.EqualTo(before));}
        [TestCase(42,"cooked-shrimp-item",50)][TestCase(38,"cooked-sardine-item",50)][TestCase(30,"cooked-sardine-item",42)][TestCase(44,"cooked-shrimp-item",50)]
        public void AutoSelectsLeastAdequateOrStrongestAndClamps(int hp,string item,int expected)
        {
            var s=Stock(hp);Assert.That(s.EatAt(0,"character-1",out _),Is.True);Assert.That(s.Vitals["character-1"].Hp,Is.EqualTo(expected));Assert.That(s.Quantity(item),Is.EqualTo(9));Assert.That(s.Characters[0].Skills.Sum(k=>k.StoredXp),Is.Zero);
        }
        [Test] public void ManualEatingWorksOutsideAndInsideCombatAndSameTimestampIsIdempotent()
        {
            var s=Stock(10);Config(s,true,10,"cooked-shrimp-item");Assert.That(s.EatAt(0,"character-1",out _),Is.True);Assert.That(s.Vitals["character-1"].Hp,Is.EqualTo(18));Assert.That(s.EatAt(0,"character-1",out _),Is.False);Assert.That(s.Quantity("cooked-shrimp-item"),Is.EqualTo(9));
            s=GatheringSession.Restore(s.Capture(),s);Assert.That(s.EatAt(0,"character-1",out _),Is.False);Start(s);Assert.That(s.EatAt(T(1),"character-1",out _),Is.True);Assert.That(s.Vitals["character-1"].Hp,Is.EqualTo(26));
        }
        [TestCase(0,10)][TestCase(50,10)][TestCase(20,0)]
        public void InvalidManualEatingNeverChangesBankOrHp(int hp,int stock)
        {var s=Stock(hp,stock,0);string before=SaveCodec.Json(s.Capture());Assert.That(s.EatAt(0,"character-1",out _),Is.False);Assert.That(SaveCodec.Json(s.Capture()),Is.EqualTo(before));}
        [Test] public void ExplicitPreferenceDoesNotFallbackAndAutoMayUseOtherFood()
        {var s=Stock(20,0,5);Config(s,true,50,"cooked-shrimp-item");Start(s);Assert.That(s.Vitals["character-1"].Hp,Is.EqualTo(20));Assert.That(s.EatAt(0,"character-1",out _),Is.False);Config(s,true,50,"auto");s.SettleTo(T(1));Assert.That(s.Vitals["character-1"].Hp,Is.EqualTo(32));Assert.That(s.Quantity("cooked-sardine-item"),Is.EqualTo(4));}
        [Test] public void CombatStartEatsMultipleServingsOnlyUntilAboveThreshold()
        {var s=Stock(1,10,0);Start(s);Assert.That(s.Vitals["character-1"].Hp,Is.EqualTo(33));Assert.That(s.Quantity("cooked-shrimp-item"),Is.EqualTo(6));Assert.That(s.Characters[0].Skills.Sum(k=>k.StoredXp),Is.Zero);}
        [Test] public void LethalHitWinsBeforeFoodAndNoResurrectionOrRefund()
        {
            var s=Stock(6,10,0);Config(s,true,10,"cooked-shrimp-item");Start(s,"character-1","dev-mage");
            var d=s.Capture();d.Characters[0].Encounter.EnemyTicks=1;s=GatheringSession.Restore(d,s);s.SettleTo(1);Assert.That(s.Vitals["character-1"].Hp,Is.Zero);Assert.That(s.Assignments.ContainsKey("character-1"),Is.False);Assert.That(s.Quantity("cooked-shrimp-item"),Is.EqualTo(10));Assert.That(s.EatAt(1,"character-1",out _),Is.False);
        }
        private static GatheringSession Race(bool cooking=true)
        {
            var s=Stock(50,0,0,5);Config(s,true,50,"cooked-shrimp-item", "character-2");
            if(cooking)Assert.That(s.StartProductionAt(0,"character-1","cooked-shrimp",false,out _),Is.True);
            Start(s,"character-2");var d=s.Capture();d.Characters[1].Hp=1;d.Characters[1].Encounter.EnemyTicks=T(3);d.Characters[1].Encounter.PlayerTicks=T(2);return GatheringSession.Restore(d,s);
        }
        [Test] public void CookingOutputAtFiveCannotPreventDeathAtThree()
        {var s=Race();s.SettleTo(T(10));Assert.That(s.Vitals["character-2"].Hp,Is.Zero);Assert.That(s.Quantity("cooked-shrimp-item"),Is.EqualTo(2));Assert.That(s.Assignments.ContainsKey("character-2"),Is.False);}
        [Test] public void SimultaneousCookingAndLethalHitStillPrioritizeDeathBeforeFood()
        {var s=Race();var d=s.Capture();d.Characters[0].Partial=T(2);s=GatheringSession.Restore(d,s);s.SettleTo(T(3));Assert.That(s.Vitals["character-2"].Hp,Is.Zero);Assert.That(s.Quantity("cooked-shrimp-item"),Is.EqualTo(1));}
        [Test] public void EarlierCookingCanSustainLaterCombatAndLongVsSmallStepsMatch()
        {
            var s=Stock(30,0,0,100);s.StartProductionAt(0,"character-1","cooked-shrimp",false,out _);Start(s,"character-2");Start(s,"character-3","dev-goblin");var split=GatheringSession.Restore(s.Capture(),s);s.SettleTo(T(300),true);var report=new OfflineReport();for(int n=1;n<=300;n++){split.SettleTo(T(n),true);report.Merge(split.LastOfflineReport);}Assert.That(SaveCodec.Json(split.Capture()),Is.EqualTo(SaveCodec.Json(s.Capture())));Assert.That(Used(s.LastOfflineReport),Is.EqualTo(Used(report)));
        }
        [Test] public void SharedFoodContestsUseStableCharacterOrderAtSameHitTime()
        {
            var s=Stock(26,1,0);Start(s);Start(s,"character-2","dev-goblin");s.SettleTo(T(3));Assert.That(s.Vitals["character-1"].Hp,Is.EqualTo(33));Assert.That(s.Vitals["character-2"].Hp,Is.EqualTo(25));Assert.That(s.Quantity("cooked-shrimp-item"),Is.Zero);
        }
        [TestCase(false,true,1)][TestCase(false,false,0)][TestCase(true,true,0)]
        public void EatingDoesNotChangeCombatXpEligibility(bool offline,bool active,int efficiency)
        {var s=Stock(26,100,0);Start(s);if(!active)s.SelectAt(0,"character-2");s.SettleTo(T(30),offline);decimal damage=s.KillsFor("character-1","dev-chicken")*12+12-s.Encounters["character-1"].EnemyHp;Assert.That(s.Characters[0].Skills.Single(k=>k.SkillId=="melee").StoredXp,Is.EqualTo(damage*3*efficiency));Assert.That(s.Quantity("cooked-shrimp-item"),Is.LessThan(100));}
        [Test] public void AutoEatCannotBeDisabledAndEmptyFoodPreservesNormalDeath()
        {var off=Stock(20,100,0);Config(off,false);Start(off);off.SettleTo(T(600));Assert.That(off.Vitals["character-1"].Hp,Is.GreaterThan(0));Assert.That(off.Quantity("cooked-shrimp-item"),Is.LessThan(100));var empty=Stock(20,0,0);Start(empty);empty.SettleTo(T(600));Assert.That(empty.Vitals["character-1"].Hp,Is.Zero);}
        [Test] public void WelcomeReportSeparatesConsumedFoodAndSurvivesRestartAndAcknowledgment()
        {
            var dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));var p=Path.Combine(dir,"player-v3.json");
            try{Func<LocalGame> open=()=>new LocalGame(new AtomicLocalSaveStore(p,version:3),()=>Stock(26,100,0),"food");var g=open();g.Open(Epoch);Start(g.Session);g.Save(Epoch);var h=open();h.Open(Epoch+T(600));Assert.That(Used(h.Pending),Is.GreaterThan(0));Assert.That(h.Pending.Characters.SelectMany(c=>c.Items).Any(i=>i.Id=="cooked-shrimp-item"),Is.False);var again=open();again.Open(Epoch+T(600));Assert.That(SaveCodec.Json(again.Session.Capture()),Is.EqualTo(SaveCodec.Json(h.Session.Capture())));again.Acknowledge(again.Pending.Id);var final=open();final.Open(Epoch+T(600));Assert.That(final.Pending,Is.Null);}finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
        }
        [TestCase(1)][TestCase(2)] public void OlderVersionsMigrateDefaultsAndPreserveOriginalBytes(int version)
        {
            var dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);var path=Path.Combine(dir,"player-v"+version+".json");
            try{
                var old=new LocalGame(new AtomicLocalSaveStore(path,version:version),()=>Factory(false,version==2),version==1?"v1":"v2");old.Open(Epoch);old.Session.AssignAt(0,"character-1","mining-1",false,out _);old.Session.SettleTo(T(7));old.Save(Epoch);var before=old.Session.Capture();var bytes=File.ReadAllBytes(path);
                var g=new LocalGame(new FoodSaveMigration(dir,()=>Factory(false,false),()=>Factory(false),()=>Factory(),"v1","v2","v3"),()=>Factory(),"v3");g.Open(Epoch);var after=g.Session.Capture();foreach(var c in after.Characters){Assert.That(c.Food.Enabled,Is.True);Assert.That(c.Food.Threshold,Is.EqualTo(50));Assert.That(c.Food.Preferred,Is.EqualTo("auto"));c.Food=null;}Assert.That(SaveCodec.Json(after),Is.EqualTo(SaveCodec.Json(before)));Assert.That(File.ReadAllBytes(path),Is.EqualTo(bytes));Assert.That(File.ReadAllBytes(path+".pre-v3"),Is.EqualTo(bytes));
            }finally{Directory.Delete(dir,true);}
        }
        [TestCase("corrupt")][TestCase("future")][TestCase("temporary")][TestCase("settings")]
        public void NewerEvidenceBlocksOlderFallback(string kind)
        {
            var dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
            try{
                var old=new LocalGame(new AtomicLocalSaveStore(Path.Combine(dir,"player-v2.json"),version:2),()=>Factory(false),"v2");old.Open(Epoch);var p=Path.Combine(dir,"player-v3.json");
                if(kind=="temporary")File.WriteAllText(p+".tmp","invalid");else if(kind=="settings"){var d=Factory().Capture();d.Characters[0].Food.Threshold=55;File.WriteAllText(p,SaveCodec.Encode(new SaveDocument{Catalog="v3",UtcTicks=Epoch,State=d},3));}else File.WriteAllText(p,kind=="future"?SaveCodec.Encode(new SaveDocument(),4):"invalid");
                var g=new LocalGame(new FoodSaveMigration(dir,()=>Factory(false,false),()=>Factory(false),()=>Factory(),"v1","v2","v3"),()=>Factory(),"v3");Assert.That(()=>g.Open(Epoch),Throws.Exception);Assert.That(g.Session,Is.Null);
            }finally{Directory.Delete(dir,true);}
        }
        [Test] public void InterruptedV3MigrationPromotesFlushedStateAndDoesNotReplayFood()
        {
            var dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
            try{
                Func<GatheringSession> legacy=()=>{var t=Factory(false);var snapshot=t.Capture();snapshot.Characters[0].Hp=26;snapshot.Bank=new[]{new ItemAmount{Id="cooked-shrimp-item",Quantity=100}};return GatheringSession.Restore(snapshot,t);};
                var old=new LocalGame(new AtomicLocalSaveStore(Path.Combine(dir,"player-v2.json"),version:2),legacy,"v2");old.Open(Epoch);Start(old.Session);old.Save(Epoch);
                var broken=new LocalGame(new FoodSaveMigration(dir,()=>Factory(false,false),()=>Factory(false),()=>Factory(),"v1","v2","v3",beforeCommit:()=>{throw new IOException("crash");}),()=>Factory(),"v3");Assert.Throws<IOException>(()=>broken.Open(Epoch+T(600)));
                var recovered=new LocalGame(new FoodSaveMigration(dir,()=>Factory(false,false),()=>Factory(false),()=>Factory(),"v1","v2","v3"),()=>Factory(),"v3");recovered.Open(Epoch+T(600));Assert.That(recovered.Session.LastSettledTicks,Is.EqualTo(T(600)));Assert.That(recovered.Pending,Is.Not.Null);Assert.That(Used(recovered.Pending),Is.GreaterThan(0));
                var again=new LocalGame(new FoodSaveMigration(dir,()=>Factory(false,false),()=>Factory(false),()=>Factory(),"v1","v2","v3"),()=>Factory(),"v3");again.Open(Epoch+T(600));Assert.That(SaveCodec.Json(again.Session.Capture()),Is.EqualTo(SaveCodec.Json(recovered.Session.Capture())));
            }finally{Directory.Delete(dir,true);}
        }

        private static GatheringSession EnchantCape(GatheringSession s,string type)
        {
            s.GrantDevelopmentGearKitAt(s.LastSettledTicks);string i=s.MaterializeBankEquipmentAt(s.LastSettledTicks,"dev-practice-cape");var d=s.Capture();d.Equipment.Single(e=>e.Instance==i).Enchantment=type+"-4";s=GatheringSession.Restore(d,s);Assert.That(s.EquipAt(s.LastSettledTicks,"character-1","dev-practice-cape",EquipmentSlot.Cape,out _,i),Is.True);return s;
        }
        [Test] public void RegenerationAndEatingArePartitionInvariantAndKeepExistingHealingValue()
        {
            var s=EnchantCape(Stock(26,100,0),"hpregeneration");Start(s);var split=GatheringSession.Restore(s.Capture(),s);var normal=Stock(26,100,0);Start(normal);s.SettleTo(T(60));normal.SettleTo(T(60));for(int i=1;i<=60;i++)split.SettleTo(T(i));Assert.That(SaveCodec.Json(split.Capture()),Is.EqualTo(SaveCodec.Json(s.Capture())));Assert.That(s.Quantity("cooked-shrimp-item"),Is.EqualTo(normal.Quantity("cooked-shrimp-item")));Assert.That(s.Vitals["character-1"].Hp-normal.Vitals["character-1"].Hp,Is.EqualTo(.55m));
        }
        [Test] public void LethalDamageAlsoWinsOverSimultaneousRegenerationAndFood()
        {
            var s=EnchantCape(Stock(6,10,0),"hpregeneration");Config(s,true,10);Start(s,"character-1","dev-mage");var d=s.Capture();d.Characters[0].Encounter.EnemyTicks=1;d.Characters[0].Regen=s.Enchantments.RegenInterval-1;s=GatheringSession.Restore(d,s);s.SettleTo(1);Assert.That(s.Vitals["character-1"].Hp,Is.Zero);Assert.That(s.Quantity("cooked-shrimp-item"),Is.EqualTo(10));
        }
        [TestCase(false,false)][TestCase(true,true)] public void EquipmentBackgroundEfficiencyAppliesOnlyToCombatXpNotEating(bool offline,bool active)
        {
            var s=EnchantCape(Stock(26,100,0),"backgroundxp");Start(s);if(!active)s.SelectAt(0,"character-2");s.SettleTo(T(30),offline);decimal damage=s.KillsFor("character-1","dev-chicken")*12+12-s.Encounters["character-1"].EnemyHp;Assert.That(s.Characters[0].Skills.Single(k=>k.SkillId=="melee").StoredXp,Is.EqualTo(damage*3*.1m));Assert.That(s.Quantity("cooked-shrimp-item"),Is.LessThan(100));
        }
        [Test] public void FoodCannotExtendOfflineCombatAndRepeatedTimestampConsumesNothing()
        {
            var s=Stock(26,10000,0);Start(s);var control=GatheringSession.Restore(s.Capture(),s);s.SettleTo(T(10*3600),true);control.SettleTo(T(4*3600),true);Assert.That(s.Quantity("cooked-shrimp-item"),Is.EqualTo(control.Quantity("cooked-shrimp-item")));Assert.That(s.KillsFor("character-1","dev-chicken"),Is.EqualTo(control.KillsFor("character-1","dev-chicken")));Assert.That(s.Vitals["character-1"].Hp,Is.EqualTo(control.Vitals["character-1"].Hp));Assert.That(s.Characters[0].Skills.Sum(k=>k.StoredXp),Is.Zero);Assert.That(s.LastOfflineReport.EligibleTicks,Is.EqualTo(T(4*3600)));string before=SaveCodec.Json(s.Capture());s.SettleTo(T(10*3600),true);Assert.That(SaveCodec.Json(s.Capture()),Is.EqualTo(before));
        }
        [Test] public void FailedLongSettlementRollsBackFoodHpOutputsAndXpTogether()
        {
            var s=Stock(26,100,0);Start(s);s.AssignAt(0,"character-2","mining-1",false,out _);var d=s.Capture();d.Bank=d.Bank.Concat(new[]{new ItemAmount{Id="metal-1",Quantity=long.MaxValue-1}}).ToArray();s=GatheringSession.Restore(d,s);string before=SaveCodec.Json(s.Capture());Assert.Throws<OverflowException>(()=>s.SettleTo(T(10)));Assert.That(SaveCodec.Json(s.Capture()),Is.EqualTo(before));
        }
        [Test] public void MidCombatFoodSettingsAndClocksRoundTripAndBackupDoesNotReplayFood()
        {
            var dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));var path=Path.Combine(dir,"player-v3.json");
            try{Func<LocalGame> create=()=>new LocalGame(new AtomicLocalSaveStore(path,version:3),()=>Stock(26,100,0),"v3");var g=create();g.Open(Epoch);Config(g.Session,true,70,"cooked-shrimp-item");Start(g.Session);g.Session.SettleTo(T(7));g.Save(Epoch+T(7));g.Save(Epoch+T(7));var before=g.Session.Capture();File.WriteAllText(path,"corrupt");var h=create();h.Open(Epoch+T(7));Assert.That(SaveCodec.Json(h.Session.Capture()),Is.EqualTo(SaveCodec.Json(before)));Assert.That(h.Session.FoodFor("character-1").Threshold,Is.EqualTo(70));var again=create();again.Open(Epoch+T(7));Assert.That(SaveCodec.Json(again.Session.Capture()),Is.EqualTo(SaveCodec.Json(before)));}finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
        }
        [Test] public void InsufficientFoodReceiptShowsConsumptionAndDefeatSeparately()
        {
            var s=Stock(26,1,0);Start(s);s.SettleTo(T(600),true);var report=s.LastOfflineReport.Characters.Single(c=>c.Character=="character-1");Assert.That(report.Consumed.Single().Quantity,Is.EqualTo(1));Assert.That(report.StopReason,Is.EqualTo("Defeated"));Assert.That(s.Vitals["character-1"].Hp,Is.Zero);
        }

        [Test] public void SettingsChangeSettlesPreviousThresholdBeforeApplyingNewThreshold()
        {
            var s=Stock(26,10,0);Config(s,true,10);Start(s);Assert.That(s.ConfigureFoodAt(T(3),"character-1",true,50,"cooked-shrimp-item",out _),Is.True);Assert.That(s.Vitals["character-1"].Hp,Is.EqualTo(25));Assert.That(s.Quantity("cooked-shrimp-item"),Is.EqualTo(10));s.SettleTo(T(4));Assert.That(s.Vitals["character-1"].Hp,Is.EqualTo(33));Assert.That(s.Quantity("cooked-shrimp-item"),Is.EqualTo(9));
        }
        [Test] public void OfflineMagicCanConsumeFoodAndEarnLootWithoutFreeMagicXp()
        {
            var s=Stock(26,100,0);s.GrantDevelopmentGearKitAt(0);s.EquipAt(0,"character-1","wood-wand-item",EquipmentSlot.Mainhand,out _);Assert.That(s.StartCombatAt(0,"character-1","dev-chicken",CombatStyle.Magic,false,out _),Is.True);s.SettleTo(T(120),true);Assert.That(s.Characters[0].Skills.Single(k=>k.SkillId=="magic").StoredXp,Is.Zero);Assert.That(Used(s.LastOfflineReport),Is.GreaterThan(0));Assert.That(s.KillsFor("character-1","dev-chicken"),Is.GreaterThan(0));
        }

        [Test] public void AuthenticV2SchemaMigratesEnchantsCombatCookingAndPendingReportExactly()
        {
            var dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
            try{
                var bytes=LegacyV2Fixture.Bytes;var path=Path.Combine(dir,"player-v2.json");File.WriteAllBytes(path,bytes);var old=SaveCodec.Decode(System.Text.Encoding.UTF8.GetString(bytes),2);
                Assert.That(old.State.Equipment.Any(e=>e.Enchantment!=null),Is.True);Assert.That(old.State.Characters.Any(c=>c.Enchant!=null),Is.True);Assert.That(old.State.Characters.Any(c=>c.Job!=null),Is.True);Assert.That(old.State.Characters.Any(c=>c.Encounter!=null),Is.True);
                var g=new LocalGame(new FoodSaveMigration(dir,()=>Factory(false,false),()=>Factory(false),()=>Factory(),"unused-v1",old.Catalog,"v3"),()=>Factory(),"v3");g.Open(old.UtcTicks);var state=g.Session.Capture();foreach(var c in state.Characters)c.Food=null;
                Assert.That(SaveCodec.Json(state),Is.EqualTo(SaveCodec.Json(old.State)));Assert.That(SaveCodec.Json(g.Pending),Is.EqualTo(SaveCodec.Json(old.Pending)));Assert.That(File.ReadAllBytes(path),Is.EqualTo(bytes));Assert.That(File.ReadAllBytes(path+".pre-v3"),Is.EqualTo(bytes));
            }finally{Directory.Delete(dir,true);}
        }

        [Test] public void DifferentOfflineAllowancesFreezeEachFighterAtItsOwnCutoff()
        {
            var s=Stock(50,10000,0);s.GrantDevelopmentGearKitAt(0);
            foreach(var id in new[]{"dev-practice-hood","dev-practice-body","dev-practice-legs","dev-practice-boots"})
            {string key=s.MaterializeBankEquipmentAt(0,id);var d=s.Capture();d.Equipment.Single(e=>e.Instance==key).Enchantment="offlineduration-4";s=GatheringSession.Restore(d,s);s.EquipAt(0,"character-2",id,s.Equipment.Definitions.Single(e=>e.Id==id).Slot,out _,key);}
            Start(s);Start(s,"character-2","dev-goblin");var control=GatheringSession.Restore(s.Capture(),s);control.SettleTo(T(4*3600),true);s.SettleTo(T(10*3600),true);
            Assert.That(s.KillsFor("character-1","dev-chicken"),Is.EqualTo(control.KillsFor("character-1","dev-chicken")));Assert.That(s.KillsFor("character-2","dev-goblin"),Is.GreaterThan(control.KillsFor("character-2","dev-goblin")));Assert.That(s.LastOfflineReport.EligibleTicks,Is.EqualTo(T(7*3600)));Assert.That(s.Encounters["character-1"].PlayerRemainingTicks,Is.EqualTo(control.Encounters["character-1"].PlayerRemainingTicks));
        }
        [Test] public void AuthenticV1SchemaMigratesDirectlyToV3WithoutWritingV2()
        {
            var dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
            try{var bytes=LegacyV1Fixture.Bytes;File.WriteAllBytes(Path.Combine(dir,"player-v1.json"),bytes);var old=SaveCodec.Decode(System.Text.Encoding.UTF8.GetString(bytes));var game=new LocalGame(new FoodSaveMigration(dir,()=>Factory(false,false),()=>Factory(false),()=>Factory(),old.Catalog,"v2","v3"),()=>Factory(),"v3");game.Open(old.UtcTicks);var d=game.Session.Capture();foreach(var c in d.Characters)c.Food=null;Assert.That(SaveCodec.Json(d),Is.EqualTo(SaveCodec.Json(old.State)));Assert.That(File.Exists(Path.Combine(dir,"player-v2.json")),Is.False);Assert.That(File.ReadAllBytes(Path.Combine(dir,"player-v1.json.pre-v3")),Is.EqualTo(bytes));}finally{Directory.Delete(dir,true);}
        }

        [Test] public void EmptyLiveFoodAndMismatchedSessionCatalogsAreRejected()
        {
            var s=Factory();Assert.Throws<ArgumentException>(()=>FoodCatalog.Load("# empty",s.Production));Assert.Throws<ArgumentException>(()=>new GatheringSession(s.Catalog,s.Curve,foods:s.Foods));
        }

        [Test] public void PreviouslyDisabledV3LoadsEnabledWithoutResettingPreferencesOrProgress()
        {
            var dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));var path=Path.Combine(dir,"player-v3.json");
            try{
                var snapshot=Stock(30,5,0).Capture();snapshot.Characters[0].Food.Enabled=false;snapshot.Characters[0].Food.Threshold=70;snapshot.Characters[0].Food.Preferred="cooked-shrimp-item";
                new AtomicLocalSaveStore(path,version:3).Write(new SaveDocument{Catalog="unchanged-v3",UtcTicks=Epoch,State=snapshot});
                var game=new LocalGame(new AtomicLocalSaveStore(path,version:3),()=>Factory(),"unchanged-v3");game.Open(Epoch);
                Assert.That(game.Session.FoodFor("character-1").Enabled,Is.True);var normalized=game.Session.Capture();snapshot.Characters[0].Food.Enabled=true;Assert.That(SaveCodec.Json(normalized),Is.EqualTo(SaveCodec.Json(snapshot)));
                Start(game.Session);Assert.That(game.Session.Vitals["character-1"].Hp,Is.EqualTo(38));Assert.That(game.Session.Quantity("cooked-shrimp-item"),Is.EqualTo(4));
                var saved=SaveCodec.Decode(File.ReadAllText(path),3);Assert.That(saved.Catalog,Is.EqualTo("unchanged-v3"));Assert.That(saved.State.Characters[0].Food.Enabled,Is.True);
            }finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
        }
        [Test] public void ExplicitBankItemEatingDoesNotChangePreferenceAndCannotDuplicateAtSameTimestamp()
        {
            var s=Stock();Config(s,true,50,"cooked-sardine-item");string initial=SaveCodec.Json(s.Capture());Assert.That(s.EatAt(0,"character-1",out _,"fish-1"),Is.False);Assert.That(SaveCodec.Json(s.Capture()),Is.EqualTo(initial));Assert.That(s.EatAt(0,"character-1",out _,"cooked-shrimp-item"),Is.True);
            Assert.That(s.Vitals["character-1"].Hp,Is.EqualTo(28));Assert.That(s.Quantity("cooked-shrimp-item"),Is.EqualTo(9));Assert.That(s.Quantity("cooked-sardine-item"),Is.EqualTo(10));Assert.That(s.FoodFor("character-1").Preferred,Is.EqualTo("cooked-sardine-item"));
            Assert.That(s.EatAt(0,"character-1",out _,"cooked-sardine-item"),Is.False);string before=SaveCodec.Json(s.Capture());Assert.That(s.EatAt(0,"character-1",out _,"fish-1"),Is.False);Assert.That(SaveCodec.Json(s.Capture()),Is.EqualTo(before));
            s=Stock(20,0,10);Assert.That(s.EatAt(0,"character-1",out _,"cooked-shrimp-item"),Is.False);Assert.That(s.Quantity("cooked-sardine-item"),Is.EqualTo(10));Assert.That(s.Vitals["character-1"].Hp,Is.EqualTo(20));
        }
        [Test] public void BankFoodTargetIsIndependentAndReturnRestoresCharacterTabSkillAndScroll()
        {
            var session=Stock();var model=new GameUiModel(new SessionRuntime(session,new object()));model.Inspect("character-1");model.ShowSkill("cooking");model.Navigate(MainScreen.Bank,.42f);model.SetBankEquipmentTarget("character-2");
            Assert.That(model.ConfigureFood(false,80,"cooked-shrimp-item"),Is.True);Assert.That(model.Eat("cooked-shrimp-item"),Is.True);Assert.That(session.FoodFor("character-2").Enabled,Is.True);Assert.That(session.FoodFor("character-1").Threshold,Is.EqualTo(50));Assert.That(session.Vitals["character-2"].Hp,Is.EqualTo(28));
            Assert.That(model.ReturnFromBank(),Is.True);Assert.That(model.InspectedId,Is.EqualTo("character-1"));Assert.That(model.CharacterTab,Is.EqualTo(CharacterScreen.Activities));Assert.That(model.SkillFilter,Is.EqualTo("cooking"));Assert.That(model.CharacterScrollPosition,Is.EqualTo(.42f));Assert.That(session.ActiveCharacterId,Is.EqualTo("character-1"));Assert.That(session.LastSettledTicks,Is.Zero);Assert.That(session.Assignments,Is.Empty);
        }
    }
}
