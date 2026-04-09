using System.Collections.Generic;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Entities;

namespace FortuneValley.UI.Panels.Credit
{
    /// <summary>
    /// Filters city lots to those available for loan purchase.
    /// Extracted from CreditExploreSubPanel to keep loops and
    /// collection logic out of the MonoBehaviour.
    /// </summary>
    public static class CreditExploreLotFilter
    {
        /// <summary>
        /// Populates results with lots that are unowned and have no active loan.
        /// Replicates the IsActive check from LoanPortfolio.HasLoanOnLot
        /// so the UI layer does not need a cross-layer method call.
        /// </summary>
        public static void FilterAvailableLots(
            IReadOnlyList<CityLotDefinition> allLots,
            IReadOnlyDictionary<string, Owner> ownership,
            IReadOnlyList<ActiveLoan> activeLoans,
            List<CityLotDefinition> results)
        {
            results.Clear();

            for (int i = 0; i < allLots.Count; i++)
            {
                var lot = allLots[i];
                if (lot == null) continue;

                bool isOwned = ownership.TryGetValue(lot.LotId, out Owner owner)
                    && owner != Owner.None;

                if (isOwned) continue;

                bool hasActiveLoan = HasActiveLoanOnLot(activeLoans, lot.LotId);
                if (hasActiveLoan) continue;

                results.Add(lot);
            }
        }

        private static bool HasActiveLoanOnLot(
            IReadOnlyList<ActiveLoan> loans, string lotId)
        {
            for (int i = 0; i < loans.Count; i++)
            {
                if (loans[i].LotId == lotId && loans[i].IsActive)
                    return true;
            }
            return false;
        }
    }
}
