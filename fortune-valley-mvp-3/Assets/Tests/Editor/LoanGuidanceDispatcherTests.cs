using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Notifications;
using FortuneValley.Domain.Notifications.Contexts;
using FortuneValley.Managers.Notifications;
using FortuneValley.Managers.Notifications.Builders;
using FortuneValley.Managers.Notifications.Dispatchers;
using FortuneValley.Tests.Fakes;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class LoanGuidanceDispatcherTests
    {
        private GameObject _dispatcherGo;
        private GameObject _controllerGo;
        private LoanGuidanceDispatcher _dispatcher;
        private GuidanceController _controller;
        private FakeGameEventBus _bus;
        private GuidanceTipSO _tip;
        private LoanTakenMessageBuilder _builder;

        [SetUp]
        public void SetUp()
        {
            _controllerGo = new GameObject("Controller");
            _controller = _controllerGo.AddComponent<GuidanceController>();
            _bus = new FakeGameEventBus();
            var now = new FakeNowProvider();
            var store = new InMemoryKeyValueStore();
            var prefs = new PlayerPrefsDebouncedFlusher(store, now);
            var filter = new RepeatPolicyFilter(now, prefs);
            _controller.Initialize(_bus, filter);

            _dispatcherGo = new GameObject("Dispatcher");
            _dispatcher = _dispatcherGo.AddComponent<LoanGuidanceDispatcher>();
            _tip = GuidanceTipFactory.Make(
                triggerKind: GuidanceTriggerKind.LoanTaken,
                severity: GuidanceSeverity.Info,
                targetIntent: GuidanceTargetIntent.LotsPanel,
                title: "Loan: {0}",
                message: "You borrowed {0} for {1}",
                repeatPolicy: RepeatPolicy.EveryTime);
            _builder = new LoanTakenMessageBuilder();
            _dispatcher.Initialize(_controller, _tip, _builder);
        }

        [TearDown]
        public void TearDown()
        {
            if (_dispatcherGo != null) Object.DestroyImmediate(_dispatcherGo);
            if (_controllerGo != null) Object.DestroyImmediate(_controllerGo);
            if (_tip != null) Object.DestroyImmediate(_tip);
        }

        [Test]
        public void HandleLoanOriginated_EmitsBannerThroughController()
        {
            var loan = new ActiveLoan("L1", "lot_a", 5000, 0.05f, 24, 250, 500, 1);

            _dispatcher.HandleLoanOriginated(loan);

            Assert.AreEqual(1, _bus.CountOf<GuidanceBannerRequest>());
            var raised = (GuidanceBannerRequest)_bus.RaisedEvents[0];
            Assert.AreEqual("Loan: $5,000", raised.Title);
            Assert.AreEqual("You borrowed $5,000 for lot_a", raised.Message);
            Assert.AreEqual(GuidanceSeverity.Info, raised.Severity);
            Assert.AreEqual(GuidanceTargetIntent.LotsPanel, raised.TargetIntent);
            Assert.AreEqual("lot_a", raised.TargetData);
        }

        [Test]
        public void HandleLoanOriginated_NullLoan_DoesNothing()
        {
            _dispatcher.HandleLoanOriginated(null);
            Assert.AreEqual(0, _bus.CountOf<GuidanceBannerRequest>());
        }

        [Test]
        public void HandleLoanOriginated_NoTipConfigured_DoesNotThrow()
        {
            _dispatcher.Initialize(_controller, null, _builder);
            var loan = new ActiveLoan("L1", "lot_a", 5000, 0.05f, 24, 250, 0, 1);
            Assert.DoesNotThrow(() => _dispatcher.HandleLoanOriginated(loan));
            Assert.AreEqual(0, _bus.CountOf<GuidanceBannerRequest>());
        }

        [Test]
        public void HandleLoanOriginated_RespectsControllerSuppression()
        {
            _controller.SetSuppressed(true);
            var loan = new ActiveLoan("L1", "lot_a", 5000, 0.05f, 24, 250, 0, 1);

            _dispatcher.HandleLoanOriginated(loan);

            Assert.AreEqual(0, _bus.CountOf<GuidanceBannerRequest>(),
                "Tutorial suppression should prevent the banner from reaching the bus");
        }

        [Test]
        public void HandleLoanOriginated_MultipleCalls_EmitMultipleBanners()
        {
            var loan1 = new ActiveLoan("L1", "lot_a", 1000, 0.05f, 12, 85, 0, 1);
            var loan2 = new ActiveLoan("L2", "lot_b", 2000, 0.05f, 12, 170, 0, 1);

            _dispatcher.HandleLoanOriginated(loan1);
            _dispatcher.HandleLoanOriginated(loan2);

            Assert.AreEqual(2, _bus.CountOf<GuidanceBannerRequest>());
        }
    }
}
