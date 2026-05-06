using System;

namespace FortuneValley.Core
{
    /// <summary>
    /// Watches solvency at each billing-cycle close. After five consecutive
    /// insolvent cycles, fires GameEvents.OnBankruptcyTriggered which the
    /// BankruptcyResetService consumes to orchestrate the soft reset.
    ///
    /// Insolvency formula (locked):
    ///   (Checking + Investing) &lt; (CC_debt + Outstanding_loan_principal)
    ///
    /// Each owning system supplies its current value via a Func at construction
    /// so this monitor stays trivially unit-testable.
    /// </summary>
    public class InsolvencyMonitor : IDisposable
    {
        public const int InsolvencyThreshold = 5;

        private readonly Func<float> _checkingFunc;
        private readonly Func<float> _investingFunc;
        private readonly Func<float> _creditCardDebtFunc;
        private readonly Func<float> _loanPrincipalFunc;
        private int _counter;
        private bool _disposed;

        public InsolvencyMonitor(
            Func<float> checking,
            Func<float> investing,
            Func<float> creditCardDebt,
            Func<float> loanPrincipal)
        {
            _checkingFunc = checking ?? throw new ArgumentNullException(nameof(checking));
            _investingFunc = investing ?? throw new ArgumentNullException(nameof(investing));
            _creditCardDebtFunc = creditCardDebt ?? throw new ArgumentNullException(nameof(creditCardDebt));
            _loanPrincipalFunc = loanPrincipal ?? throw new ArgumentNullException(nameof(loanPrincipal));

            GameEvents.OnMonthlyPaymentCycleComplete += HandleCycleComplete;
        }

        public int CurrentCounter => _counter;

        public void Dispose()
        {
            if (_disposed) return;
            GameEvents.OnMonthlyPaymentCycleComplete -= HandleCycleComplete;
            _disposed = true;
        }

        /// <summary>
        /// Reset the counter (called by BankruptcyResetService after a soft reset
        /// fires, and by GameManager on a fresh game start).
        /// </summary>
        public void ResetCounter()
        {
            _counter = 0;
        }

        /// <summary>
        /// Test seam: drive the cycle-close logic directly without going through
        /// GameEvents. Production fires via OnMonthlyPaymentCycleComplete.
        /// </summary>
        public void EvaluateCycle()
        {
            HandleCycleComplete();
        }

        private void HandleCycleComplete()
        {
            float liquid = _checkingFunc() + _investingFunc();
            float debt = _creditCardDebtFunc() + _loanPrincipalFunc();

            if (liquid < debt)
            {
                _counter++;
                if (_counter >= InsolvencyThreshold)
                {
                    // Reset before firing so subscribers see a clean counter
                    // when they read state during bankruptcy handling.
                    _counter = 0;
                    GameEvents.RaiseBankruptcyTriggered();
                }
            }
            else
            {
                _counter = 0;
            }
        }
    }
}
