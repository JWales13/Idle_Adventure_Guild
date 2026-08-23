using System;
using System.Globalization;
using IdleGuild.Core;
using UnityEngine;

namespace IdleGuild.App
{
    /// <summary>
    /// The one MonoBehaviour that starts the game: it builds the world from a
    /// <see cref="GameContent"/> asset, creates the services, and drives the clock.
    ///
    /// Everything below it is plain C#, which is what keeps the simulation testable and
    /// keeps Unity's lifecycle from leaking into the rules. This class is the seam
    /// between the two, and it stays deliberately thin.
    ///
    /// The last-seen timestamp lives in PlayerPrefs for now. That is a Day 4 placeholder:
    /// Day 6 moves it into the versioned save file alongside everything else, at which
    /// point the PlayerPrefs key is read once for migration and then dropped.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        private const string LastSeenPrefsKey = "IdleGuild.LastSeenUtcTicks";

        [SerializeField]
        [Tooltip("The catalogue this game runs on. Required.")]
        private GameContent _content;

        [Header("Determinism")]
        [SerializeField]
        [Tooltip("Run quest rolls from a fixed seed, so a session can be replayed exactly. Development only.")]
        private bool _useFixedRandomSeed;

        [SerializeField] private int _randomSeed = 1;

        public GameWorld World { get; private set; }

        public SimulationClock Clock { get; private set; }

        public BuildingUpgradeService Buildings { get; private set; }

        public RecruitmentService Recruitment { get; private set; }

        public TrainingService Training { get; private set; }

        public QuestDispatchService Dispatch { get; private set; }

        public TierAdvancementService Tiers { get; private set; }

        /// <summary>What the guild earned during the most recent absence. Zeroed on a fresh start.</summary>
        public OfflineReport LastOfflineReport { get; private set; }

        /// <summary>False when the content asset is missing, in which case nothing is wired up.</summary>
        public bool IsReady => World != null;

        private void Awake()
        {
            if (_content == null)
            {
                Debug.LogError($"{nameof(GameBootstrap)} has no GameContent assigned, so there is no game to run.", this);
                enabled = false;
                return;
            }

            IRandomSource random = _useFixedRandomSeed
                ? new SystemRandomSource(_randomSeed)
                : new SystemRandomSource();

            World = new GameWorld(_content, random);
            Dispatch = new QuestDispatchService(World);
            Clock = new SimulationClock(World, Dispatch);
            Buildings = new BuildingUpgradeService(World);
            Recruitment = new RecruitmentService(World);
            Training = new TrainingService(World);
            Tiers = new TierAdvancementService(World);

            // Day 6 replaces this with a loaded save; until then every run is a new game.
            World.ApplyStartingState();

            CatchUpFromLastSeen();
        }

        private void Update()
        {
            if (!IsReady)
            {
                return;
            }

            Clock.Advance(Time.deltaTime);
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (!IsReady)
            {
                return;
            }

            if (isPaused)
            {
                RecordLastSeen();
            }
            else
            {
                CatchUpFromLastSeen();
            }
        }

        private void OnApplicationQuit()
        {
            if (IsReady)
            {
                RecordLastSeen();
            }
        }

        /// <summary>
        /// Pay out the time since the app was last seen, then stamp the clock again. The
        /// stamp is rewritten immediately so a crash mid-session cannot pay the same
        /// stretch twice.
        /// </summary>
        public void CatchUpFromLastSeen()
        {
            DateTime now = DateTime.UtcNow;
            DateTime lastSeen = ReadLastSeen(now);
            double elapsedSeconds = (now - lastSeen).TotalSeconds;

            LastOfflineReport = OfflineProgress.CatchUp(World, Clock, elapsedSeconds);
            RecordLastSeen();
        }

        private DateTime ReadLastSeen(DateTime fallback)
        {
            string stored = PlayerPrefs.GetString(LastSeenPrefsKey, string.Empty);
            if (string.IsNullOrEmpty(stored) ||
                !long.TryParse(stored, NumberStyles.Integer, CultureInfo.InvariantCulture, out long ticks))
            {
                return fallback;
            }

            try
            {
                return new DateTime(ticks, DateTimeKind.Utc);
            }
            catch (ArgumentOutOfRangeException)
            {
                // A corrupted or hand-edited value. Treat it as "just now" rather than
                // handing the player an accidental century of offline earnings.
                return fallback;
            }
        }

        private void RecordLastSeen()
        {
            PlayerPrefs.SetString(
                LastSeenPrefsKey,
                DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
        }
    }
}
