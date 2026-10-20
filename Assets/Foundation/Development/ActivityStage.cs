using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Idlebound.Foundation.Development
{
    // Cosmetic projection only: never settles time, grants rewards, or drives an attack.
    public sealed class ActivityStage : MonoBehaviour
    {
        [SerializeField] private Sprite adventurerOverride;
        [SerializeField] private Sprite enemyOverride;
        private GameSessionHost host;
        private string characterId;
        private RectTransform hero, enemy;
        private Image target;
        private Text status, feedback;
        private long previousCycles, previousKills;
        private decimal previousXp;
        private string previousAssignment;
        private float feedbackUntil;
        public void Initialize(GameSessionHost owner, string id, Sprite heroSprite = null, Sprite targetSprite = null, bool showCaptions = true)
        {
            host = owner; characterId = id;
            if (heroSprite != null) adventurerOverride = heroSprite;
            if (targetSprite != null) enemyOverride = targetSprite;
            var r = (RectTransform)transform;
            MobileUiKit.Surface(r, new Color32(24, 36, 42, 255));
            var moon = MobileUiKit.Node("Moon", r); moon.anchorMin = moon.anchorMax = new Vector2(.82f, .8f); moon.sizeDelta = new Vector2(30,30);
            MobileUiKit.Surface(moon, new Color32(145, 161, 155, 255));
            for (int i = 0; i < 7; i++)
            {
                var rock = MobileUiKit.Node("Distant stone", r); rock.anchorMin = rock.anchorMax = new Vector2(i / 6f, .2f);
                rock.sizeDelta = new Vector2(70, 30 + (i % 3) * 20); MobileUiKit.Surface(rock, new Color32(34, 49, 53, 255));
            }
            var ground = MobileUiKit.Node("Ground", r); ground.anchorMin = Vector2.zero; ground.anchorMax = new Vector2(1,.18f); ground.offsetMin = ground.offsetMax = Vector2.zero;
            MobileUiKit.Surface(ground, new Color32(49, 54, 48, 255));
            hero = Actor(r, "Adventurer", adventurerOverride != null ? adventurerOverride : PixelArtwork.Icon("hero"), .3f);
            enemy = Actor(r, "Activity target", PixelArtwork.Icon("mining"), .76f); target = enemy.GetComponent<Image>();
            status = MobileUiKit.Label(r, "", 19, MobileUiKit.Muted); Place(status.rectTransform, new Vector2(.5f,.9f), new Vector2(370,32)); status.alignment = TextAnchor.MiddleCenter;
            feedback = MobileUiKit.Label(r, "", 20, MobileUiKit.Gold); Place(feedback.rectTransform, new Vector2(.5f,.17f), new Vector2(370,32)); feedback.alignment = TextAnchor.MiddleCenter;
            status.gameObject.SetActive(showCaptions); feedback.gameObject.SetActive(showCaptions);
            Capture();
        }
        private static RectTransform Actor(Transform parent, string name, Sprite sprite, float x)
        {
            var r = MobileUiKit.Node(name, parent); Place(r, new Vector2(x,.5f), new Vector2(105,140));
            var i = r.gameObject.AddComponent<Image>(); i.sprite = sprite; i.preserveAspect = true; i.raycastTarget = false; return r;
        }
        private static void Place(RectTransform r, Vector2 anchor, Vector2 size)
        { r.anchorMin = r.anchorMax = anchor; r.anchoredPosition = Vector2.zero; r.sizeDelta = size; }
        private void Capture()
        {
            var s = host.Runtime.Session; s.Assignments.TryGetValue(characterId, out previousAssignment);
            previousCycles = s.CompletedCycles(characterId);
            previousKills = previousAssignment == null ? 0 : s.KillsFor(characterId, previousAssignment);
            previousXp = s.Characters.First(c => c.Id == characterId).Skills.Sum(k => k.StoredXp);
        }
        private void Update()
        {
            if (host == null || hero == null) return;
            var s = host.Runtime.Session; var view = host.Model.Character(characterId);
            s.Assignments.TryGetValue(characterId, out var assignment);
            if (assignment != previousAssignment) Capture();
            bool combat = s.Encounters.TryGetValue(characterId, out var fight);
            target.sprite = combat ? enemyOverride != null ? enemyOverride : PixelArtwork.Icon(fight.MonsterId) : PixelArtwork.Icon(assignment ?? "mining");
            enemy.gameObject.SetActive(assignment != null);
            double phase = view.Status == WorkStatus.Working ? view.Progress : 0;
            // Lean near a real attack/cycle boundary; animation has no independent action clock.
            float lean = view.Status == WorkStatus.Working && phase > .72 ? (float)Math.Sin((phase - .72) / .28 * Math.PI) : 0;
            hero.anchoredPosition = new Vector2(lean * 15, (float)Math.Sin(Time.unscaledTimeAsDouble * 2) * 1.5f);
            hero.localRotation = Quaternion.Euler(0,0,-lean * (combat ? 8 : 5));
            status.text = combat ? "HP " + view.Hp + " / " + view.MaxHp + "    Enemy " + fight.EnemyHp + " HP" : view.Assignment;
            long cycles = s.CompletedCycles(characterId), kills = assignment == null ? 0 : s.KillsFor(characterId, assignment);
            decimal xp = s.Characters.First(c => c.Id == characterId).Skills.Sum(k => k.StoredXp);
            if (kills > previousKills || cycles > previousCycles || xp > previousXp)
            {
                string text = kills > previousKills ? "Defeated × " + (kills - previousKills) + " • loot sent to bank" : cycles > previousCycles ? "Completed × " + (cycles - previousCycles) : "";
                if (xp > previousXp) text += "  +" + (xp - previousXp).ToString("0.##") + " XP";
                feedback.text = text; feedbackUntil = Time.unscaledTime + 2;
            }
            previousCycles = cycles; previousKills = kills; previousXp = xp;
            if (Time.unscaledTime > feedbackUntil) feedback.text = "";
        }
    }
}
