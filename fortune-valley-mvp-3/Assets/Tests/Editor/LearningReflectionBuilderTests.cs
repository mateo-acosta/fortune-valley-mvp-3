using System.Collections.Generic;
using NUnit.Framework;
using FortuneValley.Domain.Entities;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class LearningReflectionBuilderTests
    {
        // ═══════════════════════════════════════════════════════════════
        // HELPER
        // ═══════════════════════════════════════════════════════════════

        private GameSummary MakeSummary(
            bool investGains = false, float gains = 0f,
            int investCount = 0, int daysPlayed = 100,
            int playerLots = 3, int rivalLots = 2, int totalLots = 6,
            float peakPortfolio = 0f, float spentOnLots = 0f,
            float totalPrincipalInvested = 0f,
            List<LotPurchaseRecord> lotPurchases = null)
        {
            return new GameSummary
            {
                TotalInvestmentGains = gains,
                InvestmentCount = investCount,
                DaysPlayed = daysPlayed,
                PlayerLots = playerLots,
                RivalLots = rivalLots,
                TotalLots = totalLots,
                PeakPortfolioValue = peakPortfolio,
                TotalSpentOnLots = spentOnLots,
                TotalPrincipalInvested = totalPrincipalInvested,
                LotPurchases = lotPurchases ?? new List<LotPurchaseRecord>()
            };
        }

        // ═══════════════════════════════════════════════════════════════
        // BuildHeadline
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void BuildHeadline_WinBigGains_ReturnsSmartInvestor()
        {
            var s = MakeSummary(gains: 500, investCount: 3);
            Assert.AreEqual("Smart Investor!", LearningReflectionBuilder.BuildHeadline(true, s));
        }

        [Test]
        public void BuildHeadline_WinFast_ReturnsSpeedRun()
        {
            var s = MakeSummary(gains: 50, daysPlayed: 60);
            Assert.AreEqual("Speed Run!", LearningReflectionBuilder.BuildHeadline(true, s));
        }

        [Test]
        public void BuildHeadline_WinDefault_ReturnsYouWon()
        {
            var s = MakeSummary(gains: 50, daysPlayed: 150);
            Assert.AreEqual("You Won!", LearningReflectionBuilder.BuildHeadline(true, s));
        }

        [Test]
        public void BuildHeadline_LossNoInvestments_ReturnsTryInvesting()
        {
            var s = MakeSummary(investCount: 0);
            Assert.AreEqual("Try Investing Next Time!", LearningReflectionBuilder.BuildHeadline(false, s));
        }

        [Test]
        public void BuildHeadline_LossRivalAllLots_ReturnsRivalTooFast()
        {
            var s = MakeSummary(investCount: 2, rivalLots: 6, totalLots: 6);
            Assert.AreEqual("The Rival Was Too Fast!", LearningReflectionBuilder.BuildHeadline(false, s));
        }

        [Test]
        public void BuildHeadline_LossDefault_ReturnsKeepTrying()
        {
            var s = MakeSummary(investCount: 1, rivalLots: 4, totalLots: 6);
            Assert.AreEqual("Keep Trying!", LearningReflectionBuilder.BuildHeadline(false, s));
        }

        // ═══════════════════════════════════════════════════════════════
        // BuildInvestmentInsight
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void BuildInvestmentInsight_NoInvestments_SuggestsInvesting()
        {
            var s = MakeSummary(investCount: 0);
            string result = LearningReflectionBuilder.BuildInvestmentInsight(s);
            Assert.IsTrue(result.Contains("didn't invest"));
            Assert.IsTrue(result.Contains("Try investing"));
        }

        [Test]
        public void BuildInvestmentInsight_PositiveGains_ShowsCompoundInterest()
        {
            // gains=200, principal=1000 → ROI = 20%
            var s = MakeSummary(gains: 200, peakPortfolio: 1200, daysPlayed: 90, investCount: 2,
                                totalPrincipalInvested: 1000f);
            string result = LearningReflectionBuilder.BuildInvestmentInsight(s);
            Assert.IsTrue(result.Contains("$200"));
            Assert.IsTrue(result.Contains("compound interest"));
            Assert.IsTrue(result.Contains("20"), "ROI % should be 20 (200/1000 * 100)");
        }

        [Test]
        public void BuildInvestmentInsight_NegativeGains_MentionsRisk()
        {
            var s = MakeSummary(gains: -100, investCount: 2);
            string result = LearningReflectionBuilder.BuildInvestmentInsight(s);
            Assert.IsTrue(result.Contains("lost"));
            Assert.IsTrue(result.Contains("risk"));
        }

        // ═══════════════════════════════════════════════════════════════
        // BuildOpportunityCostInsight
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void BuildOpportunityCostInsight_WithLotPurchases_ShowsIncomeEstimate()
        {
            var lots = new List<LotPurchaseRecord>
            {
                new LotPurchaseRecord
                {
                    LotId = "lot1", LotName = "Downtown", Cost = 500,
                    IncomeBonus = 10, PurchasedOnDay = 20
                }
            };
            var s = MakeSummary(daysPlayed: 100, lotPurchases: lots, spentOnLots: 500);
            string result = LearningReflectionBuilder.BuildOpportunityCostInsight(s);
            // 80 days * 10 = $800 estimated income
            Assert.IsTrue(result.Contains("Downtown"));
            Assert.IsTrue(result.Contains("Day 20"));
        }

        [Test]
        public void BuildOpportunityCostInsight_LotsSpentNoRecords_MentionsTradeoff()
        {
            var s = MakeSummary(spentOnLots: 1000);
            string result = LearningReflectionBuilder.BuildOpportunityCostInsight(s);
            Assert.IsTrue(result.Contains("$1,000"));
            Assert.IsTrue(result.Contains("couldn't invest"));
        }

        [Test]
        public void BuildOpportunityCostInsight_NoLots_WarnsNeedLots()
        {
            var s = MakeSummary(spentOnLots: 0);
            string result = LearningReflectionBuilder.BuildOpportunityCostInsight(s);
            Assert.IsTrue(result.Contains("didn't buy any lots"));
        }

        // ═══════════════════════════════════════════════════════════════
        // BuildWhatIfMessage
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void BuildWhatIfMessage_WinNoInvest_SuggestsInvesting()
        {
            var s = MakeSummary(investCount: 0);
            string result = LearningReflectionBuilder.BuildWhatIfMessage(true, s);
            Assert.IsTrue(result.Contains("invested"));
            Assert.IsTrue(result.Contains("compound interest"));
        }

        [Test]
        public void BuildWhatIfMessage_LossNoInvest_SuggestsBond()
        {
            var s = MakeSummary(investCount: 0);
            string result = LearningReflectionBuilder.BuildWhatIfMessage(false, s);
            Assert.IsTrue(result.Contains("bond"));
        }

        [Test]
        public void BuildWhatIfMessage_LossWithGains_EncouragesMore()
        {
            var s = MakeSummary(gains: 100, investCount: 2);
            string result = LearningReflectionBuilder.BuildWhatIfMessage(false, s);
            Assert.IsTrue(result.Contains("growing"));
            Assert.IsTrue(result.Contains("earlier"));
        }

        [Test]
        public void BuildWhatIfMessage_WinBigGains_AsksAboutRisk()
        {
            var s = MakeSummary(gains: 200, investCount: 3);
            string result = LearningReflectionBuilder.BuildWhatIfMessage(true, s);
            Assert.IsTrue(result.Contains("risk"));
        }

        [Test]
        public void BuildWhatIfMessage_Default_MentionsTradeOff()
        {
            // Win with small gains, invested — hits the default branch
            var s = MakeSummary(gains: 10, investCount: 1);
            string result = LearningReflectionBuilder.BuildWhatIfMessage(true, s);
            Assert.IsTrue(result.Contains("trade-off"));
        }
    }
}
