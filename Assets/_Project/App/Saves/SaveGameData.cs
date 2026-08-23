using System;

namespace IdleGuild.App.Saves
{
    /// <summary>
    /// The save file's version, and the rules that let it change without breaking a
    /// player's guild.
    ///
    /// The discipline that makes migration tractable is a rule about this file rather
    /// than about the migration code: <b>fields are only ever added, never removed and
    /// never renamed.</b> A field that stops being used stays declared and unread; a
    /// field whose meaning changes gets a new name beside the old one. That is what
    /// allows a save two versions old to be deserialised into today's classes at all —
    /// everything it wrote still has somewhere to land, and everything it did not write
    /// arrives at a neutral default.
    ///
    /// Bump <see cref="CurrentVersion"/> only when a load needs to *do* something to an
    /// older save — recompute a value, split a field, drop a stale reference. Adding a
    /// field with a sensible default needs no bump at all, which is the common case and
    /// deliberately the cheap one.
    /// </summary>
    public static class SaveSchema
    {
        /// <summary>The version this build writes.</summary>
        public const int CurrentVersion = 1;

        /// <summary>
        /// The oldest version this build can still read. Raising this abandons saves
        /// below it, so it moves only when a migration genuinely cannot be written —
        /// never merely to tidy up.
        /// </summary>
        public const int MinimumReadableVersion = 1;
    }

    /// <summary>
    /// Just enough of the file to find out what it is, read before committing to a full
    /// deserialisation.
    ///
    /// Necessary because JsonUtility does not fail on a shape it does not recognise — it
    /// fills what it recognises and defaults the rest — so an unrelated JSON file would
    /// otherwise load as a silently empty guild. A version of 0 means "not one of ours".
    /// </summary>
    [Serializable]
    public sealed class SaveVersionProbe
    {
        public int SchemaVersion;
    }

    /// <summary>
    /// The whole save, as the file holds it.
    ///
    /// These are wire-format types, not domain objects: public mutable fields because
    /// that is what JsonUtility serialises, ids instead of asset references because a
    /// file cannot hold a ScriptableObject, and no behaviour of any kind. The domain
    /// classes stay unaware that saving exists, which is what keeps the file format free
    /// to change without gameplay noticing.
    ///
    /// Enum-backed values are stored as int rather than as the enum, for two reasons.
    /// It documents that these numbers are persisted — the enums say so too, in their
    /// own comments — and it lets an unrecognised value from a newer or hand-edited file
    /// be inspected and rejected on the way in, where casting straight to an enum would
    /// have produced a valid-looking member that matches nothing.
    /// </summary>
    [Serializable]
    public sealed class SaveGameData
    {
        /// <summary>Which schema wrote this. See <see cref="SaveSchema"/>.</summary>
        public int SchemaVersion;

        /// <summary>
        /// When the file was written, in UTC ticks. This is the last-seen stamp offline
        /// progress runs from — it lives here rather than in PlayerPrefs so that the
        /// timestamp and the world it describes are written in the same atomic step and
        /// cannot disagree with each other.
        /// </summary>
        public long SavedAtUtcTicks;

        /// <summary>Application.version at the time of writing. For bug reports, never for logic.</summary>
        public string GameVersion;

        /// <summary>Id of the guild tier reached.</summary>
        public string GuildTierId;

        public SavedBuilding[] Buildings;
        public SavedCurrency[] Currencies;
        public SavedAdventurer[] Adventurers;
        public SavedQuestRun[] QuestRuns;
        public SavedAssignment[] Assignments;
        public SavedClock Clock;
    }

    /// <summary>One building's level. Level 0 means not built, exactly as at runtime.</summary>
    [Serializable]
    public sealed class SavedBuilding
    {
        public string Id;
        public int Level;
    }

    /// <summary>One balance. <see cref="Currency"/> is a CurrencyType value.</summary>
    [Serializable]
    public sealed class SavedCurrency
    {
        public int Currency;
        public double Amount;
    }

    /// <summary>
    /// One roster member. The archetype is referenced by id and everything individual —
    /// level, what they are doing, how long they have left to rest — is stored here,
    /// because that is exactly the split the runtime makes between definition and instance.
    /// </summary>
    [Serializable]
    public sealed class SavedAdventurer
    {
        public string InstanceId;
        public string DefinitionId;
        public int Level;

        /// <summary>An AdventurerActivity value. Validated on load rather than cast blindly.</summary>
        public int Activity;

        /// <summary>The run they are out on, when Activity is OnQuest. Empty otherwise.</summary>
        public string ActiveQuestInstanceId;

        public double RestRemainingSeconds;
    }

    /// <summary>
    /// One quest run in flight, including the numbers snapshotted when it was dispatched.
    ///
    /// Those numbers are saved rather than recomputed on load for the same reason they
    /// are snapshotted at dispatch: a run's payout and risk were fixed by the guild the
    /// player had when they sent the party out. Recomputing here would quietly re-price
    /// every quest in flight across a save and load, and would do it in the player's
    /// favour or against it depending on what they had upgraded in between.
    /// </summary>
    [Serializable]
    public sealed class SavedQuestRun
    {
        public string InstanceId;
        public string DefinitionId;
        public string[] PartyInstanceIds;
        public double TotalSeconds;
        public double RemainingSeconds;
        public float FailureChance;
        public double GoldOnSuccess;
        public double ReputationOnSuccess;
    }

    /// <summary>
    /// One standing order. This is the part of the save that makes the game idle: without
    /// it a loaded guild would finish the runs already in flight and then stop forever.
    /// </summary>
    [Serializable]
    public sealed class SavedAssignment
    {
        public string Id;
        public string QuestId;
        public string[] MemberInstanceIds;
        public bool Repeat;

        /// <summary>The run this order currently has out, or empty while the party rests.</summary>
        public string ActiveQuestInstanceId;
    }

    /// <summary>
    /// Lifetime simulation counters. Not needed to reconstruct the guild — kept because
    /// a success rate measured over one session is noise, and Day 13's balancing pass
    /// wants the number measured across all of them.
    /// </summary>
    [Serializable]
    public sealed class SavedClock
    {
        public double TotalSecondsSimulated;
        public long QuestsCompleted;
        public long QuestsSucceeded;
        public long QuestsFailed;
    }
}
