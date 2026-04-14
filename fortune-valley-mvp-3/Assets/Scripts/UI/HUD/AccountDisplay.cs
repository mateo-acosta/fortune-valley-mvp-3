using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;

namespace FortuneValley.UI.HUD
{
    /// <summary>
    /// Displays a single account balance (Checking or Investing).
    /// Shows the balance and visual feedback when it changes.
    /// </summary>
    public class AccountDisplay : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        [Header("Account Type")]
        [SerializeField] private AccountType _accountType;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _labelText;
        [SerializeField] private TextMeshProUGUI _balanceText;
        [SerializeField] private TextMeshProUGUI _deltaText;

        [Header("Colors")]
        [SerializeField] private Color _gainColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _lossColor = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private Color _normalColor = Color.white;

        [Header("Animation")]
        [SerializeField] private float _deltaDisplayDuration = 1.5f;

        [Header("Pulse Animation")]
        [Tooltip("Duration of the pulse effect")]
        [SerializeField] private float _pulseDuration = 0.3f;
        [Tooltip("Maximum scale during pulse")]
        [SerializeField] private float _pulseScale = 1.1f;
        [Tooltip("Color to flash during pulse")]
        [SerializeField] private Color _pulseColor = new Color(1f, 1f, 0.5f);

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private float _currentBalance;
        private float _deltaTimer;
        private float _pulseTimer;
        private bool _isPulsing;
        private Vector3 _originalScale;
        private Color _originalBalanceColor;
        private string _customLabel;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void OnEnable()
        {
            // Self-subscribe to the correct balance event based on account type.
            // Each AccountDisplay instance is responsible for its own updates.
            switch (_accountType)
            {
                case AccountType.Checking:
                    GameEvents.OnCheckingBalanceChanged += HandleBalanceChanged;
                    break;
                case AccountType.Investing:
                    GameEvents.OnInvestingBalanceChanged += HandleBalanceChanged;
                    break;
                case AccountType.CreditCard:
                    GameEvents.OnCreditCardBalanceChanged += HandleBalanceChanged;
                    break;
            }
        }

        private void OnDisable()
        {
            switch (_accountType)
            {
                case AccountType.Checking:
                    GameEvents.OnCheckingBalanceChanged -= HandleBalanceChanged;
                    break;
                case AccountType.Investing:
                    GameEvents.OnInvestingBalanceChanged -= HandleBalanceChanged;
                    break;
                case AccountType.CreditCard:
                    GameEvents.OnCreditCardBalanceChanged -= HandleBalanceChanged;
                    break;
            }
        }

        private void HandleBalanceChanged(float balance, float delta)
        {
            UpdateBalance(balance, delta);
        }

        private void Start()
        {
            // Label is intentionally hidden: HUD shows balance only.
            if (_labelText != null)
            {
                _labelText.gameObject.SetActive(false);
            }

            // Hide delta initially
            if (_deltaText != null)
            {
                _deltaText.gameObject.SetActive(false);
            }

            // Cache original values for pulse animation
            _originalScale = transform.localScale;
            if (_balanceText != null)
            {
                _originalBalanceColor = _balanceText.color;
            }
        }

        private void Update()
        {
            // Handle delta text fading
            if (_deltaTimer > 0)
            {
                _deltaTimer -= Time.deltaTime;
                if (_deltaTimer <= 0 && _deltaText != null)
                {
                    _deltaText.gameObject.SetActive(false);
                }
            }

            // Handle pulse animation
            if (_isPulsing)
            {
                _pulseTimer += Time.deltaTime;
                float progress = _pulseTimer / _pulseDuration;

                if (progress >= 1f)
                {
                    // Animation complete - reset
                    _isPulsing = false;
                    transform.localScale = _originalScale;
                    if (_balanceText != null)
                    {
                        _balanceText.color = _originalBalanceColor;
                    }
                }
                else
                {
                    // Pulse animation: scale up then back down
                    float scaleProgress = progress < 0.5f
                        ? progress * 2f  // 0 to 0.5 -> 0 to 1
                        : 1f - ((progress - 0.5f) * 2f);  // 0.5 to 1 -> 1 to 0

                    float currentScale = 1f + ((_pulseScale - 1f) * scaleProgress);
                    transform.localScale = _originalScale * currentScale;

                    // Color flash
                    if (_balanceText != null)
                    {
                        _balanceText.color = Color.Lerp(_originalBalanceColor, _pulseColor, scaleProgress);
                    }
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Override the default label text.
        /// </summary>
        public void SetLabel(string label)
        {
            _customLabel = label;
            if (_labelText != null) _labelText.text = label;
        }

        /// <summary>
        /// Update the displayed balance.
        /// </summary>
        /// <param name="newBalance">The new balance to display</param>
        /// <param name="delta">The change amount (for visual feedback)</param>
        public void UpdateBalance(float newBalance, float delta)
        {
            _currentBalance = newBalance;

            if (_balanceText != null)
            {
                _balanceText.text = FormatCurrency(newBalance);
            }

            // Show delta feedback
            if (delta != 0 && _deltaText != null)
            {
                ShowDelta(delta);
            }
        }

        /// <summary>
        /// Trigger a pulse animation to highlight income arrival.
        /// Called by IncomeFeedbackController when coin animation completes.
        /// </summary>
        public void PulseOnIncome()
        {
            _isPulsing = true;
            _pulseTimer = 0f;
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════

        private void ShowDelta(float delta)
        {
            _deltaText.gameObject.SetActive(true);

            if (delta > 0)
            {
                _deltaText.text = $"+{FormatCurrency(delta)}";
                _deltaText.color = _gainColor;
            }
            else
            {
                _deltaText.text = FormatCurrency(delta);
                _deltaText.color = _lossColor;
            }

            _deltaTimer = _deltaDisplayDuration;
        }

private string FormatCurrency(float amount)
        {
            if (Mathf.Abs(amount) >= 1000)
            {
                return $"${amount:N0}";
            }
            return $"${amount:F2}";
        }

        // ═══════════════════════════════════════════════════════════════
        // ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public AccountType AccountType => _accountType;
        public float CurrentBalance => _currentBalance;
    }
}
