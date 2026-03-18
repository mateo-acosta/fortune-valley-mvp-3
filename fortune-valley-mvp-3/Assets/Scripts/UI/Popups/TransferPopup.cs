using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;

namespace FortuneValley.UI.Popups
{
    /// <summary>
    /// Popup for transferring money between Checking and Investing accounts.
    /// </summary>
    public class TransferPopup : UIPopup
    {
        // ═══════════════════════════════════════════════════════════════
        // REFERENCES
        // ═══════════════════════════════════════════════════════════════

        [Header("Account Selection")]
        [SerializeField] private TMP_Dropdown _fromAccountDropdown;
        [SerializeField] private TMP_Dropdown _toAccountDropdown;

        [Header("Amount Input")]
        [SerializeField] private TMP_InputField _amountInput;
        [SerializeField] private Slider _amountSlider;
        [SerializeField] private Button _maxButton;

        [Header("Balance Display")]
        [SerializeField] private TextMeshProUGUI _fromBalanceText;
        [SerializeField] private TextMeshProUGUI _toBalanceText;
        [SerializeField] private TextMeshProUGUI _previewText;

        [Header("Buttons")]
        [SerializeField] private Button _transferButton;
        [SerializeField] private Button _cancelButton;

        [Header("Dependencies")]
        [SerializeField] private CurrencyManager _currencyManager;
        [SerializeField] private UIManager _uiManager;

        [Header("Colors")]
        [SerializeField] private Color _validColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _invalidColor = new Color(0.8f, 0.2f, 0.2f);

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private AccountType _fromAccount = AccountType.Checking;
        private AccountType _toAccount = AccountType.Investing;
        private float _transferAmount;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Start()
        {
            if (_currencyManager == null) Debug.LogError("[TransferPopup] _currencyManager not wired in Inspector.");

            SetupControls();
        }

        private void SetupControls()
        {
            // Account dropdowns
            if (_fromAccountDropdown != null)
            {
                _fromAccountDropdown.ClearOptions();
                _fromAccountDropdown.AddOptions(new System.Collections.Generic.List<string> { "Checking", "Investing" });
                _fromAccountDropdown.onValueChanged.AddListener(OnFromAccountChanged);
            }

            if (_toAccountDropdown != null)
            {
                _toAccountDropdown.ClearOptions();
                _toAccountDropdown.AddOptions(new System.Collections.Generic.List<string> { "Checking", "Investing" });
                _toAccountDropdown.value = 1; // Default to Investing
                _toAccountDropdown.onValueChanged.AddListener(OnToAccountChanged);
            }

            // Amount input
            if (_amountInput != null)
            {
                _amountInput.onValueChanged.AddListener(OnAmountInputChanged);
                _amountInput.contentType = TMP_InputField.ContentType.DecimalNumber;
            }

            if (_amountSlider != null)
            {
                _amountSlider.onValueChanged.AddListener(OnSliderChanged);
            }

            // Buttons
            if (_maxButton != null)
            {
                _maxButton.onClick.AddListener(OnMaxClicked);
            }

            if (_transferButton != null)
            {
                _transferButton.onClick.AddListener(OnTransferClicked);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(OnCancelClicked);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnCheckingBalanceChanged += OnBalanceChanged;
            GameEvents.OnInvestingBalanceChanged += OnBalanceChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnCheckingBalanceChanged -= OnBalanceChanged;
            GameEvents.OnInvestingBalanceChanged -= OnBalanceChanged;
        }

        private void OnBalanceChanged(float balance, float delta)
        {
            UpdateDisplay();
        }

        // ═══════════════════════════════════════════════════════════════
        // PANEL OVERRIDES
        // ═══════════════════════════════════════════════════════════════

        protected override void OnShow()
        {
            // Reset amount
            _transferAmount = 0;

            if (_amountInput != null)
            {
                _amountInput.text = "";
            }

            if (_amountSlider != null)
            {
                _amountSlider.value = 0;
            }

            UpdateDisplay();
        }

        // ═══════════════════════════════════════════════════════════════
        // CONTROL CALLBACKS
        // ═══════════════════════════════════════════════════════════════

        private void OnFromAccountChanged(int index)
        {
            _fromAccount = (AccountType)index;

            // Auto-set to account to the other one
            if (_fromAccount == _toAccount)
            {
                _toAccount = _fromAccount == AccountType.Checking ? AccountType.Investing : AccountType.Checking;
                if (_toAccountDropdown != null)
                {
                    _toAccountDropdown.SetValueWithoutNotify((int)_toAccount);
                }
            }

            UpdateDisplay();
        }

        private void OnToAccountChanged(int index)
        {
            _toAccount = (AccountType)index;

            // Auto-set from account to the other one
            if (_toAccount == _fromAccount)
            {
                _fromAccount = _toAccount == AccountType.Checking ? AccountType.Investing : AccountType.Checking;
                if (_fromAccountDropdown != null)
                {
                    _fromAccountDropdown.SetValueWithoutNotify((int)_fromAccount);
                }
            }

            UpdateDisplay();
        }

        private void OnAmountInputChanged(string value)
        {
            if (float.TryParse(value, out float amount))
            {
                _transferAmount = Mathf.Max(0, amount);
            }
            else
            {
                _transferAmount = 0;
            }

            // Update slider to match
            if (_amountSlider != null && _currencyManager != null)
            {
                float maxAmount = _currencyManager.GetBalance(_fromAccount);
                _amountSlider.SetValueWithoutNotify(maxAmount > 0 ? _transferAmount / maxAmount : 0);
            }

            UpdatePreview();
        }

        private void OnSliderChanged(float value)
        {
            if (_currencyManager == null) return;

            float maxAmount = _currencyManager.GetBalance(_fromAccount);
            _transferAmount = maxAmount * value;

            // Update input to match
            if (_amountInput != null)
            {
                _amountInput.SetTextWithoutNotify($"{_transferAmount:F2}");
            }

            UpdatePreview();
        }

        private void OnMaxClicked()
        {
            if (_currencyManager == null) return;

            _transferAmount = _currencyManager.GetBalance(_fromAccount);

            if (_amountInput != null)
            {
                _amountInput.text = $"{_transferAmount:F2}";
            }

            if (_amountSlider != null)
            {
                _amountSlider.value = 1f;
            }

            UpdatePreview();
        }

        private void OnTransferClicked()
        {
            if (_currencyManager == null || _transferAmount <= 0) return;

            bool success = _currencyManager.Transfer(_transferAmount, _fromAccount, _toAccount);

            if (success)
            {
                UnityEngine.Debug.Log($"[TransferPopup] Transferred ${_transferAmount:F2} from {_fromAccount} to {_toAccount}");
                _uiManager.HidePopup(this);
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[TransferPopup] Transfer failed");
                UpdateDisplay();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // DISPLAY UPDATES
        // ═══════════════════════════════════════════════════════════════

        private void UpdateDisplay()
        {
            if (_currencyManager == null) return;

            float fromBalance = _currencyManager.GetBalance(_fromAccount);
            float toBalance = _currencyManager.GetBalance(_toAccount);

            // Update balance texts
            if (_fromBalanceText != null)
            {
                _fromBalanceText.text = $"{_fromAccount}: ${fromBalance:N2}";
            }

            if (_toBalanceText != null)
            {
                _toBalanceText.text = $"{_toAccount}: ${toBalance:N2}";
            }

            // Update slider max
            if (_amountSlider != null)
            {
                _amountSlider.interactable = fromBalance > 0;
            }

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (_currencyManager == null) return;

            float fromBalance = _currencyManager.GetBalance(_fromAccount);
            float toBalance = _currencyManager.GetBalance(_toAccount);

            bool isValid = _transferAmount > 0 && _transferAmount <= fromBalance;

            // Preview text
            if (_previewText != null)
            {
                if (_transferAmount <= 0)
                {
                    _previewText.text = "Enter an amount to transfer";
                    _previewText.color = Color.gray;
                }
                else if (_transferAmount > fromBalance)
                {
                    _previewText.text = $"Insufficient funds (need ${_transferAmount - fromBalance:N2} more)";
                    _previewText.color = _invalidColor;
                }
                else
                {
                    float newFrom = fromBalance - _transferAmount;
                    float newTo = toBalance + _transferAmount;
                    _previewText.text = $"After: {_fromAccount} ${newFrom:N2} → {_toAccount} ${newTo:N2}";
                    _previewText.color = _validColor;
                }
            }

            // Transfer button state
            if (_transferButton != null)
            {
                _transferButton.interactable = isValid;
            }
        }
    }
}
