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
        private readonly string[] _memberInstanceIds;

        public QuestAssignment(string id, QuestDefinition quest, IReadOnlyList<string> memberInstanceIds, bool repeat)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Assignments need a stable id.", nameof(id));
            }

            Id = id;
            Quest = quest != null ? quest : throw new ArgumentNullException(nameof(quest));
            Repeat = repeat;

            _memberInstanceIds = new string[memberInstanceIds?.Count ?? 0];
            for (int index = 0; index < _memberInstanceIds.Length; index++)
            {
                _memberInstanceIds[index] = memberInstanceIds[index];
            }
        }

        public string Id { get; }

        public QuestDefinition Quest { get; }

        /// <summary>The party, by adventurer instance id. Fixed for the life of the assignment.</summary>
        public IReadOnlyList<string> MemberInstanceIds => _memberInstanceIds;

        /// <summary>
        /// Whether to start again after each run. Settable, so the player can call the
        /// party home after the current run without cancelling it mid-flight.
        /// </summary>
        public bool Repeat { get; set; }

        /// <summary>The run in progress, or null while the party is resting between runs.</summary>
        public string ActiveQuestInstanceId { get; private set; }

        public bool IsRunning => !string.IsNullOrEmpty(ActiveQuestInstanceId);

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
