using System.Collections.Generic;
using NUnit.Framework;
using FortuneValley.Domain.Entities;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class KeyDecisionBuilderTests
    {
        // ═══════════════════════════════════════════════════════════════
        // HELPER
        // ═══════════════════════════════════════════════════════════════

        private GameSummary MakeSummary(
            int investCount = 0,
            float totalGains = 0f,
            int daysPlayed = 100,
            int playerLots = 3,
            int rivalLots = 2,
            List<SellTransactionRecord> sellHistory = null)
        {
            var s = new GameSummary
            {
                InvestmentCount = investCount,
                TotalInvestmentGains = totalGains,
                DaysPlayed = daysPlayed,
                PlayerLots = playerLots,
                RivalLots = rivalLots,
                TotalLots = 7
            };
            s.SellHistory = sellHistory ?? new List<SellTransactionRecord>();
            return s;
        }

        // ═══════════════════════════════════════════════════════════════
        // TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void NoInvestments_AddsNoInvestmentNote()
        {
            var s = MakeSummary(investCount: 0, totalGains: 0f);
            KeyDecisionBuilder.Build(s, true);
            Assert.IsTrue(s.KeyDecisions.Exists(d => d.Contains("didn't use investments")));
        }

        [Test]
        public void InvestmentGainsAbove500_AddsAmountInNote()
        {
            var s = MakeSummary(investCount: 3, totalGains: 600f);
            KeyDecisionBuilder.Build(s, true);
            Assert.IsTrue(s.KeyDecisions.Exists(d => d.Contains("$600") && d.Contains("investment gains")));
        }

        /// <summary>
        /// Verifies Bug 2 fix: an investment loss branch was previously missing entirely.
        /// </summary>
        [Test]
        public void InvestmentLoss_AddsLossNote()
        {
            var s = MakeSummary(investCount: 2, totalGains: -150f);
            KeyDecisionBuilder.Build(s, true);
            Assert.IsTrue(s.KeyDecisions.Exists(d => d.Contains("lost") && d.Contains("$150")));
        }

        [Test]
        public void FastVictory_AddsSpeedNote()
        {
            var s = MakeSummary(investCount: 0, daysPlayed: 80);
            KeyDecisionBuilder.Build(s, true);
            Assert.IsTrue(s.KeyDecisions.Exists(d => d.Contains("Fast victory")));
        }

        [Test]
        public void RivalOwnsMore_AddsRivalNote()
        {
            var s = MakeSummary(playerLots: 2, rivalLots: 5);
            KeyDecisionBuilder.Build(s, false);
            Assert.IsTrue(s.KeyDecisions.Exists(d => d.Contains("rival bought lots")));
        }

        /// <summary>
        /// Verifies Area 3 Step 4: best sell note references the investment name and day.
        /// </summary>
        [Test]
        public void BestSellAbove50_AddsBestSellNote()
        {
            var sells = new List<SellTransactionRecord>
            {
                new SellTransactionRecord
                {
                    InvestmentName    = "TechCorp",
                    Category          = "Stock",
                    SharesSold        = 5,
                    SellDay           = 42,
                    SellPricePerShare = 120f,
                    CostBasisPerShare = 100f,
                    GainOrLoss        = 100f,
                    PercentageReturn  = 20f
                }
            };
            var s = MakeSummary(investCount: 1, sellHistory: sells);
            KeyDecisionBuilder.Build(s, true);
            Assert.IsTrue(s.KeyDecisions.Exists(d => d.Contains("TechCorp") && d.Contains("Day 42")));
        }

        [Test]
        public void NoSellHistory_NoBestSellNote()
        {
            // Gains > 100 so the compound-interest branch fires; sell history is empty
            var s = MakeSummary(investCount: 1, totalGains: 200f);
            KeyDecisionBuilder.Build(s, true);
            Assert.IsFalse(s.KeyDecisions.Exists(d => d.Contains("Best sell")));
        }
    }
}
