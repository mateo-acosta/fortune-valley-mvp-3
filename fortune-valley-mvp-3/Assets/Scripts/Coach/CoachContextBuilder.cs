using System.Collections.Generic;
using System.Text;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Core
{
    /// <summary>
    /// Builds a structured context string from the player's game data
    /// for injection into the AI coach's system prompt.
    /// </summary>
    public static class CoachContextBuilder
    {
        /// <summary>
        /// Assemble game data into a labeled context string for the LLM.
        /// All parameters are null-safe.
        /// </summary>
        public static string BuildContext(
            bool isPlayerWin,
            GameSummary summary,
            IReadOnlyList<ActiveInvestment> activeInvestments,
            IReadOnlyList<InvestmentDefinition> availableInvestments)
        {
            var sb = new StringBuilder();

            // ── GAME OUTCOME ──
            sb.AppendLine("=== GAME OUTCOME ===");
            sb.AppendLine(isPlayerWin ? "Result: VICTORY — Player won!" : "Result: DEFEAT — Rival won.");

            if (summary != null)
            {
                sb.AppendLine($"Days Played: {summary.DaysPlayed}");
                sb.AppendLine($"Player Lots: {summary.PlayerLots}/{summary.TotalLots}");
                sb.AppendLine($"Rival Lots: {summary.RivalLots}/{summary.TotalLots}");
            }

            sb.AppendLine();

            // ── FINANCIAL PERFORMANCE ──
            sb.AppendLine("=== FINANCIAL PERFORMANCE ===");
            if (summary != null)
            {
                sb.AppendLine($"Final Net Worth: ${summary.FinalNetWorth:N0}");
                sb.AppendLine($"Total Investment Gains: ${summary.TotalInvestmentGains:N0}");
                sb.AppendLine($"Total Restaurant Income: ${summary.TotalRestaurantIncome:N0}");
                sb.AppendLine($"Total Spent on Lots: ${summary.TotalSpentOnLots:N0}");
                sb.AppendLine($"Peak Portfolio Value: ${summary.PeakPortfolioValue:N0}");
                sb.AppendLine($"Total Principal Invested: ${summary.TotalPrincipalInvested:N0}");
                sb.AppendLine($"Number of Investments Made: {summary.InvestmentCount}");

                // Break down aggregate gains into realized (sells) vs unrealized (still held)
                // so Coach Val can explain discrepancies between sell outcomes and total gains.
                float realizedGains = 0f;
                if (summary.SellHistory != null)
                    foreach (var sell in summary.SellHistory) realizedGains += sell.GainOrLoss;
                sb.AppendLine($"  Realized Gains (sells): ${realizedGains:N0}");
                sb.AppendLine($"  Unrealized Gains (held): ${summary.TotalInvestmentGains - realizedGains:N0}");
            }
            else
            {
                sb.AppendLine("No financial data available.");
            }

            sb.AppendLine();

            // ── PORTFOLIO AT GAME END ──
            sb.AppendLine("=== PORTFOLIO AT GAME END ===");
            if (activeInvestments != null && activeInvestments.Count > 0)
            {
                foreach (var inv in activeInvestments)
                {
                    if (inv == null || inv.Definition == null) continue;

                    string gainSign = inv.TotalGain >= 0 ? "+" : "";
                    sb.AppendLine($"- {inv.Definition.DisplayName}: " +
                                  $"{inv.NumberOfShares} shares, " +
                                  $"Cost Basis: ${inv.TotalCostBasis:N0}, " +
                                  $"Current Value: ${inv.CurrentValue:N0}, " +
                                  $"Gain: {gainSign}${inv.TotalGain:N0} ({inv.PercentageReturn:F1}%), " +
                                  $"Held: {inv.TicksHeld} days (bought Day {inv.CreatedAtTick})");
                }
            }
            else
            {
                sb.AppendLine("Player had no active investments at game end.");
            }

            sb.AppendLine();

            // ── INVESTMENTS SOLD DURING GAME ──
            sb.AppendLine("=== INVESTMENTS SOLD DURING GAME ===");
            if (summary != null && summary.SellHistory != null && summary.SellHistory.Count > 0)
            {
                foreach (var sellRecord in summary.SellHistory)
                {
                    string sign = sellRecord.GainOrLoss >= 0 ? "+" : "";
                    sb.AppendLine($"- Day {sellRecord.SellDay}: Sold {sellRecord.SharesSold} shares of " +
                                  $"{sellRecord.InvestmentName} ({sellRecord.Category}) " +
                                  $"at ${sellRecord.SellPricePerShare:F2}/share — " +
                                  $"{sign}${sellRecord.GainOrLoss:N0} ({sellRecord.PercentageReturn:F1}%)");
                }
            }
            else
            {
                sb.AppendLine("Player did not sell any investments during the game.");
            }

            sb.AppendLine();

            // ── INVESTMENTS YOU NEVER TRIED ──
            // Build exclusion set from BOTH active holdings AND sell history so
            // fully-sold positions are also filtered out.
            sb.AppendLine("=== INVESTMENTS YOU NEVER TRIED ===");
            if (availableInvestments != null && availableInvestments.Count > 0)
            {
                var triedNames = new System.Collections.Generic.HashSet<string>();
                if (activeInvestments != null)
                    foreach (var inv in activeInvestments)
                        if (inv?.Definition != null) triedNames.Add(inv.Definition.DisplayName);
                if (summary?.SellHistory != null)
                    foreach (var sellRecord in summary.SellHistory)
                        if (sellRecord.InvestmentName != null) triedNames.Add(sellRecord.InvestmentName);

                bool anyUntried = false;
                foreach (var def in availableInvestments)
                {
                    if (def == null || triedNames.Contains(def.DisplayName)) continue;
                    anyUntried = true;
                    sb.AppendLine($"- {def.DisplayName} ({def.Category}): " +
                                  $"Risk={def.RiskLevel}, " +
                                  $"Expected Return={def.AnnualReturnRate * 100:F1}%/year, " +
                                  $"Fixed={def.HasFixedReturn}");
                }
                if (!anyUntried) sb.AppendLine("Player tried all available investment types.");
            }
            else
            {
                sb.AppendLine("No investment options data available.");
            }

            sb.AppendLine();

            // ── LOT PURCHASE TIMELINE ──
            sb.AppendLine("=== LOT PURCHASE TIMELINE ===");
            if (summary != null && summary.LotPurchases != null && summary.LotPurchases.Count > 0)
            {
                foreach (var lot in summary.LotPurchases)
                {
                    sb.AppendLine($"- Day {lot.PurchasedOnDay}: Bought \"{lot.LotName}\" " +
                                  $"for ${lot.Cost:N0} (+${lot.IncomeBonus:N0}/day income)");
                }
            }
            else
            {
                sb.AppendLine("No lots were purchased by the player.");
            }

            sb.AppendLine();

            // ── KEY DECISIONS ──
            sb.AppendLine("=== KEY DECISIONS ===");
            if (summary != null && summary.KeyDecisions != null && summary.KeyDecisions.Count > 0)
            {
                foreach (var decision in summary.KeyDecisions)
                {
                    sb.AppendLine($"- {decision ?? ""}");
                }
            }
            else
            {
                sb.AppendLine("No key decisions recorded.");
            }

            sb.AppendLine();

            // ── LEARNING REFLECTIONS ──
            sb.AppendLine("=== LEARNING REFLECTIONS ===");
            if (summary != null)
            {
                sb.AppendLine($"Headline: {summary.Headline ?? ""}");
                sb.AppendLine($"Investment Insight: {summary.InvestmentInsight ?? ""}");
                sb.AppendLine($"Opportunity Cost Insight: {summary.OpportunityCostInsight ?? ""}");
                sb.AppendLine($"What-If: {summary.WhatIfMessage ?? ""}");
            }
            else
            {
                sb.AppendLine("No reflection data available.");
            }

            return sb.ToString();
        }
    }
}
