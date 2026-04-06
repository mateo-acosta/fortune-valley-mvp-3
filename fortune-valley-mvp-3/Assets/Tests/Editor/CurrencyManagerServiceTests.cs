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
        public void InvestingBalance_MatchesCurrencyManager()
        {
            _currencyManager.SetInvestingBalance(300f);
            Assert.AreEqual(300f, _service.InvestingBalance, 0.001f);
        }

        [Test]
        public void TotalLiquidBalance_SumsBothAccounts()
        {
            _currencyManager.SetCheckingBalance(1000f);
            _currencyManager.SetInvestingBalance(500f);
            Assert.AreEqual(1500f, _service.TotalLiquidBalance, 0.001f);
        }

        [Test]
        public void TotalLiquidBalance_AfterTransfer_UnchangedTotal()
        {
            _currencyManager.SetCheckingBalance(1000f);
            _currencyManager.SetInvestingBalance(0f);
            float totalBefore = _service.TotalLiquidBalance;

            _currencyManager.TransferToInvesting(400f);

            Assert.AreEqual(totalBefore, _service.TotalLiquidBalance, 0.001f);
            Assert.AreEqual(600f, _service.CheckingBalance, 0.001f);
            Assert.AreEqual(400f, _service.InvestingBalance, 0.001f);
        }
    }
}
