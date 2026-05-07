using UnityEngine;
using TMPro;
using FortuneValley.Domain.Entities;

namespace FortuneValley.UI.Components
{
    /// <summary>
    /// A single row in the LoanPanel list.
    /// Shows loan details: lot name, remaining balance, monthly payment,
    /// and payment progress. Read-only display.
    ///
    /// LEARNING DESIGN: Seeing the remaining balance and payment count
    /// teaches students about amortization -- how debt slowly shrinks.
    /// </summary>
    public class LoanListItem : MonoBehaviour
    {
        [Header("Text Elements")]
        [SerializeField] private TextMeshProUGUI _lotNameText;
        [SerializeField] private TextMeshProUGUI _remainingBalanceText;
        [SerializeField] private TextMeshProUGUI _monthlyPaymentText;
        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private TextMeshProUGUI _aprText;

        // ===============================================================
        // SETUP
        // ===============================================================

        /// <summary>
        /// Configure the row with loan data.
        /// </summary>
        public void Setup(ActiveLoan loan, string lotDisplayName)
        {
            if (loan == null) return;

            if (_lotNameText != null)
                _lotNameText.text = lotDisplayName;

            if (_remainingBalanceText != null)
                _remainingBalanceText.text = $"Remaining: ${loan.RemainingBalance:N2}";

            if (_monthlyPaymentText != null)
                _monthlyPaymentText.text = $"${loan.YearlyPayment:N2}/mo";

            if (_progressText != null)
                _progressText.text = $"{loan.PaymentsMade} of {loan.TermMonths} payments";

            if (_aprText != null)
                _aprText.text = $"APR: {loan.APR * 100f:F1}%";
        }
    }
}
