using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Entities;

namespace FortuneValley.UI.Panels.Credit
{
    /// <summary>
    /// Credit "Current" tab: carousel-style loan browser.
    /// Player picks a lot from the dropdown, then browses available
    /// loan products with left/right arrows. Stats update per selection.
    /// Apply Now originates the loan and purchases the lot.
    ///
    /// LEARNING DESIGN: Students compare loan products side-by-side
    /// for the same property, seeing how APR, term, and down payment
    /// change total cost. This builds intuition for real mortgage shopping.
    /// </summary>
    public class CreditExploreSubPanel : SubPanelBase
    {
        // ===============================================================
        // DEPENDENCIES
        // ===============================================================

        [Header("Dependencies")]
        [SerializeField] private LoanSystem _loanSystem;
        [SerializeField] private CreditScoreSystem _creditCardSystem;
        [SerializeField] private CurrencyManager _currencyManager;
        [SerializeField] private CityManager _cityManager;

        // ===============================================================
        // LOT DROPDOWN
        // ===============================================================

        [Header("Lot Selection")]
        [SerializeField] private TMP_Dropdown _lotDropdown;

        // ===============================================================
        // CAROUSEL - LEFT SIDE
        // ===============================================================

        [Header("Carousel")]
        [SerializeField] private Image _loanImage;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Button _buttonLeft;
        [SerializeField] private Button _buttonRight;

        // ===============================================================
        // APPLY BUTTON
        // ===============================================================

        [Header("Apply")]
        [SerializeField] private Button _applyButton;
        [SerializeField] private TextMeshProUGUI _applyButtonText;
        [SerializeField] private TextMeshProUGUI _qualifyText;

        // ===============================================================
        // STATS TABLE - RIGHT SIDE
        // ===============================================================

        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI _aprValueText;
        [SerializeField] private TextMeshProUGUI _totalLoanValueText;
        [SerializeField] private TextMeshProUGUI _monthlyPaymentValueText;
        [SerializeField] private TextMeshProUGUI _creditScoreValueText;

        // ===============================================================
        // INCOME ESTIMATION
        // ===============================================================

        [Header("Income Estimation")]
        [Tooltip("Fraction of checking balance used as monthly income proxy for DTI")]
        [SerializeField] private float _monthlyIncomeFraction = 0.1f;

        // ===============================================================
        // STATE (extracted to pure C# class)
        // ===============================================================

        private readonly CreditExploreState _state = new CreditExploreState();

        // Lot the player was looking at when they opened Explore from the "buy but
        // can't afford" flow. Consumed on first Refresh so the dropdown pre-selects it.
        private string _pendingLotId;

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        protected override void OnEnable()
        {
            GameEvents.OnCreditScoreChanged += HandleCreditScoreChanged;
            GameEvents.OnLotPurchased += HandleLotPurchased;
            GameEvents.OnLoanOriginated += HandleLoanOriginated;
            GameEvents.OnCityInitialized += HandleCityInitialized;

            if (_buttonLeft != null) _buttonLeft.onClick.AddListener(HandlePrevLoan);
            if (_buttonRight != null) _buttonRight.onClick.AddListener(HandleNextLoan);
            if (_applyButton != null) _applyButton.onClick.AddListener(HandleApplyNow);
            if (_lotDropdown != null) _lotDropdown.onValueChanged.AddListener(HandleLotChanged);

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            GameEvents.OnCreditScoreChanged -= HandleCreditScoreChanged;
            GameEvents.OnLotPurchased -= HandleLotPurchased;
            GameEvents.OnLoanOriginated -= HandleLoanOriginated;
            GameEvents.OnCityInitialized -= HandleCityInitialized;

            if (_buttonLeft != null) _buttonLeft.onClick.RemoveListener(HandlePrevLoan);
            if (_buttonRight != null) _buttonRight.onClick.RemoveListener(HandleNextLoan);
            if (_applyButton != null) _applyButton.onClick.RemoveListener(HandleApplyNow);
            if (_lotDropdown != null) _lotDropdown.onValueChanged.RemoveListener(HandleLotChanged);

            base.OnDisable();
        }

        // ===============================================================
        // EVENT HANDLERS
        // ===============================================================

        private void HandleCreditScoreChanged(int newScore) => Refresh();
        private void HandleLotPurchased(string lotId, Owner owner) => Refresh();
        private void HandleCityInitialized(int totalLots)
        {
            // Reset cache so dropdown rebuilds with actual lot data
            _state.CachedLotCount = -1;
            Refresh();
        }
        private void HandleLoanOriginated(ActiveLoan loan) => Refresh();

        private void HandleLotChanged(int dropdownIndex)
        {
            RefreshLoansForSelectedLot();
        }

        private void HandlePrevLoan()
        {
            if (_state.FilteredLoans.Count == 0) return;

            _state.SelectedLoanIndex--;
            if (_state.SelectedLoanIndex < 0)
                _state.SelectedLoanIndex = _state.FilteredLoans.Count - 1;

            RefreshLoanDisplay();
        }

        private void HandleNextLoan()
        {
            if (_state.FilteredLoans.Count == 0) return;

            _state.SelectedLoanIndex++;
            if (_state.SelectedLoanIndex >= _state.FilteredLoans.Count)
                _state.SelectedLoanIndex = 0;

            RefreshLoanDisplay();
        }

        private void HandleApplyNow()
        {
            if (_state.AvailableLots.Count == 0) return;
            if (_state.FilteredLoans.Count == 0) return;
            if (_lotDropdown == null) return;

            int lotIndex = _lotDropdown.value;
            if (lotIndex < 0 || lotIndex >= _state.AvailableLots.Count) return;

            var lot = _state.AvailableLots[lotIndex];
            var result = _state.FilteredLoans[_state.SelectedLoanIndex];

            if (!result.IsEligible) return;

            GameEvents.RaiseLoanPurchaseRequested(
                result.Config.LoanId, lot.LotId, lot.BaseCost);
        }

        // ===============================================================
        // REFRESH
        // ===============================================================

        /// <summary>
        /// Called by LoanPanel when the player entered Explore from an unaffordable
        /// lot Buy click. The next Refresh pre-selects this lot in the dropdown.
        /// </summary>
        public void SetPendingLotId(string lotId)
        {
            _pendingLotId = lotId;
            if (isActiveAndEnabled) Refresh();
        }

        protected override void Refresh()
        {
            Debug.Log($"[CreditExplore] Refresh called. cityManager null={_cityManager == null}");
            RefreshLotDropdown();
            ApplyPendingLotSelection();
            RefreshLoansForSelectedLot();
        }

        private void ApplyPendingLotSelection()
        {
            if (string.IsNullOrEmpty(_pendingLotId)) return;
            if (_lotDropdown == null) return;

            for (int i = 0; i < _state.AvailableLots.Count; i++)
            {
                if (_state.AvailableLots[i] != null && _state.AvailableLots[i].LotId == _pendingLotId)
                {
                    _lotDropdown.value = i;
                    break;
                }
            }
            _pendingLotId = null;
        }

        private void RefreshLotDropdown()
        {
            if (_cityManager == null || _loanSystem == null)
            {
                Debug.Log($"[CreditExplore] Null check failed: city={_cityManager != null}, loan={_loanSystem != null}");
                return;
            }

            // Read properties only, then delegate filtering to pure C# helper
            var allLots = _cityManager.AllLots;
            var ownership = _cityManager.LotOwnership;
            var activeLoans = _loanSystem.Portfolio.AllLoans;

            Debug.Log($"[CreditExplore] AllLots.Count={allLots.Count}, ownership.Count={ownership.Count}, portfolio null={_loanSystem.Portfolio == null}");

            CreditExploreLotFilter.FilterAvailableLots(
                allLots, ownership, activeLoans, _state.AvailableLots);

            // Only rebuild dropdown UI when lot count changes
            if (_state.AvailableLots.Count == _state.CachedLotCount) return;
            _state.CachedLotCount = _state.AvailableLots.Count;

            if (_lotDropdown == null) return;

            _lotDropdown.ClearOptions();

            if (_state.AvailableLots.Count == 0)
            {
                // Empty state: no lots available
                _lotDropdown.interactable = false;
                ShowEmptyState("No lots available");
                return;
            }

            _lotDropdown.interactable = true;
            var options = new List<TMP_Dropdown.OptionData>(_state.AvailableLots.Count);
            for (int i = 0; i < _state.AvailableLots.Count; i++)
            {
                var lot = _state.AvailableLots[i];
                options.Add(new TMP_Dropdown.OptionData(
                    $"{lot.DisplayName} - ${lot.BaseCost:N0}"));
            }
            _lotDropdown.AddOptions(options);
            _lotDropdown.value = 0;
        }

        private void RefreshLoansForSelectedLot()
        {
            _state.FilteredLoans.Clear();
            _state.SelectedLoanIndex = 0;

            if (_state.AvailableLots.Count == 0)
            {
                ShowEmptyState("No lots available");
                return;
            }

            if (_lotDropdown == null || _loanSystem == null
                || _creditCardSystem == null || _currencyManager == null)
                return;

            int lotIndex = _lotDropdown.value;
            if (lotIndex < 0 || lotIndex >= _state.AvailableLots.Count) return;

            // Property reads only for eligibility data
            var configs = _loanSystem.AvailableLoans;
            int creditScore = _creditCardSystem.CreditScore;
            float monthlyDebt = _loanSystem.TotalYearlyDebt;
            float checkingBalance = _currencyManager.CheckingBalance;
            float monthlyIncome = CreditExploreIncomeEstimator.EstimateMonthlyIncome(
                checkingBalance, _monthlyIncomeFraction);
            float dtiRatio = CreditExploreIncomeEstimator.ComputeDtiRatio(
                monthlyDebt, monthlyIncome);

            float lotPrice = _state.AvailableLots[lotIndex].BaseCost;
            _state.SetFilteredLoans(LoanEligibilityFilter.Evaluate(configs, creditScore, dtiRatio, lotPrice));

            if (_state.FilteredLoans.Count == 0)
            {
                ShowEmptyState("No qualifying loans");
                return;
            }

            RefreshLoanDisplay();
        }

        private void RefreshLoanDisplay()
        {
            if (_state.FilteredLoans.Count == 0) return;
            if (_state.SelectedLoanIndex < 0 || _state.SelectedLoanIndex >= _state.FilteredLoans.Count)
                _state.SelectedLoanIndex = 0;

            var result = _state.FilteredLoans[_state.SelectedLoanIndex];
            var config = result.Config;

            // Get lot price for computation
            int lotIndex = _lotDropdown != null ? _lotDropdown.value : 0;
            if (lotIndex < 0 || lotIndex >= _state.AvailableLots.Count) return;
            float lotPrice = _state.AvailableLots[lotIndex].BaseCost;

            // Compute display values via extracted calculator
            var values = LoanDisplayCalculator.Calculate(lotPrice, config);

            // Update carousel left side
            if (_titleText != null)
                _titleText.text = config.DisplayName;

            if (_loanImage != null && config.LoanImage != null)
                _loanImage.sprite = config.LoanImage;

            // Update stats table right side
            if (_aprValueText != null)
                _aprValueText.text = $"{values.APRPercent:F1}%";

            if (_totalLoanValueText != null)
                _totalLoanValueText.text = $"${values.Principal:N0}";

            if (_monthlyPaymentValueText != null)
                _monthlyPaymentValueText.text = $"${values.YearlyPayment:N0}";

            if (_creditScoreValueText != null)
                _creditScoreValueText.text = $"{values.MinCreditScore}+";

            // Update qualify indicator
            if (_qualifyText != null)
                _qualifyText.text = result.IsEligible ? "Qualify" : result.Reason;

            // Update apply button state
            if (_applyButton != null)
                _applyButton.interactable = result.IsEligible;

            if (_applyButtonText != null)
                _applyButtonText.text = "Apply Now";

            // Update arrow visibility
            bool multipleLoans = _state.FilteredLoans.Count > 1;
            if (_buttonLeft != null)
                _buttonLeft.gameObject.SetActive(multipleLoans);
            if (_buttonRight != null)
                _buttonRight.gameObject.SetActive(multipleLoans);
        }

        // ===============================================================
        // EMPTY STATES
        // ===============================================================

        private void ShowEmptyState(string message)
        {
            if (_titleText != null)
                _titleText.text = message;

            if (_applyButton != null)
                _applyButton.interactable = false;

            if (_buttonLeft != null)
                _buttonLeft.gameObject.SetActive(false);
            if (_buttonRight != null)
                _buttonRight.gameObject.SetActive(false);

            ClearStatsDisplay();
        }

        private void ClearStatsDisplay()
        {
            if (_aprValueText != null) _aprValueText.text = "--";
            if (_totalLoanValueText != null) _totalLoanValueText.text = "--";
            if (_monthlyPaymentValueText != null) _monthlyPaymentValueText.text = "--";
            if (_creditScoreValueText != null) _creditScoreValueText.text = "--";
            if (_qualifyText != null) _qualifyText.text = "";
        }

    }
}
