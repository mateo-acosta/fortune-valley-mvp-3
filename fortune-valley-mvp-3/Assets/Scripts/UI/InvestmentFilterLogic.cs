using System.Collections.Generic;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;

namespace FortuneValley.UI
{
    /// <summary>
    /// Pure-logic helpers for filtering investments by category and industry.
    /// Static class so these can be unit tested without a MonoBehaviour or scene.
    ///
    /// LEARNING DESIGN: Filters help students compare investments within
    /// a category or industry, reinforcing risk vs return concepts.
    /// </summary>
    public static class InvestmentFilterLogic
    {
        /// <summary>
        /// Filter available investment definitions by category and/or industry.
        /// Both filters are AND-ed. Industry only applies to stocks;
        /// non-stock items always pass the industry filter.
        /// </summary>
        /// <param name="all">All available investment definitions</param>
        /// <param name="categoryFilter">null = all categories pass</param>
        /// <param name="industryFilter">null = all pass; non-null = stocks must match, non-stocks pass</param>
        public static List<InvestmentDefinition> FilterDefinitions(
            IReadOnlyList<InvestmentDefinition> all,
            InvestmentCategory? categoryFilter,
            Industry? industryFilter)
        {
            if (all == null)
                return new List<InvestmentDefinition>();

            var result = new List<InvestmentDefinition>(all.Count);

            for (int i = 0; i < all.Count; i++)
            {
                var def = all[i];

                if (!PassesFilters(def.Category, def.Industry, categoryFilter, industryFilter))
                    continue;

                result.Add(def);
            }

            return result;
        }

        /// <summary>
        /// Filter active (held) investments by category and/or industry.
        /// Same logic as FilterDefinitions but reads through ActiveInvestment.Definition.
        /// </summary>
        public static List<ActiveInvestment> FilterActiveInvestments(
            IReadOnlyList<ActiveInvestment> all,
            InvestmentCategory? categoryFilter,
            Industry? industryFilter)
        {
            if (all == null)
                return new List<ActiveInvestment>();

            var result = new List<ActiveInvestment>(all.Count);

            for (int i = 0; i < all.Count; i++)
            {
                var inv = all[i];
                if (inv.Definition == null) continue;

                if (!PassesFilters(
                    inv.Definition.Category, inv.Definition.Industry,
                    categoryFilter, industryFilter))
                    continue;

                result.Add(inv);
            }

            return result;
        }

        /// <summary>
        /// Core filter predicate shared by both methods.
        /// </summary>
        private static bool PassesFilters(
            InvestmentCategory itemCategory,
            Industry itemIndustry,
            InvestmentCategory? categoryFilter,
            Industry? industryFilter)
        {
            // Category filter: null means all pass
            if (categoryFilter.HasValue && itemCategory != categoryFilter.Value)
                return false;

            // Industry filter: only applies to stocks
            if (industryFilter.HasValue && itemCategory == InvestmentCategory.Stock)
            {
                if (itemIndustry != industryFilter.Value)
                    return false;
            }

            return true;
        }
    }
}
