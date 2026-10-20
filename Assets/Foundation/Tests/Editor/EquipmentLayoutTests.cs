using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Idlebound.Foundation.Development;

namespace Idlebound.Foundation.Tests
{
    public sealed class EquipmentLayoutTests
    {
        [TestCase(200)]
        [TestCase(280)]
        [TestCase(320)]
        [TestCase(360)]
        [TestCase(448)]
        [TestCase(456)]
        [TestCase(480)]
        [TestCase(640)]
        public void EverySlotIsSquareAccessibleAndClearOfTheCharacterAtPortraitWidths(float width)
        {
            var layout = new EquipmentLayout(width);
            Assert.That(layout.Slots.Keys,Is.EquivalentTo(Enum.GetValues(typeof(EquipmentSlot)).Cast<EquipmentSlot>()));
            Assert.That(layout.CharacterBounds.width,Is.GreaterThan(0));
            Assert.That(layout.CharacterBounds.height,Is.GreaterThan(0));
            foreach (var slot in layout.Slots)
            {
                Rect bounds = slot.Value;
                Assert.That(bounds.width,Is.EqualTo(bounds.height)); Assert.That(bounds.width,Is.InRange(56,72));
                Assert.That(bounds.xMin,Is.GreaterThanOrEqualTo(0)); Assert.That(bounds.yMin,Is.GreaterThanOrEqualTo(0));
                Assert.That(bounds.xMax,Is.LessThanOrEqualTo(width)); Assert.That(bounds.yMax,Is.LessThanOrEqualTo(EquipmentLayout.Height));
                Assert.That(bounds.Overlaps(layout.CharacterBounds),Is.False,slot.Key + " obscures the adventurer.");
                foreach (var other in layout.Slots.Where(p => p.Key != slot.Key))
                    Assert.That(bounds.Overlaps(other.Value),Is.False,slot.Key + " overlaps " + other.Key);
            }
        }
        [Test]
        public void NormalPhoneLayoutGivesTheCharacterMoreRoomThanThePreviousLayout()
        {
            var layout = new EquipmentLayout(448);
            Assert.That(layout.CharacterBounds.width,Is.GreaterThan(150));
            Assert.That(layout.CharacterBounds.height,Is.GreaterThan(230));
            Assert.That(layout.Slots[EquipmentSlot.Mainhand].center.x,Is.LessThan(layout.CharacterBounds.xMin));
            Assert.That(layout.Slots[EquipmentSlot.Offhand].center.x,Is.GreaterThan(layout.CharacterBounds.xMax));
            Assert.That(layout.Slots[EquipmentSlot.Head].yMin,Is.GreaterThan(layout.CharacterBounds.yMax));
            Assert.That(layout.Slots[EquipmentSlot.Feet].yMax,Is.LessThan(layout.CharacterBounds.yMin));
        }
    }
}
