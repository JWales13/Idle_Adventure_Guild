using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace IdleGuild.Core.Services
{
    /// <summary>
    /// The shipping <see cref="ISaveStore"/>: one UTF-8 file per key under
    /// <see cref="Application.persistentDataPath"/>, which is the only directory iOS
    /// guarantees survives an app update.
    ///
    /// Atomicity comes from writing a temporary file and then moving it into place, so
    /// the live save is replaced by a rename rather than by a stream of bytes. The
    /// previous save is kept as a backup through that rename, which covers the one
    /// window the sequence cannot make instantaneous: if the process dies between
    /// moving the old file aside and moving the new one in, the backup is what
    /// <see cref="Read"/> finds.
    ///
    /// Nothing here throws. Failing to save is a bad thirty seconds; an exception out
    /// of a pause handler is a crash report.
    /// </summary>
    public sealed class FileSaveStore : ISaveStore
    {
        /// <summary>The previous payload, kept so a half-completed replace is recoverable.</summary>
        private const string BackupSuffix = ".bak";

        /// <summary>The new payload before it is moved into place. Never read back.</summary>
        private const string PendingSuffix = ".tmp";

        private readonly string _directory;

        /// <summary>Writes to the platform's persistent data directory.</summary>
        public FileSaveStore()
            : this(Application.persistentDataPath)
        {
        }

        /// <summary>Writes to an explicit directory. For tests and for a future export feature.</summary>
        public FileSaveStore(string directory)
        {
            _directory = string.IsNullOrWhiteSpace(directory) ? Application.persistentDataPath : directory;
        }

        /// <inheritdoc />
        public bool Exists(string key)
        {
            if (!TryResolve(key, out string path))
            {
                return false;
            }

            return HasContent(path) || HasContent(path + BackupSuffix);
        }

        /// <inheritdoc />
        public string Read(string key)
        {
            if (!TryResolve(key, out string path))
            {
                return null;
            }

            string current = ReadOrNull(path);
            if (current != null)
            {
                return current;
            }

            string backup = ReadOrNull(path + BackupSuffix);
            if (backup != null)
            {
                Debug.LogWarning($"Save '{key}' was missing or unreadable; falling back to the previous copy.");
            }

            return backup;
        }

        /// <inheritdoc />
        public bool Write(string key, string contents)
        {
            if (!TryResolve(key, out string path) || contents == null)
            {
                return false;
            }

            string pendingPath = path + PendingSuffix;
            string backupPath = path + BackupSuffix;

            try
            {
                Directory.CreateDirectory(_directory);

                // UTF-8 without a byte order mark: JsonUtility will not parse a BOM back.
                File.WriteAllText(pendingPath, contents, new UTF8Encoding(false));

                if (File.Exists(path))
                {
                    DeleteQuietly(backupPath);
                    File.Move(path, backupPath);
                }

                File.Move(pendingPath, path);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not write save '{key}': {exception.Message}");
                DeleteQuietly(pendingPath);
                return false;
            }
        }

        /// <inheritdoc />
        public bool Discard(string key)
        {
            if (!TryResolve(key, out string path))
            {
                return false;
            }

            // Drops whatever Read would have returned, which is the current file when
            // there is one and the backup when there is not. Deliberately not both: the
            // point of stepping over a corrupt payload is to reach the copy behind it.
            // Calling this again then removes that copy too, so a store holding nothing
            // but bad payloads empties out instead of handing back the same one forever.
            return HasContent(path) ? DeleteQuietly(path) : DeleteQuietly(path + BackupSuffix);
        }

        /// <inheritdoc />
        public bool Delete(string key)
        {
            if (!TryResolve(key, out string path))
            {
                return false;
            }

            bool removed = DeleteQuietly(path);
            removed |= DeleteQuietly(path + BackupSuffix);
            removed |= DeleteQuietly(path + PendingSuffix);
            return removed;
        }

        /// <inheritdoc />
        public string DescribeLocation(string key)
        {
            return TryResolve(key, out string path) ? path : $"<invalid save key '{key}'>";
        }

        /// <summary>
        /// Turn a key into a full path, refusing anything that could climb out of the
        /// save directory. Keys are file names chosen in code, so a rejection here is a
        /// programming mistake rather than something a player can provoke — but the
        /// check costs nothing and the failure it prevents is writing over arbitrary files.
        /// </summary>
        private bool TryResolve(string key, out string path)
        {
            path = null;

            if (string.IsNullOrWhiteSpace(key) || key.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                Debug.LogWarning($"'{key}' is not a usable save key. Keys must be plain file names.");
                return false;
            }

            path = Path.Combine(_directory, key);
            return true;
        }

        private static string ReadOrNull(string path)
        {
            try
            {
                if (!HasContent(path))
                {
                    return null;
                }

                string contents = File.ReadAllText(path, Encoding.UTF8);
                return string.IsNullOrWhiteSpace(contents) ? null : contents;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not read '{path}': {exception.Message}");
                return null;
            }
        }

        /// <summary>
        /// True when the file exists and holds something. A zero-length file is treated
        /// as absent: it is what a device that ran out of storage mid-write leaves behind,
        /// and reading it as an empty save would look like a wiped guild.
        /// </summary>
        private static bool HasContent(string path)
        {
            try
            {
                FileInfo info = new FileInfo(path);
                return info.Exists && info.Length > 0L;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool DeleteQuietly(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return false;
                }

                File.Delete(path);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not delete '{path}': {exception.Message}");
                return false;
            }
        }
    }
}
