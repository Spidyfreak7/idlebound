using System;
using System.Collections.Generic;
using System.Linq;

namespace Idlebound.Foundation
{
    // Observes existing counters only. Never settles time or claims per-character bank attribution.
    public sealed class ActivityHeaderFeedback
    {
        public GatheringSession Session { get; }
        public string Latest { get; private set; } = "";
        private string observedId;
        private long cycles;
        private decimal defeats;
        private Dictionary<string, decimal> xp;
        public ActivityHeaderFeedback(GatheringSession session)
        { Session = session ?? throw new ArgumentNullException(nameof(session)); }
        public void Observe(string characterId)
        {
            var character = Session.Characters.First(c => c.Id == characterId);
            long nextCycles = Session.CompletedCycles(characterId);
            decimal nextDefeats = Session.Combat.Monsters.Sum(m => (decimal)Session.KillsFor(characterId,m.Id));
            var nextXp = character.Skills.ToDictionary(s => s.SkillId,s => s.StoredXp);
            if (observedId != characterId) Latest = "";
            else
            {
                var messages = new List<string>();
                if (nextDefeats > defeats) messages.Add("Defeated × " + (nextDefeats - defeats));
                if (nextCycles > cycles) messages.Add("Completed × " + (nextCycles - cycles));
                foreach (var skill in Session.Skills)
                {
                    decimal delta = nextXp[skill.Id] - xp[skill.Id];
                    if (delta > 0) messages.Add("+" + delta.ToString("0.##") + " " + skill.DisplayName + " XP");
                }
                if (messages.Count > 0) Latest = string.Join(" • ",messages);
            }
            observedId = characterId; cycles = nextCycles; defeats = nextDefeats; xp = nextXp;
        }
    }
}
