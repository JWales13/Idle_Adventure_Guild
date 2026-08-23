using System;
using System.Collections.Generic;
using IdleGuild.Adventurers;
using IdleGuild.Core;
using IdleGuild.Economy;
using IdleGuild.Guild;
using IdleGuild.Quests;

namespace IdleGuild.App
{
    /// <summary>
    /// Everything the running game consists of, assembled in one place.
    ///
    /// This is composition, not logic: it owns the guild, the economy, the roster and
    /// the quest log, and it hands out the read-only stats seam those systems share. It
    /// deliberately does not know how to upgrade a building or dispatch a quest — those
    /// are transactions, and they live in the small services beside this class, so this
    /// never becomes the god object the project principles rule out.
    ///
    /// Plain C# with no Unity lifecycle: a test can build a world and run years through
    /// it without a scene.
    /// </summary>
    public sealed class GameWorld
    {
        private readonly List<QuestAssignment> _assignments = new List<QuestAssignment>();

        public GameWorld(GameContent content, IRandomSource random = null)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            GuildTierDefinition startingTier = content.StartingTier;
            if (startingTier == null)
            {
                throw new InvalidOperationException(
                    $"{content.name} lists no guild tiers, so there is no tier for the guild to start at.");
            }

            Content = content;
            Random = random ?? new SystemRandomSource();

            // Empty slots in the Inspector array are routine while content is being
            // authored, and GuildState indexes buildings by Id without a null check.
            // Filtering here keeps a half-filled catalogue from throwing on startup.
            List<BuildingDefinition> buildings = new List<BuildingDefinition>(content.Buildings.Length);
            foreach (BuildingDefinition building in content.Buildings)
            {
                if (building != null)
                {
                    buildings.Add(building);
                }
            }

            GuildState = new GuildState(buildings, startingTier);
            Economy = new PlayerEconomy();
            Roster = new AdventurerRoster();
            QuestLog = new QuestLog();
        }

        public GameContent Content { get; }

        public GuildState GuildState { get; }

        public PlayerEconomy Economy { get; }

        public AdventurerRoster Roster { get; }

        public QuestLog QuestLog { get; }

        public IRandomSource Random { get; }

        /// <summary>The aggregated building effects, as everything outside Guild sees them.</summary>
        public IGuildStats Stats => GuildState;

        /// <summary>Standing orders. Save/load reads this directly.</summary>
        public IReadOnlyList<QuestAssignment> Assignments => _assignments;

        /// <summary>
        /// Grant the new-game balances. Separate from the constructor because loading a
        /// save builds the same world and must not hand out starting gold a second time.
        /// </summary>
        public void ApplyStartingState()
        {
            Economy.Grant(CurrencyType.Gold, Content.StartingGold);
            Economy.Grant(CurrencyType.Reputation, Content.StartingReputation);
        }

        public void AddAssignment(QuestAssignment assignment)
        {
            if (assignment == null || FindAssignment(assignment.Id) != null)
            {
                return;
            }

            _assignments.Add(assignment);
        }

        public bool RemoveAssignment(string assignmentId)
        {
            for (int index = 0; index < _assignments.Count; index++)
            {
                if (_assignments[index].Id == assignmentId)
                {
                    _assignments.RemoveAt(index);
                    return true;
                }
            }

            return false;
        }

        public QuestAssignment FindAssignment(string assignmentId)
        {
            if (string.IsNullOrEmpty(assignmentId))
            {
                return null;
            }

            foreach (QuestAssignment assignment in _assignments)
            {
                if (assignment.Id == assignmentId)
                {
                    return assignment;
                }
            }

            return null;
        }

        /// <summary>The assignment that started a given run, or null.</summary>
        public QuestAssignment FindAssignmentByRun(string questInstanceId)
        {
            if (string.IsNullOrEmpty(questInstanceId))
            {
                return null;
            }

            foreach (QuestAssignment assignment in _assignments)
            {
                if (assignment.ActiveQuestInstanceId == questInstanceId)
                {
                    return assignment;
                }
            }

            return null;
        }

        /// <summary>
        /// True when this adventurer already belongs to a standing order. Idle alone is
        /// not enough to be dispatchable: a member resting between runs of a repeating
        /// assignment is idle, and must not be poached into a second party.
        /// </summary>
        public bool IsAssigned(string adventurerInstanceId)
        {
            if (string.IsNullOrEmpty(adventurerInstanceId))
            {
                return false;
            }

            foreach (QuestAssignment assignment in _assignments)
            {
                foreach (string memberId in assignment.MemberInstanceIds)
                {
                    if (memberId == adventurerInstanceId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Drop every standing order. For save loading, which rebuilds them from scratch.</summary>
        public void ClearAssignments()
        {
            _assignments.Clear();
        }
    }
}
