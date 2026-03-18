using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace FortuneValley.CameraControl
{
    /// <summary>
    /// Click and drag camera panning.
    /// Includes drag threshold to prevent accidental panning on quick taps.
    /// Works with both mouse and touch input.
    /// </summary>
    public class CameraPanController : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        [Header("Pan Settings")]
        [Tooltip("Pixels of movement before panning starts (prevents accidental pan on tap)")]
        [SerializeField] private float _dragThreshold = 10f;

        [Header("References")]
        [Tooltip("Camera to use for raycasting. If null, uses Camera.main.")]
        [SerializeField] private UnityEngine.Camera _camera;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private enum DragState { Idle, DragStarted, Panning }
        private DragState _state = DragState.Idle;
        private Vector2 _dragStartPosition;
        private Vector3 _lastWorldPosition;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// True when camera is actively being dragged.
        /// Use this to suppress other input (e.g., lot selection).
        /// </summary>
        public bool IsPanning => _state == DragState.Panning;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Start()
        {
            if (_camera == null)
            {
                _camera = UnityEngine.Camera.main;
            }
        }

        private void Update()
        {
            // Don't pan when pointer is over UI
            if (IsPointerOverUI())
            {
                if (_state != DragState.Idle)
                {
                    _state = DragState.Idle;
                }
                return;
            }

            bool isPressed = GetPointerPressed();
            Vector2 pointerPos = GetPointerPosition();

            switch (_state)
            {
                case DragState.Idle:
                    HandleIdleState(isPressed, pointerPos);
                    break;

                case DragState.DragStarted:
                    HandleDragStartedState(isPressed, pointerPos);
                    break;

                case DragState.Panning:
                    HandlePanningState(isPressed, pointerPos);
                    break;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // STATE HANDLERS
        // ═══════════════════════════════════════════════════════════════

        private void HandleIdleState(bool isPressed, Vector2 pointerPos)
        {
            if (isPressed)
            {
                _state = DragState.DragStarted;
                _dragStartPosition = pointerPos;
                _lastWorldPosition = GetWorldPosition(pointerPos);
            }
        }

        private void HandleDragStartedState(bool isPressed, Vector2 pointerPos)
        {
            if (!isPressed)
            {
                // Released before threshold - this was a tap, not a drag
                _state = DragState.Idle;
            }
            else if (Vector2.Distance(pointerPos, _dragStartPosition) > _dragThreshold)
            {
                // Exceeded threshold - start panning
                _state = DragState.Panning;
                _lastWorldPosition = GetWorldPosition(pointerPos);
            }
        }

        private void HandlePanningState(bool isPressed, Vector2 pointerPos)
        {
            if (!isPressed)
            {
                _state = DragState.Idle;
            }
            else
            {
                // Calculate world-space movement and apply to camera
                Vector3 currentWorld = GetWorldPosition(pointerPos);
                Vector3 delta = _lastWorldPosition - currentWorld;

                // Move camera in world XZ plane
                transform.position += new Vector3(delta.x, 0, delta.z);

                // Update reference position for next frame
                _lastWorldPosition = GetWorldPosition(pointerPos);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // INPUT HELPERS
        // ═══════════════════════════════════════════════════════════════

        private bool GetPointerPressed()
        {
            // Check touch first (mobile), then mouse (desktop)
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                return true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                return true;
            }

            return false;
        }

        private Vector2 GetPointerPosition()
        {
            // Check touch first (mobile), then mouse (desktop)
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                return Touchscreen.current.primaryTouch.position.ReadValue();
            }

            if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }

            return Vector2.zero;
        }

        private Vector3 GetWorldPosition(Vector2 screenPos)
        {
            // Cast ray from screen to ground plane (y = 0)
            Ray ray = _camera.ScreenPointToRay(screenPos);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }

            return Vector3.zero;
        }

        private bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
