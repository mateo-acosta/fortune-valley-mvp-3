using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;

namespace FortuneValley.UI.Popups
{
    /// <summary>
    /// Popup for selecting a loan option to finance a lot purchase.
    /// Shows available loan options filtered by credit score and DTI.
    /// Fires intent event when player selects a loan.
    ///
    /// LEARNING DESIGN: Students compare loan terms side-by-side,
    /// seeing how APR and term length affect monthly payment and total cost.
    /// </summary>
    public class LoanSelectionPopup : UIPopup
    {
        // ===============================================================
        // REFERENCES
        // ===============================================================

        [Header("Lot Info")]
        [SerializeField] private TextMeshProUGUI _lotNameText;
        [SerializeField] private TextMeshProUGUI _lotPriceText;

        [Header("Loan Option Display")]
        [SerializeField] private TextMeshProUGUI _loanDetailsText;
        [SerializeField] private TextMeshProUGUI _noLoansText;

        [Header("Buttons")]
        [SerializeField] private Button _cancelButton;

        // ===============================================================
        // RUNTIME STATE
        // ===============================================================

        private string _lotId;
        private float _lotPrice;
        private string _lotDisplayName;

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        private void Start()
        {
            SetupButtons();
        }

        private void SetupButtons()
        {
            if (_cancelButton != null)
                _cancelButton.onClick.AddListener(OnCancelClicked);
        }

        // ===============================================================
        // PUBLIC METHODS
        // ===============================================================

        /// <summary>
        /// Configure the popup with lot data before showing.
        /// Called by UIManager when OnLoanSelectionRequested fires.
        /// </summary>
        public void ConfigureForLot(string lotId, float price, string displayName)
        {
            _lotId = lotId;
            _lotPrice = price;
            _lotDisplayName = displayName;
        }

        protected override void OnShow()
        {
            base.OnShow();
            UpdateDisplay();
        }

        protected override void OnHide()
        {
            base.OnHide();
            _lotId = null;
        }

        // ===============================================================
        // DISPLAY
        // ===============================================================

        private void UpdateDisplay()
        {
            if (_lotNameText != null)
                _lotNameText.text = _lotDisplayName ?? "Unknown Lot";

            if (_lotPriceText != null)
                _lotPriceText.text = $"Price: ${_lotPrice:N0}";
        }

        // ===============================================================
        // LOAN SELECTION (called by dynamically created buttons or list items)
        // ===============================================================

        /// <summary>
        /// Called when player selects a loan option.
        /// Fires intent event for LoanSystem to process.
        /// </summary>
        public void SelectLoan(string loanConfigId)
        {
            if (string.IsNullOrEmpty(_lotId) || string.IsNullOrEmpty(loanConfigId)) return;

            GameEvents.RaiseLoanPurchaseRequested(loanConfigId, _lotId, _lotPrice);
            OnCancelClicked();
        }
    }
}
