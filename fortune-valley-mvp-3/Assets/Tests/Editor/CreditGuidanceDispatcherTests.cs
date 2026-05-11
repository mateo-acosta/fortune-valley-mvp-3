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
    public class CreditGuidanceDispatcherTests
    {
        private GameObject _dispatcherGo;
        private GameObject _controllerGo;
        private CreditGuidanceDispatcher _dispatcher;
        private GuidanceController _controller;
        private FakeGameEventBus _bus;
        private GuidanceTipSO _scoreTip;
        private GuidanceTipSO _statementTip;

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
            _dispatcher = _dispatcherGo.AddComponent<CreditGuidanceDispatcher>();
            _scoreTip = GuidanceTipFactory.Make(
                triggerKind: GuidanceTriggerKind.CreditScoreChanged,
                severity: GuidanceSeverity.Info,
                title: "Score: {0}",
                message: "Your credit score is now {0}.",
                name: "tip-score-changed");
            _statementTip = GuidanceTipFactory.Make(
                triggerKind: GuidanceTriggerKind.CreditCardStatementReady,
                severity: GuidanceSeverity.Warning,
                title: "Statement: {0}",
                message: "Balance {0}, minimum {1}, interest {2}.",
                name: "tip-statement");
            _dispatcher.Initialize(_controller, _scoreTip, _statementTip);
        }

        [TearDown]
        public void TearDown()
        {
            if (_dispatcherGo != null) Object.DestroyImmediate(_dispatcherGo);
            if (_controllerGo != null) Object.DestroyImmediate(_controllerGo);
            if (_scoreTip != null) Object.DestroyImmediate(_scoreTip);
            if (_statementTip != null) Object.DestroyImmediate(_statementTip);
        }

        [Test]
        public void HandleCreditScoreChanged_EmitsBannerWithScore()
        {
            _dispatcher.HandleCreditScoreChanged(720);
            Assert.AreEqual(1, _bus.CountOf<GuidanceBannerRequest>());
            var r = (GuidanceBannerRequest)_bus.RaisedEvents[0];
            Assert.AreEqual("Score: 720", r.Title);
            Assert.AreEqual("Your credit score is now 720.", r.Message);
        }

        [Test]
        public void HandleCreditCardStatementReady_FormatsAllAmounts()
        {
            _dispatcher.HandleCreditCardStatementReady(
                statementBalance: 1234f, minimumPayment: 50f, interestCharged: 12.5f);

            Assert.AreEqual(1, _bus.CountOf<GuidanceBannerRequest>());
            var r = (GuidanceBannerRequest)_bus.RaisedEvents[0];
            Assert.AreEqual("Statement: $1,234", r.Title);
            Assert.AreEqual("Balance $1,234, minimum $50, interest $13.", r.Message);
        }

        [Test]
        public void NullScoreTip_DoesNotEmitButStatementStillWorks()
        {
            _dispatcher.Initialize(_controller, null, _statementTip);
            _dispatcher.HandleCreditScoreChanged(500);
            Assert.AreEqual(0, _bus.CountOf<GuidanceBannerRequest>());

            _dispatcher.HandleCreditCardStatementReady(1000, 25, 10);
            Assert.AreEqual(1, _bus.CountOf<GuidanceBannerRequest>());
        }
    }
}
