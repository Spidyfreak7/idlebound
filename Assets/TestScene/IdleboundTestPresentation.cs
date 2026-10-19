using UnityEngine;
using UnityEngine.UI;

namespace Idlebound.TestScene
{
    // Open Assets/Scenes/IdleboundTest.unity and enter Play mode.
    // Presentation is created only at runtime; no gameplay or persistent state.
    public sealed class IdleboundTestPresentation : MonoBehaviour
    {
        private RectTransform character;
        private float elapsed;

        private void Awake()
        {
            var canvasObject = new GameObject("Idlebound Test UI", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            var background = Panel("Background", canvasObject.transform,
                new Vector2(720, 1280), Vector2.zero, new Color(0.035f, 0.055f, 0.09f));
            background.anchorMin = Vector2.zero;
            background.anchorMax = Vector2.one;
            background.sizeDelta = Vector2.zero;

            // LegacyRuntime.ttf is Unity's built-in font; no TMP assets/import required.
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Label("Title", canvasObject.transform, "IDLEBOUND", font, 68,
                new Vector2(640, 120), new Vector2(0, 300));
            Label("Stage Label", canvasObject.transform, "Development Build — Stage 8A", font, 26,
                new Vector2(660, 70), new Vector2(0, -310));

            Panel("Ground", canvasObject.transform, new Vector2(300, 8),
                new Vector2(0, -150), new Color(0.18f, 0.25f, 0.33f));
            character = new GameObject("Character Placeholder", typeof(RectTransform))
                .GetComponent<RectTransform>();
            character.SetParent(canvasObject.transform, false);
            character.anchorMin = character.anchorMax = new Vector2(0.5f, 0.5f);
            character.sizeDelta = new Vector2(160, 220);
            Panel("Body", character, new Vector2(120, 140), new Vector2(0, -25),
                new Color(0.2f, 0.75f, 0.72f));
            Panel("Head", character, new Vector2(100, 90), new Vector2(0, 85),
                new Color(0.96f, 0.73f, 0.43f));
            Panel("Left Eye", character, new Vector2(10, 12), new Vector2(-20, 90),
                new Color(0.035f, 0.055f, 0.09f));
            Panel("Right Eye", character, new Vector2(10, 12), new Vector2(20, 90),
                new Color(0.035f, 0.055f, 0.09f));
        }

        private void Update()
        {
            // Keep the timer within one cycle to avoid long-session float precision loss.
            elapsed = Mathf.Repeat(elapsed + Time.unscaledDeltaTime, 4f);
            character.anchoredPosition = new Vector2(0, Mathf.Sin(elapsed * Mathf.PI * 0.5f) * 8);
        }

        private static RectTransform Panel(string name, Transform parent, Vector2 size,
            Vector2 position, Color color)
        {
            var rect = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            var image = rect.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static void Label(string name, Transform parent, string content, Font font,
            int fontSize, Vector2 size, Vector2 position)
        {
            var rect = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Text)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            var text = rect.GetComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.94f, 0.96f, 1);
            text.raycastTarget = false;
        }
    }
}
