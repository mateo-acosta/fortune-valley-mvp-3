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
            var field = typeof(CurrencyManager).GetField("_startingBalance",
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

        // ═══════════════════════════════════════════════════════════════
        // BALANCE TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void ResetBalance_SetsToStartingBalance()
        {
            _currencyManager.SetBalance(500f);
            _currencyManager.ResetBalance();

            Assert.AreEqual(1000f, _currencyManager.Balance);
        }

        [Test]
        public void Add_IncreasesBalance()
        {
            _currencyManager.Add(500f, "Test");

            Assert.AreEqual(1500f, _currencyManager.Balance);
        }

        [Test]
        public void Add_WithZeroAmount_DoesNotChange()
        {
            float before = _currencyManager.Balance;
            _currencyManager.Add(0f, "Test");

            Assert.AreEqual(before, _currencyManager.Balance);
        }

        [Test]
        public void Add_WithNegativeAmount_DoesNotChange()
        {
            float before = _currencyManager.Balance;
            _currencyManager.Add(-100f, "Test");

            Assert.AreEqual(before, _currencyManager.Balance);
        }

        // ═══════════════════════════════════════════════════════════════
        // SPENDING TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void TrySpend_WithSufficientFunds_ReturnsTrue()
        {
            bool result = _currencyManager.TrySpend(500f, "Test");

            Assert.IsTrue(result);
            Assert.AreEqual(500f, _currencyManager.Balance);
        }

        [Test]
        public void TrySpend_WithInsufficientFunds_ReturnsFalse()
        {
            bool result = _currencyManager.TrySpend(2000f, "Test");

            Assert.IsFalse(result);
            Assert.AreEqual(1000f, _currencyManager.Balance);
        }

        [Test]
        public void TrySpend_ExactBalance_Succeeds()
        {
            bool result = _currencyManager.TrySpend(1000f, "Test");

            Assert.IsTrue(result);
            Assert.AreEqual(0f, _currencyManager.Balance);
        }

        // ═══════════════════════════════════════════════════════════════
        // CAN AFFORD TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void CanAfford_WithSufficientFunds_ReturnsTrue()
        {
            Assert.IsTrue(_currencyManager.CanAfford(500f));
        }

        [Test]
        public void CanAfford_WithInsufficientFunds_ReturnsFalse()
        {
            Assert.IsFalse(_currencyManager.CanAfford(2000f));
        }

        // ═══════════════════════════════════════════════════════════════
        // EVENT TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void Add_FiresCurrencyChangedEvent()
        {
            float receivedBalance = 0f;
            float receivedDelta = 0f;
            GameEvents.OnCurrencyChanged += (balance, delta) =>
            {
                receivedBalance = balance;
                receivedDelta = delta;
            };

            _currencyManager.Add(250f, "Test");

            Assert.AreEqual(1250f, receivedBalance);
            Assert.AreEqual(250f, receivedDelta);
        }

        [Test]
        public void TrySpend_FiresCurrencyChangedEvent()
        {
            float receivedBalance = 0f;
            float receivedDelta = 0f;
            GameEvents.OnCurrencyChanged += (balance, delta) =>
            {
                receivedBalance = balance;
                receivedDelta = delta;
            };

            _currencyManager.TrySpend(300f, "Test");

            Assert.AreEqual(700f, receivedBalance);
            Assert.AreEqual(-300f, receivedDelta);
        }

        [Test]
        public void Add_FiresIncomeGeneratedEvent()
        {
            float receivedAmount = 0f;
            string receivedSource = "";
            GameEvents.OnIncomeGenerated += (amount, source) =>
            {
                receivedAmount = amount;
                receivedSource = source;
            };

            _currencyManager.Add(100f, "Restaurant");

            Assert.AreEqual(100f, receivedAmount);
            Assert.AreEqual("Restaurant", receivedSource);
        }
    }
}
