using System;
using System.Collections.Generic;
using IdleGuild.Adventurers;
using IdleGuild.Core;
using IdleGuild.Economy;
using IdleGuild.Guild;
using IdleGuild.Quests;
using IdleGuild.Staff;
using UnityEngine;

namespace IdleGuild.App.Saves
{
    /// <summary>
    /// Reading a running guild out into the save format.
    ///
    /// Every runtime class already exposes its state read-only, so this is a
    /// transcription and nothing more — no rules, no recalculation, no decisions about
    /// what a value should be. Anything clever belongs on the restore side, where it can
    /// be applied to old saves as well as new ones.
    /// </summary>
    public static class SaveCapture
    {
        /// <summary>
        /// Snapshot <paramref name="world"/> and <paramref name="clock"/> as of now.
        /// Cheap enough to call on a thirty-second autosave: it walks a handful of
        /// collections and allocates one object per thing the guild owns.
        /// </summary>
        public static SaveGameData Capture(GameWorld world, SimulationClock clock, DateTime savedAtUtc)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            return new SaveGameData
            {
                SchemaVersion = SaveSchema.CurrentVersion,
                SavedAtUtcTicks = savedAtUtc.ToUniversalTime().Ticks,
                GameVersion = Application.version,
                GuildTierId = world.GuildState.CurrentTier != null ? world.GuildState.CurrentTier.Id : null,
                Buildings = CaptureBuildings(world.GuildState),
                Currencies = CaptureCurrencies(world.Economy),
                Adventurers = CaptureRoster(world.Roster),
                Staff = CaptureStaff(world.Staff),
                Trade = CaptureTrade(clock),
                QuestRuns = CaptureQuestRuns(world.QuestLog),
                Assignments = CaptureAssignments(world.Assignments),
                Clock = CaptureClock(clock)
            };
        }

        private static SavedBuilding[] CaptureBuildings(GuildState guildState)
        {
            IReadOnlyDictionary<string, int> levels = guildState.BuildingLevels;
            SavedBuilding[] saved = new SavedBuilding[levels.Count];

            int index = 0;
            foreach (KeyValuePair<string, int> entry in levels)
            {
                saved[index++] = new SavedBuilding { Id = entry.Key, Level = entry.Value };
            }

            return saved;
        }

        private static SavedCurrency[] CaptureCurrencies(PlayerEconomy economy)
        {
            IReadOnlyDictionary<CurrencyType, double> balances = economy.Balances;
            SavedCurrency[] saved = new SavedCurrency[balances.Count];

            int index = 0;
            foreach (KeyValuePair<CurrencyType, double> entry in balances)
            {
                saved[index++] = new SavedCurrency { Currency = (int)entry.Key, Amount = entry.Value };
            }

            return saved;
        }

        private static SavedAdventurer[] CaptureRoster(AdventurerRoster roster)
        {
            IReadOnlyList<Adventurer> members = roster.Members;
            SavedAdventurer[] saved = new SavedAdventurer[members.Count];

            for (int index = 0; index < members.Count; index++)
            {
                Adventurer member = members[index];
                saved[index] = new SavedAdventurer
                {
                    InstanceId = member.InstanceId,
                    DefinitionId = member.Definition.Id,
                    Level = member.Level,
                    Activity = (int)member.Activity,
                    ActiveQuestInstanceId = member.ActiveQuestInstanceId,
                    RestRemainingSeconds = member.RestRemainingSeconds
                };
            }

            return saved;
        }

        private static SavedStaff[] CaptureStaff(StaffRoster staff)
        {
            IReadOnlyList<StaffMember> employees = staff.Employees;
            SavedStaff[] saved = new SavedStaff[employees.Count];

            for (int index = 0; index < employees.Count; index++)
            {
                StaffMember employee = employees[index];
                saved[index] = new SavedStaff
                {
                    InstanceId = employee.InstanceId,
                    DefinitionId = employee.Definition.Id
                };
            }

            return saved;
        }

        private static SavedTrade CaptureTrade(SimulationClock clock)
        {
            if (clock == null)
            {
                return new SavedTrade();
            }

            return new SavedTrade
            {
                GrossEarned = clock.GrossEarned,
                WagesPaid = clock.WagesPaid,
                TakingsEarned = clock.Takings.LifetimeTakings,
                WaitingCustomers = clock.Takings.WaitingCustomers
            };
        }

        private static SavedQuestRun[] CaptureQuestRuns(QuestLog questLog)
        {
            IReadOnlyList<ActiveQuest> active = questLog.Active;
            SavedQuestRun[] saved = new SavedQuestRun[active.Count];

            for (int index = 0; index < active.Count; index++)
            {
                ActiveQuest run = active[index];
                saved[index] = new SavedQuestRun
                {
                    InstanceId = run.InstanceId,
                    DefinitionId = run.Definition.Id,
                    PartyInstanceIds = ToArray(run.PartyInstanceIds),
                    TotalSeconds = run.TotalSeconds,
                    RemainingSeconds = run.RemainingSeconds,
                    FailureChance = run.FailureChance,
                    GoldOnSuccess = run.GoldOnSuccess,
                    ReputationOnSuccess = run.ReputationOnSuccess
                };
            }

            return saved;
        }

        private static SavedAssignment[] CaptureAssignments(IReadOnlyList<QuestAssignment> assignments)
        {
            SavedAssignment[] saved = new SavedAssignment[assignments.Count];

            for (int index = 0; index < assignments.Count; index++)
            {
                QuestAssignment assignment = assignments[index];
                saved[index] = new SavedAssignment
                {
                    Id = assignment.Id,
                    QuestId = assignment.Quest.Id,
                    MemberInstanceIds = ToArray(assignment.MemberInstanceIds),
                    Repeat = assignment.Repeat,
                    ActiveQuestInstanceId = assignment.ActiveQuestInstanceId
                };
            }

            return saved;
        }

        private static SavedClock CaptureClock(SimulationClock clock)
        {
            if (clock == null)
            {
                return new SavedClock();
            }

            return new SavedClock
            {
                TotalSecondsSimulated = clock.TotalSecondsSimulated,
                QuestsCompleted = clock.QuestsCompleted,
                QuestsSucceeded = clock.QuestsSucceeded,
                QuestsFailed = clock.QuestsFailed
            };
        }

        private static string[] ToArray(IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return Array.Empty<string>();
            }

            string[] copy = new string[values.Count];
            for (int index = 0; index < values.Count; index++)
            {
                copy[index] = values[index];
            }

            return copy;
        }
    }
}
