using System.Collections.Generic;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;

namespace FortuneValley.UI
{
    /// <summary>
    /// Pure-logic helpers for calculating insurance summary statistics.
    /// Static class for unit testability.
    ///
    /// LEARNING DESIGN: Summary stats help students see the cumulative
    /// impact of their insurance decisions at a glance.
    /// </summary>
    public static class InsuranceSummaryCalculator
    {
        /// <summary>
        /// Calculate summary stats for the Home tab.
        /// Expects only active policies (caller filters beforehand).
        /// </summary>
        public static void CalculateHomeSummary(
            IReadOnlyList<ActiveInsurancePolicy> activePolicies,
            out int activeCount,
            out float totalMonthlyPremium)
        {
            activeCount = 0;
            totalMonthlyPremium = 0f;

            if (activePolicies == null) return;

            for (int i = 0; i < activePolicies.Count; i++)
            {
                if (!activePolicies[i].IsActive) continue;
                activeCount++;
                totalMonthlyPremium += activePolicies[i].MonthlyPremium;
            }
        }

        /// <summary>
        /// Calculate summary stats for the History tab.
        /// Expects pre-filtered insurance transaction records only.
        /// Non-insurance record types are ignored if present.
        /// </summary>
        public static void CalculateHistorySummary(
            List<TransactionRecord> records,
            out float totalAccidentCosts,
            out float totalPremiumsPaid,
            out int accidentCount)
        {
            totalAccidentCosts = 0f;
            totalPremiumsPaid = 0f;
            accidentCount = 0;

            if (records == null) return;

            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];

                switch (record.Type)
                {
                    case TransactionType.AccidentResolved:
                        totalAccidentCosts += record.Amount;
                        accidentCount++;
                        break;
                    case TransactionType.PremiumCharged:
                        totalPremiumsPaid += record.Amount;
                        break;
                }
            }
        }
    }
}
