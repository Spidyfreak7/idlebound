using System.Linq;
using UnityEngine;

namespace Idlebound.Foundation.Development
{
    // IMGUI is intentionally limited to this development harness. State has no UI dependency.
    public sealed class FoundationDevelopmentUI : MonoBehaviour
    {
        private GameState state;
        private Vector2 scroll;
        private string message = "Select a character, then assign an activity. No XP is generated.";

        private void Awake() => state = PrototypeDefinitions.Create();

        private void OnGUI()
        {
            if (state == null) return;
            var previousMatrix = GUI.matrix;
            var previousSkin = GUI.skin;
            // Fit a logical portrait width inside the device safe area; scroll vertically.
            var safe = Screen.safeArea;
            float scale = Mathf.Max(0.01f, safe.width / 720f);
            GUI.matrix = Matrix4x4.TRS(new Vector3(safe.x, Screen.height - safe.yMax, 0),
                Quaternion.identity, new Vector3(scale, scale, 1));
            var label = new GUIStyle(GUI.skin.label) { fontSize = 25, wordWrap = true };
            var title = new GUIStyle(label) { fontSize = 38, fontStyle = FontStyle.Bold };
            var button = new GUIStyle(GUI.skin.button) { fontSize = 25, wordWrap = true };
            GUILayout.BeginArea(new Rect(16, 12, 688, safe.height / scale - 24));
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("IDLEBOUND — Stage 8B-01", title);
            GUILayout.Label("Offline in-memory prototype • resets on Play\nSynthetic definitions; XP curve pending.", label);
            GUILayout.Space(14);
            GUILayout.Label("ROSTER — 3 available / 9 capacity", title);
            for (int i = 0; i < RosterAccess.Capacity; i++)
            {
                if (i >= state.Characters.Count)
                {
                    GUILayout.Label("Slot " + (i + 1) + " — unavailable (unlock policy pending)", label);
                    continue;
                }
                var character = state.Characters[i];
                string role = state.IsActiveCharacter(character.Id) ? "ACTIVE" : "Inactive — no XP generated";
                string assignment = state.Assignments.TryGetValue(character.Id, out var id)
                    ? state.Activities.First(a => a.Id == id).DisplayName : "Unassigned";
                if (GUILayout.Button(character.DisplayName + " • " + role + "\n" + assignment,
                    button, GUILayout.MinHeight(82)))
                    state.TrySelectActive(character.Id);
            }
            var selected = state.Characters.First(c => c.Id == state.ActiveCharacterId);
            GUILayout.Space(14);
            GUILayout.Label(selected.DisplayName + " — SKILLS", title);
            foreach (var skill in selected.Skills)
                GUILayout.Label(state.Skills.First(s => s.Id == skill.SkillId).DisplayName +
                    "   Lv " + skill.Level + "/99   XP " + skill.StoredXp, label);
            GUILayout.Space(14);
            GUILayout.Label("ACTIVITIES", title);
            foreach (var activity in state.Activities)
            {
                bool occupied = state.Assignments.Any(a => a.Key != selected.Id && a.Value == activity.Id);
                GUI.enabled = activity.IsAvailable && !occupied;
                string suffix = !activity.IsAvailable ? " — unavailable" : occupied ? " — occupied" : "";
                if (GUILayout.Button(activity.DisplayName + suffix, button, GUILayout.MinHeight(68)))
                {
                    state.TryAssign(selected.Id, activity.Id, out var error);
                    message = error ?? "Assigned " + selected.DisplayName + " to " + activity.DisplayName;
                }
                GUI.enabled = true;
            }
            if (GUILayout.Button("Unassign selected character", button, GUILayout.MinHeight(68)))
            {
                state.Unassign(selected.Id);
                message = selected.DisplayName + " is unassigned.";
            }
            GUILayout.Label(message, label);
            GUILayout.Label("Assignments are not XP eligibility. Only one character is active; no character earns XP in this stage.", label);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUI.matrix = previousMatrix;
            GUI.skin = previousSkin;
        }
    }
}
