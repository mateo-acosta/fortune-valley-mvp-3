using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;
using FortuneValley.UI;

namespace FortuneValley.Tests
{
    /// <summary>
    /// EditMode unit tests for PortfolioPanelLogic.
    /// Pure logic — no MonoBehaviour or scene needed.
    /// </summary>
    [TestFixture]
    public class PortfolioPanelLogicTests
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

        /// <summary>
        /// Create a minimal InvestmentDefinition with just DisplayName and RiskLevel set.
        /// Uses reflection to reach the private serialized fields (same pattern as ActiveInvestmentTests).
        /// </summary>
        private InvestmentDefinition MakeDef(string displayName, RiskLevel risk)
        {
            var def = ScriptableObject.CreateInstance<InvestmentDefinition>();
            _created.Add(def);

            var type = typeof(InvestmentDefinition);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            type.GetField("_displayName", flags)?.SetValue(def, displayName);
            type.GetField("_riskLevel",   flags)?.SetValue(def, risk);
            // Set a non-zero base price so CurrentPrice has a value
            type.GetField("_basePricePerShare", flags)?.SetValue(def, 10f);

            return def;
        }

        /// <summary>
        /// Create an ActiveInvestment with a set share count and the given definition.
        /// </summary>
        private ActiveInvestment MakeInvestment(InvestmentDefinition def, int shares)
        {
            // Use share-based constructor (tick 0, price 10)
            var inv = new ActiveInvestment(def, shares, 10f, 0);
            return inv;
        }

        // ─── GetPortfolioRiskLabel ───────────────────────────────────────

        [Test]
        public void GetPortfolioRiskLabel_NullList_ReturnsNoHoldings()
        {
            Assert.AreEqual("No Holdings", PortfolioPanelLogic.GetPortfolioRiskLabel(null));
        }

        [Test]
        public void GetPortfolioRiskLabel_EmptyList_ReturnsNoHoldings()
        {
            var empty = new List<ActiveInvestment>();
            Assert.AreEqual("No Holdings", PortfolioPanelLogic.GetPortfolioRiskLabel(empty));
        }

        [Test]
        public void GetPortfolioRiskLabel_AllZeroShares_ReturnsNoHoldings()
        {
            // Zero-share entries should be skipped entirely
            var def = MakeDef("AAPL", RiskLevel.High);
            var inv = new ActiveInvestment(def, 1, 10f, 0);
            inv.RemoveShares(1); // drain to zero

            var holdings = new List<ActiveInvestment> { inv };
            Assert.AreEqual("No Holdings", PortfolioPanelLogic.GetPortfolioRiskLabel(holdings));
        }

        [Test]
        public void GetPortfolioRiskLabel_SingleLowRisk_ReturnsLowRisk()
        {
            var def = MakeDef("BOND", RiskLevel.Low);
            var holdings = new List<ActiveInvestment> { MakeInvestment(def, 5) };

            Assert.AreEqual("Low Risk", PortfolioPanelLogic.GetPortfolioRiskLabel(holdings));
        }

        [Test]
        public void GetPortfolioRiskLabel_SingleHighRisk_ReturnsHighRisk()
        {
            var def = MakeDef("GME", RiskLevel.High);
            var holdings = new List<ActiveInvestment> { MakeInvestment(def, 3) };

            Assert.AreEqual("High Risk", PortfolioPanelLogic.GetPortfolioRiskLabel(holdings));
        }

        [Test]
        public void GetPortfolioRiskLabel_EqualSharesLowAndHigh_ReturnsMediumRisk()
        {
            // Low=1, High=3 → avg = (5*1 + 5*3) / 10 = 2.0 → Medium
            var low  = MakeDef("BOND", RiskLevel.Low);
            var high = MakeDef("GME",  RiskLevel.High);
            var holdings = new List<ActiveInvestment>
            {
                MakeInvestment(low,  5),
                MakeInvestment(high, 5),
            };

            Assert.AreEqual("Medium Risk", PortfolioPanelLogic.GetPortfolioRiskLabel(holdings));
        }

        [Test]
        public void GetPortfolioRiskLabel_WeightedHighMajority_ReturnsHighRisk()
        {
            // Low=1 (1 share), High=3 (9 shares) → avg = (1 + 27) / 10 = 2.8 → High
            var low  = MakeDef("BOND", RiskLevel.Low);
            var high = MakeDef("GME",  RiskLevel.High);
            var holdings = new List<ActiveInvestment>
            {
                MakeInvestment(low,  1),
                MakeInvestment(high, 9),
            };

            Assert.AreEqual("High Risk", PortfolioPanelLogic.GetPortfolioRiskLabel(holdings));
        }

        [Test]
        public void GetPortfolioRiskLabel_AverageExactly1_5_ReturnsMediumRisk()
        {
            // avg exactly 1.5 → condition is avg < 1.5 → false → Medium Risk
            // Low=1 (1 share), Medium=2 (1 share) → avg = (1+2)/2 = 1.5
            var low  = MakeDef("BOND", RiskLevel.Low);
            var med  = MakeDef("SPY",  RiskLevel.Medium);
            var holdings = new List<ActiveInvestment>
            {
                MakeInvestment(low, 1),
                MakeInvestment(med, 1),
            };

            Assert.AreEqual("Medium Risk", PortfolioPanelLogic.GetPortfolioRiskLabel(holdings));
        }

        [Test]
        public void GetPortfolioRiskLabel_AverageExactly2_5_ReturnsHighRisk()
        {
            // avg exactly 2.5 → condition avg < 2.5 → false → High Risk
            // Medium=2 (1 share), High=3 (1 share) → avg = (2+3)/2 = 2.5
            var med  = MakeDef("SPY", RiskLevel.Medium);
            var high = MakeDef("GME", RiskLevel.High);
            var holdings = new List<ActiveInvestment>
            {
                MakeInvestment(med,  1),
                MakeInvestment(high, 1),
            };

            Assert.AreEqual("High Risk", PortfolioPanelLogic.GetPortfolioRiskLabel(holdings));
        }

        // ─── BuildHoldingsSummary ────────────────────────────────────────

        [Test]
        public void BuildHoldingsSummary_NullList_ReturnsPlaceholder()
        {
            string result = PortfolioPanelLogic.BuildHoldingsSummary(null);
            Assert.IsTrue(result.Contains("No holdings"), $"Expected placeholder, got: '{result}'");
        }

        [Test]
        public void BuildHoldingsSummary_EmptyList_ReturnsPlaceholder()
        {
            string result = PortfolioPanelLogic.BuildHoldingsSummary(new List<ActiveInvestment>());
            Assert.IsTrue(result.Contains("No holdings"), $"Expected placeholder, got: '{result}'");
        }

        [Test]
        public void BuildHoldingsSummary_SingleHolding_FormatsCorrectly()
        {
            var def = MakeDef("AMZN", RiskLevel.High);
            var holdings = new List<ActiveInvestment> { MakeInvestment(def, 3) };

            string result = PortfolioPanelLogic.BuildHoldingsSummary(holdings);

            Assert.IsTrue(result.Contains("AMZN"), $"Missing name. Got: '{result}'");
            Assert.IsTrue(result.Contains("3 shares"), $"Missing share count. Got: '{result}'");
        }

        [Test]
        public void BuildHoldingsSummary_MultipleHoldings_EachOnOwnLine()
        {
            var defA = MakeDef("AMZN", RiskLevel.High);
            var defB = MakeDef("AAPL", RiskLevel.Medium);
            var holdings = new List<ActiveInvestment>
            {
                MakeInvestment(defA, 3),
                MakeInvestment(defB, 7),
            };

            string result = PortfolioPanelLogic.BuildHoldingsSummary(holdings);

            Assert.IsTrue(result.Contains("AMZN"), $"Missing AMZN. Got: '{result}'");
            Assert.IsTrue(result.Contains("3 shares"), $"Missing AMZN count. Got: '{result}'");
            Assert.IsTrue(result.Contains("AAPL"), $"Missing AAPL. Got: '{result}'");
            Assert.IsTrue(result.Contains("7 shares"), $"Missing AAPL count. Got: '{result}'");

            // Multiple lines — split by newline, both names should appear on separate lines
            var lines = result.Split('\n');
            bool amznLine = System.Array.Exists(lines, l => l.Contains("AMZN"));
            bool aaplLine = System.Array.Exists(lines, l => l.Contains("AAPL"));
            Assert.IsTrue(amznLine, "AMZN not on its own line");
            Assert.IsTrue(aaplLine, "AAPL not on its own line");
        }

        [Test]
        public void BuildHoldingsSummary_NoTrailingNewline()
        {
            var def = MakeDef("AMZN", RiskLevel.High);
            var holdings = new List<ActiveInvestment> { MakeInvestment(def, 1) };

            string result = PortfolioPanelLogic.BuildHoldingsSummary(holdings);

            Assert.IsFalse(result.EndsWith("\n"), $"Result ends with newline: '{result}'");
            Assert.IsFalse(result.EndsWith("\r\n"), $"Result ends with CRLF: '{result}'");
        }
    }
}
