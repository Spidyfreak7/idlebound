using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Idlebound.Foundation.Development
{
    // UI geometry only. The character has a reserved central rectangle; no slot covers it.
    public sealed class EquipmentLayout
    {
        public const float Height = 320;
        public Rect CharacterBounds { get; }
        public ReadOnlyDictionary<EquipmentSlot, Rect> Slots { get; }
        public EquipmentLayout(float width)
        {
            if (float.IsNaN(width) || float.IsInfinity(width) || width < 200) throw new ArgumentOutOfRangeException(nameof(width));
            const float padding = 12, gap = 14;
            float side = Mathf.Clamp(width * .15f, 56, 68);
            float clusterWidth = Mathf.Min(width - 2 * padding,304);
            float left = (width - clusterWidth + side) / 2, right = width - left;
            float bottom = padding + side / 2, top = Height - bottom;
            float characterWidth = Mathf.Min(112,right - left - side - 2 * gap);
            float characterHeight = Mathf.Min(130,Height - 2 * (padding + side + gap));
            CharacterBounds = new Rect((width - characterWidth) / 2,(Height - characterHeight) / 2,characterWidth,characterHeight);
            var slots = new Dictionary<EquipmentSlot, Rect>();
            Action<EquipmentSlot, float, float> add = (slot,x,fraction) =>
                slots.Add(slot,new Rect(x - side / 2,Mathf.Lerp(bottom,top,fraction) - side / 2,side,side));
            add(EquipmentSlot.Head,width / 2,1);
            add(EquipmentSlot.Cape,left,1);
            add(EquipmentSlot.Necklace,right,1);
            add(EquipmentSlot.Body,right,2f / 3);
            add(EquipmentSlot.Mainhand,left,2f / 3);
            add(EquipmentSlot.Offhand,right,1f / 3);
            add(EquipmentSlot.Hands,left,1f / 3);
            add(EquipmentSlot.Legs,right,0);
            add(EquipmentSlot.Ring,left,0);
            add(EquipmentSlot.Feet,width / 2,0);
            Slots = new ReadOnlyDictionary<EquipmentSlot, Rect>(slots);
        }
    }
}
