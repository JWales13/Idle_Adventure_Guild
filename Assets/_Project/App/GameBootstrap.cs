using System;
using IdleGuild.App.Saves;
using IdleGuild.Core;
using IdleGuild.Core.Events;
using IdleGuild.Core.Services;
using UnityEngine;

namespace IdleGuild.App
{
    /// <summary>
    /// The one MonoBehaviour that starts the game: it builds the world from a
    /// <see cref="GameContent"/> asset, creates the services, restores the save and
    /// drives the clock.
    ///
    /// Everything below it is plain C#, which is what keeps the simulation testable and
    /// keeps Unity's lifecycle from leaking into the rules. This class is the seam
    /// between the two, and it stays deliberately thin: it decides *when* things happen —
    /// load on wake, pay for the absence, save on the way out — and never how.
    ///
    /// Saving is driven from three places, and it takes all three. Pause is the reliable
    /// one on iOS, which is where this ships; quit is not called when a player swipes the
    /// app away from the switcher, and a crash calls neither. The periodic autosave is
    /// what bounds the loss when none of the hooks fire, at the cost of one file write
    /// every half minute.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        /// <summary>
        /// Day 4's placeholder stamp, superseded by the save file. Read once and removed;
        /// see <see cref="DiscardLegacyLastSeenPreference"/> for why it is not honoured.
        /// </summary>
        private const string LegacyLastSeenPrefsKey = "IdleGuild.LastSeenUtcTicks";

        [SerializeField]
        [Tooltip("The catalogue this game runs on. Required.")]
        private GameContent _content;

        [Header("Saving")]
        [SerializeField]
        [Tooltip("Seconds between automatic saves. 0 disables them, leaving only pause and quit.")]
        [Min(0f)]
        private float _autosaveIntervalSeconds = 30f;

        [SerializeField]
        [Tooltip("Untick to start a new guild every session, leaving any existing save untouched. Development only.")]
        private bool _loadSaveOnStart = true;

        [Header("Determinism")]
        [SerializeField]
        [Tooltip("Run quest rolls from a fixed seed, so a session can be replayed exactly. Development only.")]
        private bool _useFixedRandomSeed;

        [SerializeField] private int _randomSeed = 1;

        private DateTime _lastSeenUtc = DateTime.UtcNow;
        private float _secondsSinceAutosave;
        private bool _announcedReady;

        public GameWorld World { get; private set; }

        public SimulationClock Clock { get; private set; }

        public GameSaveService Saves { get; private set; }

        public BuildingUpgradeService Buildings { get; private set; }

        public RecruitmentService Recruitment { get; private set; }

        public TrainingService Training { get; private set; }

        public QuestDispatchService Dispatch { get; private set; }

        public TierAdvancementService Tiers { get; private set; }

        /// <summary>What the guild earned during the most recent absence. Zeroed on a fresh start.</summary>
        public OfflineReport LastOfflineReport { get; private set; }

        /// <summary>True when this session began from a save rather than as a new guild.</summary>
        public bool LoadedFromSave { get; private set; }

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
            Saves = new GameSaveService(World, Clock, new FileSaveStore());

            DiscardLegacyLastSeenPreference();
            LoadOrStartNewGuild();
        }

        /// <summary>
        /// Announce that the world is readable.
        ///
        /// In Start rather than Awake on purpose: a screen that subscribes to the bus in
        /// OnEnable has not done so yet while this object's Awake runs, and would miss the
        /// one event that tells it to draw itself.
        /// </summary>
        private void Start()
        {
            AnnounceReady();
        }

        private void Update()
        {
            if (!IsReady)
            {
                return;
            }

            Clock.Advance(Time.deltaTime);
            TickAutosave();
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (!IsReady)
            {
                return;
            }

            if (isPaused)
            {
                Save("app paused");
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
                Save("app quit");
            }
        }

        /// <summary>
        /// Write the guild out and re-stamp the last-seen clock. Returns false when the
        /// write failed, which is logged and otherwise survivable: the previous save is
        /// still there, and the next attempt is thirty seconds away.
        /// </summary>
        public bool Save(string reason)
        {
            if (!IsReady)
            {
                return false;
            }

            _secondsSinceAutosave = 0f;

            if (!Saves.Save())
            {
                Debug.LogWarning(
                    $"Could not save the guild ({reason}). The session carries on and the previous save is intact.");
                return false;
            }

            // Only move the last-seen mark once the bytes are down. A failed save that
            // advanced it would quietly forfeit the player's offline time.
            _lastSeenUtc = Saves.LastSaveUtc;
            return true;
        }

        /// <summary>
        /// Pay out the time since the guild was last seen, then stamp it again. The stamp
        /// is rewritten immediately so a crash mid-session cannot pay the same stretch twice.
        /// </summary>
        public void CatchUpFromLastSeen()
        {
            if (!IsReady)
            {
                return;
            }

            double elapsedSeconds = (DateTime.UtcNow - _lastSeenUtc).TotalSeconds;
            LastOfflineReport = OfflineProgress.CatchUp(World, Clock, elapsedSeconds);
            Save("offline catch-up");
        }

        /// <summary>
        /// Throw away the running guild and load the file again. For the debug console and
        /// for a future "abandon this run" option; note that it pays no offline time, so
        /// what comes back is the guild exactly as it was written.
        /// </summary>
        public SaveLoadResult ReloadFromSave()
        {
            if (!IsReady)
            {
                return SaveLoadResult.NoSaveFound;
            }

            SaveLoadResult result = Saves.TryLoad(out DateTime savedAtUtc);
            if (result == SaveLoadResult.Loaded)
            {
                LoadedFromSave = true;
                _lastSeenUtc = savedAtUtc;
                LastOfflineReport = default;
                EventBus.Publish(new GameLoaded(true, 0d));
            }

            return result;
        }

        private void LoadOrStartNewGuild()
        {
            SaveLoadResult result = SaveLoadResult.NoSaveFound;
            DateTime savedAtUtc = DateTime.UtcNow;

            if (_loadSaveOnStart)
            {
                result = Saves.TryLoad(out savedAtUtc);
            }

            if (result == SaveLoadResult.Loaded)
            {
                LoadedFromSave = true;
                _lastSeenUtc = savedAtUtc;
                CatchUpFromLastSeen();
                return;
            }

            if (result == SaveLoadResult.Unreadable)
            {
                Debug.LogWarning(
                    "The existing save could not be read, so this session starts a new guild. The unreadable " +
                    "file has been kept alongside it rather than deleted.");
            }

            World.ApplyStartingState();
            _lastSeenUtc = DateTime.UtcNow;

            // Save straight away so the file — and its timestamp — exist from the first
            // frame. Without this a player who closes the app before the first autosave
            // would come back to a guild with no memory of having started.
            Save("new game");
        }

        private void TickAutosave()
        {
            if (_autosaveIntervalSeconds <= 0f)
            {
                return;
            }

            // Unscaled: an autosave interval that stretched with a paused or slowed
            // timeScale would stop saving exactly when the game is least responsive.
            _secondsSinceAutosave += Time.unscaledDeltaTime;
            if (_secondsSinceAutosave >= _autosaveIntervalSeconds)
            {
                Save("autosave");
            }
        }

        private void AnnounceReady()
        {
            if (!IsReady || _announcedReady)
            {
                return;
            }

            _announcedReady = true;
            EventBus.Publish(new GameLoaded(LoadedFromSave, LastOfflineReport.SecondsAway));
        }

        /// <summary>
        /// Remove Day 4's PlayerPrefs stamp, without honouring it.
        ///
        /// It is not migrated because there is nothing for it to describe: builds that
        /// wrote it never persisted a guild, so every session they stamped started from
        /// scratch. Carrying the value forward would hand a player offline earnings for a
        /// guild that did not exist while they were away. Read once and dropped, as
        /// planned — the save file has owned the timestamp since this method was written.
        /// </summary>
        private static void DiscardLegacyLastSeenPreference()
        {
            if (!PlayerPrefs.HasKey(LegacyLastSeenPrefsKey))
            {
                return;
            }

            PlayerPrefs.DeleteKey(LegacyLastSeenPrefsKey);
            PlayerPrefs.Save();
        }
    }
}
