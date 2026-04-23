using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Notifications;
using FortuneValley.Domain.Notifications.Contexts;
using FortuneValley.Managers.Notifications.Builders;

namespace FortuneValley.Managers.Notifications.Dispatchers
{
    /// <summary>
    /// Translates loan-related game events into guidance banner requests
    /// and hands them to <see cref="GuidanceController"/> for filtering,
    /// modal deferral, and suppression handling. Never emits banners
    /// directly onto the bus; the controller owns that contract.
    /// </summary>
    public class LoanGuidanceDispatcher : MonoBehaviour
    {
        [SerializeField] private GuidanceController _controller;
        [SerializeField] private GuidanceTipSO _loanTakenTip;

        private IBannerMessageBuilder<LoanTakenContext> _builder;

        private void Awake()
        {
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
        /// Injection hook for EditMode tests.
        /// </summary>
        public void Initialize(
            GuidanceController controller,
            GuidanceTipSO loanTakenTip,
            IBannerMessageBuilder<LoanTakenContext> builder)
        {
            _controller = controller;
            _loanTakenTip = loanTakenTip;
            _builder = builder;
        }

        /// <summary>
        /// Public for test-only direct invocation. Production callers come
        /// through the GameEvents subscription above.
        /// </summary>
        public void HandleLoanOriginated(ActiveLoan loan)
        {
            if (loan == null || _controller == null || _loanTakenTip == null || _builder == null) return;

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

            _controller.Submit(_loanTakenTip, request);
        }
    }
}
