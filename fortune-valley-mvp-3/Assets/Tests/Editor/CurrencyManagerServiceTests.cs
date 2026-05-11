using NUnit.Framework;
using FortuneValley.Core;
using FortuneValley.Domain.Interfaces;
using UnityEngine;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Verifies CurrencyManager satisfies the ICurrencyService contract.
    /// Uses a real CurrencyManager instance to confirm the implementation
    /// exposes the correct read-only balance properties.
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

            _currencyManager.ResetBalance();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_currencyManager.gameObject);
        }

        [Test]
        public void CheckingBalance_MatchesCurrencyManager()
        {
            _currencyManager.SetCheckingBalance(500f);
            Assert.AreEqual(500f, _service.CheckingBalance, 0.001f);
        }

        [Test]
        [NUnit.Framework.Ignore("Investing balance is now computed from portfolio value, not a stored amount")]
        public void InvestingBalance_MatchesCurrencyManager()
        {
            // Investing balance now requires InvestmentSystem with active holdings
            Assert.Pass();
        }

        [Test]
        [NUnit.Framework.Ignore("Investing balance is now computed from portfolio value, not a stored amount")]
        public void TotalLiquidBalance_SumsBothAccounts()
        {
            Assert.Pass();
        }

        [Test]
        [NUnit.Framework.Ignore("Transfers between checking and investing are no longer applicable")]
        public void TotalLiquidBalance_AfterTransfer_UnchangedTotal()
        {
            Assert.Pass();
        }
    }
}
