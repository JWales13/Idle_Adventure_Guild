using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IdleGuild.World
{
    /// <summary>
    /// The one MonoBehaviour in the World assembly: it points the camera at the hall and
    /// lets a finger drag it around.
    ///
    /// It is the third seam of its kind and deliberately the same shape as the other two.
    /// <c>GameBootstrap</c> is the seam between Unity's lifecycle and the simulation;
    /// <c>GuildScreenController</c> is the seam between Unity's lifecycle and the chrome;
    /// this is the seam between Unity's lifecycle and the hall. Everything underneath all
    /// three is plain C#.
    ///
    /// **It reads nothing from the economy, and step 2 is where that starts.** Recorded
    /// because the rule this view exists under is that it DEPICTS rather than CAUSES
    /// (section 4 of Docs/World_View_Design.md), and the way a depiction turns into a
    /// cause is one convenient calculation at a time. When the rooms arrive they read
    /// their state through <c>GuildState</c> and their trade through
    /// <c>TradeService.CollectRooms()</c>, and this class stays a camera.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldView : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The camera that looks at the hall. Uses the main camera if left empty.")]
        private Camera _camera;

        [SerializeField]
        [Tooltip("The extent of the hall in world units. Grows as new wings unlock.")]
        private Rect _floorBounds = new Rect(-16f, -12f, 32f, 24f);

        [SerializeField]
        [Min(0f)]
        [Tooltip("Grey-box grid spacing in world units. 0 draws no grid. Dies with the grey box.")]
        private float _gridSpacing = 2f;

        private GreyBoxFloor _floor;
        private Transform _floorRoot;

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
        /// It is a delegate rather than a call into <c>IdleGuild.UI</c> on purpose. World
        /// and UI are siblings above App and neither references the other; a view that
        /// reached across would put the two presentation layers into a cycle for the sake
        /// of a hit test. The overlay side knows where its own panels are, so it is the
        /// side that answers. Left unset, nothing blocks -- which is correct today,
        /// because the chrome does not yet sit over the hall.
        /// </summary>
        public Func<Vector2, bool> IsPointerOverChrome { get; set; }

        private void OnEnable()
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
                enabled = false;
                return;
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
                enabled = false;
                return;
            }

            ConfigureSorting();
            BuildFloor();
            ClampCameraIntoBounds();
        }

        private void OnDisable()
        {
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
