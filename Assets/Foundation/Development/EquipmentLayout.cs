using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Idlebound.Foundation.Development
{
    // UI geometry only. The character has a reserved central rectangle; no slot covers it.
    public sealed class EquipmentLayout
    {
        public const float Height = 500;
        public Rect CharacterBounds { get; }
        public ReadOnlyDictionary<EquipmentSlot, Rect> Slots { get; }
        public EquipmentLayout(float width)
        {
            if (float.IsNaN(width) || float.IsInfinity(width) || width < 200) throw new ArgumentOutOfRangeException(nameof(width));
            const float padding = 12, gap = 14;
            float side = Mathf.Clamp(width * .15f, 56, 72);
            float left = padding + side / 2, right = width - left;
            float bottom = left, top = Height - left;
            float inset = padding + side + gap;
            CharacterBounds = new Rect(inset, inset, width - 2 * inset, Height - 2 * inset);
            var slots = new Dictionary<EquipmentSlot, Rect>();
            Action<EquipmentSlot, float, float> add = (slot,x,fraction) =>
                slots.Add(slot,new Rect(x - side / 2,Mathf.Lerp(bottom,top,fraction) - side / 2,side,side));
            add(EquipmentSlot.Head,width / 2,1);
            add(EquipmentSlot.Cape,left,1);
            add(EquipmentSlot.Necklace,right,1);
            add(EquipmentSlot.Body,right,.78f);
            add(EquipmentSlot.Mainhand,left,.56f);
            add(EquipmentSlot.Offhand,right,.56f);
            add(EquipmentSlot.Hands,left,.34f);
            add(EquipmentSlot.Legs,right,.34f);
            add(EquipmentSlot.Ring,left,.12f);
            add(EquipmentSlot.Feet,width / 2,0);
            Slots = new ReadOnlyDictionary<EquipmentSlot, Rect>(slots);
        }
    }
}
