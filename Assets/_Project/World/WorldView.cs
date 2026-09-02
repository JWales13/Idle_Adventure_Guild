using System;
using IdleGuild.App;
using IdleGuild.Core.Events;
using IdleGuild.Guild;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IdleGuild.World
{
    /// <summary>
    /// The one MonoBehaviour in the World assembly: it points the camera at the hall, lets
    /// a finger drag it around, and draws the rooms.
    ///
    /// It is the third seam of its kind and deliberately the same shape as the other two.
    /// <c>GameBootstrap</c> is the seam between Unity's lifecycle and the simulation;
    /// <c>GuildScreenController</c> is the seam between Unity's lifecycle and the chrome;
    /// this is the seam between Unity's lifecycle and the hall. Everything underneath all
    /// three is plain C#.
    ///
    /// The refresh model is <c>GuildScreenController</c>'s, for its reasons: events set a
    /// flag and the next frame acts on it, so a handler that does nothing but assign a
    /// bool cannot take another subscriber's delivery down with it when the bus abandons a
    /// publish. The hall differs from the screen in one way only -- it has no live numbers
    /// to poll yet, so there is no timed tick, just the dirty flag. Step 4 brings the first
    /// thing that moves on its own.
    ///
    /// **It DEPICTS and does not CAUSE** (section 4 of Docs/World_View_Design.md). It reads
    /// levels and unlock state off <see cref="GuildState"/> and computes nothing. No gold
    /// is granted here, and nothing in this assembly may decide a cost, a gate or a rate.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldView : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The camera that looks at the hall. Uses the main camera if left empty.")]
        private Camera _camera;

        [SerializeField]
        [Tooltip("The bootstrap driving the simulation. Found in the scene if left empty.")]
        private GameBootstrap _bootstrap;

        [Header("The hall")]
        [SerializeField]
        [Tooltip("The extent of the hall in world units. Grows as new wings unlock.")]
        private Rect _floorBounds = HallPlan.DefaultFloor;

        [SerializeField]
        [Tooltip("Where each room stands. Keyed by BuildingDefinition Id.")]
        private HallRoom[] _plan = HallPlan.Default();

        [SerializeField]
        [Min(0f)]
        [Tooltip("Grey-box grid spacing in world units. 0 draws no grid. Dies with the grey box.")]
        private float _gridSpacing = 2f;

        private GreyBoxFloor _floor;
        private Transform _floorRoot;

        private RoomRectangles _rooms;
        private Transform _roomsRoot;
        private bool _roomsDirty = true;

        private bool _dragging;
        private bool _wasPressed;
        private Vector2 _grabbedWorldPoint;

        /// <summary>The hall's current extent in world units.</summary>
        public Rect FloorBounds => _floorBounds;

        /// <summary>
        /// Asked, at the moment a press begins, whether that screen position belongs to the
        /// interface rather than to the hall. Returning true lets the press through to the
        /// chrome and the hall does not pan.
        ///
        /// **Currently unset, and that is a decision rather than an omission.** The obvious
        /// implementation -- ask the UIDocument's panel what sits under the pixel -- makes
        /// things worse today, because every screen is a full-bleed ScrollView and the panel
        /// therefore reports the interface under every pixel of the content area. Wiring it
        /// would trade an occasional double-drag for a hall that cannot be panned at all.
        ///
        /// It goes live when section 7 of Docs/World_View_Design.md is settled and the
        /// chrome stops being the whole screen. The ambiguity it has to resolve -- dragging
        /// the empty background of a list: scroll the list, or pan the hall? -- is that
        /// section's question rather than this class's.
        ///
        /// A delegate rather than a call into <c>IdleGuild.UI</c> on purpose: World and UI
        /// are siblings above App and neither references the other, so a hit test that
        /// reached across would put the two presentation layers into a cycle.
        /// </summary>
        public Func<Vector2, bool> IsPointerOverChrome { get; set; }

        private void OnEnable()
        {
            if (!ResolveCamera())
            {
                enabled = false;
                return;
            }

            ResolveBootstrap();

            ConfigureSorting();
            BuildFloor();
            EnsureRoomsRoot();
            ClampCameraIntoBounds();

            Subscribe();
            _roomsDirty = true;
        }

        private void OnDisable()
        {
            Unsubscribe();
            _dragging = false;
            _wasPressed = false;
        }

        /// <summary>
        /// Grows (or moves) the hall, rebuilding the floor and pulling the camera back
        /// inside it. Section 5: the hall physically expands as rooms unlock, and the
        /// camera's reach has to expand with it.
        /// </summary>
        public void SetFloorBounds(Rect bounds)
        {
            _floorBounds = bounds;

            if (!isActiveAndEnabled)
            {
                return;
            }

            BuildFloor();
            ClampCameraIntoBounds();
        }

        private void Update()
        {
            HandlePan();
            RefreshRoomsIfNeeded();
        }

        // ---------------------------------------------------------------- the hall -----

        private void RefreshRoomsIfNeeded()
        {
            if (!_roomsDirty)
            {
                return;
            }

            GuildState guild = _bootstrap == null ? null : _bootstrap.World?.GuildState;

            if (guild == null)
            {
                // The world may not exist yet -- Awake ordering between two objects in one
                // scene is not something to rely on, which is the note GuildScreenController
                // carries for the same reason. Stay dirty and try again next frame.
                return;
            }

            _rooms.Rebuild(_plan, guild);
            _roomsDirty = false;
        }

        private void EnsureRoomsRoot()
        {
            if (_roomsRoot != null)
            {
                return;
            }

            var root = new GameObject("Rooms");
            root.transform.SetParent(transform, false);
            _roomsRoot = root.transform;
            _rooms = new RoomRectangles(_roomsRoot);
        }

        // ------------------------------------------------------------------- input -----

        private void HandlePan()
        {
            // One device covers both cases: Touchscreen derives from Pointer, so a finger
            // on a phone and a mouse in the editor arrive through the same two controls
            // and there is no per-platform branch to keep in step. This project is on the
            // Input System package exclusively (Active Input Handling is "Input System
            // Package"), so the legacy UnityEngine.Input class throws at runtime here --
            // it compiles perfectly and fails on the device, which is the shape worth
            // naming rather than rediscovering.
            Pointer pointer = Pointer.current;

            if (pointer == null)
            {
                _dragging = false;
                _wasPressed = false;
                return;
            }

            Vector2 pixel = pointer.position.ReadValue();
            bool pressed = pointer.press.isPressed;

            if (pressed && !_wasPressed)
            {
                BeginDrag(pixel);
            }
            else if (!pressed)
            {
                _dragging = false;
            }
            else if (_dragging)
            {
                ContinueDrag(pixel);
            }

            _wasPressed = pressed;
        }

        private void BeginDrag(Vector2 pixel)
        {
            // Decided once, when the press starts, and not re-asked while it is held. A
            // drag that begins on the treasury bar belongs to the treasury bar for its
            // whole life, even when the finger travels off the panel -- otherwise
            // dragging the mailbox flicks the hall sideways the moment you leave it.
            if (IsPointerOverChrome != null && IsPointerOverChrome(pixel))
            {
                _dragging = false;
                return;
            }

            _dragging = true;
            _grabbedWorldPoint = ScreenToWorld(pixel);
        }

        private void ContinueDrag(Vector2 pixel)
        {
            // The whole pan, and there is no speed constant in it. The world point the
            // finger landed on stays under the finger: measure where that pixel points now,
            // and move the camera by whatever closes the gap. It is correct at any
            // orthographic size and on any screen density for free, where a
            // pixels-times-speed version needs re-tuning every time either changes and is
            // never quite right at the edges of a drag.
            Vector2 pointsAtNow = ScreenToWorld(pixel);
            Vector3 position = _camera.transform.position;

            Vector2 desired =
                new Vector2(position.x, position.y) + (_grabbedWorldPoint - pointsAtNow);

            MoveCameraTo(desired);
        }

        // ------------------------------------------------------------------ events -----

        private void Subscribe()
        {
            EventBus.Subscribe<GameLoaded>(OnStructureChanged);
            EventBus.Subscribe<BuildingUpgraded>(OnStructureChanged);
            EventBus.Subscribe<GuildTierAdvanced>(OnStructureChanged);
        }

        private void Unsubscribe()
        {
            EventBus.Unsubscribe<GameLoaded>(OnStructureChanged);
            EventBus.Unsubscribe<BuildingUpgraded>(OnStructureChanged);
            EventBus.Unsubscribe<GuildTierAdvanced>(OnStructureChanged);
        }

        private void OnStructureChanged<TEvent>(TEvent _) where TEvent : struct
        {
            _roomsDirty = true;
        }

        // ------------------------------------------------------------------- setup -----

        private bool ResolveCamera()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera == null)
            {
                Debug.LogError(
                    "[World] No camera. Assign one on WorldView or tag a camera MainCamera.",
                    this);
                return false;
            }

            if (!_camera.orthographic)
            {
                // Loud rather than merely wrong. A perspective camera still renders the
                // floor, so the failure would show up as panning that drifts with distance
                // -- which reads as a tuning problem for as long as it takes to look here.
                Debug.LogError(
                    $"[World] {_camera.name} is not orthographic. The hall is a 2D floor " +
                    "plan and every world measurement here assumes an orthographic view.",
                    this);
                return false;
            }

            return true;
        }

        private void ResolveBootstrap()
        {
            if (_bootstrap == null)
            {
                _bootstrap = FindAnyObjectByType<GameBootstrap>();
            }

            if (_bootstrap == null)
            {
                Debug.LogError(
                    "[World] No GameBootstrap in the scene, so the hall has no guild to " +
                    "draw and every room will be missing. The floor will still pan.",
                    this);
            }
        }

        private void ConfigureSorting()
        {
            // Depth into a high three-quarter floor plan is world Y, so that is what the
            // camera sorts transparency along. Set here rather than in Graphics settings
            // because it is a property of this view rather than of the project, and the
            // interface -- which is UI Toolkit and does not sort this way -- should not
            // inherit it.
            _camera.transparencySortMode = TransparencySortMode.CustomAxis;
            _camera.transparencySortAxis = new Vector3(0f, 1f, 0f);
        }

        private void BuildFloor()
        {
            if (_floorRoot == null)
            {
                var root = new GameObject("Floor");
                root.transform.SetParent(transform, false);
                _floorRoot = root.transform;

                // Built together with the root, not lazily beside it. A scene change
                // destroys the root and leaves this field holding a Transform that is
                // null to Unity and not to C#, so a floor cached across that point would
                // quietly rebuild itself into an object that no longer exists.
                _floor = new GreyBoxFloor(_floorRoot);
            }

            _floor.Rebuild(_floorBounds, _gridSpacing);
        }

        // ------------------------------------------------------------------ camera -----

        private void ClampCameraIntoBounds()
        {
            MoveCameraTo(_camera.transform.position);
        }

        private void MoveCameraTo(Vector2 centre)
        {
            Vector2 clamped = WorldCameraBounds.Clamp(centre, ViewSizeInWorldUnits(), _floorBounds);
            Vector3 position = _camera.transform.position;
            _camera.transform.position = new Vector3(clamped.x, clamped.y, position.z);
        }

        private Vector2 ViewSizeInWorldUnits()
        {
            float height = _camera.orthographicSize * 2f;
            return new Vector2(height * _camera.aspect, height);
        }

        private Vector2 ScreenToWorld(Vector2 pixel)
        {
            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(pixel.x, pixel.y, 0f));
            return new Vector2(world.x, world.y);
        }
    }
}
