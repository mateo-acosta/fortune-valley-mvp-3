using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Notifications;
using FortuneValley.Managers.Notifications;
using FortuneValley.Managers.Notifications.Dispatchers;
using FortuneValley.Tests.Fakes;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class RivalGuidanceDispatcherTests
    {
        private GameObject _dispatcherGo;
        private GameObject _controllerGo;
        private RivalGuidanceDispatcher _dispatcher;
        private GuidanceController _controller;
        private FakeGameEventBus _bus;
        private GuidanceTipSO _tip;

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
            _dispatcher = _dispatcherGo.AddComponent<RivalGuidanceDispatcher>();
            _tip = GuidanceTipFactory.Make(
                triggerKind: GuidanceTriggerKind.RivalTargetingLot,
                severity: GuidanceSeverity.Warning,
                targetIntent: GuidanceTargetIntent.LotsPanel,
                title: "Rival eyeing {0}",
                message: "Your rival is targeting {0}.",
                name: "tip-rival-targeting");
            _dispatcher.Initialize(_controller, _tip);
        }

        [TearDown]
        public void TearDown()
        {
            if (_dispatcherGo != null) Object.DestroyImmediate(_dispatcherGo);
            if (_controllerGo != null) Object.DestroyImmediate(_controllerGo);
            if (_tip != null) Object.DestroyImmediate(_tip);
        }

        [Test]
        public void HandleRivalTargetingLot_EmitsBanner()
        {
            _dispatcher.HandleRivalTargetingLot("lot_block04");
            Assert.AreEqual(1, _bus.CountOf<GuidanceBannerRequest>());
            var r = (GuidanceBannerRequest)_bus.RaisedEvents[0];
            Assert.AreEqual("Rival eyeing lot_block04", r.Title);
            Assert.AreEqual("Your rival is targeting lot_block04.", r.Message);
            Assert.AreEqual(GuidanceSeverity.Warning, r.Severity);
            Assert.AreEqual("lot_block04", r.TargetData);
        }

        [Test]
        public void NullTip_DoesNotThrow()
        {
            _dispatcher.Initialize(_controller, null);
            Assert.DoesNotThrow(() => _dispatcher.HandleRivalTargetingLot("Lot_X"));
            Assert.AreEqual(0, _bus.CountOf<GuidanceBannerRequest>());
        }
    }
}
