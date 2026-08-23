using System;
using System.Globalization;
using IdleGuild.Core.Services;
using UnityEngine;

namespace IdleGuild.App.Saves
{
    /// <summary>How a load attempt ended.</summary>
    public enum SaveLoadResult
    {
        /// <summary>The guild was restored from the file.</summary>
        Loaded,

        /// <summary>Nothing has been saved yet. A first launch, or after a deliberate wipe.</summary>
        NoSaveFound,

        /// <summary>Something was there and could not be used. It has been kept aside for inspection.</summary>
        Unreadable
    }

    /// <summary>
    /// The save file as the rest of the game sees it: save now, load what is there, wipe
    /// it.
    ///
    /// The division of labour is deliberate. <see cref="SaveCapture"/> and
    /// <see cref="SaveRestore"/> know what a guild is made of; <see cref="ISaveStore"/>
    /// knows how bytes reach the disk; this class knows the policy that joins them —
    /// which key, what happens when JSON does not parse, and what a caller is told when
    /// it all fails. None of the three needs to change when another does.
    ///
    /// JsonUtility is the serialiser for three reasons: it ships with the engine, so
    /// there is no package to add and no dependency to audit before submission; it works
    /// under IL2CPP without the reflection tricks a general-purpose serialiser needs; and
    /// its limitations — flat serialisable classes, arrays, no dictionaries — are exactly
    /// the shape a versioned schema wants anyway. Its one real cost is that doubles print
    /// to about seven significant figures, which spends fractions of a gold on a balance
    /// and a millisecond on a quest timer. The one value where precision genuinely
    /// matters, the last-seen timestamp, is a long tick count and comes back exact.
    /// </summary>
    public sealed class GameSaveService
    {
        /// <summary>The single-slot save. More slots would simply be more keys.</summary>
        public const string DefaultSaveKey = "guild_save.json";

        /// <summary>
        /// Indented JSON. The file is a few kilobytes at Capital tier, so the whitespace
        /// costs nothing worth measuring, and being able to open a tester's save in a text
        /// editor and read it is worth a great deal during Weeks 2 and 3.
        /// </summary>
        private const bool PrettyPrint = true;

        private readonly GameWorld _world;
        private readonly SimulationClock _clock;
        private readonly ISaveStore _store;
        private readonly string _key;

        public GameSaveService(GameWorld world, SimulationClock clock, ISaveStore store, string key = DefaultSaveKey)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _clock = clock;
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _key = string.IsNullOrWhiteSpace(key) ? DefaultSaveKey : key;
        }

        /// <summary>
        /// When the current save was written, in UTC. This is the last-seen stamp offline
        /// progress measures from; <see cref="DateTime.MinValue"/> until something is
        /// saved or loaded.
        /// </summary>
        public DateTime LastSaveUtc { get; private set; } = DateTime.MinValue;

        /// <summary>What the last restore had to repair, if anything. For the debug console and the logs.</summary>
        public SaveRestoreReport LastRestoreReport { get; private set; }

        /// <summary>True when there is something to load.</summary>
        public bool HasSave => _store.Exists(_key);

        /// <summary>Where the save lives, for the debug console and bug reports.</summary>
        public string Location => _store.DescribeLocation(_key);

        /// <summary>
        /// Write the guild out as of now. Returns false when the store could not complete
        /// the write, in which case the previous save is still intact and the caller
        /// should carry on rather than treat it as fatal — the next autosave will try again.
        /// </summary>
        public bool Save()
        {
            DateTime savedAt = DateTime.UtcNow;

            string json;
            try
            {
                SaveGameData data = SaveCapture.Capture(_world, _clock, savedAt);
                json = JsonUtility.ToJson(data, PrettyPrint);
            }
            catch (Exception exception)
            {
                // Capturing must never take the session down with it.
                Debug.LogException(exception);
                return false;
            }

            if (!_store.Write(_key, json))
            {
                return false;
            }

            LastSaveUtc = savedAt;
            return true;
        }

        /// <summary>
        /// Restore the guild from the file, if there is a usable one.
        ///
        /// <paramref name="savedAtUtc"/> comes back as the moment the file was written,
        /// which is what offline progress measures the player's absence from.
        ///
        /// An unusable file is moved aside rather than deleted, and the attempt is made
        /// once more so that the store's previous copy gets its turn — a save corrupted
        /// by a kill mid-write is the common case, and the copy behind it is usually
        /// perfectly good and only a few seconds older.
        /// </summary>
        public SaveLoadResult TryLoad(out DateTime savedAtUtc)
        {
            savedAtUtc = DateTime.UtcNow;

            for (int attempt = 0; attempt < 2; attempt++)
            {
                string json = _store.Read(_key);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return attempt == 0 ? SaveLoadResult.NoSaveFound : SaveLoadResult.Unreadable;
                }

                if (TryParse(json, out SaveGameData data, out string reason))
                {
                    LastRestoreReport = SaveRestore.Restore(_world, _clock, data);
                    savedAtUtc = ToUtcDateTime(data.SavedAtUtcTicks);
                    LastSaveUtc = savedAtUtc;

                    if (LastRestoreReport.HasRepairs)
                    {
                        Debug.LogWarning($"Loaded the guild with repairs: {LastRestoreReport}.");
                    }

                    return SaveLoadResult.Loaded;
                }

                Debug.LogWarning($"Could not read the save because {reason}. Setting it aside and looking for an earlier copy.");
                SetAside(json);
            }

            return SaveLoadResult.Unreadable;
        }

        /// <summary>
        /// Wipe the save outright, backups included. The player asking to start over — not
        /// error recovery, which uses <see cref="SetAside"/> and keeps the evidence.
        /// </summary>
        public bool Delete()
        {
            bool deleted = _store.Delete(_key);
            if (deleted)
            {
                LastSaveUtc = DateTime.MinValue;
                LastRestoreReport = default;
            }

            return deleted;
        }

        private static bool TryParse(string json, out SaveGameData data, out string reason)
        {
            data = null;

            try
            {
                // The version is read first, on its own. JsonUtility does not object to a
                // shape it has never seen — it fills the fields it recognises and defaults
                // the rest — so an unrelated JSON file would otherwise deserialise happily
                // into an empty guild and overwrite a real one on the next autosave.
                SaveVersionProbe probe = JsonUtility.FromJson<SaveVersionProbe>(json);
                if (probe == null || probe.SchemaVersion <= 0)
                {
                    reason = "it carries no schema version, so it is not one of ours";
                    return false;
                }

                data = JsonUtility.FromJson<SaveGameData>(json);
            }
            catch (Exception exception)
            {
                reason = $"it is not valid JSON ({exception.Message})";
                return false;
            }

            if (!SaveMigrations.TryMigrate(data, out string failureReason))
            {
                data = null;
                reason = failureReason;
                return false;
            }

            reason = null;
            return true;
        }

        /// <summary>
        /// Keep an unusable payload under a timestamped key and drop it from the live one,
        /// leaving any earlier copy in place for the next read.
        ///
        /// Deliberately not a delete. A player who loses a guild wants to know it was not
        /// thrown away, and a file that failed to parse is the only evidence of why it
        /// failed — which, during Weeks 2 and 3, is usually a bug worth finding.
        /// </summary>
        private void SetAside(string json)
        {
            string quarantineKey = $"{_key}.corrupt-{DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture)}";
            if (_store.Write(quarantineKey, json))
            {
                Debug.LogWarning($"The unreadable save was kept as '{quarantineKey}'.");
            }

            _store.Discard(_key);
        }

        /// <summary>
        /// A tick count as a UTC timestamp, or now when it is not a real one. A stamp from
        /// a hand-edited or truncated file must not become an accidental century of
        /// offline earnings.
        /// </summary>
        private static DateTime ToUtcDateTime(long ticks)
        {
            if (ticks <= DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
            {
                return DateTime.UtcNow;
            }

            try
            {
                return new DateTime(ticks, DateTimeKind.Utc);
            }
            catch (ArgumentOutOfRangeException)
            {
                return DateTime.UtcNow;
            }
        }
    }
}
