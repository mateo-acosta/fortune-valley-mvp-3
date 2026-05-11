using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Notifications;
using FortuneValley.Managers.Notifications;
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
        private GuidanceTipSO _takenTip;
        private GuidanceTipSO _heldTip;

        private const int HeldThresholdTicks = 5;

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
            _dispatcher = _dispatcherGo.AddComponent<LoanGuidanceDispatcher>();
            _takenTip = GuidanceTipFactory.Make(
                triggerKind: GuidanceTriggerKind.LoanTaken,
                severity: GuidanceSeverity.Info,
                targetIntent: GuidanceTargetIntent.LotsPanel,
                title: "Loan: {0}",
                message: "You borrowed {0} for {1}",
                name: "tip-loan-taken");
            _heldTip = GuidanceTipFactory.Make(
                triggerKind: GuidanceTriggerKind.LoanHeldWithoutLotPurchase,
                severity: GuidanceSeverity.Warning,
                targetIntent: GuidanceTargetIntent.LotsPanel,
                title: "Loan not used yet",
                message: "You took {0} for {1} {2} ticks ago but haven't bought the lot.",
                name: "tip-loan-held");
            _dispatcher.Initialize(_controller, _takenTip, _heldTip, heldThresholdTicks: HeldThresholdTicks);
        }

        [TearDown]
        public void TearDown()
        {
            if (_dispatcherGo != null) Object.DestroyImmediate(_dispatcherGo);
            if (_controllerGo != null) Object.DestroyImmediate(_controllerGo);
            if (_takenTip != null) Object.DestroyImmediate(_takenTip);
            if (_heldTip != null) Object.DestroyImmediate(_heldTip);
        }

        private static ActiveLoan MakeLoan(string id = "L1", string lotId = "lot_a", float principal = 5000f) =>
            new ActiveLoan(id, lotId, principal, 0.05f, 24, 250, 500, 1);

        // ===============================================================
        // LOAN TAKEN - core banner
        // ===============================================================

        [Test]
        public void HandleLoanOriginated_EmitsBannerThroughController()
        {
            _dispatcher.HandleLoanOriginated(MakeLoan());
            Assert.AreEqual(1, _bus.CountOf<GuidanceBannerRequest>());
            var r = (GuidanceBannerRequest)_bus.RaisedEvents[0];
            Assert.AreEqual("Loan: $5,000", r.Title);
            Assert.AreEqual("You borrowed $5,000 for lot_a", r.Message);
            Assert.AreEqual("lot_a", r.TargetData);
        }

        [Test]
        public void HandleLoanOriginated_NullLoan_DoesNothing()
        {
            _dispatcher.HandleLoanOriginated(null);
            Assert.AreEqual(0, _bus.CountOf<GuidanceBannerRequest>());
            Assert.AreEqual(0, _dispatcher.PendingLoanCount);
        }

        [Test]
        public void HandleLoanOriginated_RespectsControllerSuppression()
        {
            _controller.SetSuppressed(true);
            _dispatcher.HandleLoanOriginated(MakeLoan());
            Assert.AreEqual(0, _bus.CountOf<GuidanceBannerRequest>());
        }

        // ===============================================================
        // PENDING LOAN TRACKING
        // ===============================================================

        [Test]
        public void HandleLoanOriginated_AddsToPending()
        {
            _dispatcher.HandleLoanOriginated(MakeLoan("L1", "lot_a"));
            Assert.AreEqual(1, _dispatcher.PendingLoanCount);

            _dispatcher.HandleLoanOriginated(MakeLoan("L2", "lot_b"));
            Assert.AreEqual(2, _dispatcher.PendingLoanCount);
        }

        [Test]
        public void HandleLotPurchased_Player_ClearsMatchingPending()
        {
            _dispatcher.HandleLoanOriginated(MakeLoan("L1", "lot_a"));
            _dispatcher.HandleLoanOriginated(MakeLoan("L2", "lot_b"));
            _bus.RaisedEvents.Clear();

            _dispatcher.HandleLotPurchased("lot_a", Owner.Player);

            Assert.AreEqual(1, _dispatcher.PendingLoanCount,
                "Only the pending loan for lot_a should be removed");
        }

        [Test]
        public void HandleLotPurchased_Rival_DoesNotClear()
        {
            _dispatcher.HandleLoanOriginated(MakeLoan("L1", "lot_a"));
            _dispatcher.HandleLotPurchased("lot_a", Owner.Rival);
            Assert.AreEqual(1, _dispatcher.PendingLoanCount);
        }

        [Test]
        public void HandleLotPurchased_UnrelatedLot_DoesNotClear()
        {
            _dispatcher.HandleLoanOriginated(MakeLoan("L1", "lot_a"));
            _dispatcher.HandleLotPurchased("lot_z", Owner.Player);
            Assert.AreEqual(1, _dispatcher.PendingLoanCount);
        }

        [Test]
        public void HandleLotPurchased_MultipleLoansForSameLot_AllCleared()
        {
            _dispatcher.HandleLoanOriginated(MakeLoan("L1", "lot_a"));
            _dispatcher.HandleLoanOriginated(MakeLoan("L2", "lot_a"));
            Assert.AreEqual(2, _dispatcher.PendingLoanCount);

            _dispatcher.HandleLotPurchased("lot_a", Owner.Player);
            Assert.AreEqual(0, _dispatcher.PendingLoanCount);
        }

        // ===============================================================
        // TICK AGEING - held-without-lot banner
        // ===============================================================

        [Test]
        public void HandleTick_BelowThreshold_DoesNotFireHeld()
        {
            _dispatcher.HandleLoanOriginated(MakeLoan());
            _bus.RaisedEvents.Clear();

            for (int t = 1; t < HeldThresholdTicks; t++)
            {
                _dispatcher.HandleTick(t);
            }

            Assert.AreEqual(0, _bus.CountOf<GuidanceBannerRequest>());
            Assert.AreEqual(1, _dispatcher.PendingLoanCount);
        }

        [Test]
        public void HandleTick_AtThreshold_FiresHeldBanner()
        {
            _dispatcher.HandleLoanOriginated(MakeLoan("L1", "lot_a", principal: 5000f));
            _bus.RaisedEvents.Clear();

            _dispatcher.HandleTick(HeldThresholdTicks);

            Assert.AreEqual(1, _bus.CountOf<GuidanceBannerRequest>());
            var r = (GuidanceBannerRequest)_bus.RaisedEvents[0];
            Assert.AreEqual("Loan not used yet", r.Title);
            Assert.AreEqual($"You took $5,000 for lot_a {HeldThresholdTicks} ticks ago but haven't bought the lot.", r.Message);
            Assert.AreEqual("lot_a", r.TargetData);
            Assert.AreEqual(GuidanceSeverity.Warning, r.Severity);
        }

        [Test]
        public void HandleTick_AfterFiring_PendingIsCleared()
        {
            _dispatcher.HandleLoanOriginated(MakeLoan());
            _dispatcher.HandleTick(HeldThresholdTicks);
            Assert.AreEqual(0, _dispatcher.PendingLoanCount);

            // Further ticks should not re-fire.
            _bus.RaisedEvents.Clear();
            _dispatcher.HandleTick(HeldThresholdTicks + 5);
            Assert.AreEqual(0, _bus.CountOf<GuidanceBannerRequest>());
        }

        [Test]
        public void HandleTick_MultipleLoans_EachAgesIndependently()
        {
            _dispatcher.HandleTick(0);            // set _lastSeenTick
            _dispatcher.HandleLoanOriginated(MakeLoan("L1", "lot_a"));

            _dispatcher.HandleTick(3);
            _dispatcher.HandleLoanOriginated(MakeLoan("L2", "lot_b"));

            _bus.RaisedEvents.Clear();
            // At tick 5, L1 (started at 0) has age 5 → fires.
            // L2 (started at 3) has age 2 → does not fire.
            _dispatcher.HandleTick(5);

            Assert.AreEqual(1, _bus.CountOf<GuidanceBannerRequest>());
            Assert.AreEqual(1, _dispatcher.PendingLoanCount);

            // At tick 8, L2 now has age 5 → fires.
            _bus.RaisedEvents.Clear();
            _dispatcher.HandleTick(8);

            Assert.AreEqual(1, _bus.CountOf<GuidanceBannerRequest>());
            Assert.AreEqual(0, _dispatcher.PendingLoanCount);
        }

        [Test]
        public void LotPurchased_BeforeThreshold_PreventsHeldBanner()
        {
            _dispatcher.HandleLoanOriginated(MakeLoan("L1", "lot_a"));
            _dispatcher.HandleTick(HeldThresholdTicks - 1); // one tick shy
            _dispatcher.HandleLotPurchased("lot_a", Owner.Player);

            _bus.RaisedEvents.Clear();
            _dispatcher.HandleTick(HeldThresholdTicks + 10);
            Assert.AreEqual(0, _bus.CountOf<GuidanceBannerRequest>());
        }

        [Test]
        public void HandleTick_NullHeldTip_DoesNotThrowOrClearPending()
        {
            _dispatcher.Initialize(_controller, _takenTip, loanHeldWithoutLotTip: null, heldThresholdTicks: HeldThresholdTicks);
            _dispatcher.HandleLoanOriginated(MakeLoan());

            // Origination emitted one banner (the "loan taken" tip); the tick
            // should not produce any additional banner when the held tip is null.
            int beforeTick = _bus.CountOf<GuidanceBannerRequest>();
            Assert.DoesNotThrow(() => _dispatcher.HandleTick(HeldThresholdTicks));
            Assert.AreEqual(beforeTick, _bus.CountOf<GuidanceBannerRequest>(),
                "Missing tip should suppress the banner without crashing");
            // Pending loan stays — skipped this cycle, may fire later once configured.
            Assert.AreEqual(1, _dispatcher.PendingLoanCount);
        }
    }
}
