using FortuneValley.Domain.Entities;

namespace FortuneValley.Core
{
    /// <summary>
    /// Generates student-friendly reflection strings for the game-over screen.
    /// Each method produces a short, plain-language insight that ties the player's
    /// decisions to financial concepts (compound interest, opportunity cost, etc.).
    /// </summary>
    public static class LearningReflectionBuilder
    {
        /// <summary>
        /// One-line headline summarizing the player's performance.
        /// </summary>
        public static string BuildHeadline(bool isWin, GameSummary s)
        {
            if (isWin)
            {
                if (s.TotalInvestmentGains > 200)
                    return "Smart Investor!";
                if (s.DaysPlayed < 80)
                    return "Speed Run!";
                return "You Won!";
            }

            if (s.InvestmentCount == 0)
                return "Try Investing Next Time!";
            if (s.RivalLots >= s.TotalLots)
                return "The Rival Was Too Fast!";
            return "Keep Trying!";
        }

        /// <summary>
        /// Insight about compound interest and the player's investment behavior.
        /// </summary>
        public static string BuildInvestmentInsight(GameSummary s)
        {
            if (s.InvestmentCount == 0)
            {
                return "You didn't invest any money this game. " +
                       "Even a safe bond at 5% would have turned $500 into $525 in 30 days. " +
                       "Try investing next time!";
            }

            if (s.TotalInvestmentGains > 0)
            {
                // ROI = gains / principal invested (not peak portfolio, which is a meaningless denominator)
                float returnPct = s.TotalPrincipalInvested > 0
                    ? (s.TotalInvestmentGains / s.TotalPrincipalInvested) * 100f
                    : 0f;

                return $"Your investments earned ${s.TotalInvestmentGains:N0} " +
                       $"(+{returnPct:F0}%) over {s.DaysPlayed} days. " +
                       "That's compound interest at work!";
            }

            return $"Your investments lost ${-s.TotalInvestmentGains:N0}. " +
                   "Higher risk means higher potential loss. " +
                   "Bonds are safer if you want steady growth.";
        }

        /// <summary>
        /// Insight about opportunity cost — what the player gave up with each decision.
        /// </summary>
        public static string BuildOpportunityCostInsight(GameSummary s)
        {
            if (s.LotPurchases.Count > 0)
            {
                // Find the first lot purchased and estimate its income contribution
                var first = s.LotPurchases[0];
                int daysOwned = s.DaysPlayed - first.PurchasedOnDay;
                float estimatedIncome = first.IncomeBonus * daysOwned;

                if (estimatedIncome > 0)
                {
                    return $"You bought {first.LotName} on Day {first.PurchasedOnDay}. " +
                           $"Its income bonus earned you ~${estimatedIncome:N0} over the game.";
                }
            }

            if (s.TotalSpentOnLots > 0)
            {
                return $"You spent ${s.TotalSpentOnLots:N0} on lots. " +
                       "Each dollar spent on a lot is a dollar you couldn't invest.";
            }

            return "You didn't buy any lots. Without lots, you can't win the race!";
        }

        /// <summary>
        /// Counterfactual "what if" message to encourage reflection.
        /// </summary>
        public static string BuildWhatIfMessage(bool isWin, GameSummary s)
        {
            if (isWin && s.InvestmentCount == 0)
            {
                return "What if you had invested some money? " +
                       "You might have won even faster with compound interest on your side.";
            }

            if (!isWin && s.InvestmentCount == 0)
            {
                return "What if you had put $500 into a bond early on? " +
                       "The extra earnings might have helped you buy that next lot before the rival.";
            }

            if (!isWin && s.TotalInvestmentGains > 0)
            {
                return "Your investments were growing! " +
                       "What if you had invested earlier or with a larger amount?";
            }

            if (isWin && s.TotalInvestmentGains > 100)
            {
                return "Your investments paid off! " +
                       "What if you had taken on even more risk — would it have been worth it?";
            }

            return "Every financial decision has a trade-off. " +
                   "Try a different strategy next time!";
        }
    }
}
