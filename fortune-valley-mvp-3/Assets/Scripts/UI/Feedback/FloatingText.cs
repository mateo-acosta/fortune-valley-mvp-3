using UnityEngine;
using TMPro;

namespace FortuneValley.UI.Feedback
{
    /// <summary>
    /// Pooled floating text that rises and fades when showing income/expense.
    /// Attach to a prefab with TextMeshProUGUI and CanvasGroup components.
    ///
    /// LEARNING DESIGN: Makes income feel tangible and real.
    /// Seeing "+$10" rise from the restaurant helps students understand
    /// that steady income is accumulating over time.
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        [Header("Animation Settings")]
        [Tooltip("How far the text rises in world units")]
        [SerializeField] private float _riseDistance = 1.5f;

        [Tooltip("How long the animation takes")]
        [SerializeField] private float _duration = 1.5f;

        [Tooltip("Curve for the rise animation")]
        [SerializeField] private AnimationCurve _riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Curve for the fade animation")]
        [SerializeField] private AnimationCurve _fadeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        [Header("References")]
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Colors")]
        [SerializeField] private Color _incomeColor = new Color(0.2f, 0.9f, 0.2f);
        [SerializeField] private Color _expenseColor = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private Color _neutralColor = Color.white;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private Vector3 _startPosition;
        private float _timer;
        private bool _isAnimating;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Awake()
        {
            // Try to find components if not assigned
            if (_text == null)
            {
                _text = GetComponentInChildren<TextMeshProUGUI>();
            }
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            // Ensure we start hidden
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_isAnimating) return;

            _timer += Time.deltaTime;
            float progress = Mathf.Clamp01(_timer / _duration);

            // Apply rise animation
            float riseProgress = _riseCurve.Evaluate(progress);
            transform.position = _startPosition + Vector3.up * (_riseDistance * riseProgress);

            // Apply fade animation
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = _fadeCurve.Evaluate(progress);
            }

            // Return to pool when done
            if (progress >= 1f)
            {
                ReturnToPool();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Show floating text at a world position.
        /// </summary>
        /// <param name="message">Text to display (e.g., "+$10")</param>
        /// <param name="worldPosition">Where to spawn in world space</param>
        /// <param name="isIncome">True for green (income), false for red (expense)</param>
        public void Show(string message, Vector3 worldPosition, bool isIncome = true)
        {
            // Set text and color
            if (_text != null)
            {
                _text.text = message;
                _text.color = isIncome ? _incomeColor : _expenseColor;
            }

            // Initialize position
            _startPosition = worldPosition;
            transform.position = _startPosition;

            // Reset animation state
            _timer = 0f;
            _isAnimating = true;

            // Reset alpha
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Show with custom color.
        /// </summary>
        public void Show(string message, Vector3 worldPosition, Color color)
        {
            if (_text != null)
            {
                _text.text = message;
                _text.color = color;
            }

            _startPosition = worldPosition;
            transform.position = _startPosition;
            _timer = 0f;
            _isAnimating = true;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Return this text to the pool.
        /// </summary>
        public void ReturnToPool()
        {
            _isAnimating = false;
            gameObject.SetActive(false);
        }

        // ═══════════════════════════════════════════════════════════════
        // ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public bool IsAnimating => _isAnimating;
    }
}
