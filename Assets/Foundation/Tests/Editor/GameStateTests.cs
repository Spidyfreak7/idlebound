using System;
using System.Linq;
using NUnit.Framework;

namespace Idlebound.Foundation.Tests
{
    public sealed class GameStateTests
    {
        private static CharacterState Character(GameState state, string id) =>
            new CharacterState(id, id, state.Skills.Select(s => new SkillState(s.Id, 1, 0)));

        [Test]
        public void InitialRosterHasThreeCharactersAndOneActive()
        {
            var s = PrototypeDefinitions.Create();
            Assert.That(s.Characters.Count, Is.EqualTo(3));
            Assert.That(s.Characters.Count(c => s.IsActiveCharacter(c.Id)), Is.EqualTo(1));
            Assert.That(s.TryAddCharacter(Character(s, "extra")), Is.False);
        }

        [Test]
        public void CapacityCannotExceedNineEvenWithAccess()
        {
            var fixture = PrototypeDefinitions.Create();
            var s = new GameState(new RosterAccess(9), fixture.Skills, fixture.Activities);
            for (int i = 0; i < 9; i++) Assert.That(s.TryAddCharacter(Character(s, "c" + i)), Is.True);
            Assert.That(s.TryAddCharacter(Character(s, "tenth")), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => new RosterAccess(10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RosterAccess(2));
        }

        [TestCase(0)]
        [TestCase(100)]
        public void InvalidSkillLevelRejected(int level) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new SkillState("skill", level, 0));

        [TestCase(1)]
        [TestCase(99)]
        public void ValidSkillLevelAndXpPreserved(int level)
        {
            var skill = new SkillState("skill", level, long.MaxValue);
            Assert.That(skill.Level, Is.EqualTo(level));
            Assert.That(skill.StoredXp, Is.EqualTo(long.MaxValue));
        }

        [Test]
        public void NegativeXpRejected() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new SkillState("skill", 1, -1));

        [Test]
        public void ExactlyNineDistinctSkillsRequired()
        {
            Assert.Throws<ArgumentException>(() => new CharacterState("c", "c",
                Enumerable.Range(0, 8).Select(i => new SkillState("s" + i, 1, 0))));
            Assert.Throws<ArgumentException>(() => new CharacterState("c", "c",
                Enumerable.Range(0, 9).Select(i => new SkillState("same", 1, 0))));
        }

        [Test]
        public void SelectionReplacesActiveAndRejectsUnknown()
        {
            var s = PrototypeDefinitions.Create();
            Assert.That(s.TrySelectActive("character-2"), Is.True);
            Assert.That(s.IsActiveCharacter("character-1"), Is.False);
            Assert.That(s.TrySelectActive("missing"), Is.False);
            Assert.That(s.ActiveCharacterId, Is.EqualTo("character-2"));
            Assert.That(s.Characters.Count(c => s.IsActiveCharacter(c.Id)), Is.EqualTo(1));
        }

        [Test]
        public void AssignmentIsUniqueAndFailedReassignmentIsAtomic()
        {
            var s = PrototypeDefinitions.Create();
            Assert.That(s.TryAssign("character-1", "prototype-activity-1", out _), Is.True);
            Assert.That(s.TryAssign("character-2", "prototype-activity-2", out _), Is.True);
            Assert.That(s.TryAssign("character-2", "prototype-activity-1", out _), Is.False);
            Assert.That(s.Assignments["character-2"], Is.EqualTo("prototype-activity-2"));
            Assert.That(s.TryAssign("character-1", "prototype-activity-1", out _), Is.True);
            Assert.That(s.Assignments.Count, Is.EqualTo(2));
        }

        [Test]
        public void ReassignmentAndUnassignmentReleaseExactActivity()
        {
            var s = PrototypeDefinitions.Create();
            s.TryAssign("character-1", "prototype-activity-1", out _);
            Assert.That(s.TryAssign("character-1", "prototype-activity-2", out _), Is.True);
            Assert.That(s.TryAssign("character-2", "prototype-activity-1", out _), Is.True);
            Assert.That(s.Unassign("character-1"), Is.True);
            Assert.That(s.Unassign("character-1"), Is.False);
            Assert.That(s.TryAssign("character-3", "prototype-activity-2", out _), Is.True);
        }

        [Test]
        public void MissingUnavailableAndUnknownCharacterRejected()
        {
            var s = PrototypeDefinitions.Create();
            Assert.That(s.TryAssign("character-1", "prototype-activity-4", out _), Is.False);
            Assert.That(s.TryAssign("character-1", null, out _), Is.False);
            Assert.That(s.TryAssign("missing", "prototype-activity-1", out _), Is.False);
            Assert.That(s.Assignments, Is.Empty);
        }

        [Test]
        public void InactiveAssignmentsDoNotSelectCharacterOrGenerateXp()
        {
            var s = PrototypeDefinitions.Create();
            s.TryAssign("character-2", "prototype-activity-1", out _);
            Assert.That(s.ActiveCharacterId, Is.EqualTo("character-1"));
            Assert.That(s.Characters.SelectMany(c => c.Skills).All(k => k.StoredXp == 0), Is.True);
        }

        [Test]
        public void DuplicateCharacterAndUnknownSkillsRejected()
        {
            var fixture = PrototypeDefinitions.Create();
            var s = new GameState(new RosterAccess(9), fixture.Skills, fixture.Activities);
            Assert.That(s.TryAddCharacter(Character(s, "same")), Is.True);
            Assert.That(s.TryAddCharacter(Character(s, "same")), Is.False);
            var invalid = new CharacterState("other", "other", Enumerable.Range(1, 9)
                .Select(i => new SkillState("undefined-" + i, 1, 0)));
            Assert.That(s.TryAddCharacter(invalid), Is.False);
        }
    }
}
