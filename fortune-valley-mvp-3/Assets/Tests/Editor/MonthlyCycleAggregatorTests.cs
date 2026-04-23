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
    public class MonthlyCycleAggregatorTests
    {
        private GameObject _aggGo;
        private GameObject _controllerGo;
        private MonthlyCycleAggregator _aggregator;
        private GuidanceController _controller;
        private FakeGameEventBus _bus;
        private GuidanceTipSO _summaryTip;

        [SetUp]
        public void SetUp()
        {
            _controllerGo = new GameObject("Controller");
            _controller = _controllerGo.AddComponent<GuidanceController>();
            _bus = new FakeGameEventBus();
            var now = new FakeNowProvider();
            var prefs = new PlayerPrefsDebouncedFlusher(new InMemoryKeyValueStore(), now);
            _controller.Initialize(_bus, new RepeatPolicyFilter(now, prefs));

            _aggGo = new GameObject("Aggregator");
            _aggregator = _aggGo.AddComponent<MonthlyCycleAggregator>();
            _summaryTip = GuidanceTipFactory.Make(
                triggerKind: GuidanceTriggerKind.MonthlyCycleSummary,
                severity: GuidanceSeverity.Info,
                title: "Day {0} Summary",
                message: "Paid {1} total (loans {2}, CC {3}, insurance {4}, tax {5})",
                name: "tip-monthly-summary");
            _aggregator.Initialize(_controller, _summaryTip);
        }

        [TearDown]
        public void TearDown()
        {
            if (_aggGo != null) Object.DestroyImmediate(_aggGo);
            if (_controllerGo != null) Object.DestroyImmediate(_controllerGo);
            if (_summaryTip != null) Object.DestroyImmediate(_summaryTip);
        }

        private static ActiveLoan MakeLoan() =>
            new ActiveLoan("L1", "lot_a", 5000, 0.05f, 24, 250, 0, 1);

        [Test]
        public void CompleteCycle_EmitsSingleSummaryBanner()
        {
            _aggregator.HandleMonthlyPaymentDayStarted(30);
            _aggregator.HandleLoanPaymentMade(MakeLoan(), 250f);
            _aggregator.HandleCreditCardPaymentCompleted(100f);
            _aggregator.HandleInsurancePremiumCharged("lot_a", "policy_1", 50f);
            _aggregator.AddTaxDelta(75f);
            _aggregator.HandleMonthlyPaymentCycleComplete();

            Assert.AreEqual(1, _bus.CountOf<GuidanceBannerRequest>(),
                "A whole month-end burst should collapse to exactly one summary banner");
            var r = (GuidanceBannerRequest)_bus.RaisedEvents[0];
            Assert.AreEqual("Day 30 Summary", r.Title);
            Assert.AreEqual("Paid $475 total (loans $250, CC $100, insurance $50, tax $75)", r.Message);
        }

        [Test]
        public void EventsBeforeDayStart_Ignored()
        {
            _aggregator.HandleLoanPaymentMade(MakeLoan(), 9999f);
            _aggregator.HandleCreditCardPaymentCompleted(9999f);
            _aggregator.HandleInsurancePremiumCharged("lot", "policy", 9999f);
            _aggregator.AddTaxDelta(9999f);

            _aggregator.HandleMonthlyPaymentDayStarted(30);
            _aggregator.HandleLoanPaymentMade(MakeLoan(), 100f);
            _aggregator.HandleMonthlyPaymentCycleComplete();

            var r = (GuidanceBannerRequest)_bus.RaisedEvents[0];
            Assert.AreEqual("Paid $100 total (loans $100, CC $0, insurance $0, tax $0)", r.Message,
                "Stray events before the cycle starts must not leak into the summary");
        }

        [Test]
        public void MultipleLoanPayments_Sum()
        {
            _aggregator.HandleMonthlyPaymentDayStarted(30);
            _aggregator.HandleLoanPaymentMade(MakeLoan(), 250f);
            _aggregator.HandleLoanPaymentMade(MakeLoan(), 175f);
            _aggregator.HandleLoanPaymentMade(MakeLoan(), 80f);
            _aggregator.HandleMonthlyPaymentCycleComplete();

            var r = (GuidanceBannerRequest)_bus.RaisedEvents[0];
            Assert.AreEqual("Paid $505 total (loans $505, CC $0, insurance $0, tax $0)", r.Message);
        }

        [Test]
        public void CompleteWithoutStart_DoesNotThrowOrEmit()
        {
            _aggregator.HandleMonthlyPaymentCycleComplete();
            Assert.AreEqual(0, _bus.CountOf<GuidanceBannerRequest>());
        }

        [Test]
        public void CycleCanBeRestarted()
        {
            _aggregator.HandleMonthlyPaymentDayStarted(30);
            _aggregator.HandleLoanPaymentMade(MakeLoan(), 100f);
            _aggregator.HandleMonthlyPaymentCycleComplete();

            _aggregator.HandleMonthlyPaymentDayStarted(60);
            _aggregator.HandleLoanPaymentMade(MakeLoan(), 200f);
            _aggregator.HandleMonthlyPaymentCycleComplete();

            Assert.AreEqual(2, _bus.CountOf<GuidanceBannerRequest>());
            Assert.AreEqual("Day 30 Summary", ((GuidanceBannerRequest)_bus.RaisedEvents[0]).Title);
            Assert.AreEqual("Day 60 Summary", ((GuidanceBannerRequest)_bus.RaisedEvents[1]).Title);
            Assert.AreEqual("Paid $200 total (loans $200, CC $0, insurance $0, tax $0)",
                ((GuidanceBannerRequest)_bus.RaisedEvents[1]).Message,
                "Second cycle must not carry accumulators from the first");
        }

        [Test]
        public void NullTip_DoesNotEmit()
        {
            _aggregator.Initialize(_controller, null);
            _aggregator.HandleMonthlyPaymentDayStarted(30);
            _aggregator.HandleLoanPaymentMade(MakeLoan(), 100f);
            _aggregator.HandleMonthlyPaymentCycleComplete();
            Assert.AreEqual(0, _bus.CountOf<GuidanceBannerRequest>());
        }

        [Test]
        public void IsCycleActive_TracksLifecycle()
        {
            Assert.IsFalse(_aggregator.IsCycleActive);
            _aggregator.HandleMonthlyPaymentDayStarted(30);
            Assert.IsTrue(_aggregator.IsCycleActive);
            _aggregator.HandleMonthlyPaymentCycleComplete();
            Assert.IsFalse(_aggregator.IsCycleActive);
        }
    }
}
