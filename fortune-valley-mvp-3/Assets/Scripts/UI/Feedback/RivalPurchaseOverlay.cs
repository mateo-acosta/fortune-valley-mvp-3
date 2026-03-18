using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;

namespace FortuneValley.UI.Feedback
{
    /// <summary>
    /// Full-screen overlay that briefly appears when the rival purchases a lot.
    /// Creates an impactful moment that reinforces the consequence of inaction.
    ///
    /// LEARNING DESIGN: This "ouch" moment teaches through consequence.
    /// When the rival takes a lot, the student feels it. This visceral
    /// feedback makes the competition real and motivates better planning.
    /// </summary>
    public class RivalPurchaseOverlay : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        [Header("UI References")]
        [SerializeField] private GameObject _overlayPanel;
        [SerializeField] private Image _flashImage;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _lotNameText;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Animation")]
        [Tooltip("Total duration of the overlay")]
        [SerializeField] private float _displayDuration = 1.5f;
        [Tooltip("Duration of the flash effect")]
        [SerializeField] private float _flashDuration = 0.2f;
        [Tooltip("Curve for fade in/out")]
        [SerializeField] private AnimationCurve _fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Colors")]
        [SerializeField] private Color _flashColor = new Color(1f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private Color _textColor = new Color(1f, 0.3f, 0.3f);

        [Header("Dependencies")]
        [SerializeField] private CityManager _cityManager;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private float _timer;
        private bool _isShowing;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Awake()
        {
            if (_overlayPanel != null)
            {
                _overlayPanel.SetActive(false);
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            if (_cityManager == null) Debug.LogError("[RivalPurchaseOverlay] _cityManager not wired in Inspector.");
        }

        private void OnEnable()
        {
            GameEvents.OnRivalPurchasedLot += HandleRivalPurchased;
        }

        private void OnDisable()
        {
            GameEvents.OnRivalPurchasedLot -= HandleRivalPurchased;
        }

        private void Update()
        {
            if (!_isShowing) return;

            _timer += Time.deltaTime;
            float progress = _timer / _displayDuration;

            if (progress >= 1f)
            {
                Hide();
                return;
            }

            // Flash effect at start
            if (_flashImage != null)
            {
                float flashProgress = _timer / _flashDuration;
                if (flashProgress < 1f)
                {
                    float flashAlpha = 1f - flashProgress;
                    var color = _flashColor;
                    color.a = _flashColor.a * flashAlpha;
                    _flashImage.color = color;
                }
                else
                {
                    _flashImage.color = new Color(0, 0, 0, 0);
                }
            }

            // Fade effect
            if (_canvasGroup != null)
            {
                // Fade in quickly, stay, then fade out
                float fadeProgress;
                if (progress < 0.1f)
                {
                    // Fade in (first 10%)
                    fadeProgress = progress / 0.1f;
                }
                else if (progress > 0.7f)
                {
                    // Fade out (last 30%)
                    fadeProgress = 1f - ((progress - 0.7f) / 0.3f);
                }
                else
                {
                    // Fully visible
                    fadeProgress = 1f;
                }

                _canvasGroup.alpha = _fadeCurve.Evaluate(fadeProgress);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════════

        private void HandleRivalPurchased(string lotId)
        {
            // Get lot name
            string lotName = lotId;
            if (_cityManager != null)
            {
                var lot = _cityManager.GetLot(lotId);
                if (lot != null)
                {
                    lotName = lot.DisplayName;
                }
            }

            Show(lotName);
        }

        // ═══════════════════════════════════════════════════════════════
        // DISPLAY METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Show the rival purchase overlay.
        /// </summary>
        public void Show(string lotName)
        {
            _timer = 0f;
            _isShowing = true;

            // Set text
            if (_titleText != null)
            {
                _titleText.text = "RIVAL CLAIMED";
                _titleText.color = _textColor;
            }

            if (_lotNameText != null)
            {
                _lotNameText.text = lotName;
                _lotNameText.color = _textColor;
            }

            // Reset flash
            if (_flashImage != null)
            {
                _flashImage.color = _flashColor;
            }

            // Show panel
            if (_overlayPanel != null)
            {
                _overlayPanel.SetActive(true);
            }

            // Reset alpha
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
        }

        /// <summary>
        /// Hide the overlay.
        /// </summary>
        public void Hide()
        {
            _isShowing = false;

            if (_overlayPanel != null)
            {
                _overlayPanel.SetActive(false);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public bool IsShowing => _isShowing;
    }
}
