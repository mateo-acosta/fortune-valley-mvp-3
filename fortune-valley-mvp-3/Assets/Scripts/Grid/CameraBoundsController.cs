using UnityEngine;

namespace FortuneValley.Grid
{
    /// <summary>
    /// Constrains camera movement to stay within city bounds.
    /// Attach to the camera or a camera rig parent.
    /// </summary>
    public class CameraBoundsController : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        [Header("References")]
        [Tooltip("The CityBorder component that defines the bounds")]
        [SerializeField] private CityBorder _cityBorder;

        [Header("Settings")]
        [Tooltip("Apply bounds constraint every frame")]
        [SerializeField] private bool _constrainEveryFrame = true;

        [Tooltip("Smooth clamping instead of hard stop")]
        [SerializeField] private bool _smoothClamping = true;

        [Tooltip("Smoothing speed when using smooth clamping")]
        [SerializeField] private float _smoothSpeed = 10f;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private Vector3 _targetPosition;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Start()
        {
            if (_cityBorder == null) Debug.LogError("[CameraBoundsController] _cityBorder not wired in Inspector.");

            _targetPosition = transform.position;
        }

        private void LateUpdate()
        {
            if (!_constrainEveryFrame || _cityBorder == null) return;

            ConstrainPosition();
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Manually constrain the camera position to bounds.
        /// </summary>
        public void ConstrainPosition()
        {
            if (_cityBorder == null) return;

            Vector3 currentPos = transform.position;
            Vector3 clampedPos = _cityBorder.ClampToBounds(currentPos);

            if (_smoothClamping)
            {
                // Only smooth when position needs clamping
                if (Vector3.Distance(currentPos, clampedPos) > 0.01f)
                {
                    transform.position = Vector3.Lerp(currentPos, clampedPos, _smoothSpeed * Time.deltaTime);
                }
            }
            else
            {
                transform.position = clampedPos;
            }
        }

        /// <summary>
        /// Check if a target position is within bounds.
        /// </summary>
        public bool IsPositionValid(Vector3 position)
        {
            if (_cityBorder == null) return true;
            return _cityBorder.IsWithinBounds(position);
        }

        /// <summary>
        /// Get a clamped version of a target position.
        /// </summary>
        public Vector3 GetClampedPosition(Vector3 position)
        {
            if (_cityBorder == null) return position;
            return _cityBorder.ClampToBounds(position);
        }

        // ═══════════════════════════════════════════════════════════════
        // EDITOR
        // ═══════════════════════════════════════════════════════════════

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_cityBorder == null) return;

            // Draw connection to border
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, _cityBorder.transform.position);
        }
#endif
    }
}
