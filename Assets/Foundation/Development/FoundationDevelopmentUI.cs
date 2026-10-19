using System;
using System.Linq;
using UnityEngine;

namespace Idlebound.Foundation.Development
{
    // IMGUI is a development harness, not the final production UI.
    public sealed class FoundationDevelopmentUI : MonoBehaviour
    {
        private GatheringSession session;
        private Vector2 scroll;
        private string inspected = "character-1";
        private string skillFilter = "mining";
        private string message = "Gathering ready. Production XP/action is unresolved; XP stays zero by default.";
        private string tierData;
        private string xpData;
        private string productionData;
        private string gatheringXpData;
        private string fullXpData;
        private string developmentProductionData;
        private bool developmentRecipes;
        private string productionFilter = "smithing";
        private bool running = true;
        private double fractionalTicks;
        private double previousTime;
        private bool applicationPaused;
        private bool applicationFocused = true;

        private void Awake()
        {
            var tiers = Resources.Load<TextAsset>("Idlebound/ResourceTiers");
            var xp = Resources.Load<TextAsset>("Idlebound/KnownXpMilestones");
            var recipes = Resources.Load<TextAsset>("Idlebound/StarterProductionRecipes");
            var samples = Resources.Load<TextAsset>("Idlebound/DevelopmentProductionRecipes");
            var rewards = Resources.Load<TextAsset>("Idlebound/GatheringXp");
            var fullCurve = Resources.Load<TextAsset>("Idlebound/ProductionXpCurve");
            if (tiers == null || xp == null || recipes == null || samples == null || rewards == null || fullCurve == null)
            {
                Debug.LogError("Idlebound gathering data missing; development harness disabled.");
                enabled = false;
                return;
            }
            tierData = tiers.text;
            xpData = xp.text;
            productionData = recipes.text;
            gatheringXpData = rewards.text;
            fullXpData = fullCurve.text;
            developmentProductionData = samples.text;
            ResetSession(false);
        }

        private void ResetSession(bool fixture)
        {
            var catalog = fixture ? GatheringCatalog.Load(tierData, true) : GatheringCatalog.LoadProduction(tierData, gatheringXpData);
            var production = ProductionCatalog.Load(productionData + "\n" +
                (developmentRecipes ? developmentProductionData : ""), catalog.Resources, developmentRecipes);
            session = new GatheringSession(catalog, XpCurve.Load(fixture ? xpData : fullXpData, fixture), production, refundCancelledInputs: true);
            inspected = "character-1";
            fractionalTicks = 0;
            previousTime = Time.realtimeSinceStartupAsDouble;
            message = fixture
                ? "DEVELOPMENT ONLY: 100000 XP/cycle, sparse milestone level floor. Not production balancing."
                : "Live progression: approved gathering XP, full reconstructed curve, starter recipe assumptions and cancellation refunds.";
        }

        private void Update()
        {
            // Accumulate one contiguous absence and settle it once on return.
            if (!applicationPaused && applicationFocused) SettleClock(false);
        }

        private void OnApplicationPause(bool paused) => ChangePresence(paused, applicationFocused);
        private void OnApplicationFocus(bool focused) => ChangePresence(applicationPaused, focused);

        private void ChangePresence(bool paused, bool focused)
        {
            bool wasAway = applicationPaused || !applicationFocused;
            bool nowAway = paused || !focused;
            if (wasAway != nowAway) SettleClock(wasAway);
            applicationPaused = paused;
            applicationFocused = focused;
        }

        private void SettleClock(bool offline)
        {
            double now = Time.realtimeSinceStartupAsDouble;
            double elapsed = Math.Max(0, now - previousTime);
            previousTime = now;
            if (!running || session == null) return;
            fractionalTicks += elapsed * TimeSpan.TicksPerSecond;
            long ticks = (long)Math.Floor(fractionalTicks);
            fractionalTicks -= ticks;
            session.SettleTo(checked(session.LastSettledTicks + ticks), offline);
        }

        private void Step(int seconds, bool offline = false)
        {
            session.SettleTo(checked(session.LastSettledTicks + seconds * TimeSpan.TicksPerSecond), offline);
        }

        private void OnGUI()
        {
            if (session == null) return;
            var previousMatrix = GUI.matrix;
            bool previousEnabled = GUI.enabled;
            var safe = Screen.safeArea;
            float scale = Mathf.Max(0.01f, safe.width / 720f);
            GUI.matrix = Matrix4x4.TRS(new Vector3(safe.x, Screen.height - safe.yMax, 0),
                Quaternion.identity, new Vector3(scale, scale, 1));
            var label = new GUIStyle(GUI.skin.label) { fontSize = 24, wordWrap = true };
            var title = new GUIStyle(label) { fontSize = 32, fontStyle = FontStyle.Bold };
            var button = new GUIStyle(GUI.skin.button) { fontSize = 24, wordWrap = true };
            try
            {
                GUILayout.BeginArea(new Rect(16, 12, 688, safe.height / scale - 24));
                scroll = GUILayout.BeginScrollView(scroll);
                GUILayout.Label("IDLEBOUND — Stage 8B-03B", title);
                GUILayout.Label("In-memory development state • no save/cloud", label);
                GUILayout.Label(session.UsesDevelopmentXp
                    ? "DEVELOPMENT XP FIXTURE: 100000/cycle. Level is a known milestone floor only."
                    : "LIVE XP: approved gathering rewards • full reconstructed level curve • XP capped at 10M.", label);
                if (GUILayout.Button("Reset / " + (session.UsesDevelopmentXp ? "disable" : "enable") +
                    " DEVELOPMENT XP fixture", button, GUILayout.MinHeight(64)))
                {
                    ResetSession(!session.UsesDevelopmentXp);
                    GUIUtility.ExitGUI();
                }
                GUILayout.Label(developmentRecipes
                    ? "DEVELOPMENT RECIPES ON: all sample quantities/levels/XP are test values, not approved balance."
                    : "Starter recipes: NEW STAGE 8B-03B BALANCE ASSUMPTIONS, not historical approvals.", label);
                if (GUILayout.Button("Reset / " + (developmentRecipes ? "disable" : "enable") +
                    " DEVELOPMENT recipe samples", button, GUILayout.MinHeight(64)))
                {
                    developmentRecipes = !developmentRecipes;
                    ResetSession(session.UsesDevelopmentXp);
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button(running ? "Pause automatic time" : "Resume automatic time", button, GUILayout.MinHeight(60)))
                {
                    running = !running;
                    previousTime = Time.realtimeSinceStartupAsDouble;
                }
                GUI.enabled = previousEnabled && !running;
                if (GUILayout.Button("Advance 1 second (paused)", button, GUILayout.MinHeight(60))) Step(1);
                if (GUILayout.Button("Advance 5 seconds (paused)", button, GUILayout.MinHeight(60))) Step(5);
                if (GUILayout.Button("Simulate 5 seconds OFFLINE (paused)", button, GUILayout.MinHeight(60))) Step(5, true);
                GUI.enabled = previousEnabled;
                GUILayout.Label(message, label);
                GUILayout.Label("ROSTER — 3 development characters / 9 capacity", title);
                foreach (var character in session.Characters)
                {
                    bool assigned = session.Assignments.TryGetValue(character.Id, out var id);
                    var activity = assigned ? session.Activities.First(a => a.Id == id) : null;
                    string status = activity == null ? "Unassigned" : activity.DisplayName + " • " +
                        (session.PartialTicks(character.Id) / (double)TimeSpan.TicksPerSecond).ToString("0.0") +
                        "/" + (activity.DurationTicks / (double)TimeSpan.TicksPerSecond).ToString("0.0") + "s";
                    if (activity != null && session.Production.Recipes.Any(r => r.Id == activity.Id) &&
                        !session.ProductionJobs.ContainsKey(character.Id)) status = activity.DisplayName + " • Ready for next Start";
                    if (GUILayout.Button(character.DisplayName + (session.IsActive(character.Id) ? " [ACTIVE]" : " [background]") +
                        "\n" + status, button, GUILayout.MinHeight(86))) inspected = character.Id;
                }
                GUILayout.Label("Slots 4–9 unavailable in this fixture. Production XP unlocks/entitlements pending.", label);
                var selected = session.Characters.First(c => c.Id == inspected);
                GUILayout.Label("Inspecting " + selected.DisplayName, title);
                if (GUILayout.Button("Make " + selected.DisplayName + " active", button, GUILayout.MinHeight(60)))
                    session.SelectAt(session.LastSettledTicks, selected.Id);
                GUILayout.Label(session.UsesSparseCurve ? "SKILLS — DEVELOPMENT milestone floors" : "SKILLS — full live level curve", title);
                foreach (var skill in selected.Skills)
                {
                    string background = session.BackgroundEfficiency(selected.Id, skill.SkillId).ToString("P0");
                    decimal? next = session.Curve.NextLevelXp(skill.StoredXp);
                    decimal start = session.Curve.Thresholds[skill.Level];
                    decimal progress = next.HasValue ? (skill.StoredXp - start) / (next.Value - start) : 1;
                    GUILayout.Label(session.Skills.First(s => s.Id == skill.SkillId).DisplayName +
                        (session.UsesSparseCurve ? "   Lv ≥" : "   Lv ") + skill.Level + "/99   XP " +
                        skill.StoredXp.ToString("0.###") + "   BG " + background + "\n" +
                        (next.HasValue ? (next.Value - skill.StoredXp).ToString("0.###") + " XP to " +
                            (session.UsesSparseCurve ? "next milestone" : "next level") + " (" + progress.ToString("P1") + ")" : session.UsesSparseCurve ? "Level 99 milestone (legacy fixture XP uncapped)" : "Level 99 / 10M XP cap"), label);
                }
                GUILayout.Label("GATHERING ACTIVITIES", title);
                GUILayout.BeginHorizontal();
                foreach (string id in new[] { "mining", "woodcutting", "fishing" })
                    if (GUILayout.Button(session.Skills.First(s => s.Id == id).DisplayName, button, GUILayout.MinHeight(64)))
                    {
                        skillFilter = id;
                        GUIUtility.ExitGUI();
                    }
                GUILayout.EndHorizontal();
                foreach (var activity in session.Catalog.Activities.Where(a => a.SkillId == skillFilter))
                {
                    bool occupied = session.Assignments.Any(a => a.Key != inspected && a.Value == activity.Id);
                    bool eligible = session.MeetsRequirement(inspected, activity);
                    GUILayout.Label(activity.DisplayName + " • requires " + activity.RequiredLevel + " • " +
                        (activity.DurationTicks / (double)TimeSpan.TicksPerSecond).ToString("0.#") +
                        "s • +1 resource • XP " + (activity.XpPerCycle?.ToString() ?? "TBD"), label);
                    GUI.enabled = previousEnabled && eligible;
                    if (GUILayout.Button((!eligible ? "Locked — known level too low" : occupied ? "Swap Assignment" : "Assign") +
                        " — " + activity.DisplayName, button, GUILayout.MinHeight(64)))
                    {
                        session.AssignAt(session.LastSettledTicks, inspected, activity.Id, occupied, out var error);
                        message = error ?? "Assigned " + selected.DisplayName + " to " + activity.DisplayName;
                        GUIUtility.ExitGUI();
                    }
                    GUI.enabled = previousEnabled;
                }
                if (GUILayout.Button("Unassign / cancel job (refund unfinished inputs)", button, GUILayout.MinHeight(64)))
                {
                    session.UnassignAt(session.LastSettledTicks, inspected);
                    GUIUtility.ExitGUI();
                }
                GUILayout.Label("PRODUCTION — ONE JOB PER START", title);
                GUILayout.Label("Replacement/swap eligibility includes inputs refunded from interrupted jobs; bank counts show unreserved stock.", label);
                GUILayout.Label("Inputs reserved at start; unfinished cancellation/change/swap refunds once. Completed jobs grant output/XP only. One job per Start; no automatic queue.", label);
                GUILayout.BeginHorizontal();
                foreach (string id in new[] { "smithing", "crafting", "cooking" })
                    if (GUILayout.Button(session.Skills.First(s => s.Id == id).DisplayName, button, GUILayout.MinHeight(64)))
                    {
                        productionFilter = id;
                        GUIUtility.ExitGUI();
                    }
                GUILayout.EndHorizontal();
                if (session.Production.Recipes.Count == 0)
                    GUILayout.Label("No recipes loaded.", label);
                foreach (var recipe in session.Production.Recipes.Where(r => r.SkillId == productionFilter))
                {
                    bool eligible = session.MeetsRequirement(inspected, session.Activities.First(a => a.Id == recipe.Id));
                    bool occupied = session.Assignments.Any(a => a.Key != inspected && a.Value == recipe.Id);
                    bool working = session.ProductionJobs.TryGetValue(inspected, out var job) && job.Id == recipe.Id;
                    GUILayout.Label(recipe.DisplayName + " • " + recipe.Category + " • Lv " + recipe.RequiredLevel +
                        " • " + (recipe.DurationTicks / (double)TimeSpan.TicksPerSecond).ToString("0.#") + "s\nOutput: " +
                        recipe.OutputName + " × " + recipe.OutputQuantity + " • XP " + (recipe.XpReward?.ToString() ?? "TBD"), label);
                    foreach (var input in recipe.Inputs)
                        GUILayout.Label("Needs " + session.Production.ItemNames[input.Key] + " × " + input.Value +
                            " • Bank: " + session.Quantity(input.Key), label);
                    if (recipe.IsBalanceAssumption) GUILayout.Label("NEW STAGE 8B-03B BALANCE ASSUMPTIONS", label);
                    bool canStart = session.CanStartProduction(inspected, recipe, occupied);
                    GUI.enabled = previousEnabled && eligible && canStart && !working;
                    string action = working ? "Job running" : !eligible ? "Level locked" : !canStart
                        ? "Insufficient ingredients" : occupied ? "Swap Assignment and Start" : "Start one job";
                    if (GUILayout.Button(action + " — " + recipe.DisplayName, button, GUILayout.MinHeight(64)))
                    {
                        session.StartProductionAt(session.LastSettledTicks, inspected, recipe.Id, occupied, out var error);
                        message = error ?? "Started " + recipe.DisplayName + "; inputs reserved (refundable). No output/XP until completion.";
                        GUIUtility.ExitGUI();
                    }
                    GUI.enabled = previousEnabled;
                }
                GUILayout.Label("SHARED BANK — MATERIALS AND FINISHED PRODUCTS", title);
                foreach (var item in session.Production.ItemNames)
                    if (session.Quantity(item.Key) > 0)
                        GUILayout.Label(item.Value + " × " + session.Quantity(item.Key), label);
                if (session.Inventory.Count == 0) GUILayout.Label("Empty — complete a gathering cycle.", label);
                GUILayout.Label("No background bonuses equipped. Offline uses background XP for everyone. No gem rolls, equipment stats or equipping.", label);
                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
            finally
            {
                GUI.enabled = previousEnabled;
                GUI.matrix = previousMatrix;
            }
        }
    }
}
