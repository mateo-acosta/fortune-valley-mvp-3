using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;

namespace FortuneValley.UI.HUD
{
    /// <summary>
    /// Shows which lot the rival is targeting and countdown until purchase.
    /// Displays urgency through color escalation.
    ///
    /// LEARNING DESIGN: Creates time pressure that forces financial decisions.
    /// "I could invest and grow my money, but the rival might take that lot!"
    /// This surfaces opportunity cost in a visceral way.
    /// </summary>
    public class RivalTargetIndicator : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        [Header("UI References")]
        [SerializeField] private GameObject _indicatorPanel;
        [SerializeField] private TextMeshProUGUI _targetText;
        [SerializeField] private TextMeshProUGUI _countdownText;
        [SerializeField] private Image _urgencyBackground;
        [SerializeField] private Image _warningIcon;

        [Header("Urgency Colors")]
        [Tooltip("Color when lots of time remaining (7+ days)")]
        [SerializeField] private Color _calmColor = new Color(0.9f, 0.9f, 0.9f);
        [Tooltip("Color when moderate time remaining (4-6 days)")]
        [SerializeField] private Color _warningColor = new Color(1f, 0.8f, 0.2f);
        [Tooltip("Color when urgent (1-3 days)")]
        [SerializeField] private Color _urgentColor = new Color(1f, 0.3f, 0.2f);
        [Tooltip("Color when critical (immediate)")]
        [SerializeField] private Color _criticalColor = new Color(1f, 0f, 0f);

        [Header("Urgency Thresholds")]
        [SerializeField] private int _warningDays = 6;
        [SerializeField] private int _urgentDays = 3;
        [SerializeField] private int _criticalDays = 1;

        [Header("Animation")]
        [SerializeField] private float _pulseSpeed = 2f;
        [SerializeField] private float _pulseIntensity = 0.2f;

        [Header("Dependencies")]
        [SerializeField] private CityManager _cityManager;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private string _currentTargetId;
        private int _daysRemaining;
        private float _pulseTimer;
        private bool _isActive;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Awake()
        {
            if (_indicatorPanel != null)
            {
                _indicatorPanel.SetActive(false);
            }
        }

        private void Start()
        {
            if (_cityManager == null) Debug.LogError("[RivalTargetIndicator] _cityManager not wired in Inspector.");
        }

        private void OnEnable()
        {
            GameEvents.OnRivalTargetChanged += HandleRivalTargetChanged;
            GameEvents.OnRivalPurchasedLot += HandleRivalPurchased;
            GameEvents.OnLotPurchased += HandleLotPurchased;
            GameEvents.OnTick += HandleTick;
        }

        private void OnDisable()
        {
            GameEvents.OnRivalTargetChanged -= HandleRivalTargetChanged;
            GameEvents.OnRivalPurchasedLot -= HandleRivalPurchased;
            GameEvents.OnLotPurchased -= HandleLotPurchased;
            GameEvents.OnTick -= HandleTick;
        }

        private void Update()
        {
            if (!_isActive) return;

            // Pulse animation for urgency
            _pulseTimer += Time.deltaTime * _pulseSpeed;

            if (_urgencyBackground != null && _daysRemaining <= _urgentDays)
            {
                float pulse = (Mathf.Sin(_pulseTimer * Mathf.PI) + 1f) / 2f;
                float alpha = 1f - (_pulseIntensity * pulse);
                var color = _urgencyBackground.color;
                color.a = alpha;
                _urgencyBackground.color = color;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════════

        private void HandleRivalTargetChanged(string lotId, int daysUntil)
        {
            if (string.IsNullOrEmpty(lotId))
            {
                HideIndicator();
                return;
            }

            _currentTargetId = lotId;
            _daysRemaining = daysUntil;
            _isActive = true;

            ShowIndicator(lotId, daysUntil);
        }

        private void HandleRivalPurchased(string lotId)
        {
            // Rival bought a lot - hide indicator
            if (lotId == _currentTargetId)
            {
                HideIndicator();
            }
        }

        private void HandleLotPurchased(string lotId, Owner owner)
        {
            // If player bought the targeted lot, hide indicator
            if (owner == Owner.Player && lotId == _currentTargetId)
            {
                HideIndicator();
            }
        }

        private void HandleTick(int tickNumber)
        {
            // Update countdown if active
            if (_isActive && _daysRemaining > 0)
            {
                _daysRemaining--;
                UpdateCountdown();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // DISPLAY METHODS
        // ═══════════════════════════════════════════════════════════════

        private void ShowIndicator(string lotId, int daysUntil)
        {
            if (_indicatorPanel != null)
            {
                _indicatorPanel.SetActive(true);
            }

            // Get lot name from CityManager
            string lotName = lotId;
            if (_cityManager != null)
            {
                var lot = _cityManager.GetLot(lotId);
                if (lot != null)
                {
                    lotName = lot.DisplayName;
                }
            }

            // Update target text
            if (_targetText != null)
            {
                _targetText.text = $"Rival targeting: {lotName}";
            }

            UpdateCountdown();
        }

        private void UpdateCountdown()
        {
            // Update countdown text
            if (_countdownText != null)
            {
                string dayWord = _daysRemaining == 1 ? "day" : "days";
                _countdownText.text = $"{_daysRemaining} {dayWord} remaining";
            }

            // Update urgency color
            UpdateUrgencyColor();
        }

        private void UpdateUrgencyColor()
        {
            Color targetColor;

            if (_daysRemaining <= _criticalDays)
            {
                targetColor = _criticalColor;
            }
            else if (_daysRemaining <= _urgentDays)
            {
                targetColor = _urgentColor;
            }
            else if (_daysRemaining <= _warningDays)
            {
                targetColor = _warningColor;
            }
            else
            {
                targetColor = _calmColor;
            }

            if (_urgencyBackground != null)
            {
                _urgencyBackground.color = targetColor;
            }

            if (_countdownText != null)
            {
                _countdownText.color = targetColor;
            }

            if (_warningIcon != null)
            {
                _warningIcon.color = targetColor;
            }
        }

        private void HideIndicator()
        {
            _isActive = false;
            _currentTargetId = null;

            if (_indicatorPanel != null)
            {
                _indicatorPanel.SetActive(false);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Manually show the indicator (for testing).
        /// </summary>
        public void Show(string lotId, int daysUntil)
        {
            HandleRivalTargetChanged(lotId, daysUntil);
        }

        /// <summary>
        /// Manually hide the indicator.
        /// </summary>
        public void Hide()
        {
            HideIndicator();
        }

        // ═══════════════════════════════════════════════════════════════
        // ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public bool IsActive => _isActive;
        public string CurrentTargetId => _currentTargetId;
        public int DaysRemaining => _daysRemaining;
    }
}
