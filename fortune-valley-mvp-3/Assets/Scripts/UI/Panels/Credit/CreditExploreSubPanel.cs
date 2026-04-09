using System.Collections.Generic;
using UnityEngine;
using TMPro;
using FortuneValley.Core;

namespace FortuneValley.UI.Panels.Credit
{
    /// <summary>
    /// Credit Explore tab: browse available loan products.
    /// Shows eligibility based on credit score and DTI ratio.
    /// Ineligible loans are greyed out with a reason.
    ///
    /// LEARNING DESIGN: Students see that better credit scores
    /// unlock better loan terms, connecting financial behavior to outcomes.
    /// </summary>
    public class CreditExploreSubPanel : SubPanelBase
    {
        // ===============================================================
        // REFERENCES
        // ===============================================================

        [Header("Dependencies")]
        [SerializeField] private LoanSystem _loanSystem;
        [SerializeField] private CreditCardSystem _creditCardSystem;
        [SerializeField] private CurrencyManager _currencyManager;

        [Header("Card List")]
        [SerializeField] private Transform _cardContainer;
        [SerializeField] private GameObject _loanProductCardPrefab;

        // ===============================================================
        // STATE
        // ===============================================================

        private List<GameObject> _cardInstances = new List<GameObject>();

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        protected override void OnEnable()
        {
            GameEvents.OnCreditScoreChanged += HandleCreditScoreChanged;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            GameEvents.OnCreditScoreChanged -= HandleCreditScoreChanged;

            base.OnDisable();
        }

        // ===============================================================
        // EVENT HANDLERS
        // ===============================================================

        private void HandleCreditScoreChanged(int newScore) => Refresh();

        // ===============================================================
        // REFRESH
        // ===============================================================

        protected override void Refresh()
        {
            ClearCards();

            if (_loanSystem == null || _creditCardSystem == null) return;
            if (_loanProductCardPrefab == null || _cardContainer == null) return;

            // Property reads only -- no cross-layer method calls
            var configs = _loanSystem.AvailableLoans;
            int creditScore = _creditCardSystem.CreditScore;

            // Calculate DTI: monthly debt / monthly income
            // Use loan system's total monthly debt as numerator
            float monthlyDebt = _loanSystem.TotalMonthlyDebt;
            float monthlyIncome = GetEstimatedMonthlyIncome();
            float dtiRatio = monthlyIncome > 0f ? monthlyDebt / monthlyIncome : 0f;

            var results = LoanEligibilityFilter.Evaluate(configs, creditScore, dtiRatio);

            for (int i = 0; i < results.Count; i++)
            {
                SpawnLoanCard(results[i]);
            }
        }

        private void SpawnLoanCard(LoanEligibilityResult result)
        {
            var card = Instantiate(_loanProductCardPrefab, _cardContainer);
            _cardInstances.Add(card);

            // Populate card text fields
            var config = result.Config;
            var texts = card.GetComponentsInChildren<TextMeshProUGUI>(true);

            // Set card content based on available text fields
            // Card layout expected: Title, APR, Term, Down Payment, Status
            if (texts.Length > 0) texts[0].text = config.DisplayName;
            if (texts.Length > 1) texts[1].text = $"APR: {config.APR * 100f:F1}%";
            if (texts.Length > 2) texts[2].text = $"Term: {config.TermMonths} months";
            if (texts.Length > 3) texts[3].text = $"Down: {config.DownPaymentPercent:P0}";

            if (!result.IsEligible)
            {
                // Grey out ineligible cards
                var canvasGroup = card.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = card.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 0.5f;
                canvasGroup.interactable = false;

                // Show reason on the last text field
                if (texts.Length > 4) texts[4].text = result.Reason;
            }
            else
            {
                if (texts.Length > 4) texts[4].text = "Eligible";
            }
        }

        private void ClearCards()
        {
            for (int i = 0; i < _cardInstances.Count; i++)
            {
                if (_cardInstances[i] != null)
                    Destroy(_cardInstances[i]);
            }
            _cardInstances.Clear();
        }

        /// <summary>
        /// Rough monthly income estimate for DTI calculation.
        /// Uses checking balance as a proxy in this POC.
        /// </summary>
        private float GetEstimatedMonthlyIncome()
        {
            // For POC: use a baseline monthly income estimate
            // In a full game this would come from RestaurantSystem's per-tick income
            if (_currencyManager == null) return 1f;
            return _currencyManager.CheckingBalance > 0f ? _currencyManager.CheckingBalance * 0.1f : 1f;
        }
    }
}
