using System.Collections.Generic;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Core
{
    /// <summary>
    /// Pure-logic helpers for PortfolioPanel.
    /// Static class so these can be unit tested without a MonoBehaviour or scene.
    /// </summary>
    public static class PortfolioPanelLogic
    {
        /// <summary>
        /// Weighted-average risk label based on shares held.
        /// Low=1, Medium=2, High=3, weighted by share count.
        /// Entries with 0 shares are skipped (stale).
        /// Thresholds: avg < 1.5 → "Low Risk", 1.5–2.5 → "Medium Risk", > 2.5 → "High Risk".
        /// Returns "No Holdings" when list is empty or all share counts are zero.
        /// </summary>
        public static string GetPortfolioRiskLabel(IReadOnlyList<ActiveInvestment> holdings)
        {
            if (holdings == null || holdings.Count == 0) return "No Holdings";

            float weightedSum = 0f;
            int totalShares = 0;

            foreach (var inv in holdings)
            {
                if (inv.NumberOfShares <= 0) continue; // skip stale zero-share entries

                int score = inv.Definition.RiskLevel switch
                {
                    RiskLevel.Low    => 1,
                    RiskLevel.Medium => 2,
                    RiskLevel.High   => 3,
                    _                => 2
                };
                weightedSum  += inv.NumberOfShares * score;
                totalShares  += inv.NumberOfShares;
            }

            if (totalShares == 0) return "No Holdings";

            float avg = weightedSum / totalShares;
            if (avg < 1.5f) return "Low Risk";
            if (avg < 2.5f) return "Medium Risk";
            return "High Risk";
        }

        /// <summary>
        /// Multi-line holdings summary, one line per position.
        /// Example: "AMZN: 3 shares\nAAPL: 7 shares"
        /// Returns a placeholder message when holdings are empty.
        /// </summary>
        public static string BuildHoldingsSummary(IReadOnlyList<ActiveInvestment> holdings)
        {
            if (holdings == null || holdings.Count == 0)
                return "No holdings yet.\nBuy shares in the Invest tab.";

            var sb = new System.Text.StringBuilder();
            foreach (var inv in holdings)
                sb.AppendLine($"{inv.Definition.DisplayName}: {inv.NumberOfShares} shares @ ${inv.AveragePurchasePrice:F2} avg");

            return sb.ToString().TrimEnd();
        }
    }
}
