using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;

namespace FortuneValley.UI.Popups
{
    /// <summary>
    /// Monthly credit card statement popup.
    /// Shows balance, interest, and payment options.
    /// Game pauses while this popup is open.
    ///
    /// LEARNING DESIGN: Forces the player to confront their debt every month.
    /// The interest preview shows what carrying a balance actually costs,
    /// teaching students why paying in full matters.
    /// </summary>
    public class CreditCardStatementPopup : UIPopup
    {
        // ===============================================================
        // REFERENCES
        // ===============================================================

        [Header("Statement Display")]
        [SerializeField] private TextMeshProUGUI _statementBalanceText;
        [SerializeField] private TextMeshProUGUI _interestChargedText;
        [SerializeField] private TextMeshProUGUI _minimumPaymentText;
        [SerializeField] private TextMeshProUGUI _creditScoreText;

        [Header("Payment Options")]
        [SerializeField] private Button _payFullButton;
        [SerializeField] private TextMeshProUGUI _payFullLabel;
        [SerializeField] private Button _payMinimumButton;
        [SerializeField] private TextMeshProUGUI _payMinimumLabel;
        [SerializeField] private Button _payCustomButton;

        [Header("Custom Amount")]
        [SerializeField] private GameObject _customAmountGroup;
        [SerializeField] private TMP_InputField _customAmountInput;
        [SerializeField] private Slider _customAmountSlider;
        [SerializeField] private Button _confirmCustomButton;

        [Header("Info")]
        [SerializeField] private TextMeshProUGUI _warningText;

        // ===============================================================
        // STATE
        // ===============================================================

        private float _statementBalance;
        private float _minimumPayment;
        private float _checkingBalance;
        private float _customAmount;

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        private void Start()
        {
            WireButtons();
        }

        private void OnEnable()
        {
            GameEvents.OnCreditCardStatementReady += HandleStatementReady;
            GameEvents.OnCheckingBalanceChanged += HandleCheckingChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnCreditCardStatementReady -= HandleStatementReady;
            GameEvents.OnCheckingBalanceChanged -= HandleCheckingChanged;
        }

        // ===============================================================
        // SETUP
        // ===============================================================

        private void WireButtons()
        {
            if (_payFullButton != null)
                _payFullButton.onClick.AddListener(OnPayFullClicked);
            if (_payMinimumButton != null)
                _payMinimumButton.onClick.AddListener(OnPayMinimumClicked);
            if (_payCustomButton != null)
                _payCustomButton.onClick.AddListener(OnPayCustomToggle);
            if (_confirmCustomButton != null)
                _confirmCustomButton.onClick.AddListener(OnConfirmCustomClicked);
            if (_customAmountInput != null)
                _customAmountInput.onValueChanged.AddListener(OnCustomInputChanged);
            if (_customAmountSlider != null)
                _customAmountSlider.onValueChanged.AddListener(OnCustomSliderChanged);
        }

        /// <summary>
        /// Configure the popup with current statement data.
        /// Called by the controller before Show().
        /// </summary>
        public void Configure(float statementBalance, float minimumPayment,
                              float interestCharged, float checkingBalance, int creditScore)
        {
            _statementBalance = statementBalance;
            _minimumPayment = minimumPayment;
            _checkingBalance = checkingBalance;

            if (_statementBalanceText != null)
                _statementBalanceText.text = $"Statement Balance: ${statementBalance:N2}";

            if (_interestChargedText != null)
                _interestChargedText.text = $"Interest Charged: ${interestCharged:N2}";

            if (_minimumPaymentText != null)
                _minimumPaymentText.text = $"Minimum Payment: ${minimumPayment:N2}";

            if (_creditScoreText != null)
                _creditScoreText.text = $"Credit Score: {creditScore}";

            if (_payFullLabel != null)
                _payFullLabel.text = $"Pay Full (${statementBalance:N2})";

            if (_payMinimumLabel != null)
                _payMinimumLabel.text = $"Pay Minimum (${minimumPayment:N2})";

            // Hide custom amount group initially
            if (_customAmountGroup != null)
                _customAmountGroup.SetActive(false);

            UpdateButtonStates();
        }

        // ===============================================================
        // EVENT HANDLERS
        // ===============================================================

        private void HandleStatementReady()
        {
            // Statement is ready; the controller will call Configure() then Show()
        }

        private void HandleCheckingChanged(float newBalance, float delta)
        {
            _checkingBalance = newBalance;
            UpdateButtonStates();
        }

        // ===============================================================
        // BUTTON CALLBACKS
        // ===============================================================

        private void OnPayFullClicked()
        {
            GameEvents.RaiseCreditCardPaymentRequested(_statementBalance);
            OnCancelClicked(); // Close popup
        }

        private void OnPayMinimumClicked()
        {
            GameEvents.RaiseCreditCardPaymentRequested(_minimumPayment);
            OnCancelClicked();
        }

        private void OnPayCustomToggle()
        {
            if (_customAmountGroup != null)
            {
                bool show = !_customAmountGroup.activeSelf;
                _customAmountGroup.SetActive(show);

                if (show && _customAmountSlider != null)
                {
                    _customAmountSlider.minValue = _minimumPayment;
                    _customAmountSlider.maxValue = _statementBalance;
                    _customAmountSlider.value = _minimumPayment;
                    _customAmount = _minimumPayment;
                }
            }
        }

        private void OnCustomInputChanged(string value)
        {
            if (float.TryParse(value, out float amount))
            {
                _customAmount = Mathf.Clamp(amount, 0, _statementBalance);
            }

            if (_customAmountSlider != null && _statementBalance > 0)
                _customAmountSlider.SetValueWithoutNotify(_customAmount);

            UpdateButtonStates();
        }

        private void OnCustomSliderChanged(float value)
        {
            _customAmount = value;

            if (_customAmountInput != null)
                _customAmountInput.SetTextWithoutNotify($"{_customAmount:F2}");

            UpdateButtonStates();
        }

        private void OnConfirmCustomClicked()
        {
            if (_customAmount >= _minimumPayment)
            {
                GameEvents.RaiseCreditCardPaymentRequested(_customAmount);
                OnCancelClicked();
            }
        }

        // ===============================================================
        // DISPLAY
        // ===============================================================

        private void UpdateButtonStates()
        {
            bool canPayFull = _checkingBalance >= _statementBalance;
            bool canPayMin = _checkingBalance >= _minimumPayment;

            if (_payFullButton != null)
                _payFullButton.interactable = canPayFull && _statementBalance > 0;

            if (_payMinimumButton != null)
                _payMinimumButton.interactable = canPayMin && _minimumPayment > 0;

            if (_confirmCustomButton != null)
                _confirmCustomButton.interactable = _customAmount >= _minimumPayment
                    && _checkingBalance >= _customAmount;

            // Warning when can't afford minimum
            if (_warningText != null)
            {
                if (!canPayMin && _minimumPayment > 0)
                {
                    _warningText.text = $"Insufficient funds! Need ${(_minimumPayment - _checkingBalance):N2} more for minimum payment.\n" +
                                        "Missing a payment will hurt your credit score.";
                    _warningText.gameObject.SetActive(true);
                }
                else
                {
                    _warningText.gameObject.SetActive(false);
                }
            }
        }
    }
}
