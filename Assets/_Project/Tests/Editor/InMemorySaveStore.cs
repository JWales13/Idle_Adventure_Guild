using System.Collections.Generic;
using IdleGuild.Core.Services;

namespace IdleGuild.Tests
{
    /// <summary>
    /// An <see cref="ISaveStore"/> that keeps everything in a dictionary.
    ///
    /// The interface's own documentation invites this — "a cloud save, an iCloud
    /// key-value store or an in-memory double for tests all satisfy this contract" — and
    /// it is the reason the save tests can exercise the real
    /// <see cref="IdleGuild.App.Saves.GameSaveService"/> rather than a stubbed one:
    /// version probing, migration and quarantine all run exactly as they do on a device.
    ///
    /// It keeps one copy behind the current payload, matching FileSaveStore, because
    /// <see cref="Discard"/> stepping back to an earlier copy is the behaviour corrupt-
    /// save recovery depends on.
    /// </summary>
    internal sealed class InMemorySaveStore : ISaveStore
    {
        private const int CopiesKept = 2;

        private readonly Dictionary<string, List<string>> _payloads = new Dictionary<string, List<string>>();

        /// <summary>Every key ever written, for asserting that a quarantine copy was made.</summary>
        public IEnumerable<string> Keys => _payloads.Keys;

        public bool FailNextWrite { get; set; }

        public bool Exists(string key)
        {
            return _payloads.TryGetValue(key, out List<string> history) && history.Count > 0;
        }

        public string Read(string key)
        {
            return _payloads.TryGetValue(key, out List<string> history) && history.Count > 0
                ? history[history.Count - 1]
                : null;
        }

        public bool Write(string key, string contents)
        {
            if (FailNextWrite)
            {
                FailNextWrite = false;
                return false;
            }

            if (!_payloads.TryGetValue(key, out List<string> history))
            {
                history = new List<string>();
                _payloads[key] = history;
            }

            history.Add(contents);
            while (history.Count > CopiesKept)
            {
                history.RemoveAt(0);
            }

            return true;
        }

        public bool Discard(string key)
        {
            if (!_payloads.TryGetValue(key, out List<string> history) || history.Count == 0)
            {
                return false;
            }

            history.RemoveAt(history.Count - 1);
            return true;
        }

        public bool Delete(string key)
        {
            return _payloads.Remove(key);
        }

        public string DescribeLocation(string key)
        {
            return $"in memory ({key})";
        }
    }
}
