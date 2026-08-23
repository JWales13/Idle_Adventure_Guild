using System;

namespace IdleGuild.App.Saves
{
    /// <summary>
    /// Bringing an older save up to the current schema, and refusing the ones that
    /// cannot be.
    ///
    /// There are no migration steps yet, because version 1 is the first version. The
    /// machinery exists anyway, and deliberately so: the roadmap put save versioning on
    /// Day 6 rather than on the day it is first needed, because the alternative is
    /// discovering on Day 20 that a Week 3 content change cannot be shipped without
    /// wiping the testers' progress. An empty ladder costs a few lines now; adding one
    /// after the fact costs a release.
    ///
    /// The shape a future step takes: a case in <see cref="Upgrade"/> that mutates the
    /// data from version N to version N+1 and returns, with the loop running it as many
    /// times as the gap requires. Steps must be independent of the game's current
    /// balance — they run against whatever a player saved months ago — so they read only
    /// the data handed to them, never a ScriptableObject.
    /// </summary>
    public static class SaveMigrations
    {
        /// <summary>
        /// Bring <paramref name="data"/> to <see cref="SaveSchema.CurrentVersion"/> in
        /// place. Returns false with a reason when the file cannot be used, which the
        /// caller treats as "start a new guild" rather than as an error to throw.
        /// </summary>
        public static bool TryMigrate(SaveGameData data, out string failureReason)
        {
            if (data == null)
            {
                failureReason = "the file did not deserialise into a save at all";
                return false;
            }

            if (data.SchemaVersion > SaveSchema.CurrentVersion)
            {
                // A build that shipped, then a player who installed a newer one and rolled
                // back — a TestFlight tester will do this within a week. Reading it would
                // mean guessing at fields this build has never heard of, so it declines and
                // the file is kept aside rather than overwritten.
                failureReason =
                    $"it was written by a newer build (schema {data.SchemaVersion}, this build reads {SaveSchema.CurrentVersion})";
                return false;
            }

            if (data.SchemaVersion < SaveSchema.MinimumReadableVersion)
            {
                failureReason =
                    $"schema {data.SchemaVersion} is older than this build can read ({SaveSchema.MinimumReadableVersion})";
                return false;
            }

            while (data.SchemaVersion < SaveSchema.CurrentVersion)
            {
                int before = data.SchemaVersion;
                if (!Upgrade(data))
                {
                    failureReason = $"no migration exists from schema {before}";
                    return false;
                }

                if (data.SchemaVersion <= before)
                {
                    // A step that forgets to raise the version would spin here forever.
                    failureReason = $"the migration from schema {before} did not advance the version";
                    return false;
                }
            }

            Normalise(data);
            failureReason = null;
            return true;
        }

        /// <summary>
        /// Apply the single step that upgrades <paramref name="data"/> from its current
        /// version to the next one. Returns false when no such step exists.
        /// </summary>
        private static bool Upgrade(SaveGameData data)
        {
            switch (data.SchemaVersion)
            {
                // case 1: MigrateOneToTwo(data); data.SchemaVersion = 2; return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Replace the nulls JsonUtility leaves behind for absent arrays and objects with
        /// empty ones, so restoration reads a uniform shape whatever version wrote the
        /// file. Done once here rather than as a guard at every use — a null check
        /// repeated twenty times is twenty chances to forget the twenty-first.
        /// </summary>
        private static void Normalise(SaveGameData data)
        {
            data.Buildings ??= Array.Empty<SavedBuilding>();
            data.Currencies ??= Array.Empty<SavedCurrency>();
            data.Adventurers ??= Array.Empty<SavedAdventurer>();
            data.QuestRuns ??= Array.Empty<SavedQuestRun>();
            data.Assignments ??= Array.Empty<SavedAssignment>();
            data.Clock ??= new SavedClock();

            foreach (SavedQuestRun run in data.QuestRuns)
            {
                if (run != null)
                {
                    run.PartyInstanceIds ??= Array.Empty<string>();
                }
            }

            foreach (SavedAssignment assignment in data.Assignments)
            {
                if (assignment != null)
                {
                    assignment.MemberInstanceIds ??= Array.Empty<string>();
                }
            }
        }
    }
}
