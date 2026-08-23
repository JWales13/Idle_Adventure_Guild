using System;
using System.Collections.Generic;
using IdleGuild.Adventurers;
using IdleGuild.Core;
using IdleGuild.Guild;
using IdleGuild.Quests;
using UnityEngine;

namespace IdleGuild.App.Saves
{
    /// <summary>
    /// What restoring a save had to fix up on the way in.
    ///
    /// Worth reporting rather than swallowing: every non-zero count here means the save
    /// referred to content the catalogue no longer has, which during Weeks 2 and 3 is
    /// far more likely to be a mistake in an asset than a legitimate content removal.
    /// </summary>
    public readonly struct SaveRestoreReport
    {
        public SaveRestoreReport(
            int unknownBuildings,
            int droppedAdventurers,
            int droppedQuestRuns,
            int droppedAssignments,
            int repairedAdventurers,
            bool tierFellBack)
        {
            UnknownBuildings = unknownBuildings;
            DroppedAdventurers = droppedAdventurers;
            DroppedQuestRuns = droppedQuestRuns;
            DroppedAssignments = droppedAssignments;
            RepairedAdventurers = repairedAdventurers;
            TierFellBack = tierFellBack;
        }

        /// <summary>Saved building ids the catalogue no longer contains. Their levels are lost.</summary>
        public int UnknownBuildings { get; }

        /// <summary>Roster members whose archetype is gone, or whose id was unusable.</summary>
        public int DroppedAdventurers { get; }

        /// <summary>Runs in flight whose quest is gone. The party is sent home instead.</summary>
        public int DroppedQuestRuns { get; }

        /// <summary>Standing orders whose quest or party no longer exists.</summary>
        public int DroppedAssignments { get; }

        /// <summary>Members found out on a quest that is not in the log, and sent home.</summary>
        public int RepairedAdventurers { get; }

        /// <summary>True when the saved tier id was not found and the guild fell back to the first tier.</summary>
        public bool TierFellBack { get; }

        public bool HasRepairs =>
            UnknownBuildings > 0 || DroppedAdventurers > 0 || DroppedQuestRuns > 0 ||
            DroppedAssignments > 0 || RepairedAdventurers > 0 || TierFellBack;

        public override string ToString()
        {
            if (!HasRepairs)
            {
                return "clean";
            }

            return $"{UnknownBuildings} unknown building(s), {DroppedAdventurers} adventurer(s) dropped, " +
                   $"{DroppedQuestRuns} run(s) dropped, {DroppedAssignments} order(s) dropped, " +
                   $"{RepairedAdventurers} adventurer(s) sent home" + (TierFellBack ? ", tier fell back" : string.Empty);
        }
    }

    /// <summary>
    /// Rebuilding a guild from a save.
    ///
    /// Two rules run through all of it. The first is that restoration is *quiet*: it uses
    /// the Restore-shaped methods that skip the transitions gameplay enforces, so loading
    /// a level-4 Tavern does not announce four upgrades and loading a City guild does not
    /// congratulate the player on reaching City again.
    ///
    /// The second is that a save is never trusted to be consistent with today's content.
    /// A quest asset renamed in Week 2, an adventurer archetype cut in Week 3, a building
    /// id fixed after a typo — each one leaves saves in the wild pointing at something
    /// that no longer exists, and the difference between a game that survives that and
    /// one that does not is entirely in this file. Anything unresolvable is dropped, the
    /// guild around it is left standing, and the drop is counted in the returned report
    /// rather than passing in silence.
    ///
    /// The order below is not arbitrary. Tier and buildings come first because they
    /// produce the stats everything else is measured against; the quest log comes before
    /// adventurer activity because "out on quest X" can only be validated once X exists;
    /// standing orders come last because they reference both.
    /// </summary>
    public static class SaveRestore
    {
        /// <summary>
        /// Overwrite <paramref name="world"/> and <paramref name="clock"/> with the
        /// contents of <paramref name="data"/>. Safe to call on a world that is already
        /// running — every collection is cleared and every balance is written, so a load
        /// mid-session leaves nothing of the previous guild behind.
        /// </summary>
        public static SaveRestoreReport Restore(GameWorld world, SimulationClock clock, SaveGameData data)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            bool tierFellBack = RestoreGuild(world, data, out int unknownBuildings);
            RestoreEconomy(world, data);

            int droppedAdventurers = RestoreRoster(world, data);
            int droppedQuestRuns = RestoreQuestRuns(world, data);
            int repairedAdventurers = RestoreAdventurerActivity(world, data);
            int droppedAssignments = RestoreAssignments(world, data);

            RestoreClock(clock, data);

            return new SaveRestoreReport(
                unknownBuildings,
                droppedAdventurers,
                droppedQuestRuns,
                droppedAssignments,
                repairedAdventurers,
                tierFellBack);
        }

        /// <summary>
        /// Tier and building levels, applied together in one quiet call so the stats are
        /// recalculated once from the finished picture rather than after each building.
        /// Returns true when the saved tier could not be found.
        /// </summary>
        private static bool RestoreGuild(GameWorld world, SaveGameData data, out int unknownBuildings)
        {
            unknownBuildings = 0;

            GuildTierDefinition tier = world.Content.FindTier(data.GuildTierId);
            bool fellBack = tier == null;
            if (fellBack)
            {
                tier = world.Content.StartingTier;
                Debug.LogWarning(
                    $"Save names guild tier '{data.GuildTierId}', which is not in the catalogue. " +
                    "Falling back to the starting tier — building levels and balances are kept.");
            }

            Dictionary<string, int> levels = new Dictionary<string, int>(data.Buildings.Length);
            foreach (SavedBuilding saved in data.Buildings)
            {
                if (saved == null || string.IsNullOrEmpty(saved.Id))
                {
                    continue;
                }

                if (world.Content.FindBuilding(saved.Id) == null)
                {
                    unknownBuildings++;
                    continue;
                }

                levels[saved.Id] = saved.Level;
            }

            world.GuildState.RestoreState(tier, levels);
            return fellBack;
        }

        /// <summary>
        /// Every balance, including the ones the save does not mention — a currency added
        /// after the file was written must read zero, not whatever the world happened to
        /// hold, which matters because a load can happen on a session already in progress.
        /// </summary>
        private static void RestoreEconomy(GameWorld world, SaveGameData data)
        {
            Dictionary<CurrencyType, double> saved = new Dictionary<CurrencyType, double>();
            foreach (SavedCurrency entry in data.Currencies)
            {
                if (entry == null || !Enum.IsDefined(typeof(CurrencyType), entry.Currency))
                {
                    continue;
                }

                saved[(CurrencyType)entry.Currency] = entry.Amount;
            }

            foreach (CurrencyType currency in Enum.GetValues(typeof(CurrencyType)))
            {
                world.Economy.Restore(currency, saved.TryGetValue(currency, out double amount) ? amount : 0d);
            }
        }

        /// <summary>
        /// Rebuild the roster, level included. Activity is left at its default here and
        /// set later, once the quest log exists to validate it against.
        ///
        /// Housing capacity is deliberately not enforced: an Inn rebalanced downwards
        /// between builds would otherwise silently delete people the player recruited and
        /// trained. Being temporarily over capacity is visible, harmless, and resolves
        /// itself the moment they upgrade.
        /// </summary>
        private static int RestoreRoster(GameWorld world, SaveGameData data)
        {
            world.Roster.Clear();

            int dropped = 0;
            foreach (SavedAdventurer saved in data.Adventurers)
            {
                if (saved == null || string.IsNullOrWhiteSpace(saved.InstanceId))
                {
                    dropped++;
                    continue;
                }

                AdventurerDefinition definition = world.Content.FindAdventurer(saved.DefinitionId);
                if (definition == null)
                {
                    dropped++;
                    Debug.LogWarning(
                        $"Save holds an adventurer of archetype '{saved.DefinitionId}', which is not in the " +
                        "catalogue. That roster member has been dropped.");
                    continue;
                }

                Adventurer member = new Adventurer(saved.InstanceId, definition, saved.Level);
                if (!world.Roster.Add(member))
                {
                    // Two members sharing an instance id. Only a hand-edited file gets here.
                    dropped++;
                }
            }

            return dropped;
        }

        /// <summary>
        /// Rebuild the runs in flight from their dispatch-time snapshots. A run whose
        /// quest asset is gone is dropped; its party is picked up by the activity pass
        /// below, which finds them pointing at a run that no longer exists and sends them
        /// home rather than leaving them permanently out.
        /// </summary>
        private static int RestoreQuestRuns(GameWorld world, SaveGameData data)
        {
            world.QuestLog.Clear();

            int dropped = 0;
            foreach (SavedQuestRun saved in data.QuestRuns)
            {
                if (saved == null || string.IsNullOrWhiteSpace(saved.InstanceId))
                {
                    dropped++;
                    continue;
                }

                QuestDefinition definition = world.Content.FindQuest(saved.DefinitionId);
                if (definition == null)
                {
                    dropped++;
                    Debug.LogWarning(
                        $"Save holds a run of quest '{saved.DefinitionId}', which is not in the catalogue. " +
                        "That run has been dropped and its party sent home.");
                    continue;
                }

                ActiveQuest run = new ActiveQuest(
                    saved.InstanceId,
                    definition,
                    saved.PartyInstanceIds,
                    saved.TotalSeconds,
                    saved.FailureChance,
                    saved.GoldOnSuccess,
                    saved.ReputationOnSuccess);

                run.RestoreRemainingSeconds(saved.RemainingSeconds);

                if (!world.QuestLog.Add(run))
                {
                    dropped++;
                }
            }

            return dropped;
        }

        /// <summary>
        /// Put each roster member back into what they were doing, having first checked
        /// that it is still possible.
        ///
        /// The check that matters is "out on a quest": a member whose run is missing — or
        /// whose run exists but does not list them — would otherwise sit at OnQuest
        /// forever, because nothing except the run's completion ever brings them back.
        /// They are sent home idle instead, which costs the player the run's reward and
        /// nothing else. Resting needs no such check; its timer resolves itself.
        /// </summary>
        private static int RestoreAdventurerActivity(GameWorld world, SaveGameData data)
        {
            int repaired = 0;

            foreach (SavedAdventurer saved in data.Adventurers)
            {
                if (saved == null)
                {
                    continue;
                }

                Adventurer member = world.Roster.Find(saved.InstanceId);
                if (member == null)
                {
                    continue;
                }

                AdventurerActivity activity = ToActivity(saved.Activity);
                if (activity == AdventurerActivity.OnQuest && !IsOnRun(world, saved))
                {
                    activity = AdventurerActivity.Idle;
                    repaired++;
                }

                member.RestoreState(activity, saved.ActiveQuestInstanceId, saved.RestRemainingSeconds);
            }

            return repaired;
        }

        /// <summary>
        /// Rebuild the standing orders, which is what makes a loaded guild keep earning
        /// rather than finishing its current runs and stopping.
        ///
        /// An order is kept only when its quest and its whole party survived. A partial
        /// party is dropped rather than trimmed: <see cref="QuestDispatchService"/> will
        /// not start a run for a member it cannot find, so a trimmed order would sit in
        /// the list looking active and never run again — worse than an order that is
        /// visibly gone.
        /// </summary>
        private static int RestoreAssignments(GameWorld world, SaveGameData data)
        {
            world.ClearAssignments();

            int dropped = 0;
            foreach (SavedAssignment saved in data.Assignments)
            {
                if (saved == null || string.IsNullOrWhiteSpace(saved.Id))
                {
                    dropped++;
                    continue;
                }

                QuestDefinition quest = world.Content.FindQuest(saved.QuestId);
                if (quest == null || !PartyIsIntact(world, saved.MemberInstanceIds))
                {
                    dropped++;
                    Debug.LogWarning(
                        $"Save holds a standing order for quest '{saved.QuestId}' whose quest or party no longer " +
                        "exists. That order has been dropped; its party is free to be reassigned.");
                    continue;
                }

                QuestAssignment assignment = new QuestAssignment(
                    saved.Id,
                    quest,
                    saved.MemberInstanceIds,
                    saved.Repeat);

                // Only claim a run that actually came back. Claiming a missing one would
                // leave the order permanently "out" and never restarting.
                if (world.QuestLog.Find(saved.ActiveQuestInstanceId) != null)
                {
                    assignment.MarkStarted(saved.ActiveQuestInstanceId);
                }

                world.AddAssignment(assignment);
            }

            return dropped;
        }

        private static void RestoreClock(SimulationClock clock, SaveGameData data)
        {
            clock?.RestoreCounters(
                data.Clock.QuestsCompleted,
                data.Clock.QuestsSucceeded,
                data.Clock.QuestsFailed,
                data.Clock.TotalSecondsSimulated);
        }

        /// <summary>True when the run this member claims to be on exists and lists them.</summary>
        private static bool IsOnRun(GameWorld world, SavedAdventurer saved)
        {
            ActiveQuest run = world.QuestLog.Find(saved.ActiveQuestInstanceId);
            if (run == null)
            {
                return false;
            }

            foreach (string memberId in run.PartyInstanceIds)
            {
                if (memberId == saved.InstanceId)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool PartyIsIntact(GameWorld world, IReadOnlyList<string> memberInstanceIds)
        {
            if (memberInstanceIds == null || memberInstanceIds.Count == 0)
            {
                return false;
            }

            foreach (string memberId in memberInstanceIds)
            {
                if (world.Roster.Find(memberId) == null)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// A stored activity value as an enum, or Idle when it is not one this build
        /// recognises. Casting an arbitrary int to an enum succeeds in C# and produces a
        /// member equal to nothing, which would leave that adventurer neither dispatchable
        /// nor recoverable — so the value is checked rather than cast.
        /// </summary>
        private static AdventurerActivity ToActivity(int stored)
        {
            return Enum.IsDefined(typeof(AdventurerActivity), stored)
                ? (AdventurerActivity)stored
                : AdventurerActivity.Idle;
        }
    }
}
