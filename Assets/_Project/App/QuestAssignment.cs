using System;
using System.Collections.Generic;
using IdleGuild.Quests;

namespace IdleGuild.App
{
    /// <summary>
    /// A standing order: this party runs this quest, and — if repeating — runs it again
    /// each time they finish resting.
    ///
    /// This is what makes the game idle. An <see cref="ActiveQuest"/> is one run and
    /// ends; an assignment outlives the run, holds the party together across rests, and
    /// is the thing the player actually sets up. It is also what offline catch-up
    /// replays: without a standing order there would be nothing to repeat while the app
    /// is closed.
    /// </summary>
    public sealed class QuestAssignment
    {
        private string[] _memberInstanceIds;

        public QuestAssignment(string id, QuestDefinition quest, IReadOnlyList<string> memberInstanceIds, bool repeat)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Assignments need a stable id.", nameof(id));
            }

            Id = id;
            Quest = quest != null ? quest : throw new ArgumentNullException(nameof(quest));
            Repeat = repeat;

            SetParty(memberInstanceIds);
        }

        public string Id { get; }

        public QuestDefinition Quest { get; }

        /// <summary>
        /// The party, by adventurer instance id.
        ///
        /// Fixed for the life of a <i>run</i>, not of the order — a distinction that only
        /// became visible on Day 12. <see cref="ActiveQuest"/> snapshots its own party at
        /// dispatch and the clock sends that snapshot home, so replacing this list never
        /// disturbs a run already in flight. It decides who goes out next time.
        /// </summary>
        public IReadOnlyList<string> MemberInstanceIds => _memberInstanceIds;

        /// <summary>
        /// Whether to start again after each run. Settable, so the player can call the
        /// party home after the current run without cancelling it mid-flight.
        /// </summary>
        public bool Repeat { get; set; }

        /// <summary>The run in progress, or null while the party is resting between runs.</summary>
        public string ActiveQuestInstanceId { get; private set; }

        public bool IsRunning => !string.IsNullOrEmpty(ActiveQuestInstanceId);

        /// <summary>
        /// Replace the party, from the next run onwards.
        ///
        /// Day 12 made this settable, and the reason is worth keeping written down: an
        /// order whose party could never change meant that hiring somebody better did
        /// nothing at all until the player worked out for themselves that they had to
        /// cancel the order and dispatch again. The best adventurer in the game could sit
        /// on the bench indefinitely with nothing on screen saying why.
        ///
        /// Who may join is not decided here. <see cref="QuestDispatchService.TryReformParty"/>
        /// is the only thing that should call this — as everywhere else in the model, the
        /// data holds no rules.
        /// </summary>
        public void SetParty(IReadOnlyList<string> memberInstanceIds)
        {
            string[] replacement = new string[memberInstanceIds?.Count ?? 0];
            for (int index = 0; index < replacement.Length; index++)
            {
                replacement[index] = memberInstanceIds[index];
            }

            _memberInstanceIds = replacement;
        }

        public void MarkStarted(string questInstanceId)
        {
            ActiveQuestInstanceId = questInstanceId;
        }

        public void MarkFinished()
        {
            ActiveQuestInstanceId = null;
        }
    }
}
