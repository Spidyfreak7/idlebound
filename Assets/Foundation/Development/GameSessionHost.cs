using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Idlebound.Foundation.Development
{
    [DefaultExecutionOrder(-1000)]
    public sealed class GameSessionHost : MonoBehaviour
    {
        public static GameSessionHost Instance { get; private set; }
        public SessionRuntime Runtime { get; private set; }
        public GameUiModel Model { get; private set; }
        public int Generation { get; private set; }
        public string ClockError { get; private set; }
        public string PersistenceError { get; private set; }
        public bool IsLoading { get; private set; }
        public OfflineReport PendingReport => local?.Pending;
        private bool paused, focused = true, diskEnabled, inBackground;
        private LocalGame local;
        private IUtcClock utc;
        private long boundary;
        private float nextCheck, nextSave;
        private string lastStamp;
        private static TestScope tests;
        public bool UsesLocalSave => diskEnabled;
        public bool Running => Runtime != null && Runtime.Running;
        public static GameSessionHost Ensure(GameObject owner)
        {
            if (Instance != null) return Instance;
            return tests != null ? owner.AddComponent<GameSessionHost>() : new GameObject("Idlebound Session").AddComponent<GameSessionHost>();
        }
        // Deliberate fixtures never read or write a developer's real save directory.
        public static IDisposable IsolateForTests(string directory = null, IUtcClock clock = null)
        {
            if (Instance != null || tests != null) throw new InvalidOperationException("Close the existing test/session first.");
            tests = new TestScope { Directory = directory, Clock = clock }; return tests;
        }
        private sealed class TestScope : IDisposable
        {
            internal string Directory; internal IUtcClock Clock;
            public void Dispose() { tests = null; }
        }
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            if (tests == null) DontDestroyOnLoad(gameObject);
            utc = tests?.Clock ?? new SystemUtcClock(); diskEnabled = tests == null || tests.Directory != null;
            if (!diskEnabled) { InstallNew(false,false); return; }
            BeginPersistentLoad();
        }
        private void BeginPersistentLoad()
        {
            try
            {
                var template = Catalogs(false,false);
                Func<GatheringSession> factory = () => new GatheringSession(template.Catalog,template.Curve,template.Production,true,template.Equipment,template.Combat,enchantments:template.Enchantments);
                string catalog = SaveCodec.Hash(string.Join("\n",new[] { "Idlebound-live-v1", Data("ResourceTiers").Replace("\r\n","\n"),Data("GatheringXp").Replace("\r\n","\n"),Data("ProductionXpCurve").Replace("\r\n","\n"),Data("StarterProductionRecipes").Replace("\r\n","\n"),Data("DevelopmentGear").Replace("\r\n","\n"),Data("DevelopmentCombatRules").Replace("\r\n","\n"),Data("DevelopmentMonsters").Replace("\r\n","\n"),Data("DevelopmentLoot").Replace("\r\n","\n") }));
                if(catalog!="8ffe4a43d52f9cd90751176ecfb45e19ec3500129fb139bdc15a71ba801c445a") throw new SaveCompatibilityException("Legacy catalog identity changed; migration requires review.");
                var legacyTemplate=new GatheringSession(template.Catalog,template.Curve,template.Production,true,template.Equipment,template.Combat);
                string currentCatalog=SaveCodec.Hash("Idlebound-live-v2\n"+catalog+"\n"+Data("Enchantments").Replace("\r\n","\n")+"\n"+Data("EnchantmentRules").Replace("\r\n","\n"));
                var directory=tests?.Directory ?? Path.Combine(Application.persistentDataPath,"Idlebound");
                Func<GatheringSession> legacyFactory=()=>new GatheringSession(legacyTemplate.Catalog,legacyTemplate.Curve,legacyTemplate.Production,true,legacyTemplate.Equipment,legacyTemplate.Combat);
                local = new LocalGame(new MigratingLocalSaveStore(directory,legacyFactory,catalog,currentCatalog,compatibleLegacyCatalogs:new[]{catalog,"175f0663b565aa2cd019a877dcce34313f66314d8813a03eb3fb59a6b60a2e26"}),factory,currentCatalog);
                StartCoroutine(LoadOrResume(true));
            }
            catch (Exception error)
            {
                PersistenceError = "Local catalogs or save could not be opened. Existing files have been preserved.";
                Debug.LogException(error);
            }
        }
        private static string Data(string name)
        {
            var data = Resources.Load<TextAsset>("Idlebound/" + name);
            if (data == null) throw new InvalidOperationException("Missing Idlebound catalog: " + name);
            return data.text;
        }
        private static GatheringSession Catalogs(bool fixture, bool recipes)
        {
            var gathering = fixture ? GatheringCatalog.Load(Data("ResourceTiers"),true) : GatheringCatalog.LoadProduction(Data("ResourceTiers"),Data("GatheringXp"));
            var production = ProductionCatalog.Load(Data("StarterProductionRecipes") + "\n" + (recipes ? Data("DevelopmentProductionRecipes") : ""),gathering.Resources,recipes);
            return new GatheringSession(gathering,XpCurve.Load(Data(fixture ? "KnownXpMilestones" : "ProductionXpCurve"),fixture),production,true,
                EquipmentCatalog.LoadDevelopment(Data("DevelopmentGear")),CombatCatalog.LoadDevelopment(Data("DevelopmentCombatRules"),Data("DevelopmentMonsters"),Data("DevelopmentLoot")),enchantments:EnchantmentCatalog.Load(Data("Enchantments"),Data("EnchantmentRules"),gathering.Resources));
        }
        private void InstallNew(bool fixture, bool recipes)
        {
            bool wasRunning = Runtime == null || Runtime.Running;
            Runtime = new SessionRuntime(Catalogs(fixture,recipes),this,Time.realtimeSinceStartupAsDouble);
            if (!wasRunning) Runtime.SetRunning(this,Time.realtimeSinceStartupAsDouble,false);
            Model = new GameUiModel(Runtime); Model.Changed += SaveCurrent;
            Generation++; ClockError = null; boundary = utc.UtcTicks;
        }
        // Explicit debug resets are sandboxed and never replace a player's on-disk state.
        public void ResetSession(bool fixture, bool recipes)
        {
            if (IsLoading) throw new InvalidOperationException("Wait for local loading to finish.");
            if (diskEnabled) { SaveCurrent(); if (PersistenceError != null) return; }
            diskEnabled = false; local = null; InstallNew(fixture,recipes);
            Debug.Log("Idlebound development sandbox: reset state is not written over the player's save.");
        }
        private IEnumerator LoadOrResume(bool first)
        {
            if (IsLoading) yield break;
            IsLoading = true; PersistenceError = null;
            long nowUtc = utc.UtcTicks;
            var task = Task.Run(() => { if (first) local.Open(nowUtc); else local.Resume(nowUtc); });
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted)
            {
                PersistenceError = "Local save could not be opened or updated. Your save files have been preserved.";
                Debug.LogException(task.Exception.GetBaseException()); IsLoading = false; yield break;
            }
            if (Runtime == null)
            { Runtime = new SessionRuntime(local.Session,this,Time.realtimeSinceStartupAsDouble,paused || !focused); Model = new GameUiModel(Runtime); Model.Changed += SaveCurrent; }
            else Runtime.AdoptRestored(this,local.Session,Time.realtimeSinceStartupAsDouble,paused || !focused);
            inBackground = paused || !focused;
            if (inBackground && !Runtime.IsAway) Runtime.SetPresence(this,Time.realtimeSinceStartupAsDouble,true,false);
            if (first && local.RecoveryDiagnostic != null) Debug.LogWarning(local.RecoveryDiagnostic);
            boundary = local.BoundaryUtcTicks; Generation++; lastStamp = Stamp();
            nextCheck = Time.unscaledTime + 1; nextSave = Time.unscaledTime + 15; IsLoading = false;
        }
        private string Stamp()
        {
            var snapshot = Runtime.Session.Capture(); snapshot.Ticks = 0;
            foreach (var c in snapshot.Characters)
            { c.Partial = 0; if (c.Encounter != null) { c.Encounter.PlayerTicks = 0; c.Encounter.EnemyTicks = 0; } }
            return SaveCodec.Hash(SaveCodec.Json(snapshot));
        }
        private void Update()
        {
            if (Instance != this || Runtime == null || IsLoading || PersistenceError != null || ClockError != null || inBackground) return;
            try
            {
                Runtime.Tick(this,Time.realtimeSinceStartupAsDouble); boundary = Math.Max(boundary,utc.UtcTicks);
                if (diskEnabled && Time.unscaledTime >= nextCheck)
                {
                    nextCheck = Time.unscaledTime + 1;
                    if (Time.unscaledTime >= nextSave || Stamp() != lastStamp) SaveCurrent();
                }
            }
            catch (Exception error) { ClockError = "Time settlement paused."; Debug.LogException(error); }
        }
        public void SaveCurrent()
        {
            if (!diskEnabled || local == null || Runtime == null || IsLoading || PersistenceError != null) return;
            try { local.Save(boundary); lastStamp = Stamp(); nextSave = Time.unscaledTime + 15; }
            catch (Exception error) { PersistenceError = "Unable to save locally. Gameplay is paused to protect your progress."; Debug.LogException(error); }
        }
        private void OnApplicationPause(bool value) { if (tests != null) return; paused = value; Presence(); }
        private void OnApplicationFocus(bool value) { if (tests != null) return; focused = value; Presence(); }
        private void Presence()
        {
            if (Instance != this || Runtime == null || IsLoading || PersistenceError != null || ClockError != null) return;
            try
            {
                bool away = paused || !focused;
                if (!diskEnabled) { Runtime.SetPresence(this,Time.realtimeSinceStartupAsDouble,paused,focused); return; }
                if (away && !inBackground)
                {
                    Runtime.SetPresence(this,Time.realtimeSinceStartupAsDouble,true,false);
                    boundary = Math.Max(boundary,utc.UtcTicks); inBackground = true; SaveCurrent();
                }
                else if (!away && inBackground) StartCoroutine(LoadOrResume(false));
            }
            catch (Exception error) { ClockError = "Time settlement paused."; Debug.LogException(error); SaveCurrent(); }
        }
        public void RetryPersistence()
        {
            if (IsLoading || !diskEnabled) return;
            PersistenceError = null;
            if (local == null) { BeginPersistentLoad(); return; }
            if (Runtime != null) { SaveCurrent(); if (PersistenceError != null) return; }
            StartCoroutine(LoadOrResume(Runtime == null));
        }
        public bool AcknowledgeWelcome(string id)
        {
            if (IsLoading || PersistenceError != null) return false;
            SaveCurrent(); if (PersistenceError != null) return false;
            try { local?.Acknowledge(id); return true; }
            catch (Exception error) { PersistenceError = "Unable to save your acknowledgment. Please retry."; Debug.LogException(error); return false; }
        }
        public void SetRunning(bool value) { Runtime.SetRunning(this,Time.realtimeSinceStartupAsDouble,value); boundary = Math.Max(boundary,utc.UtcTicks); }
        public void Step(int seconds, bool offline = false) { Runtime.Step(this,checked(seconds * TimeSpan.TicksPerSecond),offline); SaveCurrent(); }
        private void OnApplicationQuit() { SaveCurrent(); }
        private void OnDestroy() { if (Instance == this) { SaveCurrent(); Instance = null; } }
    }
}
