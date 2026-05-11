using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Runtime tests for CurrencyManager.
    /// Tests require MonoBehaviour lifecycle.
    /// </summary>
    [TestFixture]
    public class CurrencyManagerTests
    {
        private GameObject _testObject;
        private CurrencyManager _currencyManager;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();
            _testObject = new GameObject("TestCurrencyManager");
            _currencyManager = _testObject.AddComponent<CurrencyManager>();

            // Set starting balance via reflection
            var field = typeof(CurrencyManager).GetField("_startingCheckingBalance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(_currencyManager, 1000f);

            _currencyManager.ResetBalance();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_testObject);
            GameEvents.ClearAllSubscriptions();
        }

        // ===============================================================
        // CHECKING BALANCE TESTS
        // ===============================================================

        [Test]
        public void ResetBalance_SetsToStartingBalance()
        {
            _currencyManager.SetCheckingBalance(500f);
            _currencyManager.ResetBalance();

            Assert.AreEqual(1000f, _currencyManager.CheckingBalance);
        }

        [Test]
        public void AddToChecking_IncreasesBalance()
        {
            _currencyManager.AddToChecking(500f, "Test");

            Assert.AreEqual(1500f, _currencyManager.CheckingBalance);
        }

        [Test]
        public void AddToChecking_WithZeroAmount_DoesNotChange()
        {
            float before = _currencyManager.CheckingBalance;
            _currencyManager.AddToChecking(0f, "Test");

            Assert.AreEqual(before, _currencyManager.CheckingBalance);
        }

        [Test]
        public void AddToChecking_WithNegativeAmount_DoesNotChange()
        {
            float before = _currencyManager.CheckingBalance;
            _currencyManager.AddToChecking(-100f, "Test");

            Assert.AreEqual(before, _currencyManager.CheckingBalance);
        }

        // ===============================================================
        // CHECKING SPENDING TESTS
        // ===============================================================

        [Test]
        public void TrySpendChecking_WithSufficientFunds_ReturnsTrue()
        {
            bool result = _currencyManager.TrySpendChecking(500f, "Test");

            Assert.IsTrue(result);
            Assert.AreEqual(500f, _currencyManager.CheckingBalance);
        }

        [Test]
        public void TrySpendChecking_WithInsufficientFunds_ReturnsFalse()
        {
            bool result = _currencyManager.TrySpendChecking(2000f, "Test");

            Assert.IsFalse(result);
            Assert.AreEqual(1000f, _currencyManager.CheckingBalance);
        }

        [Test]
        public void TrySpendChecking_ExactBalance_Succeeds()
        {
            bool result = _currencyManager.TrySpendChecking(1000f, "Test");

            Assert.IsTrue(result);
            Assert.AreEqual(0f, _currencyManager.CheckingBalance);
        }

        // ===============================================================
        // CAN AFFORD TESTS
        // ===============================================================

        [Test]
        public void CanAffordChecking_WithSufficientFunds_ReturnsTrue()
        {
            Assert.IsTrue(_currencyManager.CanAffordChecking(500f));
        }

        [Test]
        public void CanAffordChecking_WithInsufficientFunds_ReturnsFalse()
        {
            Assert.IsFalse(_currencyManager.CanAffordChecking(2000f));
        }

        // ===============================================================
        // INVESTING ACCOUNT TESTS
        // ===============================================================

        [Test]
        [NUnit.Framework.Ignore("Investing balance is now computed from portfolio value, not a cash account")]
        public void AddToInvesting_IncreasesInvestingBalance() { Assert.Pass(); }

        [Test]
        [NUnit.Framework.Ignore("Investing balance is now computed from portfolio value, not a cash account")]
        public void TrySpendInvesting_WithSufficientFunds_ReturnsTrue() { Assert.Pass(); }

        [Test]
        [NUnit.Framework.Ignore("Investing balance is now computed from portfolio value, not a cash account")]
        public void TrySpendInvesting_WithInsufficientFunds_ReturnsFalse() { Assert.Pass(); }

        [Test]
        [NUnit.Framework.Ignore("Investing balance is now computed from portfolio value, not a cash account")]
        public void CanAffordInvesting_WithSufficientFunds_ReturnsTrue() { Assert.Pass(); }

        [Test]
        [NUnit.Framework.Ignore("Investing balance is now computed from portfolio value, not a cash account")]
        public void CanAffordInvesting_WithInsufficientFunds_ReturnsFalse() { Assert.Pass(); }

        // ===============================================================
        // TRANSFER TESTS (obsolete -- buying/selling handles money flow directly)
        // ===============================================================

        [Test]
        [NUnit.Framework.Ignore("Transfers are obsolete -- buying deducts from checking, selling adds to checking")]
        public void TransferToInvesting_MovesBalance() { Assert.Pass(); }

        [Test]
        [NUnit.Framework.Ignore("Transfers are obsolete -- buying deducts from checking, selling adds to checking")]
        public void TransferToInvesting_InsufficientChecking_ReturnsFalse() { Assert.Pass(); }

        [Test]
        [NUnit.Framework.Ignore("Transfers are obsolete -- buying deducts from checking, selling adds to checking")]
        public void TransferFromInvesting_MovesBalance() { Assert.Pass(); }

        [Test]
        [NUnit.Framework.Ignore("Transfers are obsolete -- buying deducts from checking, selling adds to checking")]
        public void TransferFromInvesting_InsufficientInvesting_ReturnsFalse() { Assert.Pass(); }

        [Test]
        [NUnit.Framework.Ignore("Investing balance is now computed from portfolio value")]
        public void TotalLiquidBalance_SumsBothAccounts() { Assert.Pass(); }

        // ===============================================================
        // ADVERSARIAL TESTS
        // ===============================================================

        [Test]
        [NUnit.Framework.Ignore("Transfers are obsolete")]
        public void TransferToInvesting_ZeroAmount_ReturnsFalse() { Assert.Pass(); }

        [Test]
        [NUnit.Framework.Ignore("Transfers are obsolete")]
        public void TransferToInvesting_NegativeAmount_ReturnsFalse() { Assert.Pass(); }

        [Test]
        public void TrySpendChecking_ZeroAmount_ReturnsFalse()
        {
            bool result = _currencyManager.TrySpendChecking(0f, "Test");
            Assert.IsFalse(result);
        }

        [Test]
        public void TrySpendChecking_NegativeAmount_ReturnsFalse()
        {
            bool result = _currencyManager.TrySpendChecking(-50f, "Test");
            Assert.IsFalse(result);
        }

        // ===============================================================
        // EVENT TESTS
        // ===============================================================

        [Test]
        public void AddToChecking_FiresCurrencyChangedEvent()
        {
            float receivedBalance = 0f;
            float receivedDelta = 0f;
            GameEvents.OnCurrencyChanged += (balance, delta) =>
            {
                receivedBalance = balance;
                receivedDelta = delta;
            };

            _currencyManager.AddToChecking(250f, "Test");

            Assert.AreEqual(1250f, receivedBalance);
            Assert.AreEqual(250f, receivedDelta);
        }

        [Test]
        public void TrySpendChecking_FiresCurrencyChangedEvent()
        {
            float receivedBalance = 0f;
            float receivedDelta = 0f;
            GameEvents.OnCurrencyChanged += (balance, delta) =>
            {
                receivedBalance = balance;
                receivedDelta = delta;
            };

            _currencyManager.TrySpendChecking(300f, "Test");

            Assert.AreEqual(700f, receivedBalance);
            Assert.AreEqual(-300f, receivedDelta);
        }

        [Test]
        public void AddToChecking_FiresCheckingBalanceChangedEvent()
        {
            float receivedBalance = 0f;
            float receivedDelta = 0f;
            GameEvents.OnCheckingBalanceChanged += (balance, delta) =>
            {
                receivedBalance = balance;
                receivedDelta = delta;
            };

            _currencyManager.AddToChecking(250f, "Test");

            Assert.AreEqual(1250f, receivedBalance);
            Assert.AreEqual(250f, receivedDelta);
        }

        [Test]
        [NUnit.Framework.Ignore("AddToInvesting removed -- investing balance is now computed from portfolio value")]
        public void AddToInvesting_FiresInvestingBalanceChangedEvent() { Assert.Pass(); }

        [Test]
        public void AddToChecking_FiresIncomeGeneratedEvent()
        {
            float receivedAmount = 0f;
            string receivedSource = "";
            GameEvents.OnIncomeGenerated += (amount, source) =>
            {
                receivedAmount = amount;
                receivedSource = source;
            };

            _currencyManager.AddToChecking(100f, "Restaurant");

            Assert.AreEqual(100f, receivedAmount);
            Assert.AreEqual("Restaurant", receivedSource);
        }
    }
}
