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
        private GameObject _go;
        private LoanGuidanceDispatcher _dispatcher;
        private FakeGameEventBus _bus;
        private GuidanceTipSO _tip;
        private LoanTakenMessageBuilder _builder;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Dispatcher");
            _dispatcher = _go.AddComponent<LoanGuidanceDispatcher>();
            _bus = new FakeGameEventBus();
            _tip = MakeTip(
                triggerKind: GuidanceTriggerKind.LoanTaken,
                severity: GuidanceSeverity.Info,
                targetIntent: GuidanceTargetIntent.LotsPanel,
                title: "Loan: {0}",
                message: "You borrowed {0} for {1}");
            _builder = new LoanTakenMessageBuilder();
            _dispatcher.Initialize(_bus, _tip, _builder);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            if (_tip != null) Object.DestroyImmediate(_tip);
        }

        [Test]
        public void HandleLoanOriginated_RaisesBannerRequestOnBus()
        {
            var loan = new ActiveLoan("L1", "lot_a", principal: 5000, apr: 0.05f,
                termMonths: 24, monthlyPayment: 250, downPayment: 500, startDay: 1);

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
            _dispatcher.Initialize(_bus, null, _builder);
            var loan = new ActiveLoan("L1", "lot_a", 5000, 0.05f, 24, 250, 0, 1);
            Assert.DoesNotThrow(() => _dispatcher.HandleLoanOriginated(loan));
            Assert.AreEqual(0, _bus.CountOf<GuidanceBannerRequest>());
        }

        [Test]
        public void HandleLoanOriginated_UsesTipSeverityAndIntent()
        {
            var warningTip = MakeTip(
                GuidanceTriggerKind.LoanTaken,
                GuidanceSeverity.Warning,
                GuidanceTargetIntent.LoanPanel,
                "t", "m");
            _dispatcher.Initialize(_bus, warningTip, _builder);

            var loan = new ActiveLoan("L2", "lot_b", 1000, 0.04f, 12, 85, 0, 1);
            _dispatcher.HandleLoanOriginated(loan);

            var raised = (GuidanceBannerRequest)_bus.RaisedEvents[0];
            Assert.AreEqual(GuidanceSeverity.Warning, raised.Severity);
            Assert.AreEqual(GuidanceTargetIntent.LoanPanel, raised.TargetIntent);

            Object.DestroyImmediate(warningTip);
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

        // ===============================================================
        // HELPERS
        // ===============================================================

        private static GuidanceTipSO MakeTip(
            GuidanceTriggerKind triggerKind,
            GuidanceSeverity severity,
            GuidanceTargetIntent targetIntent,
            string title,
            string message)
        {
            var tip = ScriptableObject.CreateInstance<GuidanceTipSO>();
            tip.name = "test-tip";
            SetPrivateField(tip, "_triggerKind", triggerKind);
            SetPrivateField(tip, "_severity", severity);
            SetPrivateField(tip, "_targetIntent", targetIntent);
            SetPrivateField(tip, "_titleTemplate", title);
            SetPrivateField(tip, "_messageTemplate", message);
            return tip;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(target, value);
        }
    }
}
