using System;
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
        private bool paused, focused = true;
        public bool Running => Runtime != null && Runtime.Running;
        public static GameSessionHost Ensure(GameObject owner)
        { return Instance != null ? Instance : owner.AddComponent<GameSessionHost>(); }
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this; ResetSession(false, false);
        }
        private static string Data(string name)
        {
            var data = Resources.Load<TextAsset>("Idlebound/" + name);
            if (data == null) throw new InvalidOperationException("Missing Idlebound catalog: " + name);
            return data.text;
        }
        // Reset is exclusively an explicit development command, never a navigation operation.
        public void ResetSession(bool fixture, bool recipes)
        {
            var gathering = fixture ? GatheringCatalog.Load(Data("ResourceTiers"), true) : GatheringCatalog.LoadProduction(Data("ResourceTiers"), Data("GatheringXp"));
            var production = ProductionCatalog.Load(Data("StarterProductionRecipes") + "\n" + (recipes ? Data("DevelopmentProductionRecipes") : ""), gathering.Resources, recipes);
            var session = new GatheringSession(gathering, XpCurve.Load(Data(fixture ? "KnownXpMilestones" : "ProductionXpCurve"), fixture), production, true,
                EquipmentCatalog.LoadDevelopment(Data("DevelopmentGear")),
                CombatCatalog.LoadDevelopment(Data("DevelopmentCombatRules"), Data("DevelopmentMonsters"), Data("DevelopmentLoot")));
            bool wasRunning = Runtime == null || Runtime.Running;
            double now = Time.realtimeSinceStartupAsDouble;
            Runtime = new SessionRuntime(session, this, now);
            if (!wasRunning) Runtime.SetRunning(this,now,false);
            Runtime.SetPresence(this,now,paused,focused);
            Model = new GameUiModel(Runtime); Generation++; ClockError = null;
        }
        private void Update()
        {
            if (Instance != this || Runtime == null || ClockError != null) return;
            try { Runtime.Tick(this, Time.realtimeSinceStartupAsDouble); }
            catch (Exception error) { ClockError = "Time settlement paused: " + error.Message; Debug.LogException(error); }
        }
        private void OnApplicationPause(bool value) { paused = value; Presence(); }
        private void OnApplicationFocus(bool value) { focused = value; Presence(); }
        private void Presence()
        {
            if (Instance != this || Runtime == null || ClockError != null) return;
            try { Runtime.SetPresence(this, Time.realtimeSinceStartupAsDouble, paused, focused); }
            catch (Exception error) { ClockError = "Offline settlement paused: " + error.Message; Debug.LogException(error); }
        }
        public void SetRunning(bool value) => Runtime.SetRunning(this, Time.realtimeSinceStartupAsDouble, value);
        public void Step(int seconds, bool offline = false) => Runtime.Step(this, checked(seconds * TimeSpan.TicksPerSecond), offline);
        private void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
