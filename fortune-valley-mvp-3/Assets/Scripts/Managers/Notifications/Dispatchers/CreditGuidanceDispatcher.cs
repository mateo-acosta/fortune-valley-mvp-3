using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Notifications;
using FortuneValley.Domain.Notifications.Contexts;
using FortuneValley.Managers.Notifications.Builders;

namespace FortuneValley.Managers.Notifications.Dispatchers
{
    /// <summary>
    /// Surfaces credit-domain events as banners:
    /// - OnCreditScoreChanged   → "credit score changed" tip
    /// - OnCreditCardStatementReady → "statement ready" tip (with amounts)
    ///
    /// Direction-of-change tips (score-up vs score-down) can be layered
    /// later by splitting the tip asset and using different copy; the
    /// dispatcher does not currently track deltas.
    /// </summary>
    public class CreditGuidanceDispatcher : MonoBehaviour
    {
        [SerializeField] private GuidanceController _controller;
        [SerializeField] private GuidanceTipSO _creditScoreChangedTip;
        [SerializeField] private GuidanceTipSO _creditCardStatementTip;

        private IBannerMessageBuilder<CreditScoreChangedContext> _scoreBuilder;
        private IBannerMessageBuilder<CreditCardStatementContext> _statementBuilder;

        private void Awake()
        {
            _scoreBuilder = new CreditScoreChangedMessageBuilder();
            _statementBuilder = new CreditCardStatementMessageBuilder();
        }

        private void OnEnable()
        {
            GameEvents.OnCreditScoreChanged += HandleCreditScoreChanged;
            GameEvents.OnCreditCardStatementReady += HandleCreditCardStatementReady;
        }

        private void OnDisable()
        {
            GameEvents.OnCreditScoreChanged -= HandleCreditScoreChanged;
            GameEvents.OnCreditCardStatementReady -= HandleCreditCardStatementReady;
        }

        public void Initialize(
            GuidanceController controller,
            GuidanceTipSO creditScoreChangedTip,
            GuidanceTipSO creditCardStatementTip,
            IBannerMessageBuilder<CreditScoreChangedContext> scoreBuilder = null,
            IBannerMessageBuilder<CreditCardStatementContext> statementBuilder = null)
        {
            _controller = controller;
            _creditScoreChangedTip = creditScoreChangedTip;
            _creditCardStatementTip = creditCardStatementTip;
            _scoreBuilder = scoreBuilder ?? new CreditScoreChangedMessageBuilder();
            _statementBuilder = statementBuilder ?? new CreditCardStatementMessageBuilder();
        }

        public void HandleCreditScoreChanged(int newScore)
        {
            if (_controller == null || _creditScoreChangedTip == null || _scoreBuilder == null) return;

            var context = new CreditScoreChangedContext(newScore);
            var (title, message) = _scoreBuilder.Build(
                _creditScoreChangedTip.TitleTemplate, _creditScoreChangedTip.MessageTemplate, context);

            var request = new GuidanceBannerRequest(
                title, message,
                _creditScoreChangedTip.Severity,
                _creditScoreChangedTip.TargetIntent,
                targetData: null,
                sourceTipId: _creditScoreChangedTip.name);

            _controller.Submit(_creditScoreChangedTip, request);
        }

        public void HandleCreditCardStatementReady(float statementBalance, float minimumPayment, float interestCharged)
        {
            if (_controller == null || _creditCardStatementTip == null || _statementBuilder == null) return;

            var context = new CreditCardStatementContext(statementBalance, minimumPayment, interestCharged);
            var (title, message) = _statementBuilder.Build(
                _creditCardStatementTip.TitleTemplate, _creditCardStatementTip.MessageTemplate, context);

            var request = new GuidanceBannerRequest(
                title, message,
                _creditCardStatementTip.Severity,
                _creditCardStatementTip.TargetIntent,
                targetData: null,
                sourceTipId: _creditCardStatementTip.name);

            _controller.Submit(_creditCardStatementTip, request);
        }
    }
}
