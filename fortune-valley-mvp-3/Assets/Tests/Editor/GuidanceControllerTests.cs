using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Notifications;
using FortuneValley.Managers.Notifications;
using FortuneValley.Tests.Fakes;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class GuidanceControllerTests
    {
        private GameObject _go;
        private GuidanceController _controller;
        private FakeGameEventBus _bus;
        private FakeNowProvider _clock;
        private RepeatPolicyFilter _filter;
        private GuidanceTipSO _tip;
        private BannerQueue _deferredQueue;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("GuidanceController");
            _controller = _go.AddComponent<GuidanceController>();
            _bus = new FakeGameEventBus();
            _clock = new FakeNowProvider();
            var store = new InMemoryKeyValueStore();
            var prefs = new PlayerPrefsDebouncedFlusher(store, _clock);
            _filter = new RepeatPolicyFilter(_clock, prefs);
            _deferredQueue = new BannerQueue();
            _controller.Initialize(_bus, _filter, _deferredQueue);

            _tip = GuidanceTipFactory.Make(
                triggerKind: GuidanceTriggerKind.LoanTaken,
                severity: GuidanceSeverity.Info,
                title: "t", message: "m",
                repeatPolicy: RepeatPolicy.EveryTime);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            if (_tip != null) Object.DestroyImmediate(_tip);
        }

        private static GuidanceBannerRequest MakeRequest(
            GuidanceSeverity severity = GuidanceSeverity.Info,
            string id = "r")
        {
            return new GuidanceBannerRequest("t", "m", severity, GuidanceTargetIntent.None, null, id);
        }

        // ===============================================================
        // HAPPY PATH
        // ===============================================================

        [Test]
        public void Submit_RaisesOnBus_WhenNoSuppression()
        {
            _controller.Submit(_tip, MakeRequest());
            Assert.AreEqual(1, _bus.CountOf<GuidanceBannerRequest>());
        }

        [Test]
        public void Submit_NullTip_DoesNothing()
        {
            _controller.Submit(null, MakeRequest());
            Assert.AreEqual(0, _bus.CountOf<GuidanceBannerRequest>());
        }

        // ===============================================================
        // TUTORIAL SUPPRESSION
        // ===============================================================

        [Test]
        public void Submit_WhileSuppressed_DropsWithoutMarkingFired()
        {
            var onceTip = GuidanceTipFactory.Make(repeatPolicy: RepeatPolicy.OncePerPlayer, name: "once");
            _controller.SetSuppressed(true);

            _controller.Submit(onceTip, MakeRequest());

            Assert.AreEqual(0, _bus.CountOf<GuidanceBannerRequest>());

            // Unsuppress and submit again; the tip should still be allowed
            // because it was never marked fired.
            _controller.SetSuppressed(false);
            _controller.Submit(onceTip, MakeRequest());
            Assert.AreEqual(1, _bus.CountOf<GuidanceBannerRequest>(),
                "Suppressed submits must not consume OncePerPlayer slots");

            Object.DestroyImmediate(onceTip);
        }

        [Test]
        public void SuppressionState_DefaultsFalse()
        {
            Assert.IsFalse(_controller.IsSuppressed);
        }

        [Test]
        public void SetSuppressed_TogglesFlag()
        {
            _controller.SetSuppressed(true);
            Assert.IsTrue(_controller.IsSuppressed);
            _controller.SetSuppressed(false);
            Assert.IsFalse(_controller.IsSuppressed);
        }

        // ===============================================================
        // REPEAT POLICY FILTERING
        // ===============================================================

        [Test]
        public void Submit_OncePerSession_DropsSecondAttempt()
        {
            var tip = GuidanceTipFactory.Make(repeatPolicy: RepeatPolicy.OncePerSession, name: "o-sess");
            _controller.Submit(tip, MakeRequest());
            _controller.Submit(tip, MakeRequest());
            Assert.AreEqual(1, _bus.CountOf<GuidanceBannerRequest>());
            Object.DestroyImmediate(tip);
        }

        [Test]
        public void Submit_OncePerCooldown_RespectsWindow()
        {
            var tip = GuidanceTipFactory.Make(
                repeatPolicy: RepeatPolicy.OncePerCooldown,
                cooldownSeconds: 60,
                name: "o-cd");

            _controller.Submit(tip, MakeRequest());
            Assert.AreEqual(1, _bus.CountOf<GuidanceBannerRequest>());

            _clock.AdvanceSeconds(30);
            _controller.Submit(tip, MakeRequest());
            Assert.AreEqual(1, _bus.CountOf<GuidanceBannerRequest>(),
                "Still inside cooldown window; request should be dropped");

            _clock.AdvanceSeconds(31);
            _controller.Submit(tip, MakeRequest());
            Assert.AreEqual(2, _bus.CountOf<GuidanceBannerRequest>(),
                "Past cooldown window; request should pass");

            Object.DestroyImmediate(tip);
        }

        // ===============================================================
        // MODAL POPUP DEFERRAL
        // ===============================================================

        [Test]
        public void Submit_WhileModalOpen_EnqueuesInsteadOfRaising()
        {
            _controller.HandleBlockingPanelOpenChanged(true);

            _controller.Submit(_tip, MakeRequest(id: "queued-1"));
            _controller.Submit(_tip, MakeRequest(id: "queued-2"));

            Assert.AreEqual(0, _bus.CountOf<GuidanceBannerRequest>());
            Assert.AreEqual(2, _controller.ModalDeferredCount);
        }

        [Test]
        public void ModalClose_DrainsQueueInFifoOrder()
        {
            _controller.HandleBlockingPanelOpenChanged(true);
            _controller.Submit(_tip, MakeRequest(id: "q1"));
            _controller.Submit(_tip, MakeRequest(id: "q2"));
            _controller.Submit(_tip, MakeRequest(id: "q3"));

            _controller.HandleBlockingPanelOpenChanged(false);

            Assert.AreEqual(3, _bus.CountOf<GuidanceBannerRequest>());
            Assert.AreEqual(0, _controller.ModalDeferredCount);

            var raised = _bus.RaisedEvents;
            Assert.AreEqual("q1", ((GuidanceBannerRequest)raised[0]).SourceTipId);
            Assert.AreEqual("q2", ((GuidanceBannerRequest)raised[1]).SourceTipId);
            Assert.AreEqual("q3", ((GuidanceBannerRequest)raised[2]).SourceTipId);
        }

        [Test]
        public void NestedModals_DrainOnlyOnFinalClose()
        {
            _controller.HandleBlockingPanelOpenChanged(true);
            _controller.HandleBlockingPanelOpenChanged(true); // nested
            _controller.Submit(_tip, MakeRequest(id: "inner"));

            _controller.HandleBlockingPanelOpenChanged(false);
            Assert.AreEqual(0, _bus.CountOf<GuidanceBannerRequest>(),
                "Outer modal still open; queue should not drain yet");
            Assert.AreEqual(1, _controller.ModalDeferredCount);

            _controller.HandleBlockingPanelOpenChanged(false);
            Assert.AreEqual(1, _bus.CountOf<GuidanceBannerRequest>());
        }

        [Test]
        public void ModalClose_BelowZero_DoesNotThrowOrDrain()
        {
            // Defensive: close fired without a matching open (e.g. mismatched
            // event stream during a hot reload).
            Assert.DoesNotThrow(() => _controller.HandleBlockingPanelOpenChanged(false));
            Assert.AreEqual(0, _controller.ModalOpenCount);
        }

        [Test]
        public void DeferredQueue_EvictsLowestSeverityWhenFull()
        {
            _controller.HandleBlockingPanelOpenChanged(true);

            for (int i = 0; i < BannerQueue.DefaultCapacity; i++)
            {
                _controller.Submit(_tip, MakeRequest(GuidanceSeverity.Info, $"info-{i}"));
            }
            var critical = MakeRequest(GuidanceSeverity.Critical, "crit");
            _controller.Submit(_tip, critical);

            _controller.HandleBlockingPanelOpenChanged(false);

            // Critical must have made it onto the bus; the oldest Info was evicted.
            bool sawCritical = false;
            foreach (var e in _bus.RaisedEvents)
            {
                if (e is GuidanceBannerRequest r && r.SourceTipId == "crit") sawCritical = true;
            }
            Assert.IsTrue(sawCritical);
            Assert.AreEqual(BannerQueue.DefaultCapacity, _bus.CountOf<GuidanceBannerRequest>());
        }

        [Test]
        public void Submit_MarksFired_BeforeQueuingForModal()
        {
            // Optimistic MarkFired: a OncePerSession tip consumed during a
            // modal window should not fire twice even if the same dispatcher
            // submits again before modal close.
            var tip = GuidanceTipFactory.Make(repeatPolicy: RepeatPolicy.OncePerSession, name: "o-sess-modal");

            _controller.HandleBlockingPanelOpenChanged(true);
            _controller.Submit(tip, MakeRequest(id: "first"));
            _controller.Submit(tip, MakeRequest(id: "second"));
            _controller.HandleBlockingPanelOpenChanged(false);

            Assert.AreEqual(1, _bus.CountOf<GuidanceBannerRequest>(),
                "OncePerSession should suppress the second submit at Submit time, not drain time");
            Assert.AreEqual("first", ((GuidanceBannerRequest)_bus.RaisedEvents[0]).SourceTipId);

            Object.DestroyImmediate(tip);
        }
    }
}
