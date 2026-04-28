using UnityEngine;
using TMPro;
using FortuneValley.Core;

namespace FortuneValley.UI.HUD
{
    /// <summary>
    /// Displays combined outstanding debt (loan principal + credit card balance)
    /// in the Homebase HUD top bar. Updates on any loan event or CC balance change.
    /// </summary>
    public class CreditBalanceHUD : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private LoanSystem _loanSystem;
        [SerializeField] private CreditCardSystem _creditCardSystem;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _balanceText;

        private void OnEnable()
        {
            GameEvents.OnLoanBalanceChanged += HandleLoanBalanceChanged;
            GameEvents.OnCreditCardBalanceChanged += HandleCreditCardBalanceChanged;
            Refresh();
        }

        private void OnDisable()
        {
            GameEvents.OnLoanBalanceChanged -= HandleLoanBalanceChanged;
            GameEvents.OnCreditCardBalanceChanged -= HandleCreditCardBalanceChanged;
        }

        private void HandleLoanBalanceChanged(float total, float delta) => Refresh();
        private void HandleCreditCardBalanceChanged(float balance, float delta) => Refresh();

        private void Refresh()
        {
            if (_balanceText == null) return;

            float loanDebt = _loanSystem != null ? _loanSystem.TotalOutstandingPrincipal : 0f;
            float ccDebt = _creditCardSystem != null ? _creditCardSystem.CurrentBalance : 0f;
            _balanceText.text = FormatCurrency(loanDebt + ccDebt);
        }

        private string FormatCurrency(float amount)
        {
            if (Mathf.Abs(amount) >= 1000)
            {
                return $"${amount:N0}";
            }
            return $"${amount:F2}";
        }
    }
}
