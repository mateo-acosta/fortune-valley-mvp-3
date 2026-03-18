using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Tests for PortfolioHistoryTracker.
    /// Uses reflection to invoke private handlers directly — avoids coupling to the
    /// full event chain where other subscribers (CurrencyManager, InvestmentSystem)
    /// could interfere with the tracker under test.
    /// </summary>
    [TestFixture]
    public class PortfolioHistoryTrackerTests
    {
        private GameObject _rootGO;
        private PortfolioHistoryTracker _tracker;
        private CurrencyManager _currency;
        private InvestmentSystem _investment;

        [SetUp]
        public void SetUp()
        {
            _rootGO    = new GameObject("TestRoot");
            _tracker   = _rootGO.AddComponent<PortfolioHistoryTracker>();
            _currency  = _rootGO.AddComponent<CurrencyManager>();
            _investment = _rootGO.AddComponent<InvestmentSystem>();

            // Wire dependencies via reflection
            SetField(_tracker, "_currencyManager", _currency);
            SetField(_tracker, "_investmentSystem", _investment);

            // Pre-seed available investments with empty list so InvestmentSystem doesn't throw
            SetField(_investment, "_availableInvestments", new List<InvestmentDefinition>());

            // Set balance directly on CurrencyManager's backing field
            SetField(_currency, "_balance", 500f);
        }

        [TearDown]
        public void TearDown()
        {
            // DestroyImmediate triggers OnDisable → event unsubscription
            Object.DestroyImmediate(_rootGO);
        }

        // ═══════════════════════════════════════════════════════════════
        // TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void RecordsSnapshotOnTick1_NotOnlyMultiplesOfFive()
        {
            // With default _snapshotInterval = 1, every tick records a snapshot.
            // Old default of 5 would miss tick 1 (1 % 5 != 0).
            InvokeGameStart();
            int countAfterStart = _tracker.DataPointCount;

            InvokeTick(1);

            Assert.Greater(_tracker.DataPointCount, countAfterStart,
                "A snapshot should be recorded on tick 1 with snapshotInterval = 1");
        }

        [Test]
        public void MaxDataPoints_200_Enforced()
        {
            // After 201+ snapshots the tracker should cap at 200.
            InvokeGameStart(); // 1 initial snapshot

            for (int t = 1; t <= 201; t++)
                InvokeTick(t); // 201 more → 202 total → trimmed to 200

            Assert.LessOrEqual(_tracker.DataPointCount, 200,
                "DataPointCount must never exceed 200 (default maxDataPoints)");
        }

        [Test]
        public void TotalWealthHistory_Snapshot_EqualsBalancePlusPortfolioValue()
        {
            // balance = 500, no active investments → TotalPortfolioValue = 0
            // Snapshot must equal 500 + 0 = 500, validating: totalWealth = Balance + TotalPortfolioValue
            SetField(_currency, "_balance", 500f);

            InvokeGameStart(); // records the first snapshot

            float lastSnapshot = _tracker.TotalWealthHistory[_tracker.TotalWealthHistory.Count - 1];
            float expected = 500f + _investment.TotalPortfolioValue; // 500 + 0

            Assert.AreEqual(expected, lastSnapshot, 0.01f,
                "Snapshot must equal Balance + TotalPortfolioValue");
        }

        // ═══════════════════════════════════════════════════════════════
        // HELPERS — invoke private handler methods directly to avoid
        // coupling to the global event chain
        // ═══════════════════════════════════════════════════════════════

        private void InvokeGameStart()
        {
            var m = typeof(PortfolioHistoryTracker).GetMethod("HandleGameStart",
                BindingFlags.NonPublic | BindingFlags.Instance);
            m?.Invoke(_tracker, null);
        }

        private void InvokeTick(int tick)
        {
            var m = typeof(PortfolioHistoryTracker).GetMethod("HandleTick",
                BindingFlags.NonPublic | BindingFlags.Instance);
            m?.Invoke(_tracker, new object[] { tick });
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var f = target.GetType().GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            f?.SetValue(target, value);
        }
    }
}
