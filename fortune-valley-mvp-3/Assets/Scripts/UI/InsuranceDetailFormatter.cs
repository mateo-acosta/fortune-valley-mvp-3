using System.Collections.Generic;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;

namespace FortuneValley.UI
{
    /// <summary>
    /// Pure-logic helpers for formatting insurance detail popup content.
    /// Extracted from InsuranceDetailPopup to keep the MonoBehaviour thin
    /// and to make comparison logic unit-testable.
    ///
    /// LEARNING DESIGN: The coverage comparison ("you paid X, with insurance
    /// you would have paid Y") is the strongest learning signal in the
    /// insurance panel. This logic must be correct.
    /// </summary>
    public static class InsuranceDetailFormatter
    {
        /// <summary>
        /// Format owned policy details for the Home tab detail modal.
        /// </summary>
        public static OwnedPolicyDetails FormatOwnedPolicy(
            ActiveInsurancePolicy policy, string lotDisplayName)
        {
            var coveredList = new List<string>();
            if (policy.CoveredAccidentIds != null)
            {
                for (int i = 0; i < policy.CoveredAccidentIds.Count; i++)
                    coveredList.Add(policy.CoveredAccidentIds[i]);
            }

            return new OwnedPolicyDetails(
                policy.PolicyId,
                policy.PolicyType.ToString(),
                lotDisplayName,
                $"${policy.MonthlyPremium:N2}/mo",
                $"${policy.Deductible:N2}",
                $"{(int)System.Math.Round(policy.CoveragePercent * 100.0)}%",
                $"${policy.TotalPremiumsPaid:N2}",
                policy.IsPastDue ? "Past Due" : "Active",
                coveredList);
        }

        /// <summary>
        /// Format available policy details for the Explore tab detail modal.
        /// </summary>
        public static AvailablePolicyDetails FormatAvailablePolicy(InsurancePolicyConfig config)
        {
            var coveredNames = new List<string>();
            if (config.CoveredAccidents != null)
            {
                for (int i = 0; i < config.CoveredAccidents.Count; i++)
                {
                    if (config.CoveredAccidents[i] != null)
                        coveredNames.Add(config.CoveredAccidents[i].DisplayName);
                }
            }

            return new AvailablePolicyDetails(
                config.DisplayName,
                config.PolicyType.ToString(),
                $"${config.MonthlyPremium:N2}/mo",
                $"${config.Deductible:N2}",
                $"{(int)System.Math.Round(config.CoveragePercent * 100.0)}%",
                coveredNames);
        }

        /// <summary>
        /// Format transaction details for the History tab detail modal.
        /// </summary>
        public static InsuranceTransactionDetails FormatTransaction(TransactionRecord record)
        {
            return new InsuranceTransactionDetails(
                FormatTransactionType(record.Type),
                record.EntityId ?? "N/A",
                record.Amount > 0f ? $"${record.Amount:N2}" : "N/A",
                record.Description);
        }

        /// <summary>
        /// Calculate the best available coverage comparison for an accident.
        /// Shows what the player paid vs what they would have paid with
        /// the best available policy. Shown for both insured and uninsured
        /// accidents to reinforce learning.
        /// </summary>
        public static CoverageComparison CalculateBestCoverageComparison(
            float damageCost,
            float playerCost,
            bool wasCovered,
            IReadOnlyList<InsurancePolicyConfig> configs)
        {
            if (configs == null || configs.Count == 0)
            {
                return new CoverageComparison(
                    $"${playerCost:N2}", $"${damageCost:N2}",
                    wasCovered, false, null, null, null, null);
            }

            // Find the policy with the lowest deductible (best for player)
            float bestDeductible = float.MaxValue;
            string bestPolicyName = null;

            for (int i = 0; i < configs.Count; i++)
            {
                var config = configs[i];
                if (config.Deductible < bestDeductible)
                {
                    bestDeductible = config.Deductible;
                    bestPolicyName = config.DisplayName;
                }
            }

            if (bestPolicyName == null)
            {
                return new CoverageComparison(
                    $"${playerCost:N2}", $"${damageCost:N2}",
                    wasCovered, false, null, null, null, null);
            }

            // Player pays deductible capped at damage cost
            float wouldHavePaid = bestDeductible < damageCost ? bestDeductible : damageCost;

            string comparisonText;
            if (wasCovered)
            {
                comparisonText =
                    $"You paid ${playerCost:N2} deductible. Without insurance, you would have paid ${damageCost:N2}.";
            }
            else
            {
                comparisonText =
                    $"You paid ${playerCost:N2}. With {bestPolicyName}, you would have paid only ${wouldHavePaid:N2}.";
            }

            return new CoverageComparison(
                $"${playerCost:N2}", $"${damageCost:N2}",
                wasCovered, true,
                bestPolicyName, $"${bestDeductible:N2}",
                $"${wouldHavePaid:N2}", comparisonText);
        }

        private static string FormatTransactionType(TransactionType type)
        {
            return type switch
            {
                TransactionType.InsurancePurchased => "Purchase",
                TransactionType.InsuranceCanceled => "Cancellation",
                TransactionType.AccidentResolved => "Accident",
                TransactionType.PremiumCharged => "Premium Charge",
                _ => type.ToString()
            };
        }
    }
}
