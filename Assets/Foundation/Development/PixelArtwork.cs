using System;
using System.Collections.Generic;
using UnityEngine;

namespace Idlebound.Foundation.Development
{
    // Original palette-drawn prototype art, authored for Idlebound. No third-party assets.
    // All generated sprites are point filtered. Replace via ActivityStage sprite overrides later.
    internal static class PixelArtwork
    {
        private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        private static Color32 C(uint rgb) => new Color32((byte)((rgb >> 16) & 255), (byte)((rgb >> 8) & 255), (byte)(rgb & 255), 255);
        internal static Sprite Icon(string id)
        {
            string key = id == "hero" ? "hero" : id.Contains("chicken") ? "chicken" : id.Contains("goblin") ? "goblin" : id.Contains("mage") ? "mage" :
                id.Contains("book") || id.Contains("tome") ? "book" : id.Contains("bow") || id.Contains("ranged") ? "bow" : id.Contains("wand") || id.Contains("magic") ? "wand" :
                id.Contains("metal") || id.Contains("mining") ? "ore" : id.Contains("wood") || id.Contains("woodcut") ? "wood" :
                id.Contains("fish") || id.Contains("shrimp") || id.Contains("cook") ? "fish" :
                id.Contains("cloth") || id.Contains("leather") || id.Contains("craft") ? "cloth" :
                id.Contains("helm") || id.Contains("Head") || id.Contains("hood") ? "helm" :
                id.Contains("bow") || id.Contains("ranged") ? "bow" : id.Contains("wand") || id.Contains("magic") ? "wand" :
                id.Contains("feather") ? "feather" : id.Contains("egg") ? "egg" : id.Contains("Body") || id.Contains("body") ? "body" :
                id.Contains("Legs") || id.Contains("legs") ? "legs" : id.Contains("Feet") || id.Contains("boots") ? "boots" :
                id.Contains("Ring") || id.Contains("ring") ? "ring" : id.Contains("Necklace") || id.Contains("necklace") ? "necklace" :
                id.Contains("Cape") || id.Contains("cape") ? "cape" : id.Contains("Offhand") || id.Contains("board") ? "shield" :
                id.Contains("Hands") || id.Contains("gloves") ? "gloves" : "sword";
            if (sprites.TryGetValue(key, out var cached) && cached != null) return cached;
            int w = 48, h = 64; var pixels = new Color32[w * h];
            Action<int, int, int, int, uint> box = (x, y, width, height, color) =>
            { for (int yy = y; yy < y + height; yy++) for (int xx = x; xx < x + width; xx++) if (xx >= 0 && xx < w && yy >= 0 && yy < h) pixels[yy * w + xx] = C(color); };
            Action<Vector2Int[], uint> poly = (points, color) =>
            {
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                {
                    bool inside = false;
                    for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
                        if ((points[i].y > y) != (points[j].y > y) && x < (points[j].x - points[i].x) * (y - points[i].y) / (float)(points[j].y - points[i].y) + points[i].x) inside = !inside;
                    if (inside) pixels[y * w + x] = C(color);
                }
            };
            Action<int[], uint> shape = (coords, color) => { var p = new Vector2Int[coords.Length / 2]; for (int i = 0; i < p.Length; i++) p[i] = new Vector2Int(coords[i * 2], coords[i * 2 + 1]); poly(p, color); };
            if (key == "hero" || key == "goblin" || key == "mage")
            {
                uint cloak = key == "mage" ? 0x65538fU : key == "goblin" ? 0x8c4c35U : 0x315f60U;
                uint skin = key == "goblin" ? 0x8ca65bU : 0xd6a77eU;
                shape(new[] { 15,48, 29,48, 38,9, 8,9 }, 0x111a24);
                shape(new[] { 14,45, 28,45, 35,12, 11,13 }, cloak);
                box(15,5,7,19,0x1c2633); box(26,5,7,19,0x253447); box(12,3,11,6,0x493b32); box(25,3,12,6,0x493b32);
                box(15,23,18,23,0x17232b); box(17,28,14,16,0x62767a); box(19,33,12,9,0x8b9c95);
                box(12,34,6,11,0x82918b); box(31,33,7,10,0x82918b); box(12,25,5,10,skin); box(33,26,5,9,skin);
                box(16,24,17,4,0x6d4d33); box(24,24,4,4,0xd8b76b); box(13,20,5,7,0x8b663d);
                shape(new[] { 14,49, 16,58, 24,62, 32,57, 35,48 }, 0x17212a);
                shape(new[] { 16,49, 17,56, 24,59, 30,55, 32,49 }, cloak);
                box(19,45,12,10,skin); box(20,46,10,3,0xb67c5b); box(20,52,3,2,0x17232b); box(28,52,3,2,0x17232b); box(24,48,4,1,0xe8c298);
                box(18,55,14,2,0x483b32); box(18,44,5,6,0x483b32);
                if (key == "mage") { box(38,14,2,34,0x9b7747); box(35,46,8,8,0x9a83cd); box(38,49,3,3,0xdfddf2); }
                else { shape(new[] { 37,25, 40,25, 43,44, 40,46 }, 0x18232b); shape(new[] { 38,29, 40,29, 42,43, 41,44 }, 0xb9c9c4); box(35,26,8,2,0xb99a5e); }
            }
            else if (key == "chicken")
            {
                shape(new[] { 8,23, 3,36, 16,31, 29,42, 38,40, 39,28, 33,17, 16,16 }, 0x19232a);
                shape(new[] { 9,25, 6,33, 17,29, 28,39, 35,38, 36,28, 31,20, 17,19 }, 0xe8dfc2);
                box(27,39,9,6,0xb84f41); box(35,31,7,4,0xe2b15c); box(31,36,2,2,0x17232b);
                shape(new[] { 14,27, 29,29, 25,21, 17,22 }, 0xb6af98); box(18,10,2,9,0xd2a05a); box(28,10,2,9,0xd2a05a); box(15,9,8,2,0xd2a05a); box(26,9,8,2,0xd2a05a);
            }
            else if (key == "ore") { shape(new[] { 5,17, 12,40, 29,46, 43,29, 36,13 }, 0x26343c); shape(new[] { 10,19, 15,37, 28,41, 38,28, 32,18 }, 0x8e7654); shape(new[] { 15,37, 28,41, 25,29, 10,19 }, 0xc4a378); box(28,23,7,6,0x6c5941); }
            else if (key == "wood") { box(10,15,29,32,0x312a27); box(12,17,25,28,0x8e623b); box(14,18,5,26,0xbf8e50); box(23,18,3,26,0x67452f); box(31,20,3,20,0xa87b45); }
            else if (key == "fish") { shape(new[] { 7,30, 14,38, 29,40, 37,33, 44,39, 42,22, 35,28, 25,22, 14,24 }, 0x293c48); shape(new[] { 9,30, 16,36, 28,37, 36,31, 26,25, 15,27 }, 0x8eb9b5); box(14,31,3,3,0x14252f); box(22,27,7,2,0xe0d7b4); }
            else if (key == "book") { box(9,13,29,37,0x17232b); box(11,15,25,33,0x764e43); box(12,17,4,30,0xb49969); box(19,18,14,3,0xd7c9a1); box(23,28,6,12,0xa994ba); box(20,31,12,5,0xa994ba); }
            else if (key == "bow") { shape(new[] { 15,12, 30,20, 35,32, 29,46, 15,52, 25,43, 29,32, 24,21 }, 0xbb8b50); box(15,12,1,40,0xe4d4a5); }
            else if (key == "wand") { shape(new[] { 13,12, 18,12, 35,43, 31,47 }, 0x9a724e); shape(new[] { 24,44, 34,55, 42,45, 34,37 }, 0x9878bd); box(33,46,3,4,0xddd0ed); }
            else if (key == "ring" || key == "necklace") { shape(new[] { 10,25, 13,39, 23,45, 36,39, 39,26, 30,17, 18,17 }, 0xd8b66b); shape(new[] { 15,26, 17,36, 24,40, 32,36, 34,27, 28,22, 20,22 }, 0x20282e); box(21,39,7,6,0x82b29b); }
            else if (key == "helm") { shape(new[] { 7,22, 9,42, 21,51, 36,45, 41,22, 31,24, 26,37, 20,26 }, 0x96a5a0); box(12,40,21,3,0xd9c7a0); }
            else if (key == "boots" || key == "legs") { box(10,15,10,28,0x638184); box(28,15,10,28,0x638184); box(8,12,15,8,0x8d7050); box(26,12,15,8,0x8d7050); }
            else if (key == "body" || key == "cloth" || key == "cape" || key == "gloves") { shape(new[] { 9,13, 13,44, 19,50, 29,50, 36,43, 40,13 }, 0x335d61); box(15,15,4,27,0x668d89); box(21,44,7,5,0xc3a76c); }
            else if (key == "shield") { shape(new[] { 8,46, 39,46, 35,23, 24,12, 12,23 }, 0xb99d65); shape(new[] { 12,42, 35,42, 32,25, 24,17, 16,25 }, 0x49686a); }
            else if (key == "feather") { shape(new[] { 11,15, 16,41, 32,52, 36,42, 27,22 }, 0xcbd2c1); box(19,13,2,24,0x887951); }
            else if (key == "egg") { shape(new[] { 13,18, 9,27, 15,43, 23,48, 32,43, 38,27, 34,18 }, 0xdbcfad); box(17,25,4,12,0xf1e4c1); }
            else { shape(new[] { 11,11, 16,11, 38,47, 37,53, 31,50 }, 0xc0cbc5); box(11,17,16,3,0xc8a265); box(14,9,4,12,0x795036); }
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "Idlebound original " + key, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels); texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(.5f, 0), 16);
            sprites[key] = sprite; return sprite;
        }
    }
}
