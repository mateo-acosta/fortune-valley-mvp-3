using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Orchestrates the billing-cycle payment day sequence.
    /// Runs every billing cycle (default: day 30, 60, 90...).
    ///
    /// Sequence:
    ///   Step 1 - Process loan payments (deduct from checking)
    ///   Step 2 - Generate CC statement (close billing cycle, calc interest)
    ///   Step 3 - Show CC payment popup (pause time, wait for player)
    ///   Step 4 - Update credit score (after player pays)
    ///   Step 5 - Charge insurance premiums to credit card
    ///   Step 6 - Fire cycle-complete event, resume time
    ///
    /// LIFE GOALS REVISION: With the locked tuning of 1 in-game year =
    /// 1 billing cycle = 30 days, this controller fires once per year of
    /// the player's life (40 times across a full life). User-facing strings
    /// say "Annual Statement"; the class name is preserved as
    /// MonthlyPaymentDayController to keep scene wiring + tests stable
    /// (Issue 4 / 4A in the review). Treat the class as a billing-cycle
    /// orchestrator regardless of what the surface label says.
    ///
    /// LEARNING DESIGN: The cycle makes financial obligations concrete.
    /// Students must manage cash flow across loans, credit card bills, and
    /// insurance premiums -- the same decisions adults face.
    /// </summary>
    public class MonthlyPaymentDayController : MonoBehaviour
    {
        // ===============================================================
        // DEPENDENCIES
        // ===============================================================

        [Header("Financial Systems")]
        [SerializeField] private CreditCardSystem _creditCardSystem;
        [SerializeField] private LoanSystem _loanSystem;
        [SerializeField] private InsuranceSystem _insuranceSystem;
        [SerializeField] private RestaurantSystem _restaurantSystem;

        [Header("Time")]
        [SerializeField] private TimeManager _timeManager;

        [Header("Debug")]
        [SerializeField] private bool _logCycle;

        // ===============================================================
        // STATE MACHINE
        // ===============================================================

        private PaymentState _state = PaymentState.Idle;

        // Stored before pausing so time resumes at the same speed
        private int _prePaymentSpeedIndex;

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        private void OnEnable()
        {
            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnDayEnd += HandleDayEnd;
            GameEvents.OnCreditCardPaymentCompleted += HandleCCPaymentCompleted;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnDayEnd -= HandleDayEnd;
            GameEvents.OnCreditCardPaymentCompleted -= HandleCCPaymentCompleted;
        }

        private void Start()
        {
            if (_creditCardSystem == null)
                Debug.LogError("[MonthlyPaymentDayController] _creditCardSystem not wired in Inspector.");
            if (_loanSystem == null)
                Debug.LogError("[MonthlyPaymentDayController] _loanSystem not wired in Inspector.");
            if (_insuranceSystem == null)
                Debug.LogError("[MonthlyPaymentDayController] _insuranceSystem not wired in Inspector.");
            if (_restaurantSystem == null)
                Debug.LogError("[MonthlyPaymentDayController] _restaurantSystem not wired in Inspector.");
            if (_timeManager == null)
                Debug.LogError("[MonthlyPaymentDayController] _timeManager not wired in Inspector.");
        }

        // ===============================================================
        // GAME START
        // ===============================================================

        private void HandleGameStart()
        {
            // Reset state machine so a mid-game restart does not leave
            // the controller stuck in WaitingForCCPayment
            _state = PaymentState.Idle;
        }

        // ===============================================================
        // PAYMENT DAY TRIGGER
        // ===============================================================

        private void HandleDayEnd(int dayNumber)
        {
            if (_state != PaymentState.Idle) return;
            if (_creditCardSystem == null) return;

            // Only run on billing cycle days (day 30, 60, 90...)
            int billingCycleDays = _creditCardSystem.BillingCycleDays;
            if (billingCycleDays <= 0 || dayNumber % billingCycleDays != 0) return;

            RunPrePaymentSteps(dayNumber);
        }

        // ===============================================================
        // STEP 1-2: PRE-PAYMENT (synchronous)
        // ===============================================================

        private void RunPrePaymentSteps(int dayNumber)
        {
            if (_logCycle)
                Debug.Log($"[MonthlyPaymentDayController] Payment day {dayNumber} -- starting cycle.");

            GameEvents.RaiseMonthlyPaymentDayStarted(dayNumber);

            // Step 1: process loan payments (deduct from checking)
            if (_loanSystem != null)
                _loanSystem.ProcessMonthlyPayments();

            // Step 2: generate CC statement (close cycle, accrue interest)
            _creditCardSystem.GenerateStatement();

            // Zero-balance shortcut: if nothing is owed, skip popup
            if (_creditCardSystem.StatementBalance <= 0f)
            {
                RunPostPaymentSteps();
                return;
            }

            // Step 3: pause time and show CC payment popup
            PauseTime();
            _state = PaymentState.WaitingForCCPayment;

            // CreditCardStatementReady event signals the popup to display
            // (CreditCardSystem.GenerateStatement already fired this above)
        }

        // ===============================================================
        // CC PAYMENT RECEIVED (fired by CreditCardSystem after player pays)
        // ===============================================================

        private void HandleCCPaymentCompleted(float amountPaid)
        {
            if (_state != PaymentState.WaitingForCCPayment) return;

            _state = PaymentState.Idle;
            RunPostPaymentSteps();
        }

        // ===============================================================
        // STEP 4-6: POST-PAYMENT (runs after popup resolved)
        // ===============================================================

        private void RunPostPaymentSteps()
        {
            // Step 4: update credit score using DTI (debt-to-income ratio)
            if (_creditCardSystem != null && _restaurantSystem != null && _timeManager != null)
            {
                float totalDebt = DtiCalculator.ComputeTotalMonthlyDebt(
                    _loanSystem != null ? _loanSystem.TotalMonthlyDebt : 0f,
                    _creditCardSystem.MinimumPaymentDue);

                float monthlyIncome = DtiCalculator.ComputeMonthlyIncome(
                    _restaurantSystem.TotalIncomePerTick,
                    _timeManager.TicksPerDay,
                    _creditCardSystem.BillingCycleDays);

                float dti = DtiCalculator.Compute(totalDebt, monthlyIncome);
                _creditCardSystem.UpdateCreditScore(dti);

                if (_logCycle)
                    Debug.Log($"[MonthlyPaymentDayController] DTI: {dti:P1} (debt: ${totalDebt:F2}, income: ${monthlyIncome:F2})");
            }

            // Step 5: charge insurance premiums to credit card.
            // POC: insurance disabled means no premium charges; flag is the
            // controlling intent, null check is leftover defensive cruft.
            if (FeatureFlags.InsuranceEnabled && _insuranceSystem != null)
                _insuranceSystem.ChargePremiums();

            // Step 6: resume time and signal cycle complete
            ResumeTime();
            GameEvents.RaiseMonthlyPaymentCycleComplete();

            if (_logCycle)
                Debug.Log("[MonthlyPaymentDayController] Payment cycle complete.");
        }

        // ===============================================================
        // TIME CONTROL
        // ===============================================================

        private void PauseTime()
        {
            if (_timeManager == null) return;
            _prePaymentSpeedIndex = GetCurrentSpeedIndex();
            _timeManager.SetSpeedIndex(0);
        }

        private void ResumeTime()
        {
            if (_timeManager == null) return;
            _timeManager.SetSpeedIndex(_prePaymentSpeedIndex > 0 ? _prePaymentSpeedIndex : 1);
        }

        private int GetCurrentSpeedIndex()
        {
            // TimeManager does not expose speed index directly -- use a safe default
            // If already paused before payment day, resume to 1x after
            return _timeManager.IsPaused ? 0 : 1;
        }
    }
}
