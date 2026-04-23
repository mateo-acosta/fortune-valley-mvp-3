using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace FortuneValley.CameraControl
{
    /// <summary>
    /// Click and drag camera panning with scroll/pinch zoom.
    /// Constrains the camera to an inspector-editable rectangular fence on the XZ plane
    /// and to a min/max orthographic zoom range. Zoom is anchored to the cursor position
    /// (the world point under the cursor stays stationary during zoom).
    /// </summary>
    public class CameraPanController : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        [Header("Pan Settings")]
        [Tooltip("Pixels of movement before panning starts (prevents accidental pan on tap)")]
        [SerializeField] private float _dragThreshold = 10f;

        [Header("Zoom Settings")]
        [Tooltip("Mouse wheel zoom speed (orthographic size units per scroll tick)")]
        [SerializeField] private float _scrollZoomSpeed = 0.01f;

        [Tooltip("Touch pinch zoom speed (orthographic size units per pixel of pinch delta)")]
        [SerializeField] private float _pinchZoomSpeed = 0.01f;

        [Tooltip("Minimum orthographic size (most zoomed in)")]
        [SerializeField] private float _minZoom = 4f;

        [Tooltip("Maximum orthographic size (most zoomed out)")]
        [SerializeField] private float _maxZoom = 14f;

        [Header("Camera Fence (XZ world bounds)")]
        [SerializeField] private float _minX = -20f;
        [SerializeField] private float _maxX = 20f;
        [SerializeField] private float _minZ = -20f;
        [SerializeField] private float _maxZ = 20f;

        [Header("Gizmo")]
        [SerializeField] private Color _fenceGizmoColor = new Color(1f, 0.92f, 0.016f, 0.8f);
        [Tooltip("Y level at which the fence gizmo is drawn (visual only)")]
        [SerializeField] private float _fenceGizmoY = 0f;

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
        private float _lastPinchDistance;
        private bool _pinchActive;
        private readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>();
        private PointerEventData _cachedPointerEventData;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

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
            bool pointerOverUI = IsPointerOverUI();

            // Zoom runs independently of pan state; skip when pointer is over UI
            if (!pointerOverUI)
            {
                HandleZoom();
            }

            if (pointerOverUI)
            {
                if (_state != DragState.Idle)
                {
                    _state = DragState.Idle;
                }
                ClampCameraToFence();
                return;
            }

            // Two-finger touch is for pinch zoom, not pan
            if (IsTwoFingerTouch())
            {
                _state = DragState.Idle;
                ClampCameraToFence();
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

            ClampCameraToFence();
        }

        // ═══════════════════════════════════════════════════════════════
        // PAN STATE HANDLERS
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
                _state = DragState.Idle;
            }
            else if (Vector2.Distance(pointerPos, _dragStartPosition) > _dragThreshold)
            {
                _state = DragState.Panning;
                _lastWorldPosition = GetWorldPosition(pointerPos);
            }
        }

        private void HandlePanningState(bool isPressed, Vector2 pointerPos)
        {
            if (!isPressed)
            {
                _state = DragState.Idle;
                return;
            }

            Vector3 currentWorld = GetWorldPosition(pointerPos);
            Vector3 delta = _lastWorldPosition - currentWorld;
            transform.position += new Vector3(delta.x, 0, delta.z);
            _lastWorldPosition = GetWorldPosition(pointerPos);
        }

        // ═══════════════════════════════════════════════════════════════
        // ZOOM
        // ═══════════════════════════════════════════════════════════════

        private void HandleZoom()
        {
            if (_camera == null || !_camera.orthographic) return;

            // Two-finger pinch takes priority when active
            if (HandlePinchZoom()) return;

            float scroll = 0f;
            if (Mouse.current != null)
            {
                scroll = Mouse.current.scroll.y.ReadValue();
            }

            if (Mathf.Approximately(scroll, 0f)) return;

            Vector2 pointerPos = GetPointerPosition();
            float delta = -scroll * _scrollZoomSpeed;
            ApplyZoom(delta, pointerPos);
        }

        private bool HandlePinchZoom()
        {
            if (Touchscreen.current == null) return false;

            var touches = Touchscreen.current.touches;
            if (touches.Count < 2) { _pinchActive = false; return false; }

            var t0 = touches[0];
            var t1 = touches[1];
            if (!t0.press.isPressed || !t1.press.isPressed) { _pinchActive = false; return false; }

            Vector2 p0 = t0.position.ReadValue();
            Vector2 p1 = t1.position.ReadValue();
            float dist = Vector2.Distance(p0, p1);

            if (!_pinchActive)
            {
                _pinchActive = true;
                _lastPinchDistance = dist;
                return true;
            }

            float pinchDelta = dist - _lastPinchDistance;
            _lastPinchDistance = dist;

            Vector2 midpoint = (p0 + p1) * 0.5f;
            float zoomDelta = -pinchDelta * _pinchZoomSpeed;
            ApplyZoom(zoomDelta, midpoint);
            return true;
        }

        /// <summary>
        /// Change orthographic size by delta, keeping the world point under the
        /// given screen position anchored (zoom-to-cursor).
        /// </summary>
        private void ApplyZoom(float sizeDelta, Vector2 anchorScreenPos)
        {
            Vector3 worldBefore = GetWorldPosition(anchorScreenPos);

            float newSize = Mathf.Clamp(_camera.orthographicSize + sizeDelta, _minZoom, _maxZoom);
            if (Mathf.Approximately(newSize, _camera.orthographicSize)) return;

            _camera.orthographicSize = newSize;

            Vector3 worldAfter = GetWorldPosition(anchorScreenPos);
            Vector3 shift = worldBefore - worldAfter;
            transform.position += new Vector3(shift.x, 0f, shift.z);
        }

        // ═══════════════════════════════════════════════════════════════
        // FENCE
        // ═══════════════════════════════════════════════════════════════

        private void ClampCameraToFence()
        {
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x, _minX, _maxX);
            p.z = Mathf.Clamp(p.z, _minZ, _maxZ);
            transform.position = p;
        }

        // ═══════════════════════════════════════════════════════════════
        // INPUT HELPERS
        // ═══════════════════════════════════════════════════════════════

        private bool IsTwoFingerTouch()
        {
            if (Touchscreen.current == null) return false;
            var touches = Touchscreen.current.touches;
            if (touches.Count < 2) return false;
            return touches[0].press.isPressed && touches[1].press.isPressed;
        }

        private bool GetPointerPressed()
        {
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
            if (EventSystem.current == null) return false;

            if (_cachedPointerEventData == null)
                _cachedPointerEventData = new PointerEventData(EventSystem.current);
            _cachedPointerEventData.position = GetPointerPosition();

            _uiRaycastResults.Clear();
            EventSystem.current.RaycastAll(_cachedPointerEventData, _uiRaycastResults);

            for (int i = 0; i < _uiRaycastResults.Count; i++)
            {
                if (!(_uiRaycastResults[i].module is PhysicsRaycaster))
                    return true;
            }
            return false;
        }

        // ═══════════════════════════════════════════════════════════════
        // EDITOR
        // ═══════════════════════════════════════════════════════════════

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = _fenceGizmoColor;
            Vector3 a = new Vector3(_minX, _fenceGizmoY, _minZ);
            Vector3 b = new Vector3(_maxX, _fenceGizmoY, _minZ);
            Vector3 c = new Vector3(_maxX, _fenceGizmoY, _maxZ);
            Vector3 d = new Vector3(_minX, _fenceGizmoY, _maxZ);
            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
        }
#endif
    }
}
