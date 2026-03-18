using UnityEngine;
using UnityEngine.UI;
using System;

namespace FortuneValley.UI.Feedback
{
    /// <summary>
    /// Animates a coin sprite flying from one screen position to another.
    /// Uses a bezier curve for a satisfying arc motion.
    ///
    /// LEARNING DESIGN: Creates a tangible connection between income source
    /// (restaurant) and the player's account. Money doesn't just appear -
    /// students see it travel, reinforcing the cause-and-effect of earning.
    /// </summary>
    public class CoinFlyAnimation : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        [Header("Animation Settings")]
        [Tooltip("Duration of the flight in seconds")]
        [SerializeField] private float _flightDuration = 0.8f;

        [Tooltip("Height of the arc (as fraction of distance)")]
        [SerializeField] private float _arcHeight = 0.3f;

        [Tooltip("Scale at start")]
        [SerializeField] private float _startScale = 1.0f;

        [Tooltip("Scale at end")]
        [SerializeField] private float _endScale = 0.5f;

        [Header("References")]
        [SerializeField] private Image _coinImage;
        [SerializeField] private RectTransform _rectTransform;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private Vector2 _startPos;
        private Vector2 _endPos;
        private Vector2 _controlPoint;
        private float _timer;
        private bool _isAnimating;
        private Action _onComplete;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Awake()
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }
            if (_coinImage == null)
            {
                _coinImage = GetComponent<Image>();
            }

            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_isAnimating) return;

            _timer += Time.deltaTime;
            float progress = Mathf.Clamp01(_timer / _flightDuration);

            // Bezier curve position (quadratic)
            Vector2 pos = CalculateBezierPoint(progress, _startPos, _controlPoint, _endPos);
            _rectTransform.anchoredPosition = pos;

            // Scale interpolation
            float scale = Mathf.Lerp(_startScale, _endScale, progress);
            _rectTransform.localScale = Vector3.one * scale;

            // Complete
            if (progress >= 1f)
            {
                Complete();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Start the coin flight animation from one position to another.
        /// </summary>
        /// <param name="startScreenPos">Start position in screen space</param>
        /// <param name="endScreenPos">End position in screen space</param>
        /// <param name="onComplete">Callback when animation completes</param>
        public void Fly(Vector2 startScreenPos, Vector2 endScreenPos, Action onComplete = null)
        {
            // Convert screen positions to canvas positions
            _startPos = startScreenPos;
            _endPos = endScreenPos;
            _onComplete = onComplete;

            // Calculate control point for arc (above the midpoint)
            Vector2 midPoint = (_startPos + _endPos) / 2f;
            float distance = Vector2.Distance(_startPos, _endPos);
            _controlPoint = midPoint + Vector2.up * (distance * _arcHeight);

            // Initialize
            _rectTransform.anchoredPosition = _startPos;
            _rectTransform.localScale = Vector3.one * _startScale;
            _timer = 0f;
            _isAnimating = true;

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Start flight from world position to screen position.
        /// </summary>
        public void FlyFromWorld(Vector3 worldPos, Vector2 endScreenPos, UnityEngine.Camera camera, Action onComplete = null)
        {
            Vector3 screenPos = camera.WorldToScreenPoint(worldPos);
            Fly(new Vector2(screenPos.x, screenPos.y), endScreenPos, onComplete);
        }

        /// <summary>
        /// Return to pool.
        /// </summary>
        public void ReturnToPool()
        {
            _isAnimating = false;
            _onComplete = null;
            gameObject.SetActive(false);
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════

        private void Complete()
        {
            _isAnimating = false;
            _onComplete?.Invoke();
            _onComplete = null;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Calculate a point on a quadratic bezier curve.
        /// </summary>
        private Vector2 CalculateBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }

        // ═══════════════════════════════════════════════════════════════
        // ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public bool IsAnimating => _isAnimating;
    }
}
