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
        public void AddToInvesting_IncreasesInvestingBalance()
        {
            _currencyManager.AddToInvesting(300f, "Test");

            Assert.AreEqual(300f, _currencyManager.InvestingBalance);
        }

        [Test]
        public void TrySpendInvesting_WithSufficientFunds_ReturnsTrue()
        {
            _currencyManager.SetInvestingBalance(500f);
            bool result = _currencyManager.TrySpendInvesting(200f, "Test");

            Assert.IsTrue(result);
            Assert.AreEqual(300f, _currencyManager.InvestingBalance);
        }

        [Test]
        public void TrySpendInvesting_WithInsufficientFunds_ReturnsFalse()
        {
            _currencyManager.SetInvestingBalance(100f);
            bool result = _currencyManager.TrySpendInvesting(200f, "Test");

            Assert.IsFalse(result);
            Assert.AreEqual(100f, _currencyManager.InvestingBalance);
        }

        [Test]
        public void CanAffordInvesting_WithSufficientFunds_ReturnsTrue()
        {
            _currencyManager.SetInvestingBalance(500f);
            Assert.IsTrue(_currencyManager.CanAffordInvesting(500f));
        }

        [Test]
        public void CanAffordInvesting_WithInsufficientFunds_ReturnsFalse()
        {
            _currencyManager.SetInvestingBalance(100f);
            Assert.IsFalse(_currencyManager.CanAffordInvesting(200f));
        }

        // ===============================================================
        // TRANSFER TESTS
        // ===============================================================

        [Test]
        public void TransferToInvesting_MovesBalance()
        {
            bool result = _currencyManager.TransferToInvesting(400f);

            Assert.IsTrue(result);
            Assert.AreEqual(600f, _currencyManager.CheckingBalance);
            Assert.AreEqual(400f, _currencyManager.InvestingBalance);
        }

        [Test]
        public void TransferToInvesting_InsufficientChecking_ReturnsFalse()
        {
            bool result = _currencyManager.TransferToInvesting(2000f);

            Assert.IsFalse(result);
            Assert.AreEqual(1000f, _currencyManager.CheckingBalance);
            Assert.AreEqual(0f, _currencyManager.InvestingBalance);
        }

        [Test]
        public void TransferFromInvesting_MovesBalance()
        {
            _currencyManager.SetInvestingBalance(500f);
            bool result = _currencyManager.TransferFromInvesting(300f);

            Assert.IsTrue(result);
            Assert.AreEqual(200f, _currencyManager.InvestingBalance);
            Assert.AreEqual(1300f, _currencyManager.CheckingBalance);
        }

        [Test]
        public void TransferFromInvesting_InsufficientInvesting_ReturnsFalse()
        {
            _currencyManager.SetInvestingBalance(100f);
            bool result = _currencyManager.TransferFromInvesting(200f);

            Assert.IsFalse(result);
            Assert.AreEqual(100f, _currencyManager.InvestingBalance);
        }

        [Test]
        public void TotalLiquidBalance_SumsBothAccounts()
        {
            _currencyManager.SetInvestingBalance(500f);

            Assert.AreEqual(1500f, _currencyManager.TotalLiquidBalance);
        }

        // ===============================================================
        // ADVERSARIAL TESTS
        // ===============================================================

        [Test]
        public void TransferToInvesting_ZeroAmount_ReturnsFalse()
        {
            bool result = _currencyManager.TransferToInvesting(0f);
            Assert.IsFalse(result);
        }

        [Test]
        public void TransferToInvesting_NegativeAmount_ReturnsFalse()
        {
            bool result = _currencyManager.TransferToInvesting(-100f);
            Assert.IsFalse(result);
        }

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
        public void AddToInvesting_FiresInvestingBalanceChangedEvent()
        {
            float receivedBalance = 0f;
            float receivedDelta = 0f;
            GameEvents.OnInvestingBalanceChanged += (balance, delta) =>
            {
                receivedBalance = balance;
                receivedDelta = delta;
            };

            _currencyManager.AddToInvesting(200f, "Test");

            Assert.AreEqual(200f, receivedBalance);
            Assert.AreEqual(200f, receivedDelta);
        }

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
