using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Notifications;
using FortuneValley.Domain.Notifications.Contexts;
using FortuneValley.Managers.Notifications.Builders;

namespace FortuneValley.Managers.Notifications.Dispatchers
{
    /// <summary>
    /// Surfaces accident events. Premium charges flow through the
    /// MonthlyCycleAggregator instead of firing per-event banners at the
    /// end of every month.
    /// </summary>
    public class InsuranceGuidanceDispatcher : MonoBehaviour
    {
        [SerializeField] private GuidanceController _controller;
        [SerializeField] private GuidanceTipSO _accidentOccurredTip;
        [SerializeField] private GuidanceTipSO _accidentResolvedTip;

        private IBannerMessageBuilder<AccidentOccurredContext> _occurredBuilder;
        private IBannerMessageBuilder<AccidentResolvedContext> _resolvedBuilder;

        private void Awake()
        {
            _occurredBuilder = new AccidentOccurredMessageBuilder();
            _resolvedBuilder = new AccidentResolvedMessageBuilder();
        }

        private void OnEnable()
        {
            // POC: insurance disabled, so accident banners never surface.
            if (!FeatureFlags.InsuranceEnabled) return;

            GameEvents.OnAccidentOccurred += HandleAccidentOccurred;
            GameEvents.OnAccidentResolved += HandleAccidentResolved;
        }

        private void OnDisable()
        {
            if (!FeatureFlags.InsuranceEnabled) return;

            GameEvents.OnAccidentOccurred -= HandleAccidentOccurred;
            GameEvents.OnAccidentResolved -= HandleAccidentResolved;
        }

        public void Initialize(
            GuidanceController controller,
            GuidanceTipSO accidentOccurredTip,
            GuidanceTipSO accidentResolvedTip,
            IBannerMessageBuilder<AccidentOccurredContext> occurredBuilder = null,
            IBannerMessageBuilder<AccidentResolvedContext> resolvedBuilder = null)
        {
            _controller = controller;
            _accidentOccurredTip = accidentOccurredTip;
            _accidentResolvedTip = accidentResolvedTip;
            _occurredBuilder = occurredBuilder ?? new AccidentOccurredMessageBuilder();
            _resolvedBuilder = resolvedBuilder ?? new AccidentResolvedMessageBuilder();
        }

        public void HandleAccidentOccurred(AccidentRollResult roll)
        {
            if (_controller == null || _accidentOccurredTip == null || _occurredBuilder == null) return;

            var context = new AccidentOccurredContext(roll.LotId, roll.AccidentName, roll.DamageCost);
            var (title, message) = _occurredBuilder.Build(
                _accidentOccurredTip.TitleTemplate, _accidentOccurredTip.MessageTemplate, context);

            var request = new GuidanceBannerRequest(
                title, message,
                _accidentOccurredTip.Severity,
                _accidentOccurredTip.TargetIntent,
                roll.LotId,
                _accidentOccurredTip.name);

            _controller.Submit(_accidentOccurredTip, request);
        }

        public void HandleAccidentResolved(string lotId, string accidentName, float totalDamage, bool wasCovered, float playerCost)
        {
            if (_controller == null || _accidentResolvedTip == null || _resolvedBuilder == null) return;

            var context = new AccidentResolvedContext(lotId, accidentName, totalDamage, wasCovered, playerCost);
            var (title, message) = _resolvedBuilder.Build(
                _accidentResolvedTip.TitleTemplate, _accidentResolvedTip.MessageTemplate, context);

            var request = new GuidanceBannerRequest(
                title, message,
                _accidentResolvedTip.Severity,
                _accidentResolvedTip.TargetIntent,
                lotId,
                _accidentResolvedTip.name);

            _controller.Submit(_accidentResolvedTip, request);
        }
    }
}
