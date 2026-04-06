using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;
using FortuneValley.UI.HUD;

namespace FortuneValley.Tests
{
    /// <summary>
    /// EditMode tests for AccountDisplay.
    /// Verifies each AccountType subscribes to the correct GameEvent.
    /// </summary>
    [TestFixture]
    public class AccountDisplayTests
    {
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            GameEvents.RaiseGameStart();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
                Object.DestroyImmediate(_go);
            GameEvents.ClearAllSubscriptions();
        }

        // ===============================================================
        // SUBSCRIPTION ROUTING
        // ===============================================================

        [Test]
        public void CheckingDisplay_SubscribesToOnCheckingBalanceChanged()
        {
            var display = CreateDisplay(AccountType.Checking);

            GameEvents.RaiseCheckingBalanceChanged(250f, 50f);

            Assert.AreEqual(250f, display.CurrentBalance, 0.001f);
        }

        [Test]
        public void InvestingDisplay_SubscribesToOnInvestingBalanceChanged()
        {
            var display = CreateDisplay(AccountType.Investing);

            GameEvents.RaiseInvestingBalanceChanged(1200f, 200f);

            Assert.AreEqual(1200f, display.CurrentBalance, 0.001f);
        }

        [Test]
        public void CreditCardDisplay_SubscribesToOnCreditCardBalanceChanged()
        {
            var display = CreateDisplay(AccountType.CreditCard);

            GameEvents.RaiseCreditCardBalanceChanged(450f, 100f);

            Assert.AreEqual(450f, display.CurrentBalance, 0.001f);
        }

        [Test]
        public void CheckingDisplay_DoesNotRespondToInvestingEvent()
        {
            var display = CreateDisplay(AccountType.Checking);

            GameEvents.RaiseInvestingBalanceChanged(999f, 999f);

            Assert.AreEqual(0f, display.CurrentBalance, 0.001f);
        }

        [Test]
        public void InvestingDisplay_DoesNotRespondToCheckingEvent()
        {
            var display = CreateDisplay(AccountType.Investing);

            GameEvents.RaiseCheckingBalanceChanged(999f, 999f);

            Assert.AreEqual(0f, display.CurrentBalance, 0.001f);
        }

        // ===============================================================
        // HELPERS
        // ===============================================================

        private AccountDisplay CreateDisplay(AccountType type)
        {
            _go = new GameObject("TestDisplay");
            var display = _go.AddComponent<AccountDisplay>();

            // Set _accountType via reflection (SerializeField)
            var field = typeof(AccountDisplay).GetField("_accountType",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(display, type);

            // Force OnEnable to re-run with the correct account type
            display.enabled = false;
            display.enabled = true;

            return display;
        }
    }
}
