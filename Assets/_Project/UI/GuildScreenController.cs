using IdleGuild.App;
using IdleGuild.Core.Events;
using IdleGuild.Guild;
using IdleGuild.UI.Views;
using UnityEngine;
using UnityEngine.UIElements;

namespace IdleGuild.UI
{
    /// <summary>
    /// The one MonoBehaviour in the UI assembly: it builds the screen, keeps it in step
    /// with the simulation, and takes itself off the event bus when it goes away.
    ///
    /// It mirrors <c>GameBootstrap</c> deliberately. That class is the seam between
    /// Unity's lifecycle and the simulation; this one is the seam between Unity's
    /// lifecycle and the interface. Everything below either of them is plain C# that a
    /// test could drive without a scene.
    ///
    /// The refresh model is the part worth understanding. Events never rebuild anything
    /// directly — they set a flag, and the next tick acts on it. Two reasons. An idle
    /// game's numbers change continuously while its *structure* changes rarely, so
    /// polling values and rebuilding on demand costs far less than either alone; and
    /// EventBus abandons the remaining handlers for a publish if one of them throws, so
    /// a handler that does nothing but set a bool cannot take another subscriber's
    /// delivery down with it.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class GuildScreenController : MonoBehaviour
    {
        /// <summary>
        /// How often live values are refreshed. Ten times a second is smooth enough for a
        /// countdown and a progress bar, and an order of magnitude cheaper than doing it
        /// per frame — which matters on a phone that is going to run this for hours.
        /// </summary>
        private const long TickMilliseconds = 100L;

        [SerializeField]
        [Tooltip("The bootstrap driving the simulation. Found in the scene if left empty.")]
        private GameBootstrap _bootstrap;

        [Header("Stylesheets")]
        [SerializeField]
        [Tooltip("Tokens.uss — colour, spacing and type scale. Must be added before the theme.")]
        private StyleSheet _tokens;

        [SerializeField]
        [Tooltip("GuildTheme.uss — the component styles that consume those tokens.")]
        private StyleSheet _theme;

        private UIDocument _document;
        private GuildContext _context;

        private VisualElement _root;
        private TreasuryBar _treasury;
        private TabBar _tabs;
        private ToastBar _toast;
        private HallView _hall;
        private QuestsView _quests;
        private RosterView _roster;
        private BuildingDetailOverlay _buildingDetail;
        private PartyOverlay _party;
        private ConfirmOverlay _confirm;

        private IVisualElementScheduledItem _tick;
        private GuildScreen _screen = GuildScreen.Hall;
        private bool _structureDirty = true;

        private void OnEnable()
        {
            _document = GetComponent<UIDocument>();

            if (_bootstrap == null)
            {
                _bootstrap = FindAnyObjectByType<GameBootstrap>();
            }

            if (!BuildShell())
            {
                enabled = false;
                return;
            }

            Subscribe();

            // The world may or may not exist yet — Awake ordering between two objects in
            // the same scene is not something to rely on. The tick fills the screen in as
            // soon as it does, and GameLoaded covers the normal case.
            _tick = _root.schedule.Execute(Tick).Every(TickMilliseconds);

            // And the belt to that pair of braces: a root can be attached to no panel for
            // reasons other than a missing asset — a second panel component claiming the
            // settings, a disabled renderer — and every one of them fails the same silent
            // way. Deferred by a frame ON PURPOSE, because attachment happens across
            // OnEnable and a check that runs too early cannot tell "not yet" from "never".
            // That is the Days 10–11 OnValidate lesson exactly: a check that cannot tell a
            // half-loaded object from a half-filled one is not a check.
            _root.schedule.Execute(WarnIfNothingIsBeingDrawn).ExecuteLater(0L);
        }

        private void OnDisable()
        {
            // The bus holds a strong reference to these handlers, so failing to detach
            // here would keep a destroyed screen alive and delivering to it.
            Unsubscribe();

            _tick?.Pause();
            _tick = null;
            _context = null;
            _structureDirty = true;
        }

        /// <summary>
        /// One frame after the screen is built, confirm it is actually attached to a panel.
        ///
        /// This exists because the failure it catches has no other symptom. Everything
        /// builds, nothing throws, the log stays clean, and the Game view shows the
        /// camera's clear colour — which is indistinguishable from a scene that is simply
        /// empty. Fifteen days of this project went by in exactly that state.
        /// </summary>
        private void WarnIfNothingIsBeingDrawn()
        {
            if (_root != null && _root.panel == null)
            {
                Debug.LogError(
                    $"{nameof(GuildScreenController)} built the screen into an element that belongs to " +
                    "no panel, so none of it is being drawn. Check that exactly one panel component on " +
                    "this object has a Panel Settings asset assigned.", this);
            }
        }

        /// <summary>
        /// Build everything whose existence does not depend on the world: the chrome, the
        /// three empty screens and the overlay layer. Safe to run before the simulation
        /// is ready, which is the point.
        /// </summary>
        private bool BuildShell()
        {
            // A UIDocument with no Panel Settings still hands back a perfectly good root
            // element. It is simply an ORPHAN — never attached to a panel, never drawn.
            // So the whole screen builds without a single error and renders nothing, which
            // is what this project shipped from Day 7 to Day 15 without noticing: the game
            // was played through the debug console, and a blank Game view reads exactly
            // like a camera with nothing in front of it.
            //
            // Checked before the root, because it is the cause and the root is the symptom.
            if (_document.panelSettings == null)
            {
                Debug.LogError(
                    $"{nameof(GuildScreenController)}: the UIDocument on this object has no Panel " +
                    "Settings asset, so nothing it builds will ever be drawn. Assign " +
                    "GuildPanelSettings.asset to the UIDocument's Panel Settings field.", this);
                return false;
            }

            _root = _document.rootVisualElement;
            if (_root == null)
            {
                Debug.LogError(
                    $"{nameof(GuildScreenController)} found no root element. Assign a Panel Settings " +
                    "asset to the UIDocument on this object.", this);
                return false;
            }

            _root.Clear();
            _root.AddToClassList("guild-root");

            if (_tokens != null)
            {
                _root.styleSheets.Add(_tokens);
            }

            if (_theme != null)
            {
                _root.styleSheets.Add(_theme);
            }

            if (_tokens == null || _theme == null)
            {
                Debug.LogWarning(
                    $"{nameof(GuildScreenController)} is missing a stylesheet, so the screen will render " +
                    "with Unity's default theme. Assign Tokens.uss and GuildTheme.uss in the Inspector.", this);
            }

            _treasury = new TreasuryBar();
            _hall = new HallView(OnBuildingSelected);
            _quests = new QuestsView(ChoosePartyFor);
            _roster = new RosterView(AskToConfirm);

            VisualElement content = Ui.Box("guild-content");
            content.Add(_hall);
            content.Add(_quests);
            content.Add(_roster);

            _toast = new ToastBar();
            _tabs = new TabBar(Show);
            _buildingDetail = new BuildingDetailOverlay();
            _party = new PartyOverlay();
            _confirm = new ConfirmOverlay();

            _root.Add(_treasury);
            _root.Add(content);
            _root.Add(_toast);
            _root.Add(_tabs);

            // Overlays are added last and in the order they may stack. UI Toolkit paints
            // siblings in tree order, so the confirmation goes on top of the party picker
            // rather than behind it — which matters the day an overlay raises a dialog of
            // its own, and costs nothing before then.
            _root.Add(_buildingDetail);
            _root.Add(_party);
            _root.Add(_confirm);

            Show(_screen);

            // Safe-area insets need the panel's resolved size to convert screen pixels
            // into panel units, so they are applied once the layout has happened and
            // again whenever it changes — a rotation, or a resized Game view.
            _root.RegisterCallback<GeometryChangedEvent>(_ => SafeArea.Apply(_root));
            return true;
        }

        private void Subscribe()
        {
            EventBus.Subscribe<GameLoaded>(OnGameLoaded);
            EventBus.Subscribe<BuildingUpgraded>(OnStructureChanged);
            EventBus.Subscribe<GuildTierAdvanced>(OnStructureChanged);
            EventBus.Subscribe<AdventurerRecruited>(OnStructureChanged);
            EventBus.Subscribe<AdventurerDismissed>(OnStructureChanged);
            EventBus.Subscribe<QuestStarted>(OnStructureChanged);
            EventBus.Subscribe<QuestCompleted>(OnStructureChanged);
            EventBus.Subscribe<QuestPartyReformed>(OnStructureChanged);
            EventBus.Subscribe<QuestOrderChanged>(OnStructureChanged);
        }

        private void Unsubscribe()
        {
            EventBus.Unsubscribe<GameLoaded>(OnGameLoaded);
            EventBus.Unsubscribe<BuildingUpgraded>(OnStructureChanged);
            EventBus.Unsubscribe<GuildTierAdvanced>(OnStructureChanged);
            EventBus.Unsubscribe<AdventurerRecruited>(OnStructureChanged);
            EventBus.Unsubscribe<AdventurerDismissed>(OnStructureChanged);
            EventBus.Unsubscribe<QuestStarted>(OnStructureChanged);
            EventBus.Unsubscribe<QuestCompleted>(OnStructureChanged);
            EventBus.Unsubscribe<QuestPartyReformed>(OnStructureChanged);
            EventBus.Unsubscribe<QuestOrderChanged>(OnStructureChanged);
        }

        private void OnGameLoaded(GameLoaded loaded)
        {
            _structureDirty = true;

            if (loaded.RestoredFromSave && loaded.SecondsSinceSave >= 60d)
            {
                OfflineReport report = _bootstrap.LastOfflineReport;
                if (report.HasEarnings)
                {
                    _toast.Show(
                        $"While you were away: {Format.Amount(report.GoldEarned)} gold and " +
                        $"{report.QuestsCompleted} quest(s) over {Format.Duration(report.SecondsSimulated)}.",
                        true);
                }
            }
        }

        /// <summary>
        /// Every structural event lands here. It deliberately does no work beyond raising
        /// a flag — see the note on the class about why a handler that cannot throw is
        /// worth more than one that reacts immediately.
        /// </summary>
        private void OnStructureChanged<TEvent>(TEvent _) where TEvent : struct
        {
            _structureDirty = true;
        }

        private void Tick()
        {
            if (_bootstrap == null || !_bootstrap.IsReady)
            {
                return;
            }

            if (_context == null)
            {
                _context = new GuildContext(
                    _bootstrap.World,
                    _bootstrap.Buildings,
                    _bootstrap.Recruitment,
                    _bootstrap.Training,
                    _bootstrap.Dispatch,
                    _bootstrap.Tiers,
                    Report);
                _structureDirty = true;
            }

            if (_structureDirty)
            {
                _structureDirty = false;
                _hall.Rebuild(_context);
                _quests.Rebuild(_context);
                _roster.Rebuild(_context);
            }

            _treasury.Refresh(_context);
            _hall.Refresh(_context);
            _quests.Refresh(_context);
            _roster.Refresh(_context);
            _buildingDetail.Refresh(_context);
            _party.Refresh(_context);
        }

        private void Show(GuildScreen screen)
        {
            _screen = screen;
            _hall.EnableInClassList("guild-screen--hidden", screen != GuildScreen.Hall);
            _quests.EnableInClassList("guild-screen--hidden", screen != GuildScreen.Quests);
            _roster.EnableInClassList("guild-screen--hidden", screen != GuildScreen.Roster);
            _tabs.SetActive(screen);
        }

        private void OnBuildingSelected(BuildingDefinition building)
        {
            if (_context != null)
            {
                _buildingDetail.Open(_context, building);
            }
        }

        /// <summary>
        /// Raise the party picker for a quest or for an existing order. The screens ask
        /// through this rather than owning an overlay of their own, so the picker is one
        /// element that two screens borrow instead of two elements that can disagree.
        /// </summary>
        private void ChoosePartyFor(PartyRequest request)
        {
            if (_context != null)
            {
                _party.Open(_context, request);
            }
        }

        private void AskToConfirm(ConfirmRequest request)
        {
            _confirm.Ask(request);
        }

        private void Report(string message, bool succeeded)
        {
            _toast.Show(message, succeeded);
        }
    }
}
