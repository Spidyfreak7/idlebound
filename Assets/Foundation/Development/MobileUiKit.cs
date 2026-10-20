using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Idlebound.Foundation.Development
{
    internal static class MobileUiKit
    {
        internal static readonly Color Background = new Color32(17, 22, 27, 255);
        internal static readonly Color Panel = new Color32(32, 40, 46, 255);
        internal static readonly Color Raised = new Color32(46, 55, 60, 255);
        internal static readonly Color Gold = new Color32(220, 182, 107, 255);
        internal static readonly Color Ink = new Color32(230, 230, 219, 255);
        internal static readonly Color Muted = new Color32(159, 174, 178, 255);
        internal static readonly Color Good = new Color32(116, 212, 156, 255);
        internal static readonly Color Bad = new Color32(246, 137, 129, 255);
        private static Font font;
        internal static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }
        internal static void Fill(RectTransform rect, float margin = 0)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(margin, margin); rect.offsetMax = new Vector2(-margin, -margin); }
        internal static LayoutElement Size(Component node, float height = -1, float flex = 0)
        {
            var e = node.GetComponent<LayoutElement>() ?? node.gameObject.AddComponent<LayoutElement>();
            if (height >= 0) { e.minHeight = height; e.preferredHeight = height; }
            e.flexibleHeight = flex; return e;
        }
        internal static Image Surface(RectTransform rect, Color color)
        { var i = rect.gameObject.AddComponent<Image>(); i.color = color; i.raycastTarget = false; return i; }
        internal static RectTransform Column(Transform parent, string name = "Column", int padding = 0)
        {
            var r = Node(name, parent); var l = r.gameObject.AddComponent<VerticalLayoutGroup>();
            l.padding = new RectOffset(padding, padding, padding, padding); l.spacing = 10;
            l.childControlWidth = l.childControlHeight = true; l.childForceExpandWidth = true; l.childForceExpandHeight = false;
            return r;
        }
        internal static RectTransform Row(Transform parent, float height = 64)
        {
            var r = Node("Row", parent); Size(r, height);
            var l = r.gameObject.AddComponent<HorizontalLayoutGroup>(); l.spacing = 8;
            l.childControlWidth = l.childControlHeight = true; l.childForceExpandWidth = true; l.childForceExpandHeight = true;
            return r;
        }
        internal static Text Label(Transform parent, string value, int size = 24, Color? color = null, float height = -1)
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var r = Node("Text", parent); var t = r.gameObject.AddComponent<Text>(); t.font = font; t.text = value;
            t.fontSize = size; t.color = color ?? Ink; t.supportRichText = false; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
            t.alignment = TextAnchor.MiddleLeft;
            if (height >= 0) Size(r, height); return t;
        }
        internal static Button Button(Transform parent, string value, UnityAction action, bool selected = false, float height = 68)
        {
            var r = Node(value, parent); Size(r, height);
            var i = Surface(r, selected ? new Color32(96, 78, 46, 255) : Raised); i.raycastTarget = true;
            var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = i;
            var colors = b.colors; colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f); colors.pressedColor = new Color(.7f, .7f, .7f);
            colors.disabledColor = new Color(.5f, .5f, .5f); b.colors = colors; b.onClick.AddListener(action);
            var text = Label(r, value, 23, selected ? Gold : Ink); text.alignment = TextAnchor.MiddleCenter;
            Fill((RectTransform)text.transform, 8); return b;
        }
        internal static RectTransform Card(Transform parent)
        { var r = Column(parent, "Card", 14); Surface(r, Panel); return r; }
        internal static Image Progress(Transform parent)
        {
            var r = Node("Progress", parent); Size(r, 10); Surface(r, Raised);
            var child = Node("Fill", r); Fill(child); var i = Surface(child, Gold); return i;
        }
        internal static void SetProgress(Image image, double fraction)
        { var r = image.rectTransform; r.anchorMax = new Vector2(Mathf.Clamp01((float)fraction), 1); r.offsetMin = r.offsetMax = Vector2.zero; }
        internal static Image Icon(Transform parent, string id, float size = 60)
        {
            var r = Node("Icon", parent); r.sizeDelta = new Vector2(size,size); var e = Size(r, size); e.minWidth = size; e.preferredWidth = size; e.flexibleWidth = 0;
            var i = r.gameObject.AddComponent<Image>(); i.sprite = PixelArtwork.Icon(id); i.preserveAspect = true; i.raycastTarget = false; return i;
        }
        internal static void Clear(Transform parent)
        { for (int i = parent.childCount - 1; i >= 0; i--) { var go = parent.GetChild(i).gameObject; go.SetActive(false); UnityEngine.Object.Destroy(go); } }
    }
}
