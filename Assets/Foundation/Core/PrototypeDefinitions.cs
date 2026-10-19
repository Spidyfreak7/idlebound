using System.Linq;

namespace Idlebound.Foundation
{
    // Synthetic fixture, NOT approved skill/activity names, unlock rules, or starting stats.
    public static class PrototypeDefinitions
    {
        public static GameState Create()
        {
            var skills = Enumerable.Range(1, 9)
                .Select(i => new SkillDefinition("prototype-skill-" + i, "Prototype skill " + i)).ToArray();
            var activities = Enumerable.Range(1, 4).Select(i => new ActivityDefinition(
                "prototype-activity-" + i, "Prototype activity " + i,
                skills[(i - 1) % skills.Length].Id, i != 4)).ToArray();
            var state = new GameState(new RosterAccess(), skills, activities);
            for (int i = 1; i <= RosterAccess.InitialAvailableSlots; i++)
                state.TryAddCharacter(new CharacterState("character-" + i, "Character " + i,
                    skills.Select(s => new SkillState(s.Id, 1, 0))));
            return state;
        }
    }
}
