using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Notifications;
using FortuneValley.Domain.Notifications.Contexts;
using FortuneValley.Managers.Notifications.Builders;

namespace FortuneValley.Managers.Notifications.Dispatchers
{
    /// <summary>
    /// Collapses the month-end subsystem burst (loan payments, credit card
    /// statement, insurance premiums, tax liability) into a single summary
    /// banner so students see one "day 30: you paid $X total" moment
    /// instead of five banners racing past in under a second.
    ///
    /// Lifecycle:
    /// 1. OnMonthlyPaymentDayStarted → zero all accumulators, record day.
    /// 2. Subsystem events during the cycle (loan paid, CC paid, premium
    ///    charged, statement interest) sum into the accumulators.
    /// 3. OnMonthlyPaymentCycleComplete → build a MonthlyCycleSummaryContext
    ///    and submit a single banner.
    ///
    /// Tax totals come from the player-state tax_liability_ytd snapshot
    /// deltas; for v1 we rely on whatever event the tax subsystem raises
    /// (or leave taxes as zero if no event wires up).
    /// </summary>
    public class MonthlyCycleAggregator : MonoBehaviour
    {
        [SerializeField] private GuidanceController _controller;
        [SerializeField] private GuidanceTipSO _monthlySummaryTip;

        private IBannerMessageBuilder<MonthlyCycleSummaryContext> _builder;

        private int _currentDay;
        private float _loanPayments;
        private float _creditCardPayment;
        private float _insurancePremiums;
        private float _taxes;
        private bool _cycleActive;

        private void Awake() => _builder = new MonthlyCycleSummaryMessageBuilder();

        private void OnEnable()
        {
            GameEvents.OnMonthlyPaymentDayStarted += HandleMonthlyPaymentDayStarted;
            GameEvents.OnMonthlyPaymentCycleComplete += HandleMonthlyPaymentCycleComplete;
            GameEvents.OnLoanPaymentMade += HandleLoanPaymentMade;
            GameEvents.OnCreditCardPaymentCompleted += HandleCreditCardPaymentCompleted;
            GameEvents.OnInsurancePremiumCharged += HandleInsurancePremiumCharged;
        }

        private void OnDisable()
        {
            GameEvents.OnMonthlyPaymentDayStarted -= HandleMonthlyPaymentDayStarted;
            GameEvents.OnMonthlyPaymentCycleComplete -= HandleMonthlyPaymentCycleComplete;
            GameEvents.OnLoanPaymentMade -= HandleLoanPaymentMade;
            GameEvents.OnCreditCardPaymentCompleted -= HandleCreditCardPaymentCompleted;
            GameEvents.OnInsurancePremiumCharged -= HandleInsurancePremiumCharged;
        }

        public void Initialize(
            GuidanceController controller,
            GuidanceTipSO monthlySummaryTip,
            IBannerMessageBuilder<MonthlyCycleSummaryContext> builder = null)
        {
            _controller = controller;
            _monthlySummaryTip = monthlySummaryTip;
            _builder = builder ?? new MonthlyCycleSummaryMessageBuilder();
        }

        public bool IsCycleActive => _cycleActive;
        public int CurrentDay => _currentDay;

        public void HandleMonthlyPaymentDayStarted(int dayNumber)
        {
            _currentDay = dayNumber;
            _loanPayments = 0f;
            _creditCardPayment = 0f;
            _insurancePremiums = 0f;
            _taxes = 0f;
            _cycleActive = true;
        }

        public void HandleLoanPaymentMade(ActiveLoan loan, float amount)
        {
            if (!_cycleActive) return;
            _loanPayments += amount;
        }

        public void HandleCreditCardPaymentCompleted(float amount)
        {
            if (!_cycleActive) return;
            _creditCardPayment += amount;
        }

        public void HandleInsurancePremiumCharged(string lotId, string policyId, float amount)
        {
            if (!_cycleActive) return;
            _insurancePremiums += amount;
        }

        /// <summary>
        /// Tax hook for subsystems that know the monthly tax delta. The
        /// tax subsystem in Fortune Valley does not yet raise a dedicated
        /// per-month event, so test and integration paths call this
        /// directly.
        /// </summary>
        public void AddTaxDelta(float amount)
        {
            if (!_cycleActive) return;
            _taxes += amount;
        }

        public void HandleMonthlyPaymentCycleComplete()
        {
            if (!_cycleActive) return;
            _cycleActive = false;

            if (_controller == null || _monthlySummaryTip == null || _builder == null) return;

            var context = new MonthlyCycleSummaryContext(
                _currentDay, _loanPayments, _creditCardPayment, _insurancePremiums, _taxes);

            var (title, message) = _builder.Build(
                _monthlySummaryTip.TitleTemplate, _monthlySummaryTip.MessageTemplate, context);

            var request = new GuidanceBannerRequest(
                title, message,
                _monthlySummaryTip.Severity,
                _monthlySummaryTip.TargetIntent,
                targetData: null,
                sourceTipId: _monthlySummaryTip.name);

            _controller.Submit(_monthlySummaryTip, request);
        }
    }
}
