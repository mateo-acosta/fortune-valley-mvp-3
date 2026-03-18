using NUnit.Framework;
using FortuneValley.Core;
using FortuneValley.Domain.Interfaces;
using UnityEngine;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Verifies CurrencyManager satisfies the ICurrencyService contract.
    /// Uses a real CurrencyManager instance (not mocked) to confirm the
    /// implementation matches the interface expectations.
    /// </summary>
    public class CurrencyManagerServiceTests
    {
        private CurrencyManager _currencyManager;
        private ICurrencyService _service;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("CurrencyManager");
            _currencyManager = go.AddComponent<CurrencyManager>();
            _service = _currencyManager;

            // Mirror starting balance from CurrencyManager default (1000f)
            _currencyManager.ResetBalance();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_currencyManager.gameObject);
        }

        [Test]
        public void Add_PositiveAmount_IncreasesBalance()
        {
            float before = _service.Balance;
            _service.Add(200f, "test");
            Assert.AreEqual(before + 200f, _service.Balance, 0.001f);
        }

        [Test]
        public void TrySpend_ExactBalance_ReturnsTrue()
        {
            float balance = _service.Balance;
            bool result = _service.TrySpend(balance, "exact spend");
            Assert.IsTrue(result);
            Assert.AreEqual(0f, _service.Balance, 0.001f);
        }

        [Test]
        public void TrySpend_InsufficientBalance_ReturnsFalse()
        {
            float balance = _service.Balance;
            bool result = _service.TrySpend(balance + 1f, "over budget");
            Assert.IsFalse(result);
            Assert.AreEqual(balance, _service.Balance, 0.001f);
        }

        [Test]
        public void CanAfford_ExactBalance_ReturnsTrue()
        {
            float balance = _service.Balance;
            Assert.IsTrue(_service.CanAfford(balance));
            Assert.IsFalse(_service.CanAfford(balance + 0.01f));
        }
    }
}
