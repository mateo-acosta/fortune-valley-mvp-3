using NUnit.Framework;
using UnityEngine;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Tests for InvestmentSystem lifetime tracking.
    /// Uses reflection to wire private serialized fields since InvestmentSystem is a MonoBehaviour.
    /// Sets _balance directly to avoid event-ordering issues in Editor tests.
    /// </summary>
    [TestFixture]
    public class InvestmentSystemTests
    {
        private GameObject _rootGO;
        private InvestmentSystem _system;
        private CurrencyManager _currency;
        private TimeManager _time;
        private InvestmentDefinition _stockDef;

        [SetUp]
        public void SetUp()
        {
            _rootGO = new GameObject("TestRoot");

            _currency = _rootGO.AddComponent<CurrencyManager>();
            _time = _rootGO.AddComponent<TimeManager>();
            _system = _rootGO.AddComponent<InvestmentSystem>();

            // Create a test investment definition
            _stockDef = ScriptableObject.CreateInstance<InvestmentDefinition>();
            SetField(_stockDef, "_displayName", "TestStock");
            SetField(_stockDef, "_riskLevel", RiskLevel.Medium);
            SetField(_stockDef, "_annualReturnRate", 0.10f);
            SetField(_stockDef, "_basePricePerShare", 100f);
            SetField(_stockDef, "_category", InvestmentCategory.Stock);
            _stockDef.InitializePrice();

            // Wire dependencies via reflection
            SetField(_system, "_currencyManager", _currency);
            SetField(_system, "_timeManager", _time);
            var list = new System.Collections.Generic.List<InvestmentDefinition> { _stockDef };
            SetField(_system, "_availableInvestments", list);

            // Set balance directly (avoid relying on event chain). Investment
            // buys deduct from checking via TrySpendChecking; the InvestingBalance
            // is now a computed property over InvestmentSystem.TotalPortfolioValue,
            // so the legacy _investingBalance / _startingInvestingBalance backing
            // fields no longer exist on CurrencyManager and don't need seeding.
            SetField(_currency, "_checkingBalance", 10000f);
            SetField(_currency, "_startingCheckingBalance", 10000f);
        }

        [TearDown]
        public void TearDown()
        {
            // Unsubscribe events before destroying to avoid stale callbacks
            Object.DestroyImmediate(_rootGO);
            Object.DestroyImmediate(_stockDef);
        }

        // ═══════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════

        private static void SetField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
                type = type.BaseType;
            }
            throw new System.Exception($"Field '{fieldName}' not found on {target.GetType().Name}");
        }

        // ═══════════════════════════════════════════════════════════════
        // TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void BuyShares_IncrementsLifetimeCountAndPrincipal()
        {
            var inv = _system.BuyShares(_stockDef, 5);

            Assert.IsNotNull(inv, "BuyShares returned null");
            Assert.AreEqual(1, _system.LifetimeTotalInvestmentsMade);
            Assert.AreEqual(5 * _stockDef.CurrentPrice, _system.LifetimeTotalPrincipalInvested, 1f);
        }

        [Test]
        public void BuyThenSellAll_LifetimeCountPreserved()
        {
            var inv = _system.BuyShares(_stockDef, 5);
            Assert.IsNotNull(inv, "BuyShares returned null");

            _system.SellAllShares(inv);

            // Lifetime count should still be 1 even after selling
            Assert.AreEqual(1, _system.LifetimeTotalInvestmentsMade);
            Assert.AreEqual(0, _system.ActiveInvestments.Count);
        }

        [Test]
        public void SellAllShares_RecordsRealizedGain()
        {
            var inv = _system.BuyShares(_stockDef, 10);
            Assert.IsNotNull(inv, "BuyShares returned null");
            float buyPrice = inv.AveragePurchasePrice;

            // Nudge the price up to simulate a gain
            SetField(_stockDef, "_currentPrice", buyPrice * 1.1f);

            _system.SellAllShares(inv);

            // Realized gain should be positive (all sold, no unrealized left)
            Assert.Greater(_system.LifetimeTotalGain, 0f,
                "LifetimeTotalGain should be positive after selling at profit");
        }

        [Test]
        public void PartialSell_RecordsProportionalGain()
        {
            var inv = _system.BuyShares(_stockDef, 10);
            Assert.IsNotNull(inv, "BuyShares returned null");
            float buyPrice = inv.AveragePurchasePrice;

            // Price goes up 10%
            float newPrice = buyPrice * 1.1f;
            SetField(_stockDef, "_currentPrice", newPrice);

            // Sell 5 of 10 shares
            _system.SellShares(inv, 5);

            // Expected realized gain: 5 shares * (newPrice - buyPrice)
            float expectedRealized = 5f * (newPrice - buyPrice);
            // Unrealized: 5 remaining shares * (newPrice - buyPrice) = same
            float expectedUnrealized = 5f * (newPrice - buyPrice);
            float expectedTotal = expectedRealized + expectedUnrealized;

            Assert.AreEqual(expectedTotal, _system.LifetimeTotalGain, 1f);
        }

        [Test]
        public void HandleGameStart_ResetsAllLifetimeFields()
        {
            // Buy and sell to accumulate lifetime data
            var inv = _system.BuyShares(_stockDef, 5);
            Assert.IsNotNull(inv, "BuyShares returned null");
            _system.SellAllShares(inv);

            Assert.Greater(_system.LifetimeTotalInvestmentsMade, 0,
                "Should have recorded a buy before testing reset");

            // Invoke HandleGameStart directly via reflection
            // (OnEnable event subscription may not fire in Editor tests)
            var method = typeof(InvestmentSystem).GetMethod("HandleGameStart",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(_system, null);

            Assert.AreEqual(0, _system.LifetimeTotalInvestmentsMade);
            Assert.AreEqual(0f, _system.LifetimeTotalPrincipalInvested);
            Assert.AreEqual(0f, _system.LifetimeTotalGain);
            Assert.AreEqual(0f, _system.PeakPortfolioValue);
        }

        [Test]
        public void MultipleBuySellCycles_AccumulatesLifetimeData()
        {
            float price = _stockDef.CurrentPrice;

            // Cycle 1
            var inv1 = _system.BuyShares(_stockDef, 3);
            Assert.IsNotNull(inv1, "BuyShares (cycle 1) returned null");
            _system.SellAllShares(inv1);

            // Cycle 2
            var inv2 = _system.BuyShares(_stockDef, 7);
            Assert.IsNotNull(inv2, "BuyShares (cycle 2) returned null");
            _system.SellAllShares(inv2);

            Assert.AreEqual(2, _system.LifetimeTotalInvestmentsMade);
            // 3 shares + 7 shares at the same price
            float expectedPrincipal = (3 + 7) * price;
            Assert.AreEqual(expectedPrincipal, _system.LifetimeTotalPrincipalInvested, 1f);
        }

        [Test]
        public void LifetimeTotalGain_EqualsRealizedPlusUnrealized()
        {
            // Buy 10 shares, sell 5, keep 5
            var inv = _system.BuyShares(_stockDef, 10);
            Assert.IsNotNull(inv, "BuyShares returned null");
            float buyPrice = inv.AveragePurchasePrice;

            // Price up 20%
            float newPrice = buyPrice * 1.2f;
            SetField(_stockDef, "_currentPrice", newPrice);

            _system.SellShares(inv, 5);

            // Realized: 5 * (newPrice - buyPrice)
            float realized = 5f * (newPrice - buyPrice);
            // Unrealized: TotalGain on remaining 5 shares
            float unrealized = _system.TotalGain;
            float expected = realized + unrealized;

            Assert.AreEqual(expected, _system.LifetimeTotalGain, 0.1f);
        }

        [Test]
        public void SellAtLoss_RecordsNegativeRealizedGain()
        {
            var inv = _system.BuyShares(_stockDef, 10);
            Assert.IsNotNull(inv, "BuyShares returned null");
            float buyPrice = inv.AveragePurchasePrice;

            // Price drops 15%
            SetField(_stockDef, "_currentPrice", buyPrice * 0.85f);

            _system.SellAllShares(inv);

            Assert.Less(_system.LifetimeTotalGain, 0f,
                "LifetimeTotalGain should be negative after selling at loss");
        }

        // ═══════════════════════════════════════════════════════════════
        // SELL TRANSACTION HISTORY TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void SellAllShares_RecordsSellTransaction()
        {
            var inv = _system.BuyShares(_stockDef, 5);
            Assert.IsNotNull(inv, "BuyShares returned null");
            float buyPrice = inv.AveragePurchasePrice;

            SetField(_stockDef, "_currentPrice", buyPrice * 1.2f);
            _system.SellAllShares(inv);

            Assert.AreEqual(1, _system.SellHistory.Count, "Should record one transaction");
            var record = _system.SellHistory[0];
            Assert.AreEqual("TestStock", record.InvestmentName);
            Assert.AreEqual("Stock", record.Category);
            Assert.AreEqual(5, record.SharesSold);
            Assert.AreEqual(buyPrice, record.CostBasisPerShare, 0.01f);
            Assert.Greater(record.GainOrLoss, 0f, "Should record a positive gain");
        }

        [Test]
        public void SellShares_Partial_RecordsPartialTransaction()
        {
            var inv = _system.BuyShares(_stockDef, 10);
            Assert.IsNotNull(inv, "BuyShares returned null");

            _system.SellShares(inv, 3);

            Assert.AreEqual(1, _system.SellHistory.Count);
            Assert.AreEqual(3, _system.SellHistory[0].SharesSold,
                "SharesSold should match requested count, not total shares owned");
        }

        [Test]
        public void SellShares_PartialThenFull_RecordsTwoTransactions()
        {
            var inv = _system.BuyShares(_stockDef, 10);
            Assert.IsNotNull(inv, "BuyShares returned null");

            _system.SellShares(inv, 4);
            _system.SellAllShares(inv);

            Assert.AreEqual(2, _system.SellHistory.Count, "Should record two separate transactions");
            Assert.AreEqual(4, _system.SellHistory[0].SharesSold, "First sell: 4 shares");
            Assert.AreEqual(6, _system.SellHistory[1].SharesSold, "Second sell: remaining 6 shares");
        }

        [Test]
        public void HandleGameStart_ClearsSellHistory()
        {
            var inv = _system.BuyShares(_stockDef, 5);
            Assert.IsNotNull(inv, "BuyShares returned null");
            _system.SellAllShares(inv);
            Assert.AreEqual(1, _system.SellHistory.Count, "Should have a record before restart");

            var method = typeof(InvestmentSystem).GetMethod("HandleGameStart",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(_system, null);

            Assert.AreEqual(0, _system.SellHistory.Count, "SellHistory should be empty after game restart");
        }

        // ═══════════════════════════════════════════════════════════════
        // INTENT EVENT HANDLER TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void RaiseBuySharesRequested_BuysShares()
        {
            GameEvents.ClearAllSubscriptions();
            // Use reflection instead of SendMessage to avoid ShouldRunBehaviour assertion
            var onEnable = typeof(InvestmentSystem).GetMethod("OnEnable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onEnable.Invoke(_system, null);

            int countBefore = _system.ActiveInvestments.Count;
            GameEvents.RaiseBuySharesRequested(_stockDef, 3);

            Assert.AreEqual(countBefore + 1, _system.ActiveInvestments.Count,
                "InvestmentSystem should buy shares when OnBuySharesRequested fires");

            var onDisable = typeof(InvestmentSystem).GetMethod("OnDisable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onDisable.Invoke(_system, null);
        }

        [Test]
        public void RaiseSellSharesRequested_SellsShares()
        {
            // Buy first so we have something to sell
            var inv = _system.BuyShares(_stockDef, 5);
            Assert.IsNotNull(inv, "BuyShares returned null");

            GameEvents.ClearAllSubscriptions();
            var onEnable = typeof(InvestmentSystem).GetMethod("OnEnable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onEnable.Invoke(_system, null);

            GameEvents.RaiseSellSharesRequested(inv, 2);

            Assert.AreEqual(3, inv.NumberOfShares,
                "InvestmentSystem should sell shares when OnSellSharesRequested fires");

            var onDisable = typeof(InvestmentSystem).GetMethod("OnDisable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onDisable.Invoke(_system, null);
        }

        // ═══════════════════════════════════════════════════════════════
        // SELL TRANSACTION HISTORY TESTS (continued)
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void SellTransactionRecord_ZeroCostBasis_PercentageReturnIsZero()
        {
            var inv = _system.BuyShares(_stockDef, 5);
            Assert.IsNotNull(inv, "BuyShares returned null");

            // Force cost basis to 0 to test divide-by-zero guard in RecordRealizedGain
            SetField(inv, "<AveragePurchasePrice>k__BackingField", 0f);

            _system.SellAllShares(inv);

            Assert.AreEqual(1, _system.SellHistory.Count);
            Assert.AreEqual(0f, _system.SellHistory[0].PercentageReturn, 0.001f,
                "PercentageReturn should be 0 when cost basis is 0 (guard against divide-by-zero)");
        }
    }
}
