using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.UI.Popups;

namespace FortuneValley.Tests
{
    /// <summary>
    /// EditMode tests for CreditCardStatementPopup.
    /// Verifies Configure(), button state logic, and enriched event wiring.
    /// </summary>
    [TestFixture]
    public class CreditCardStatementPopupTests
    {
        private GameObject _go;
        private CreditCardStatementPopupTestable _popup;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestPopup");
            _popup = _go.AddComponent<CreditCardStatementPopupTestable>();
            GameEvents.RaiseGameStart();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            GameEvents.ClearAllSubscriptions();
        }

        // ===============================================================
        // CONFIGURE
        // ===============================================================

        [Test]
        public void Configure_StoresStatementBalance()
        {
            _popup.Configure(500f, 25f, 10f, 1000f, 650);
            Assert.AreEqual(500f, _popup.StatementBalance);
        }

        [Test]
        public void Configure_StoresMinimumPayment()
        {
            _popup.Configure(500f, 25f, 10f, 1000f, 650);
            Assert.AreEqual(25f, _popup.MinimumPayment);
        }

        // ===============================================================
        // BUTTON STATES
        // ===============================================================

        [Test]
        public void ButtonStates_CanAffordFull_FullButtonEnabled()
        {
            // Checking = 600, balance = 500 -- can pay full
            _popup.Configure(500f, 25f, 0f, 600f, 650);
            Assert.IsTrue(_popup.CanPayFull);
        }

        [Test]
        public void ButtonStates_CannotAffordFull_FullButtonDisabled()
        {
            // Checking = 400, balance = 500 -- cannot pay full
            _popup.Configure(500f, 25f, 0f, 400f, 650);
            Assert.IsFalse(_popup.CanPayFull);
        }

        [Test]
        public void ButtonStates_CanAffordMinimumOnly_MinButtonEnabled_FullDisabled()
        {
            // Checking = 30, min = 25, full = 500
            _popup.Configure(500f, 25f, 0f, 30f, 650);
            Assert.IsTrue(_popup.CanPayMin);
            Assert.IsFalse(_popup.CanPayFull);
        }

        [Test]
        public void ButtonStates_CannotAffordMinimum_BothButtonsDisabled()
        {
            // Checking = 10, min = 25
            _popup.Configure(500f, 25f, 0f, 10f, 650);
            Assert.IsFalse(_popup.CanPayMin);
            Assert.IsFalse(_popup.CanPayFull);
        }

        // ===============================================================
        // ENRICHED EVENT WIRING
        // ===============================================================

        [Test]
        public void StatementReadyEvent_SetsStatementBalance()
        {
            // Simulate checking balance available via event
            GameEvents.RaiseCheckingBalanceChanged(800f, 0f);

            // Fire enriched event -- popup should self-configure
            GameEvents.RaiseCreditCardStatementReady(300f, 15f, 5f);

            Assert.AreEqual(300f, _popup.StatementBalance);
        }

        [Test]
        public void StatementReadyEvent_SetsMinimumPayment()
        {
            GameEvents.RaiseCheckingBalanceChanged(800f, 0f);
            GameEvents.RaiseCreditCardStatementReady(300f, 15f, 5f);

            Assert.AreEqual(15f, _popup.MinimumPayment);
        }

        // ===============================================================
        // PAYMENT INTENT EVENTS
        // ===============================================================

        [Test]
        public void PayFull_FiresPaymentRequestedWithStatementBalance()
        {
            _popup.Configure(500f, 25f, 0f, 600f, 650);

            float firedAmount = -1f;
            GameEvents.OnCreditCardPaymentRequested += amount => firedAmount = amount;

            _popup.SimulatePayFullClick();

            Assert.AreEqual(500f, firedAmount, 0.001f);
        }

        [Test]
        public void PayMinimum_FiresPaymentRequestedWithMinimum()
        {
            _popup.Configure(500f, 25f, 0f, 600f, 650);

            float firedAmount = -1f;
            GameEvents.OnCreditCardPaymentRequested += amount => firedAmount = amount;

            _popup.SimulatePayMinimumClick();

            Assert.AreEqual(25f, firedAmount, 0.001f);
        }
    }

    /// <summary>
    /// Testable subclass that exposes internal state and simulates button clicks.
    /// </summary>
    public class CreditCardStatementPopupTestable : CreditCardStatementPopup
    {
        public float StatementBalance => GetStatementBalance();
        public float MinimumPayment => GetMinimumPayment();
        public bool CanPayFull => GetCanPayFull();
        public bool CanPayMin => GetCanPayMin();

        public void SimulatePayFullClick() => SimulatePayFull();
        public void SimulatePayMinimumClick() => SimulatePayMin();

        private float GetStatementBalance()
        {
            var field = typeof(CreditCardStatementPopup).GetField("_statementBalance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (float)(field?.GetValue(this) ?? 0f);
        }

        private float GetMinimumPayment()
        {
            var field = typeof(CreditCardStatementPopup).GetField("_minimumPayment",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (float)(field?.GetValue(this) ?? 0f);
        }

        private bool GetCanPayFull()
        {
            var check = typeof(CreditCardStatementPopup).GetField("_checkingBalance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var stmt = typeof(CreditCardStatementPopup).GetField("_statementBalance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (check == null || stmt == null) return false;
            return (float)check.GetValue(this) >= (float)stmt.GetValue(this);
        }

        private bool GetCanPayMin()
        {
            var check = typeof(CreditCardStatementPopup).GetField("_checkingBalance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var min = typeof(CreditCardStatementPopup).GetField("_minimumPayment",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (check == null || min == null) return false;
            return (float)check.GetValue(this) >= (float)min.GetValue(this);
        }

        private void SimulatePayFull()
        {
            var method = typeof(CreditCardStatementPopup).GetMethod("OnPayFullClicked",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(this, null);
        }

        private void SimulatePayMin()
        {
            var method = typeof(CreditCardStatementPopup).GetMethod("OnPayMinimumClicked",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(this, null);
        }
    }
}
