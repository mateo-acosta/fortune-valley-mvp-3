using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Notifications;
using FortuneValley.Domain.Notifications.Contexts;
using FortuneValley.Managers.Notifications.Builders;

namespace FortuneValley.Managers.Notifications.Dispatchers
{
    /// <summary>
    /// Emits a heads-up banner when the rival begins targeting a lot
    /// (OnRivalTargetingLot). OnRivalPurchasedLot is intentionally NOT
    /// subscribed: the pre-existing RivalPurchaseOverlay handles that
    /// full-screen moment so this system does not double-notify.
    /// </summary>
    public class RivalGuidanceDispatcher : MonoBehaviour
    {
        [SerializeField] private GuidanceController _controller;
        [SerializeField] private GuidanceTipSO _rivalTargetingTip;

        private IBannerMessageBuilder<RivalTargetingLotContext> _builder;

        private void Awake() => _builder = new RivalTargetingLotMessageBuilder();

        private void OnEnable() => GameEvents.OnRivalTargetingLot += HandleRivalTargetingLot;
        private void OnDisable() => GameEvents.OnRivalTargetingLot -= HandleRivalTargetingLot;

        public void Initialize(
            GuidanceController controller,
            GuidanceTipSO rivalTargetingTip,
            IBannerMessageBuilder<RivalTargetingLotContext> builder = null)
        {
            _controller = controller;
            _rivalTargetingTip = rivalTargetingTip;
            _builder = builder ?? new RivalTargetingLotMessageBuilder();
        }

        public void HandleRivalTargetingLot(string lotId)
        {
            if (_controller == null || _rivalTargetingTip == null || _builder == null) return;

            var context = new RivalTargetingLotContext(lotId);
            var (title, message) = _builder.Build(
                _rivalTargetingTip.TitleTemplate, _rivalTargetingTip.MessageTemplate, context);

            var request = new GuidanceBannerRequest(
                title, message,
                _rivalTargetingTip.Severity,
                _rivalTargetingTip.TargetIntent,
                lotId,
                _rivalTargetingTip.name);

            _controller.Submit(_rivalTargetingTip, request);
        }
    }
}
