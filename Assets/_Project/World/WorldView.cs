using System;
using IdleGuild.App;
using IdleGuild.Core.Events;
using IdleGuild.Guild;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

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

        [SerializeField]
        [Tooltip("The interface, so a press that lands on it does not also reach the hall. " +
                 "Found in the scene if left empty.")]
        private UIDocument _chromeDocument;

        [Header("The hall")]
        [SerializeField]
        [Tooltip("Where each room stands. Keyed by BuildingDefinition Id.")]
        private HallRoom[] _plan = HallPlan.Default();

        [SerializeField]
        [Min(0f)]
        [Tooltip("Grey-box grid spacing in world units. 0 draws no grid. Dies with the grey box.")]
        private float _gridSpacing = 2f;

        /// <summary>
        /// How much of the hall the screen's shorter edge shows. A room is eight units
        /// wide, so fourteen puts one room across roughly half the width of a portrait
        /// phone with its neighbour and the corridor visible past it -- close enough to
        /// read as a floor plan rather than as a wall.
        /// </summary>
        private const float WorldUnitsAcrossTheShortEdge = 14f;

        private Rect _floorBounds;
        private GreyBoxFloor _floor;
        private Transform _floorRoot;

        private RoomRectangles _rooms;
        private Transform _roomsRoot;
        private bool _roomsDirty = true;

        private ChromeHitTest _chrome;

        private bool _pressOnTheHall;
        private bool _dragging;
        private bool _wasPressed;
        private Vector2 _pressStartPixel;
        private float _travelledPixels;
        private Vector2 _grabbedWorldPoint;

        /// <summary>The hall's current extent in world units.</summary>
        public Rect FloorBounds => _floorBounds;

        /// <summary>
        /// Asked, at the moment a press begins, whether that screen position belongs to the
        /// interface rather than to the hall. Returning true lets the press through to the
        /// chrome and the hall does not pan.
        ///
        /// Wired from <see cref="_chromeDocument"/> when the scene has an interface, and
        /// left settable so a test or a future overlay owner can answer instead.
        ///
        /// It is a delegate rather than a call into <c>IdleGuild.UI</c> on purpose: World
        /// and UI are siblings above App and neither references the other, so a hit test
        /// that reached across would put the two presentation layers into a cycle.
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
            ResolveChrome();

            _floorBounds = HallPlan.FloorFor(_plan);

            ConfigureCamera();
            FrameHall();
            BuildFloor();
            EnsureRooms();
            ClampCameraIntoBounds();

            Subscribe();
            _roomsDirty = true;
        }

        private void OnDisable()
        {
            Unsubscribe();
            _pressOnTheHall = false;
            _dragging = false;
            _wasPressed = false;
        }

        private void Update()
        {
            HandlePointer();
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

            EnsureRooms();
            _rooms.Rebuild(_plan, guild);
            _roomsDirty = false;
        }

        private void EnsureRooms()
        {
            if (EnsureChild("Rooms", ref _roomsRoot))
            {
                _rooms = null;
            }

            if (_rooms == null)
            {
                _rooms = new RoomRectangles(_roomsRoot);
            }
        }

        // ------------------------------------------------------------------- input -----

        private void HandlePointer()
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
                _pressOnTheHall = false;
                _dragging = false;
                _wasPressed = false;
                return;
            }

            Vector2 pixel = pointer.position.ReadValue();
            bool pressed = pointer.press.isPressed;

            if (pressed && !_wasPressed)
            {
                BeginPress(pixel);
            }
            else if (!pressed && _wasPressed)
            {
                EndPress(pixel);
            }
            else if (pressed && _dragging)
            {
                ContinueDrag(pixel);
            }

            _wasPressed = pressed;
        }

        private void BeginPress(Vector2 pixel)
        {
            // Decided once, when the press starts, and not re-asked while it is held. A
            // press that begins on the treasury bar belongs to the treasury bar for its
            // whole life, even when the finger travels off the panel -- otherwise dragging
            // the mailbox flicks the hall sideways the moment you leave it.
            if (IsPointerOverChrome != null && IsPointerOverChrome(pixel))
            {
                _pressOnTheHall = false;
                _dragging = false;
                return;
            }

            _pressOnTheHall = true;
            _dragging = true;
            _pressStartPixel = pixel;
            _travelledPixels = 0f;
            _grabbedWorldPoint = ScreenToWorld(pixel);
        }

        private void EndPress(Vector2 pixel)
        {
            bool wasATap = _pressOnTheHall && _travelledPixels <= TapSlackPixels();

            _pressOnTheHall = false;
            _dragging = false;

            if (wasATap)
            {
                TapAt(pixel);
            }
        }

        /// <summary>
        /// How far a finger may wander and still have meant a tap, in pixels.
        ///
        /// Measured as a fraction of the screen rather than as a pixel count, because a
        /// pixel is not a fixed size: forty pixels of slack is a comfortable thumb on a
        /// phone and an invisible twitch on a tablet, and the tolerance the player
        /// actually has is a fraction of the thing they are looking at.
        /// </summary>
        private static float TapSlackPixels()
        {
            const float FractionOfScreenHeight = 0.02f;
            return Screen.height * FractionOfScreenHeight;
        }

        private void TapAt(Vector2 pixel)
        {
            HallRoom room = HallPlan.FindAt(_plan, ScreenToWorld(pixel));

            if (room == null)
            {
                // Floor, corridor or street. Tapping nothing is a legitimate thing to do
                // and must stay silent -- step 6 gives the street its own meaning when the
                // tap is re-homed onto a waiting customer.
                return;
            }

            // The hall states what the player touched and stops there. It does not open the
            // panel, because it does not own one, and it does not check whether the room is
            // affordable or even built -- that is a rule, and rules live in services. See
            // PresentationEvents for why this crosses through Core rather than through a
            // reference.
            EventBus.Publish(new RoomSelected(room.BuildingId));
        }

        private void ContinueDrag(Vector2 pixel)
        {
            // The whole pan, and there is no speed constant in it. The world point the
            // finger landed on stays under the finger: measure where that pixel points now,
            // and move the camera by whatever closes the gap. It is correct at any
            // orthographic size and on any screen density for free, where a
            // pixels-times-speed version needs re-tuning every time either changes and is
            // never quite right at the edges of a drag.
            // Furthest from where the finger landed, not distance travelled: a finger that
            // wanders out and comes back has still not tapped, and a path length would
            // forgive it. Recorded on every frame of the drag so the release can tell a tap
            // from a pan without a timer, which would make a slow deliberate tap fail.
            _travelledPixels = Mathf.Max(
                _travelledPixels, Vector2.Distance(pixel, _pressStartPixel));

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

        private void ResolveChrome()
        {
            if (_chromeDocument == null)
            {
                _chromeDocument = FindAnyObjectByType<UIDocument>();
            }

            if (_chromeDocument == null)
            {
                // Not an error: a scene with no interface is a legitimate way to look at
                // the hall on its own, and nothing then needs to block a press.
                return;
            }

            _chrome = new ChromeHitTest(_chromeDocument);
            IsPointerOverChrome = _chrome.Covers;
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

        private void ConfigureCamera()
        {
            // Depth into a high three-quarter floor plan is world Y, so that is what the
            // camera sorts transparency along. Set here rather than in Graphics settings
            // because it is a property of this view rather than of the project, and the
            // interface -- which is UI Toolkit and does not sort this way -- should not
            // inherit it.
            _camera.transparencySortMode = TransparencySortMode.CustomAxis;
            _camera.transparencySortAxis = new Vector3(0f, 1f, 0f);

            // What lies beyond the floor. The hall is smaller than the screen early on and
            // section 5 wants outside visible at the entrance anyway, so something is
            // always showing past the edge -- and the default was Unity's blue skybox,
            // which reads as a void rather than as ground the guild stands on.
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = GreyBoxPalette.Outside;
        }

        private void BuildFloor()
        {
            if (EnsureChild("Floor", ref _floorRoot))
            {
                _floor = null;
            }

            if (_floor == null)
            {
                _floor = new GreyBoxFloor(_floorRoot);
            }

            _floor.Rebuild(_floorBounds, _gridSpacing);
        }

        /// <summary>
        /// Finds this view's named child, making it if it has gone, and reports whether it
        /// had to.
        ///
        /// **The two halves of a pair like this must be checked separately, and an earlier
        /// version of this class checked them together and threw.** A plain C# helper and
        /// the Transform it draws into do not have the same lifetime: a domain reload while
        /// the editor sits in Play mode -- which is what happens every time a script is
        /// recompiled mid-session, so it is the common case here rather than an exotic one
        /// -- clears the managed object and leaves the Unity reference standing. Building
        /// the helper only inside the "the child is missing" branch then skips it forever
        /// and the next call dereferences null.
        ///
        /// The reverse direction is real too, which is why this reports the creation rather
        /// than swallowing it: a helper that survives while its parent is replaced would go
        /// on drawing into an object nothing renders, and that failure is silent, where
        /// this one at least threw.
        /// </summary>
        private bool EnsureChild(string name, ref Transform holder)
        {
            if (holder != null)
            {
                return false;
            }

            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            holder = child.transform;
            return true;
        }

        // ------------------------------------------------------------------ camera -----

        /// <summary>
        /// Sets the zoom from the plan and the screen, rather than from a number typed into
        /// the scene.
        ///
        /// The camera shipped at orthographic size 5 -- a ten-unit window onto a
        /// twenty-four-unit floor -- which was right when the floor was empty and made a
        /// single room larger than the viewport the moment rooms existed. That is a magic
        /// number in the exact sense Principle 01 means: a value that has to agree with
        /// something else and has nothing keeping it honest.
        ///
        /// The policy itself lives in <see cref="WorldCameraBounds.OrthographicSizeFor"/>,
        /// where it can be tested. This is the whole of the project's zoom handling for
        /// now, and it is a placeholder: pinch-to-zoom supersedes it and is not among
        /// section 9's nine steps.
        /// </summary>
        private void FrameHall()
        {
            _camera.orthographicSize = WorldCameraBounds.OrthographicSizeFor(
                WorldUnitsAcrossTheShortEdge, _camera.aspect);
        }

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
