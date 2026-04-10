using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;
using FortuneValley.UI;

namespace FortuneValley.Tests
{
    /// <summary>
    /// EditMode unit tests for InvestmentFilterLogic.
    /// Pure logic -- no MonoBehaviour or scene needed.
    /// </summary>
    [TestFixture]
    public class InvestmentFilterLogicTests
    {
        // ─── helpers ────────────────────────────────────────────────────

        private List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created)
                Object.DestroyImmediate(obj);
            _created.Clear();
        }

        private InvestmentDefinition MakeDef(
            string name, InvestmentCategory category,
            Industry industry = Industry.None)
        {
            var def = ScriptableObject.CreateInstance<InvestmentDefinition>();
            _created.Add(def);

            var type = typeof(InvestmentDefinition);
            var flags = System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance;

            type.GetField("_displayName", flags)?.SetValue(def, name);
            type.GetField("_category", flags)?.SetValue(def, category);
            type.GetField("_industry", flags)?.SetValue(def, industry);
            type.GetField("_basePricePerShare", flags)?.SetValue(def, 10f);

            return def;
        }

        private ActiveInvestment MakeHolding(InvestmentDefinition def, int shares)
        {
            return new ActiveInvestment(def, shares, 10f, 0);
        }

        private List<InvestmentDefinition> MakeMixedDefinitions()
        {
            return new List<InvestmentDefinition>
            {
                MakeDef("TechStock", InvestmentCategory.Stock, Industry.Technology),
                MakeDef("FinStock", InvestmentCategory.Stock, Industry.Financials),
                MakeDef("EnergyStock", InvestmentCategory.Stock, Industry.Energy),
                MakeDef("SP500", InvestmentCategory.ETF),
                MakeDef("GovBond", InvestmentCategory.Bond),
                MakeDef("TBill90", InvestmentCategory.TBill)
            };
        }

        // ─── FilterDefinitions ──────────────────────────────────────────

        [Test]
        public void FilterDefinitions_NoFilters_ReturnsAll()
        {
            var all = MakeMixedDefinitions();

            var result = InvestmentFilterLogic.FilterDefinitions(all, null, null);

            Assert.AreEqual(6, result.Count);
        }

        [Test]
        public void FilterDefinitions_NullInput_ReturnsEmpty()
        {
            var result = InvestmentFilterLogic.FilterDefinitions(null, null, null);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void FilterDefinitions_EmptyInput_ReturnsEmpty()
        {
            var result = InvestmentFilterLogic.FilterDefinitions(
                new List<InvestmentDefinition>(), null, null);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void FilterDefinitions_CategoryOnly_Stock_ReturnsOnlyStocks()
        {
            var all = MakeMixedDefinitions();

            var result = InvestmentFilterLogic.FilterDefinitions(
                all, InvestmentCategory.Stock, null);

            Assert.AreEqual(3, result.Count);
            foreach (var def in result)
                Assert.AreEqual(InvestmentCategory.Stock, def.Category);
        }

        [Test]
        public void FilterDefinitions_CategoryOnly_Bond_ReturnsOnlyBonds()
        {
            var all = MakeMixedDefinitions();

            var result = InvestmentFilterLogic.FilterDefinitions(
                all, InvestmentCategory.Bond, null);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("GovBond", result[0].DisplayName);
        }

        [Test]
        public void FilterDefinitions_CategoryOnly_ETF_ReturnsOnlyETFs()
        {
            var all = MakeMixedDefinitions();

            var result = InvestmentFilterLogic.FilterDefinitions(
                all, InvestmentCategory.ETF, null);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("SP500", result[0].DisplayName);
        }

        [Test]
        public void FilterDefinitions_IndustryOnly_ReturnsMatchingStocksPlusAllNonStocks()
        {
            var all = MakeMixedDefinitions();

            var result = InvestmentFilterLogic.FilterDefinitions(
                all, null, Industry.Technology);

            // TechStock + SP500 (ETF) + GovBond + TBill90 = 4
            // FinStock and EnergyStock excluded (stocks that don't match)
            Assert.AreEqual(4, result.Count);
        }

        [Test]
        public void FilterDefinitions_CategoryAndIndustry_ReturnsOnlyMatchingStocks()
        {
            var all = MakeMixedDefinitions();

            var result = InvestmentFilterLogic.FilterDefinitions(
                all, InvestmentCategory.Stock, Industry.Technology);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("TechStock", result[0].DisplayName);
        }

        [Test]
        public void FilterDefinitions_IndustryFilter_NonStockAlwaysPasses()
        {
            var all = new List<InvestmentDefinition>
            {
                MakeDef("Bond1", InvestmentCategory.Bond),
                MakeDef("ETF1", InvestmentCategory.ETF),
                MakeDef("TBill1", InvestmentCategory.TBill)
            };

            // Even with a specific industry, non-stocks pass through
            var result = InvestmentFilterLogic.FilterDefinitions(
                all, null, Industry.Healthcare);

            Assert.AreEqual(3, result.Count);
        }

        [Test]
        public void FilterDefinitions_StockWithIndustryNone_ExcludedByIndustryFilter()
        {
            var stock = MakeDef("NoIndustryStock", InvestmentCategory.Stock, Industry.None);
            var all = new List<InvestmentDefinition> { stock };

            var result = InvestmentFilterLogic.FilterDefinitions(
                all, null, Industry.Technology);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void FilterDefinitions_NoMatches_ReturnsEmpty()
        {
            var all = MakeMixedDefinitions();

            // No stocks have Healthcare industry in our test set
            var result = InvestmentFilterLogic.FilterDefinitions(
                all, InvestmentCategory.Stock, Industry.Healthcare);

            Assert.AreEqual(0, result.Count);
        }

        // ─── FilterActiveInvestments ────────────────────────────────────

        [Test]
        public void FilterActiveInvestments_NoFilters_ReturnsAll()
        {
            var defs = MakeMixedDefinitions();
            var holdings = new List<ActiveInvestment>();
            foreach (var def in defs)
                holdings.Add(MakeHolding(def, 5));

            var result = InvestmentFilterLogic.FilterActiveInvestments(
                holdings, null, null);

            Assert.AreEqual(6, result.Count);
        }

        [Test]
        public void FilterActiveInvestments_CategoryFilter_FiltersCorrectly()
        {
            var defs = MakeMixedDefinitions();
            var holdings = new List<ActiveInvestment>();
            foreach (var def in defs)
                holdings.Add(MakeHolding(def, 5));

            var result = InvestmentFilterLogic.FilterActiveInvestments(
                holdings, InvestmentCategory.Stock, null);

            Assert.AreEqual(3, result.Count);
            foreach (var inv in result)
                Assert.AreEqual(InvestmentCategory.Stock, inv.Definition.Category);
        }

        [Test]
        public void FilterActiveInvestments_NullInput_ReturnsEmpty()
        {
            var result = InvestmentFilterLogic.FilterActiveInvestments(
                null, null, null);

            Assert.AreEqual(0, result.Count);
        }
    }
}
