using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Tests for StockPriceHistoryStore — windowing, unknown definitions, cap enforcement,
    /// and SimulateHistory determinism (used for pre-game history seeding).
    /// </summary>
    [TestFixture]
    public class StockPriceHistoryStoreTests
    {
        private GameObject _rootGO;
        private StockPriceHistoryStore _store;
        private InvestmentSystem _investSystem;
        private InvestmentDefinition _def;

        [SetUp]
        public void SetUp()
        {
            _rootGO    = new GameObject("TestRoot");
            _store     = _rootGO.AddComponent<StockPriceHistoryStore>();
            _investSystem = _rootGO.AddComponent<InvestmentSystem>();

            // Create a simple test investment definition
            _def = ScriptableObject.CreateInstance<InvestmentDefinition>();
            SetField(_def, "_displayName",      "TestStock");
            SetField(_def, "_riskLevel",        RiskLevel.Medium);
            SetField(_def, "_annualReturnRate",  0.10f);
            SetField(_def, "_basePricePerShare", 100f);
            SetField(_def, "_category",         InvestmentCategory.Stock);
            _def.InitializePrice();

            // Wire InvestmentSystem → store and add the test def
            SetField(_store, "_investmentSystem", _investSystem);
            var defList = new List<InvestmentDefinition> { _def };
            SetField(_investSystem, "_availableInvestments", defList);

            // Inject a pre-populated history for this def directly
            var dict = GetHistory(_store);
            dict[_def] = new List<float>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_rootGO);
            Object.DestroyImmediate(_def);
        }

        // ═══════════════════════════════════════════════════════════════
        // GetWindow tests
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void GetWindow_ExactlyWindowSize_ReturnsAll()
        {
            // 30 entries → window of 30 → returns all 30
            SetHistory(_def, GenerateList(30));
            var result = _store.GetWindow(_def, 30);
            Assert.AreEqual(30, result.Count);
        }

        [Test]
        public void GetWindow_MoreThanWindowSize_ReturnsLast()
        {
            // 35 entries, window 30 → returns last 30 (not first 30)
            var list = GenerateList(35); // values: 0,1,2,...,34
            SetHistory(_def, list);

            var result = _store.GetWindow(_def, 30);

            Assert.AreEqual(30, result.Count, "Should return exactly 30 entries");
            Assert.AreEqual(5f, result[0], 0.001f,
                "First element should be index 5 (the 6th, since last 30 of 35)");
            Assert.AreEqual(34f, result[result.Count - 1], 0.001f,
                "Last element should be the final entry (index 34)");
        }

        [Test]
        public void GetWindow_FewerThanWindowSize_ReturnsAll()
        {
            // 5 entries → window of 30 → returns all 5
            SetHistory(_def, GenerateList(5));
            var result = _store.GetWindow(_def, 30);
            Assert.AreEqual(5, result.Count);
        }

        [Test]
        public void GetWindow_UnknownDefinition_ReturnsEmptyList()
        {
            // Unknown def must return empty list — never null, never exception
            var unknownDef = ScriptableObject.CreateInstance<InvestmentDefinition>();
            try
            {
                var result = _store.GetWindow(unknownDef, 30);
                Assert.IsNotNull(result, "Result must not be null for unknown definition");
                Assert.AreEqual(0, result.Count, "Result must be empty for unknown definition");
            }
            finally
            {
                Object.DestroyImmediate(unknownDef);
            }
        }

        [Test]
        public void HandleTick_MaxCapAt200_Enforced()
        {
            // Start with 199 entries, fire 2 ticks → count should stay at 200 (cap)
            SetHistory(_def, GenerateList(199));

            GameEvents.RaiseTick(1);
            GameEvents.RaiseTick(2);

            var history = GetHistory(_store);
            int count = history.ContainsKey(_def) ? history[_def].Count : 0;

            Assert.LessOrEqual(count, 200,
                "History count must not exceed 200 after overflow");
        }

        [Test]
        public void SimulateHistory_Determinism_SameSeedProducesSameOutput()
        {
            // SimulateHistory uses System.Random(seed) — same seed must produce identical arrays
            float[] run1 = _def.SimulateHistory(30, 12345);
            float[] run2 = _def.SimulateHistory(30, 12345);

            Assert.AreEqual(run1.Length, run2.Length);
            for (int i = 0; i < run1.Length; i++)
                Assert.AreEqual(run1[i], run2[i], 0.0001f,
                    $"SimulateHistory not deterministic at index {i}");
        }

        // ═══════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════

        private static List<float> GenerateList(int count)
        {
            var list = new List<float>(count);
            for (int i = 0; i < count; i++) list.Add((float)i);
            return list;
        }

        private void SetHistory(InvestmentDefinition def, List<float> values)
        {
            var dict = GetHistory(_store);
            dict[def] = values;
        }

        private static Dictionary<InvestmentDefinition, List<float>> GetHistory(StockPriceHistoryStore store)
        {
            var f = typeof(StockPriceHistoryStore).GetField("_history",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (Dictionary<InvestmentDefinition, List<float>>)f.GetValue(store);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var f = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            f?.SetValue(target, value);
        }
    }
}
