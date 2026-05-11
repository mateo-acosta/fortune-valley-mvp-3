using System.Collections.Generic;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Interfaces;

namespace FortuneValley.Managers
{
    /// <summary>
    /// Pure C# builder that assembles a GameSummary from service data.
    /// Separated from GameManager so it can be tested without a scene.
    /// </summary>
    public static class GameSummaryBuilder
    {
        /// <summary>
        /// Assemble a complete GameSummary from the current state of all services.
        /// Any null service contributes zero values for its fields.
        /// </summary>
        public static GameSummary Build(
            bool isPlayerWin,
            int daysPlayed,
            ICityService cityManager,
            ICurrencyService currencyManager,
            IInvestmentService investmentSystem,
            IRestaurantService restaurantSystem,
            List<LotPurchaseRecord> lotPurchases = null,
            List<SellTransactionRecord> sellHistory = null,
            GoalScorecard scorecard = null)
        {
            var summary = new GameSummary();

            summary.DaysPlayed = daysPlayed;

            // City data
            if (cityManager != null)
            {
                summary.PlayerLots = cityManager.PlayerLotCount;
                summary.RivalLots = cityManager.RivalLotCount;
                summary.TotalLots = cityManager.TotalLots;
            }

            // Lot purchase history
            if (lotPurchases != null)
            {
                summary.LotPurchases.AddRange(lotPurchases);
                float totalSpent = 0f;
                foreach (var record in lotPurchases)
                    totalSpent += record.Cost;
                summary.TotalSpentOnLots = totalSpent;
            }

            // Financial data
            if (currencyManager != null)
            {
                summary.FinalNetWorth = currencyManager.TotalLiquidBalance;
            }

            if (investmentSystem != null)
            {
                summary.TotalInvestmentGains = investmentSystem.LifetimeTotalGain;
                summary.FinalNetWorth += investmentSystem.TotalPortfolioValue;
                summary.InvestmentCount = investmentSystem.LifetimeTotalInvestmentsMade;
                summary.PeakPortfolioValue = investmentSystem.PeakPortfolioValue;
                summary.TotalPrincipalInvested = investmentSystem.LifetimeTotalPrincipalInvested;
            }

            // Sell history comes from InvestmentSystem.SellHistory (concrete type),
            // not from IInvestmentService. GameManager passes it explicitly.
            if (sellHistory != null)
            {
                summary.SellHistory.AddRange(sellHistory);
            }

            if (restaurantSystem != null)
            {
                summary.TotalRestaurantIncome = restaurantSystem.TotalEarned;
            }

            // Narrative content
            KeyDecisionBuilder.Build(summary, isPlayerWin);
            summary.Headline = LearningReflectionBuilder.BuildHeadline(isPlayerWin, summary);
            summary.InvestmentInsight = LearningReflectionBuilder.BuildInvestmentInsight(summary);
            summary.OpportunityCostInsight = LearningReflectionBuilder.BuildOpportunityCostInsight(summary);
            summary.WhatIfMessage = LearningReflectionBuilder.BuildWhatIfMessage(isPlayerWin, summary);

            // Life Goals scorecard (set by RetirementEvaluator on retirement;
            // null on legacy or non-retirement game-end paths).
            summary.Scorecard = scorecard;

            return summary;
        }
    }
}
