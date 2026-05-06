using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Notifications;
using FortuneValley.Managers.Notifications;
using FortuneValley.Managers.Notifications.Dispatchers;
using FortuneValley.Tests.Fakes;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class InsuranceGuidanceDispatcherTests
    {
        private GameObject _dispatcherGo;
        private GameObject _controllerGo;
        private InsuranceGuidanceDispatcher _dispatcher;
        private GuidanceController _controller;
        private FakeGameEventBus _bus;
        private GuidanceTipSO _occurredTip;
        private GuidanceTipSO _resolvedTip;

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
            _dispatcher = _dispatcherGo.AddComponent<InsuranceGuidanceDispatcher>();

            _occurredTip = GuidanceTipFactory.Make(
                triggerKind: GuidanceTriggerKind.AccidentOccurred,
                severity: GuidanceSeverity.Alert,
                title: "{0} at {1}",
                message: "Damage: {2}",
                name: "tip-accident-occurred");
            _resolvedTip = GuidanceTipFactory.Make(
                triggerKind: GuidanceTriggerKind.AccidentResolved,
                severity: GuidanceSeverity.Info,
                title: "{0} resolved",
                message: "Total {2}, you paid {3} ({4})",
                name: "tip-accident-resolved");

            _dispatcher.Initialize(_controller, _occurredTip, _resolvedTip);
        }

        [TearDown]
        public void TearDown()
        {
            if (_dispatcherGo != null) Object.DestroyImmediate(_dispatcherGo);
            if (_controllerGo != null) Object.DestroyImmediate(_controllerGo);
            if (_occurredTip != null) Object.DestroyImmediate(_occurredTip);
            if (_resolvedTip != null) Object.DestroyImmediate(_resolvedTip);
        }

        [Test]
        public void HandleAccidentOccurred_EmitsBannerWithNameLotAndDamage()
        {
            var roll = new AccidentRollResult("lot_block02", "fire", "Fire", 1200f);
            _dispatcher.HandleAccidentOccurred(roll);

            Assert.AreEqual(1, _bus.CountOf<GuidanceBannerRequest>());
            var r = (GuidanceBannerRequest)_bus.RaisedEvents[0];
            Assert.AreEqual("Fire at lot_block02", r.Title);
            Assert.AreEqual("Damage: $1,200", r.Message);
            Assert.AreEqual("lot_block02", r.TargetData);
            Assert.AreEqual(GuidanceSeverity.Alert, r.Severity);
        }

        [Test]
        public void HandleAccidentResolved_Covered_UsesCoveredLabel()
        {
            _dispatcher.HandleAccidentResolved("lot_block02", "Fire",
                totalDamage: 1200f, wasCovered: true, playerCost: 200f);

            var r = (GuidanceBannerRequest)_bus.RaisedEvents[0];
            Assert.AreEqual("Fire resolved", r.Title);
            Assert.AreEqual("Total $1,200, you paid $200 (covered)", r.Message);
        }

        [Test]
        public void HandleAccidentResolved_Uncovered_UsesUncoveredLabel()
        {
            _dispatcher.HandleAccidentResolved("lot_block02", "Flood",
                totalDamage: 5000f, wasCovered: false, playerCost: 5000f);

            var r = (GuidanceBannerRequest)_bus.RaisedEvents[0];
            Assert.AreEqual("Flood resolved", r.Title);
            Assert.AreEqual("Total $5,000, you paid $5,000 (uncovered)", r.Message);
        }

        [Test]
        public void NullTips_DoNotThrow()
        {
            _dispatcher.Initialize(_controller, null, null);
            Assert.DoesNotThrow(() =>
                _dispatcher.HandleAccidentOccurred(new AccidentRollResult("lot", "id", "name", 0)));
            Assert.DoesNotThrow(() =>
                _dispatcher.HandleAccidentResolved("lot", "name", 0, false, 0));
            Assert.AreEqual(0, _bus.CountOf<GuidanceBannerRequest>());
        }
    }
}
