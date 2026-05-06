using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Notifications;
using FortuneValley.Domain.Notifications.Contexts;
using FortuneValley.Managers.Notifications;
using FortuneValley.Managers.Notifications.Builders;
using FortuneValley.Managers.Notifications.Dispatchers;
using FortuneValley.Tests.Fakes;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class LotGuidanceDispatcherTests
    {
        private GameObject _dispatcherGo;
        private GameObject _controllerGo;
        private LotGuidanceDispatcher _dispatcher;
        private GuidanceController _controller;
        private FakeGameEventBus _bus;
        private GuidanceTipSO _lotTip;
        private GuidanceTipSO _upgradeTip;

        [SetUp]
        public void SetUp()
        {
            _controllerGo = new GameObject("Controller");
            _controller = _controllerGo.AddComponent<GuidanceController>();
            _bus = new FakeGameEventBus();
            var now = new FakeNowProvider();
            var prefs = new PlayerPrefsDebouncedFlusher(new InMemoryKeyValueStore(), now);
            _controller.Initialize(_bus, new RepeatPolicyFilter(now, prefs));

            _dispatcherGo = new GameObject("Dispatcher");
            _dispatcher = _dispatcherGo.AddComponent<LotGuidanceDispatcher>();

            _lotTip = GuidanceTipFactory.Make(
                triggerKind: GuidanceTriggerKind.LotPurchased,
                severity: GuidanceSeverity.Positive,
                targetIntent: GuidanceTargetIntent.LotsPanel,
                title: "Lot {0} purchased",
                message: "You bought {0}!",
                name: "tip-lot-purchased");
            _upgradeTip = GuidanceTipFactory.Make(
                triggerKind: GuidanceTriggerKind.RestaurantUpgraded,
                severity: GuidanceSeverity.Positive,
                title: "Tier {0} reached",
                message: "Your restaurant is now {1}",
                name: "tip-restaurant-upgraded");

            _dispatcher.Initialize(_controller, _lotTip, _upgradeTip);
        }

        [TearDown]
        public void TearDown()
        {
            if (_dispatcherGo != null) Object.DestroyImmediate(_dispatcherGo);
            if (_controllerGo != null) Object.DestroyImmediate(_controllerGo);
            if (_lotTip != null) Object.DestroyImmediate(_lotTip);
            if (_upgradeTip != null) Object.DestroyImmediate(_upgradeTip);
        }

        [Test]
        public void HandleLotPurchased_Player_EmitsBanner()
        {
            _dispatcher.HandleLotPurchased("lot_block02", Owner.Player);

            Assert.AreEqual(1, _bus.CountOf<GuidanceBannerRequest>());
            var raised = (GuidanceBannerRequest)_bus.RaisedEvents[0];
            Assert.AreEqual("Lot lot_block02 purchased", raised.Title);
            Assert.AreEqual("You bought lot_block02!", raised.Message);
            Assert.AreEqual("lot_block02", raised.TargetData);
            Assert.AreEqual(GuidanceSeverity.Positive, raised.Severity);
        }

        [Test]
        public void HandleLotPurchased_Rival_DoesNotEmit()
        {
            _dispatcher.HandleLotPurchased("lot_block02", Owner.Rival);
            Assert.AreEqual(0, _bus.CountOf<GuidanceBannerRequest>(),
                "Rival purchases must not produce a banner (RivalPurchaseOverlay handles that)");
        }

        [Test]
        public void HandleLotPurchased_None_DoesNotEmit()
        {
            _dispatcher.HandleLotPurchased("lot_block02", Owner.None);
            Assert.AreEqual(0, _bus.CountOf<GuidanceBannerRequest>());
        }

        [Test]
        public void HandleRestaurantUpgraded_EmitsWithTierLabel()
        {
            _dispatcher.HandleRestaurantUpgraded(2);

            Assert.AreEqual(1, _bus.CountOf<GuidanceBannerRequest>());
            var raised = (GuidanceBannerRequest)_bus.RaisedEvents[0];
            Assert.AreEqual("Tier 2 reached", raised.Title);
            Assert.AreEqual("Your restaurant is now finished", raised.Message);
        }

        [Test]
        public void HandleRestaurantUpgraded_Tier3_UsesThrivingLabel()
        {
            _dispatcher.HandleRestaurantUpgraded(3);
            var raised = (GuidanceBannerRequest)_bus.RaisedEvents[0];
            Assert.AreEqual("Your restaurant is now thriving", raised.Message);
        }

        [Test]
        public void HandleRestaurantUpgraded_Tier1_UsesDilapidatedLabel()
        {
            _dispatcher.HandleRestaurantUpgraded(1);
            var raised = (GuidanceBannerRequest)_bus.RaisedEvents[0];
            Assert.AreEqual("Your restaurant is now dilapidated", raised.Message);
        }

        [Test]
        public void HandleRestaurantUpgraded_UnknownTier_UsesFallbackLabel()
        {
            _dispatcher.HandleRestaurantUpgraded(99);
            var raised = (GuidanceBannerRequest)_bus.RaisedEvents[0];
            Assert.AreEqual("Your restaurant is now unknown", raised.Message);
        }

        [Test]
        public void NullTips_DoesNotThrow()
        {
            _dispatcher.Initialize(_controller, null, null);
            Assert.DoesNotThrow(() => _dispatcher.HandleLotPurchased("lot", Owner.Player));
            Assert.DoesNotThrow(() => _dispatcher.HandleRestaurantUpgraded(2));
            Assert.AreEqual(0, _bus.CountOf<GuidanceBannerRequest>());
        }
    }
}
