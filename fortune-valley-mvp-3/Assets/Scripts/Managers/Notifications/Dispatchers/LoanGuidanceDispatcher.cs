using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Notifications;
using FortuneValley.Domain.Notifications.Contexts;
using FortuneValley.Managers.Notifications.Builders;

namespace FortuneValley.Managers.Notifications.Dispatchers
{
    /// <summary>
    /// Translates loan-related game events into guidance banner requests.
    /// Step 6 emits directly onto the IGameEventBus, where BannerStackUI is
    /// subscribed for visual end-to-end verification. Step 7 will insert a
    /// GuidanceController between this dispatcher and the bus so repeat
    /// policies, cooldowns, suppression, and modal-queue deferral become
    /// observable.
    /// </summary>
    public class LoanGuidanceDispatcher : MonoBehaviour
    {
        [SerializeField] private GameEventBusBehaviour _busBehaviour;
        [SerializeField] private GuidanceTipSO _loanTakenTip;

        private IGameEventBus _bus;
        private IBannerMessageBuilder<LoanTakenContext> _builder;

        private void Awake()
        {
            if (_busBehaviour != null) _bus = _busBehaviour.Bus;
            _builder = new LoanTakenMessageBuilder();
        }

        private void OnEnable()
        {
            GameEvents.OnLoanOriginated += HandleLoanOriginated;
        }

        private void OnDisable()
        {
            GameEvents.OnLoanOriginated -= HandleLoanOriginated;
        }

        /// <summary>
        /// Injection hook for EditMode tests. Allows substituting the bus,
        /// tip asset, and builder without routing through the scene lifecycle.
        /// </summary>
        public void Initialize(
            IGameEventBus bus,
            GuidanceTipSO loanTakenTip,
            IBannerMessageBuilder<LoanTakenContext> builder)
        {
            _bus = bus;
            _loanTakenTip = loanTakenTip;
            _builder = builder;
        }

        /// <summary>
        /// Public for test-only direct invocation. Production callers go
        /// through the GameEvents subscription above.
        /// </summary>
        public void HandleLoanOriginated(ActiveLoan loan)
        {
            if (loan == null || _bus == null || _loanTakenTip == null || _builder == null) return;

            var context = new LoanTakenContext(
                principal: loan.Principal,
                lotId: loan.LotId,
                termMonths: loan.TermMonths,
                monthlyPayment: loan.MonthlyPayment);

            var (title, message) = _builder.Build(_loanTakenTip.TitleTemplate, _loanTakenTip.MessageTemplate, context);

            var request = new GuidanceBannerRequest(
                title: title,
                message: message,
                severity: _loanTakenTip.Severity,
                targetIntent: _loanTakenTip.TargetIntent,
                targetData: loan.LotId,
                sourceTipId: _loanTakenTip.name);

            _bus.Raise(request);
        }
    }
}
