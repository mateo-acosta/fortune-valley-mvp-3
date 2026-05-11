using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Notifications;
using FortuneValley.Domain.Notifications.Contexts;
using FortuneValley.Managers.Notifications.Builders;

namespace FortuneValley.Managers.Notifications.Dispatchers
{
    /// <summary>
    /// Translates lot-ownership and restaurant-upgrade events into guidance
    /// banner submissions. Rival lot purchases are intentionally NOT routed
    /// here; RivalPurchaseOverlay owns that full-screen notification.
    /// </summary>
    public class LotGuidanceDispatcher : MonoBehaviour
    {
        [SerializeField] private GuidanceController _controller;
        [SerializeField] private GuidanceTipSO _lotPurchasedTip;
        [SerializeField] private GuidanceTipSO _restaurantUpgradedTip;

        private IBannerMessageBuilder<LotPurchasedContext> _lotBuilder;
        private IBannerMessageBuilder<RestaurantUpgradedContext> _upgradeBuilder;

        private void Awake()
        {
            _lotBuilder = new LotPurchasedMessageBuilder();
            _upgradeBuilder = new RestaurantUpgradedMessageBuilder();
        }

        private void OnEnable()
        {
            GameEvents.OnLotPurchased += HandleLotPurchased;
            GameEvents.OnRestaurantUpgraded += HandleRestaurantUpgraded;
        }

        private void OnDisable()
        {
            GameEvents.OnLotPurchased -= HandleLotPurchased;
            GameEvents.OnRestaurantUpgraded -= HandleRestaurantUpgraded;
        }

        public void Initialize(
            GuidanceController controller,
            GuidanceTipSO lotPurchasedTip,
            GuidanceTipSO restaurantUpgradedTip,
            IBannerMessageBuilder<LotPurchasedContext> lotBuilder = null,
            IBannerMessageBuilder<RestaurantUpgradedContext> upgradeBuilder = null)
        {
            _controller = controller;
            _lotPurchasedTip = lotPurchasedTip;
            _restaurantUpgradedTip = restaurantUpgradedTip;
            _lotBuilder = lotBuilder ?? new LotPurchasedMessageBuilder();
            _upgradeBuilder = upgradeBuilder ?? new RestaurantUpgradedMessageBuilder();
        }

        public void HandleLotPurchased(string lotId, Owner newOwner)
        {
            if (newOwner != Owner.Player) return;
            if (_controller == null || _lotPurchasedTip == null || _lotBuilder == null) return;

            var context = new LotPurchasedContext(lotId);
            var (title, message) = _lotBuilder.Build(
                _lotPurchasedTip.TitleTemplate, _lotPurchasedTip.MessageTemplate, context);

            var request = new GuidanceBannerRequest(
                title, message,
                _lotPurchasedTip.Severity,
                _lotPurchasedTip.TargetIntent,
                lotId,
                _lotPurchasedTip.name);

            _controller.Submit(_lotPurchasedTip, request);
        }

        public void HandleRestaurantUpgraded(int newLevel)
        {
            if (_controller == null || _restaurantUpgradedTip == null || _upgradeBuilder == null) return;

            var context = new RestaurantUpgradedContext(newLevel);
            var (title, message) = _upgradeBuilder.Build(
                _restaurantUpgradedTip.TitleTemplate, _restaurantUpgradedTip.MessageTemplate, context);

            var request = new GuidanceBannerRequest(
                title, message,
                _restaurantUpgradedTip.Severity,
                _restaurantUpgradedTip.TargetIntent,
                targetData: null,
                sourceTipId: _restaurantUpgradedTip.name);

            _controller.Submit(_restaurantUpgradedTip, request);
        }
    }
}
