using System.Linq;

namespace Idlebound.Foundation
{
    // Legacy assignment-test fixture. Skill names are approved; activities remain synthetic.
    public static class PrototypeDefinitions
    {
        public static GameState Create()
        {
            var skills = SkillCatalog.Create().ToArray();
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
