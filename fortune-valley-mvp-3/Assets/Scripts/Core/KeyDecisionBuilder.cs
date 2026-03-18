using System.Linq;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Core
{
    /// <summary>
    /// Generates key decision notes for the game-end recap screen.
    /// Extracted from GameManager as a pure static class so it can be unit-tested
    /// independently of MonoBehaviour lifecycle.
    /// </summary>
    public static class KeyDecisionBuilder
    {
        /// <summary>
        /// Populate summary.KeyDecisions based on the player's financial outcomes.
        /// Called once at game end after all summary fields are populated.
        /// </summary>
        public static void Build(GameSummary summary, bool isPlayerWin)
        {
            // ── Investment outcome ──
            if (summary.TotalInvestmentGains > 500)
            {
                summary.AddKeyDecision($"Your ${summary.TotalInvestmentGains:N0} in investment gains helped you win!");
            }
            else if (summary.TotalInvestmentGains > 100)
            {
                summary.AddKeyDecision($"Compound interest earned you ${summary.TotalInvestmentGains:N0} this game.");
            }
            else if (summary.InvestmentCount > 0 && summary.TotalInvestmentGains <= 0)
            {
                // Bug 2 fix: loss branch was previously missing
                summary.AddKeyDecision(
                    $"Your investments lost ${-summary.TotalInvestmentGains:N0}. Riskier assets can drop — bonds are more predictable.");
            }
            else if (summary.InvestmentCount == 0)
            {
                summary.AddKeyDecision("You didn't use investments — compound interest could have helped!");
            }

            // ── Speed of acquisition ──
            if (isPlayerWin && summary.DaysPlayed < 100)
            {
                summary.AddKeyDecision("Fast victory! Efficient use of resources.");
            }
            else if (!isPlayerWin && summary.DaysPlayed > 200)
            {
                summary.AddKeyDecision("The rival outpaced you over time.");
            }

            // ── Lot ownership ──
            if (summary.PlayerLots > 0 && summary.RivalLots > summary.PlayerLots)
            {
                summary.AddKeyDecision("The rival bought lots faster than you.");
            }

            // ── Best sell (positive signal only; worst sells surface via SellHistory to Coach Val) ──
            if (summary.SellHistory != null && summary.SellHistory.Count > 0)
            {
                var best = summary.SellHistory.OrderByDescending(s => s.GainOrLoss).First();
                if (best.GainOrLoss > 50)
                {
                    summary.AddKeyDecision(
                        $"Best sell: {best.InvestmentName} on Day {best.SellDay} " +
                        $"(+${best.GainOrLoss:N0}, {best.PercentageReturn:F0}%)");
                }
            }
        }
    }
}
