using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
namespace Idlebound.Foundation.Tests
{
    public sealed class EnchantmentTests
    {
        private static string Data(string n)=>Resources.Load<TextAsset>("Idlebound/"+n).text;
        private static long T(long seconds)=>seconds*TimeSpan.TicksPerSecond;
        private static readonly long Epoch=new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc).Ticks;
        public static GatheringSession Factory(bool enchant=true)
        {
            var g=GatheringCatalog.LoadProduction(Data("ResourceTiers"),Data("GatheringXp"));
            return new GatheringSession(g,XpCurve.Load(Data("ProductionXpCurve"),false),ProductionCatalog.Load(Data("StarterProductionRecipes"),g.Resources),true,
                EquipmentCatalog.LoadDevelopment(Data("DevelopmentGear")),CombatCatalog.LoadDevelopment(Data("DevelopmentCombatRules"),Data("DevelopmentMonsters"),Data("DevelopmentLoot")),
                enchantments:enchant?EnchantmentCatalog.Load(Data("Enchantments"),Data("EnchantmentRules"),g.Resources):null);
        }
        private static GatheringSession Stock(int magic=99)
        {
            var s=Factory();var d=s.Capture();
            d.Bank=s.Catalog.Resources.Select(r=>new ItemAmount{Id=r.Id,Quantity=100000}).ToArray();
            foreach(var c in d.Characters)foreach(var k in c.Skills){k.Level=k.Id=="magic"?magic:99;k.Xp=s.Curve.Thresholds[k.Level];}
            s=GatheringSession.Restore(d,s);s.GrantDevelopmentGearKitAt(0);return s;
        }
        private static string Target(GatheringSession s,string item="bronze-dagger-item")=>s.MaterializeBankEquipmentAt(s.LastSettledTicks,item);
        private static void Start(GatheringSession s,string target,string enchant="miningspeed-1",string c="character-1")=>Assert.That(s.StartEnchantAt(s.LastSettledTicks,c,target,enchant,false,out var e),Is.True,e);
        private static GatheringSession Wear(GatheringSession s,string type,int tier=4,bool all=false)
        {
            string[] ids=all?new[]{"dev-practice-hood","dev-practice-body","dev-practice-legs","dev-practice-boots","dev-practice-gloves","bronze-dagger-item","dev-practice-board","dev-practice-necklace","dev-practice-ring","dev-practice-cape"}:new[]{"dev-practice-hood","dev-practice-body","dev-practice-legs","dev-practice-boots"};
            foreach(var id in ids){string i=Target(s,id);var d=s.Capture();d.Equipment.Single(e=>e.Instance==i).Enchantment=type+"-"+tier;s=GatheringSession.Restore(d,s);Assert.That(s.EquipAt(s.LastSettledTicks,"character-1",id,s.Equipment.Definitions.Single(e=>e.Id==id).Slot,out var error,i),Is.True,error);}
            return s;
        }
        [Test] public void Exact48UnlocksAndAllBalanceRows()
        {
            int[][] levels={new[]{15,36,58,79},new[]{17,38,60,81},new[]{19,40,61,83},new[]{20,42,63,85},new[]{22,44,65,86},new[]{24,45,67,88},new[]{26,47,69,90},new[]{28,49,70,92},new[]{29,51,72,94},new[]{31,53,74,95},new[]{33,54,76,97},new[]{35,56,78,99}};
            decimal[][] potency={new[]{.02m,.04m,.07m,.1m},new[]{.01m,.02m,.03m,.05m},new[]{.01m,.02m,.04m,.06m},new[]{.005m,.01m,.02m,.03m},new[]{.03m,.06m,.1m,.15m}};
            var s=Factory();Assert.That(s.Enchantments.Definitions.Count,Is.EqualTo(48));
            foreach(var e in s.Enchantments.Definitions){int group=(int)e.Type<6 || (int)e.Type>=10?0:(int)e.Type-5;Assert.That(e.Potency,Is.EqualTo(potency[group][e.Tier-1]));Assert.That(e.Cap,Is.EqualTo(EnchantmentDefinition.MaximumCap(e.Type)));}
            foreach(var d in s.Enchantments.Definitions){int j=d.Tier-1;Assert.That(d.Level,Is.EqualTo(levels[(int)d.Type][j]));Assert.That(d.DurationTicks,Is.EqualTo(T(new[]{30,60,150,360}[j])));Assert.That(d.Xp,Is.EqualTo(new[]{400,950,3000,8500}[j]));Assert.That(d.Inputs["metal-"+new[]{1,20,40,70}[j]],Is.EqualTo(new[]{40,80,160,320}[j]));Assert.That(d.Inputs["wood-"+new[]{1,20,40,70}[j]],Is.EqualTo(new[]{20,40,80,160}[j]));}
        }
        [TestCase("type")][TestCase("tier")][TestCase("id")][TestCase("level")][TestCase("potency")][TestCase("cap")][TestCase("cost")][TestCase("duration")][TestCase("xp")]
        public void RejectInvalidDefinitions(string field)
        {
            Assert.Throws<ArgumentException>(()=>new EnchantmentDefinition(field=="id"?"BAD ID":"test",field=="type"?(EnchantmentType)99:EnchantmentType.MiningSpeed,field=="tier"?5:1,field=="level"?0:15,field=="potency"?-1:.02m,field=="cap"?0:.5m,new Dictionary<string,long>{{"metal-1",field=="cost"?0:40},{"wood-1",20}},field=="duration"?0:T(30),field=="xp"?0:400));
        }
        [Test] public void DuplicateIdsAndTypeRanksAndUnknownMaterialsAreRejected()
        {
            var s=Factory();var d=s.Enchantments.Definitions[0];Assert.Throws<ArgumentException>(()=>new EnchantmentCatalog(new[]{d,d}));
            Assert.Throws<ArgumentException>(()=>EnchantmentCatalog.Load(Data("Enchantments").Replace("metal-1,","absent,"),Data("EnchantmentRules"),s.Catalog.Resources));
        }
        [Test] public void ExactBoundaryTargetLockPermanentIdentityAndNoRepeat()
        {
            var s=Stock(15);string i=Target(s);decimal before=s.Characters[0].Skills.Single(k=>k.SkillId=="magic").StoredXp;
            Start(s,i);Assert.That(s.Quantity("metal-1"),Is.EqualTo(99960));Assert.That(s.Quantity("wood-1"),Is.EqualTo(99980));
            Assert.That(s.EquipAt(0,"character-2","bronze-dagger-item",EquipmentSlot.Mainhand,out _,i),Is.False);
            Assert.That(s.EquipAt(0,"character-2","bronze-dagger-item",EquipmentSlot.Mainhand,out _),Is.False);
            Assert.That(s.StartEnchantAt(0,"character-2",i,"miningspeed-1",true,out _),Is.False);
            s.SettleTo(T(30)-1);Assert.That(s.BankEquipmentInstances[i].PermanentEnchantmentId,Is.Null);
            s.SettleTo(T(30));Assert.That(s.BankEquipmentInstances[i].PermanentEnchantmentId,Is.EqualTo("miningspeed-1"));Assert.That(s.EnchantJobs,Is.Empty);Assert.That(s.Assignments.ContainsKey("character-1"),Is.False);
            Assert.That(s.Characters[0].Skills.Single(k=>k.SkillId=="magic").StoredXp,Is.EqualTo(before+400));
            var done=SaveCodec.Json(s.Capture());s.SettleTo(T(30));Assert.That(SaveCodec.Json(s.Capture()),Is.EqualTo(done));
            Assert.That(s.StartEnchantAt(T(30),"character-1",i,"miningspeed-1",false,out _),Is.False);Assert.That(s.UnassignAt(T(30),"character-1"),Is.False);Assert.That(s.Quantity("metal-1"),Is.EqualTo(99960));
        }
        [Test] public void CancelSwapAndRefundOverflowAreAtomic()
        {
            var s=Stock();var a=Target(s);var b=Target(s,"wood-wand-item");Start(s,a);s.SettleTo(T(7));
            Assert.That(s.StartEnchantAt(T(7),"character-2",b,"miningspeed-1",false,out _),Is.False);
            Assert.That(s.StartEnchantAt(T(7),"character-2",b,"miningspeed-1",true,out var e),Is.True,e);Assert.That(s.IsItemLocked(a),Is.False);Assert.That(s.IsItemLocked(b),Is.True);Assert.That(s.Quantity("metal-1"),Is.EqualTo(99960));
            var d=s.Capture();d.Bank.Single(x=>x.Id=="metal-1").Quantity=long.MaxValue;s=GatheringSession.Restore(d,s);var before=SaveCodec.Json(s.Capture());Assert.Throws<OverflowException>(()=>s.UnassignAt(T(7),"character-2"));Assert.That(SaveCodec.Json(s.Capture()),Is.EqualTo(before));
            d.Bank.Single(x=>x.Id=="metal-1").Quantity=99960;s=GatheringSession.Restore(d,s);Assert.That(s.UnassignAt(T(7),"character-2"),Is.True);Assert.That(s.UnassignAt(T(7),"character-2"),Is.False);Assert.That(s.Quantity("metal-1"),Is.EqualTo(100000));
        }
        [Test] public void MissingMaterialsLevelGateAndDuplicateTapPreserveState()
        {
            var s=Stock(14);var i=Target(s);var before=SaveCodec.Json(s.Capture());Assert.That(s.StartEnchantAt(0,"character-1",i,"miningspeed-1",false,out _),Is.False);Assert.That(SaveCodec.Json(s.Capture()),Is.EqualTo(before));
            s=Stock();i=Target(s);var d=s.Capture();d.Bank.Single(x=>x.Id=="wood-1").Quantity=1;s=GatheringSession.Restore(d,s);before=SaveCodec.Json(s.Capture());Assert.That(s.StartEnchantAt(0,"character-1",i,"miningspeed-1",false,out _),Is.False);Assert.That(SaveCodec.Json(s.Capture()),Is.EqualTo(before));
            s=Stock();i=Target(s);Start(s,i);before=SaveCodec.Json(s.Capture());Assert.That(s.StartEnchantAt(0,"character-1",i,"miningspeed-1",false,out _),Is.False);Assert.That(SaveCodec.Json(s.Capture()),Is.EqualTo(before));
        }
        [TestCase(false,true,0,400)][TestCase(false,false,0,0)][TestCase(true,true,0,0)][TestCase(true,true,.9,200)]
        public void MagicXpUsesExistingEligibility(bool offline,bool active,double effect,int xp)
        {
            var s=Stock(15);var i=Target(s);Start(s,i);if(!active)s.SelectAt(0,"character-2");s.SetEffectsAt(0,"character-1",new[]{new BackgroundXpEffect("test","magic",(decimal)effect)});decimal before=s.Characters[0].Skills.Single(k=>k.SkillId=="magic").StoredXp;s.SettleTo(T(30),offline);Assert.That(s.Characters[0].Skills.Single(k=>k.SkillId=="magic").StoredXp,Is.EqualTo(before+xp));
        }
        [TestCase("miningspeed",.5)][TestCase("fishingspeed",.5)][TestCase("woodcuttingspeed",.5)][TestCase("smithingspeed",.5)][TestCase("cookingspeed",.5)][TestCase("craftingspeed",.5)]
        [TestCase("defense",.25)][TestCase("attackspeed",.3)][TestCase("criticalchance",.15)][TestCase("offlineduration",.75)][TestCase("backgroundxp",.5)][TestCase("hpregeneration",.5)]
        public void AllTwelveAggregateCapsAndUnequip(string type,double cap)
        {
            var s=Wear(Stock(),type,4,true);var t=s.Enchantments.Definitions.Single(e=>e.Id==type+"-4").Type;Assert.That(s.Bonus("character-1",t),Is.EqualTo((decimal)cap));
            Assert.That(s.HasMatchingSet("character-1"),Is.True);s.UnequipAt(0,"character-1",EquipmentSlot.Head,out _);Assert.That(s.HasMatchingSet("character-1"),Is.False);
            foreach(var slot in s.Loadouts["character-1"].Slots.Keys.ToArray())s.UnequipAt(0,"character-1",slot,out _);Assert.That(s.Bonus("character-1",t),Is.Zero);
        }
        [Test] public void ExactTierSetAndEquipmentEffectsNeverBecomeSavedSyntheticBonuses()
        {
            var s=Wear(Stock(),"backgroundxp");Assert.That(s.BackgroundEfficiency("character-1","mining"),Is.EqualTo(.5m));
            var d=s.Capture();Assert.That(d.Characters[0].Effects,Is.Empty);s=GatheringSession.Restore(d,s);s.SetEffectsAt(0,"character-1",new[]{new BackgroundXpEffect("test","*",.9m)});Assert.That(s.BackgroundEfficiency("character-1","mining"),Is.EqualTo(.5m));
            d=s.Capture();d.Equipment.Single(e=>e.Owner=="character-1"&&e.Item=="dev-practice-boots").Enchantment="backgroundxp-3";s=GatheringSession.Restore(d,s);Assert.That(s.HasMatchingSet("character-1"),Is.False);Assert.That(s.Bonus("character-1",EnchantmentType.BackgroundXp),Is.EqualTo(.37m));
        }
        [TestCase("miningspeed","mining-1")][TestCase("fishingspeed","fishing-1")][TestCase("woodcuttingspeed","woodcutting-1")]
        public void GatheringSpeedUsesCeilingTicksAndOnlyNamedSkill(string type,string activity)
        {
            var s=Wear(Stock(),type);var a=s.Activities.Single(x=>x.Id==activity);long duration=s.EffectiveDuration("character-1",a);Assert.That(duration,Is.EqualTo((long)decimal.Ceiling(a.DurationTicks/1.5m)));
            s.AssignAt(0,"character-1",activity,false,out _);long old=s.Quantity(a.ResourceId);s.SettleTo(duration-1);Assert.That(s.Quantity(a.ResourceId),Is.EqualTo(old));s.SettleTo(duration);Assert.That(s.Quantity(a.ResourceId),Is.EqualTo(old+1));
            Assert.That(s.EffectiveDuration("character-2",a),Is.EqualTo(a.DurationTicks));
        }
        [TestCase("smithingspeed","bronze-dagger")][TestCase("craftingspeed","wood-shortbow")][TestCase("cookingspeed","cooked-shrimp")]
        public void ContinuousProductionUsesRealSpeed(string type,string recipe)
        {
            var s=Wear(Stock(),type);var a=s.Activities.Single(x=>x.Id==recipe);var d=s.EffectiveDuration("character-1",a);Assert.That(s.StartProductionAt(0,"character-1",recipe,false,out _),Is.True);long before=s.Quantity(a.ResourceId);s.SettleTo(3*d);Assert.That(s.Quantity(a.ResourceId),Is.EqualTo(before+3));Assert.That(s.ProductionJobs.Count,Is.EqualTo(1));
        }
        [Test] public void OfflineLimitsApplyPerCharacterAndCannotReplayCappedTime()
        {
            var s=Wear(Stock(),"offlineduration");s.AssignAt(0,"character-1","mining-1",false,out _);s.AssignAt(0,"character-2","fishing-1",false,out _);long metal=s.Quantity("metal-1"),fish=s.Quantity("fish-1");s.SettleTo(T(10*3600),true);Assert.That(s.Quantity("metal-1")-metal,Is.EqualTo(5040));Assert.That(s.Quantity("fish-1")-fish,Is.EqualTo(2880));Assert.That(s.LastOfflineReport.EligibleTicks,Is.EqualTo(T(7*3600)));var before=SaveCodec.Json(s.Capture());s.SettleTo(T(10*3600),true);Assert.That(SaveCodec.Json(s.Capture()),Is.EqualTo(before));
        }
        [Test] public void EnchantMidCycleRestoreAndPartitionedCompletionMatch()
        {
            var s=Stock(15);var i=Target(s);Start(s,i);s.SettleTo(T(7));var split=GatheringSession.Restore(s.Capture(),Factory());s.SettleTo(T(65),true);for(int n=8;n<=65;n++)split.SettleTo(T(n),true);Assert.That(SaveCodec.Json(split.Capture()),Is.EqualTo(SaveCodec.Json(s.Capture())));
        }
        [TestCase("defense")][TestCase("attackspeed")][TestCase("criticalchance")][TestCase("hpregeneration")]
        public void CombatEffectsAndRestoredLongVsSmallStepsAreDeterministic(string type)
        {
            var s=Wear(Stock(),type,4,true);var snapshot=s.Capture();foreach(var c in snapshot.Characters)c.Hp=50;s=GatheringSession.Restore(snapshot,s);
            Assert.That(s.StartCombatAt(0,"character-1","dev-chicken",CombatStyle.Melee,false,out _),Is.True);var split=GatheringSession.Restore(s.Capture(),s);s.SettleTo(T(180));for(int t=1;t<=180;t++){split.SettleTo(T(t));if(t==31)split=GatheringSession.Restore(split.Capture(),split);}Assert.That(SaveCodec.Json(split.Capture()),Is.EqualTo(SaveCodec.Json(s.Capture())));
            if(type=="attackspeed")Assert.That(s.Profile("character-1").IntervalTicks,Is.LessThan(T(2)));
            if(type=="defense")Assert.That(s.Profile("character-1").DamageReduction,Is.EqualTo(.25m));
            if(type=="criticalchance")Assert.That(s.Capture().Characters[0].Attacks,Is.GreaterThan(0));
        }
        [Test] public void RegenerationIsRealOnlyWithSourceAndNeverResurrects()
        {
            var s=Wear(Stock(),"hpregeneration");var d=s.Capture();d.Characters[0].Hp=25;d.Characters[1].Hp=25;d.Characters[2].Hp=0;s=GatheringSession.Restore(d,s);s.SettleTo(T(60));Assert.That(s.Vitals["character-1"].Hp,Is.EqualTo(25.75m));Assert.That(s.Vitals["character-2"].Hp,Is.EqualTo(25));Assert.That(s.Vitals["character-3"].Hp,Is.Zero);
        }
        [Test] public void LegacyMigrationArchivesExactBytesAndSettlesOnce()
        {
            var dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
            try
            {
                var old=new LocalGame(new AtomicLocalSaveStore(Path.Combine(dir,"player-v1.json")),()=>Factory(false),"legacy");old.Open(Epoch);old.Session.AssignAt(0,"character-1","mining-1",false,out _);old.Session.GrantDevelopmentGearKitAt(0);old.Session.EquipAt(0,"character-1","bronze-dagger-item",EquipmentSlot.Mainhand,out _);old.Save(Epoch);
                var bytes=File.ReadAllBytes(Path.Combine(dir,"player-v1.json"));
                Func<LocalGame> open=()=>new LocalGame(new MigratingLocalSaveStore(dir,()=>Factory(false),"legacy","new"),()=>Factory(),"new");
                var g=open();g.Open(Epoch+T(1800));Assert.That(g.Session.Quantity("metal-1"),Is.EqualTo(360));Assert.That(g.Pending,Is.Not.Null);Assert.That(File.ReadAllBytes(Path.Combine(dir,"player-v1.json")),Is.EqualTo(bytes));Assert.That(File.ReadAllBytes(Path.Combine(dir,"player-v1.json.pre-v2")),Is.EqualTo(bytes));var restored=open();restored.Open(Epoch+T(1800));Assert.That(SaveCodec.Json(restored.Session.Capture()),Is.EqualTo(SaveCodec.Json(g.Session.Capture())));Assert.That(restored.Pending.Id,Is.EqualTo(g.Pending.Id));restored.Acknowledge(restored.Pending.Id);var again=open();again.Open(Epoch+T(1800));Assert.That(again.Pending,Is.Null);
            }finally{Directory.Delete(dir,true);}
        }
        [TestCase("corrupt")][TestCase("future")][TestCase("interrupted")][TestCase("mismatch")]
        public void MigrationCannotFallBackToObsoleteLegacyWhenNewEvidenceExists(string kind)
        {
            var dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
            try{
                var old=new LocalGame(new AtomicLocalSaveStore(Path.Combine(dir,"player-v1.json")),()=>Factory(false),"legacy");old.Open(Epoch);
                var p=Path.Combine(dir,"player-v2.json");if(kind=="interrupted")File.WriteAllText(p+".tmp","interrupted");else if(kind=="future")File.WriteAllText(p,SaveCodec.Encode(new SaveDocument(),3));else if(kind=="mismatch")new AtomicLocalSaveStore(p,null,2).Write(new SaveDocument{Catalog="wrong",UtcTicks=Epoch,State=Factory().Capture()});else File.WriteAllText(p,"broken");
                var g=new LocalGame(new MigratingLocalSaveStore(dir,()=>Factory(false),"legacy","new"),()=>Factory(),"new");Assert.That(()=>g.Open(Epoch),Throws.Exception);Assert.That(g.Session,Is.Null);
            }finally{Directory.Delete(dir,true);}
        }
        [Test] public void NewSaveRestartsMidEnchantAndBackupRecoversWithoutDuplicateWork()
        {
            var dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);var p=Path.Combine(dir,"player-v2.json");
            try{
                Func<LocalGame> open=()=>new LocalGame(new AtomicLocalSaveStore(p,null,2),()=>Stock(15),"v2");var g=open();g.Open(Epoch);var i=Target(g.Session);Start(g.Session,i);g.Session.SettleTo(T(10));g.Save(Epoch+T(10));g.Save(Epoch+T(10));File.WriteAllText(p,"corrupt");var h=open();h.Open(Epoch+T(30));Assert.That(h.Session.BankEquipmentInstances[i].PermanentEnchantmentId,Is.EqualTo("miningspeed-1"));Assert.That(h.Session.Quantity("metal-1"),Is.EqualTo(99960));var again=open();again.Open(Epoch+T(30));Assert.That(SaveCodec.Json(again.Session.Capture()),Is.EqualTo(SaveCodec.Json(h.Session.Capture())));
            }finally{Directory.Delete(dir,true);}
        }

        [Test] public void ActualCriticalDamageXpExcludesOverkillAndRollsAreReproducible()
        {
            var s=Wear(Stock(),"criticalchance",4,true);var d=s.Capture();var skill=d.Characters[0].Skills.Single(k=>k.Id=="melee");skill.Level=1;skill.Xp=0;s=GatheringSession.Restore(d,s);
            s.StartCombatAt(0,"character-1","dev-chicken",CombatStyle.Melee,false,out _);
            bool sawCritical=false;decimal previousXp=0;
            for(int i=1;i<=80;i++)
            {
                s.SettleTo(T(2*i));decimal xp=s.Characters[0].Skills.Single(k=>k.SkillId=="melee").StoredXp;
                decimal removed=s.KillsFor("character-1","dev-chicken")*12+12-s.Encounters["character-1"].EnemyHp;
                Assert.That(xp,Is.EqualTo(removed*3));if(xp-previousXp>15)sawCritical=true;previousXp=xp;
            }
            Assert.That(sawCritical,Is.True,"Seeded critical chance must affect actual damage, not only profile labels.");
        }
        [Test] public void GearChangePreservesActionFractionAndDoesNotGrantRetroactiveCycles()
        {
            var s=Wear(Stock(),"miningspeed");s.AssignAt(0,"character-1","mining-1",false,out _);s.SettleTo(T(1));long count=s.Quantity("metal-1"),oldDuration=s.EffectiveDuration("character-1",s.Activities.Single(a=>a.Id=="mining-1"));
            s.UnequipAt(T(1),"character-1",EquipmentSlot.Head,out _);long duration=s.EffectiveDuration("character-1",s.Activities.Single(a=>a.Id=="mining-1"));Assert.That(s.PartialTicks("character-1"),Is.EqualTo((long)decimal.Floor(T(1)*(decimal)duration/oldDuration)));Assert.That(s.Quantity("metal-1"),Is.EqualTo(count));
        }
        [Test] public void PerCharacterOfflineLimitsAndSharedProductionRemainPartitionInvariant()
        {
            var s=Wear(Stock(),"offlineduration");s.AssignAt(0,"character-1","mining-1",false,out _);s.StartProductionAt(0,"character-2","bronze-dagger",false,out _);var split=GatheringSession.Restore(s.Capture(),s);
            // Each call below remains within its allowance; both represent the same seven-hour eligible window for C1.
            s.SettleTo(T(7*3600),true);
            // A single absence must not be naively split into separate capped absences: compare online chunks with per-character stop at its offline boundary.
            split.SettleTo(T(4*3600),true);split.UnassignAt(T(4*3600),"character-2");split.SettleTo(T(7*3600),true);
            Assert.That(split.Quantity("metal-1"),Is.EqualTo(s.Quantity("metal-1")+2)); // explicit cancel refunds next reserved dagger
            Assert.That(split.Quantity("bronze-dagger-item"),Is.EqualTo(s.Quantity("bronze-dagger-item")));
        }
        [Test] public void InterruptedFlushedMigrationRecoversNewTransactionNotLegacyReplay()
        {
            var dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
            try{
                var old=new LocalGame(new AtomicLocalSaveStore(Path.Combine(dir,"player-v1.json")),()=>Factory(false),"legacy");old.Open(Epoch);old.Session.AssignAt(0,"character-1","mining-1",false,out _);old.Save(Epoch);
                var failed=new LocalGame(new MigratingLocalSaveStore(dir,()=>Factory(false),"legacy","new",()=>{throw new IOException("power loss");}),()=>Factory(),"new");Assert.Throws<IOException>(()=>failed.Open(Epoch+T(1800)));
                var recovered=new LocalGame(new MigratingLocalSaveStore(dir,()=>Factory(false),"legacy","new"),()=>Factory(),"new");recovered.Open(Epoch+T(1800));Assert.That(recovered.Session.Quantity("metal-1"),Is.EqualTo(360));Assert.That(recovered.Pending,Is.Not.Null);Assert.That(File.Exists(Path.Combine(dir,"player-v1.json.pre-v2")),Is.True);
            }finally{Directory.Delete(dir,true);}
        }
        [Test] public void UnknownEnchantAndDuplicateTargetSnapshotsAreRejected()
        {
            var s=Stock();string i=Target(s);Start(s,i);var d=s.Capture();d.Characters[0].Enchant.Definition="missing";Assert.Throws<ArgumentException>(()=>GatheringSession.Restore(d,s));
            d=s.Capture();d.Equipment.Single(x=>x.Instance==i).Enchantment="miningspeed-1";Assert.Throws<ArgumentException>(()=>GatheringSession.Restore(d,s));
        }

        [Test] public void ExplicitOwnJobReplacementRefundsOldReservationIncludingSameExactActivity()
        {
            var s=Stock();var a=Target(s);var b=Target(s,"wood-wand-item");Start(s,a);s.SettleTo(T(7));
            Assert.That(s.StartEnchantAt(T(7),"character-1",b,"miningspeed-1",false,out _),Is.False);
            Assert.That(s.StartEnchantAt(T(7),"character-1",b,"miningspeed-1",true,out var error),Is.True,error);
            Assert.That(s.Quantity("metal-1"),Is.EqualTo(99960));Assert.That(s.IsItemLocked(a),Is.False);Assert.That(s.IsItemLocked(b),Is.True);Assert.That(s.PartialTicks("character-1"),Is.Zero);
            s.UnassignAt(T(7),"character-1");Assert.That(s.Quantity("metal-1"),Is.EqualTo(100000));
        }
        [Test] public void ProductionAndPendingReportSurviveZeroElapsedMigrationWithoutChanges()
        {
            var dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
            try{
                var s=Factory(false);var data=s.Capture();data.Bank=new[]{new ItemAmount{Id="fish-1",Quantity=100}};s=GatheringSession.Restore(data,s);s.StartProductionAt(0,"character-1","cooked-shrimp",false,out _);s.SettleTo(T(2));s.SelectAt(T(2),"character-2");
                var report=new OfflineReport{AwayTicks=T(600),EligibleTicks=T(600)};report.For("character-1","cooked-shrimp").Item("cooked-shrimp-item",1);
                var document=new SaveDocument{Catalog="legacy",State=s.Capture(),UtcTicks=Epoch,Pending=report};new AtomicLocalSaveStore(Path.Combine(dir,"player-v1.json")).Write(document);
                var g=new LocalGame(new MigratingLocalSaveStore(dir,()=>Factory(false),"legacy","new"),()=>Factory(),"new");g.Open(Epoch);Assert.That(SaveCodec.Json(g.Session.Capture()),Is.EqualTo(SaveCodec.Json(s.Capture())));Assert.That(SaveCodec.Json(g.Pending),Is.EqualTo(SaveCodec.Json(report)));
                g.Session.SettleTo(T(15),true);Assert.That(g.Session.Quantity("cooked-shrimp-item"),Is.EqualTo(3));Assert.That(g.Session.ProductionJobs.Count,Is.EqualTo(1));
            }finally{Directory.Delete(dir,true);}
        }

        [Test] public void BothAuthenticLegacyLineEndingIdentitiesArePinned()
        {
            var names=new[]{"ResourceTiers","GatheringXp","ProductionXpCurve","StarterProductionRecipes","DevelopmentGear","DevelopmentCombatRules","DevelopmentMonsters","DevelopmentLoot"};
            var data=names.Select(n=>Data(n).Replace("\r\n","\n")).ToArray();
            Assert.That(SaveCodec.Hash(string.Join("\n",new[]{"Idlebound-live-v1"}.Concat(data))),Is.EqualTo("8ffe4a43d52f9cd90751176ecfb45e19ec3500129fb139bdc15a71ba801c445a"));
            Assert.That(SaveCodec.Hash(string.Join("\n",new[]{"Idlebound-live-v1"}.Concat(data.Select(x=>x.Replace("\n","\r\n"))))),Is.EqualTo("175f0663b565aa2cd019a877dcce34313f66314d8813a03eb3fb59a6b60a2e26"));
        }
        [Test] public void SupportedWindowsIdentityMigratesButUnknownLegacyIdentityIsBlocked()
        {
            var dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
            try{
                var old=new LocalGame(new AtomicLocalSaveStore(Path.Combine(dir,"player-v1.json")),()=>Factory(false),"windows-authentic");old.Open(Epoch);
                var blocked=new LocalGame(new MigratingLocalSaveStore(dir,()=>Factory(false),"canonical","new"),()=>Factory(),"new");Assert.Throws<SaveCompatibilityException>(()=>blocked.Open(Epoch));
                var allowed=new LocalGame(new MigratingLocalSaveStore(dir,()=>Factory(false),"canonical","new",compatibleLegacyCatalogs:new[]{"canonical","windows-authentic"}),()=>Factory(),"new");allowed.Open(Epoch);Assert.That(allowed.Session.Characters.Count,Is.EqualTo(3));
            }finally{Directory.Delete(dir,true);}
        }

        [Test] public void OriginalBaselineBinaryProducedSaveMigratesEveryFieldAndPendingReport()
        {
            var dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
            try{
                var bytes=LegacyV1Fixture.Bytes;File.WriteAllBytes(Path.Combine(dir,"player-v1.json"),bytes);var old=SaveCodec.Decode(System.Text.Encoding.UTF8.GetString(bytes));
                Assert.That(old.State.Characters.Any(c=>c.Job!=null),Is.True);Assert.That(old.State.Characters.Any(c=>c.Encounter!=null),Is.True);Assert.That(old.State.Equipment.Any(e=>e.Owner!=null),Is.True);Assert.That(old.Pending,Is.Not.Null);
                var game=new LocalGame(new MigratingLocalSaveStore(dir,()=>Factory(false),"8ffe4a43d52f9cd90751176ecfb45e19ec3500129fb139bdc15a71ba801c445a","new"),()=>Factory(),"new");game.Open(old.UtcTicks);
                Assert.That(SaveCodec.Json(game.Session.Capture()),Is.EqualTo(SaveCodec.Json(old.State)));Assert.That(SaveCodec.Json(game.Pending),Is.EqualTo(SaveCodec.Json(old.Pending)));Assert.That(File.ReadAllBytes(Path.Combine(dir,"player-v1.json.pre-v2")),Is.EqualTo(bytes));
            }finally{Directory.Delete(dir,true);}
        }
    }
}
