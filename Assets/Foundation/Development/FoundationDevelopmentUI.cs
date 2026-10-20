using System;
using System.Linq;
using UnityEngine;

namespace Idlebound.Foundation.Development
{
    // IMGUI is a development harness, not the final production UI.
    public sealed class FoundationDevelopmentUI : MonoBehaviour
    {
        private GameSessionHost host;
        private GatheringSession session => host != null ? host.Runtime.Session : null;
        private Vector2 scroll;
        private string inspected = "character-1";
        private string skillFilter = "mining";
        private string message = "Development harness • the session host owns the only clock.";
        private bool developmentRecipes;
        private string productionFilter = "smithing";
        private bool running => host.Running;
        private void Awake() { host = GameSessionHost.Ensure(gameObject); }
        private void ResetSession(bool fixture)
        {
            host.ResetSession(fixture, developmentRecipes);
            inspected = "character-1";
            message = "Explicit development reset. All state is in memory.";
        }
        private void Step(int seconds, bool offline = false) { host.Step(seconds, offline); }

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
                GUILayout.Label("IDLEBOUND — Development mode (8B-05)", title);
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
                    host.SetRunning(!running);
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
                    if (session.Encounters.TryGetValue(character.Id, out var encounter))
                        status = "Fighting " + encounter.MonsterId + " • enemy HP " + encounter.EnemyHp +
                            " • next hit " + (encounter.PlayerRemainingTicks / (double)TimeSpan.TicksPerSecond).ToString("0.0") + "s";
                    status += " • HP " + session.Vitals[character.Id].Hp + "/" + session.Vitals[character.Id].MaxHp;
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
                GUILayout.Label("EQUIPMENT / COMBAT — DEVELOPMENT ONLY", title);
                GUILayout.Label("All gear stats, enemies, loot odds, HP, damage and XP coefficients are test data, not final balance. Style follows mainhand; unarmed = Melee. No passive healing.", label);
                var profile = session.Profile(inspected);
                GUILayout.Label("Style " + profile.Style + " • damage " + profile.Damage + " • defense " + profile.Defense +
                    " • interval " + (profile.IntervalTicks / (double)TimeSpan.TicksPerSecond) + "s • HP " + session.Vitals[inspected].Hp + "/" + session.Vitals[inspected].MaxHp, label);
                foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
                {
                    bool occupied = session.Loadouts[inspected].Slots.TryGetValue(slot, out var equipped);
                    GUILayout.Label(slot + ": " + (occupied ? equipped.Definition.Name + " [" + equipped.InstanceId + "]" : "Empty"), label);
                    if (occupied && GUILayout.Button("Unequip " + slot, button))
                    {
                        session.UnequipAt(session.LastSettledTicks, inspected, slot, out var error);
                        message = error ?? "Returned item to shared bank.";
                        GUIUtility.ExitGUI();
                    }
                }
                foreach (var gear in session.Equipment.Definitions)
                {
                    GUILayout.Label(gear.Name + " • " + gear.Slot + " • requires " +
                        string.Join(" / ", gear.Requirements.Select(r => r.Key + " " + r.Value)) +
                        " • bank " + session.Quantity(gear.Id) + " • damage +" + gear.DamageBonus + " • defense +" + gear.DefenseBonus, label);
                    if (GUILayout.Button("Equip / choose style: " + (gear.Style?.ToString() ?? gear.Slot.ToString()), button))
                    {
                        session.EquipAt(session.LastSettledTicks, inspected, gear.Id, gear.Slot, out var error);
                        message = error ?? "Equipped " + gear.Name;
                        GUIUtility.ExitGUI();
                    }
                }
                if (GUILayout.Button("DEV ONLY: add one sample gear kit to bank", button))
                {
                    session.GrantDevelopmentGearKitAt(session.LastSettledTicks);
                    message = "Explicit testing grant only; not a production reward.";
                    GUIUtility.ExitGUI();
                }
                foreach (var monster in session.Combat.Monsters)
                {
                    GUILayout.Label(monster.Name + " • " + monster.Style + " • HP " + monster.MaxHp + " • damage " + monster.Damage +
                        " • kills by this character " + session.KillsFor(inspected, monster.Id), label);
                    foreach (var drop in session.Combat.Drops.Where(d => d.MonsterId == monster.Id))
                        GUILayout.Label("[DEV loot] " + drop.ItemName + " × " + drop.Quantity + " @ " + drop.Chance.ToString("P0"), label);
                    bool occupied = session.Assignments.Any(a => a.Key != inspected && a.Value == monster.Id);
                    if (GUILayout.Button((occupied ? "Swap assignment and fight " : "Fight ") + monster.Name + " as " + profile.Style, button))
                    {
                        session.StartCombatAt(session.LastSettledTicks, inspected, monster.Id, profile.Style, occupied, out var error);
                        message = error ?? "Combat started. Normal monsters repeat; death stops combat.";
                        GUIUtility.ExitGUI();
                    }
                }
                if (GUILayout.Button("Stop combat / unassign", button))
                {
                    session.UnassignAt(session.LastSettledTicks, inspected);
                    message = "Stopped. Enemy progress discarded; character HP retained.";
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("DEV ONLY: restore HP after stopping combat", button))
                {
                    session.RestoreDevelopmentHpAt(session.LastSettledTicks, inspected, out var error);
                    message = error ?? "Explicit test HP reset. No automatic recovery policy.";
                    GUIUtility.ExitGUI();
                }
                GUILayout.Label("SHARED BANK — MATERIALS AND FINISHED PRODUCTS", title);
                foreach (var item in session.BankItems)
                    if (session.Quantity(item.Key) > 0)
                        GUILayout.Label(item.Value + " × " + session.Quantity(item.Key), label);
                if (session.Inventory.Count == 0) GUILayout.Label("Empty — complete a gathering cycle.", label);
                GUILayout.Label("No background bonuses equipped. Offline uses background XP for everyone. Combat gear does not grant background XP bonuses. No persistence or gem rolls.", label);
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
