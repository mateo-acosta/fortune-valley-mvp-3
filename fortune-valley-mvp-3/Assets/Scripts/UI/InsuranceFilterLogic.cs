using System.Collections.Generic;
using UnityEngine;
using TMPro;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;

namespace FortuneValley.UI
{
    /// <summary>
    /// Pure-logic helpers for filtering insurance data.
    /// Static class so these can be unit tested without a MonoBehaviour or scene.
    ///
    /// LEARNING DESIGN: Filters help students focus on specific policy types
    /// or lots, making it easier to compare coverage options.
    /// </summary>
    public static class InsuranceFilterLogic
    {
        /// <summary>
        /// Filter active policies by policy type and/or lot.
        /// Both filters are AND-ed. Null means all pass.
        /// </summary>
        public static List<ActiveInsurancePolicy> FilterActivePolicies(
            IReadOnlyList<ActiveInsurancePolicy> all,
            InsurancePolicyType? policyTypeFilter,
            string lotIdFilter)
        {
            if (all == null)
                return new List<ActiveInsurancePolicy>();

            var result = new List<ActiveInsurancePolicy>(all.Count);

            for (int i = 0; i < all.Count; i++)
            {
                var policy = all[i];
                if (!policy.IsActive) continue;

                if (policyTypeFilter.HasValue && policy.PolicyType != policyTypeFilter.Value)
                    continue;

                if (lotIdFilter != null && policy.LotId != lotIdFilter)
                    continue;

                result.Add(policy);
            }

            return result;
        }

        /// <summary>
        /// Filter available policy configs for the Explore tab.
        /// Returns results paired with whether the policy is fully covered
        /// (owned on all player lots).
        /// </summary>
        /// <param name="configs">All available policy configs</param>
        /// <param name="policyTypeFilter">null = all types pass</param>
        /// <param name="coverageStatusFilter">null = all pass</param>
        /// <param name="coverageMap">Pre-computed map of lotId -> owned policy types</param>
        /// <param name="ownedLotIds">List of player-owned lot IDs</param>
        public static List<InsurancePolicyConfigFilterResult> FilterPolicyConfigs(
            IReadOnlyList<InsurancePolicyConfig> configs,
            InsurancePolicyType? policyTypeFilter,
            InsuranceCoverageStatus? coverageStatusFilter,
            Dictionary<string, HashSet<InsurancePolicyType>> coverageMap,
            IReadOnlyList<string> ownedLotIds)
        {
            if (configs == null)
                return new List<InsurancePolicyConfigFilterResult>();

            var result = new List<InsurancePolicyConfigFilterResult>(configs.Count);

            for (int i = 0; i < configs.Count; i++)
            {
                var config = configs[i];

                if (policyTypeFilter.HasValue && config.PolicyType != policyTypeFilter.Value)
                    continue;

                bool isFullyCovered = IsFullyCoveredOnAllLots(
                    config.PolicyType, coverageMap, ownedLotIds);

                if (coverageStatusFilter.HasValue)
                {
                    if (coverageStatusFilter.Value == InsuranceCoverageStatus.Available && isFullyCovered)
                        continue;
                    if (coverageStatusFilter.Value == InsuranceCoverageStatus.FullyCovered && !isFullyCovered)
                        continue;
                }

                result.Add(new InsurancePolicyConfigFilterResult(config, isFullyCovered));
            }

            return result;
        }

        /// <summary>
        /// Filter transaction records by transaction type and/or entity ID (lot).
        /// Both filters are AND-ed. Null means all pass.
        /// </summary>
        public static List<TransactionRecord> FilterInsuranceTransactions(
            List<TransactionRecord> records,
            TransactionType? transactionTypeFilter,
            string entityIdFilter)
        {
            if (records == null)
                return new List<TransactionRecord>();

            var result = new List<TransactionRecord>(records.Count);

            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];

                if (transactionTypeFilter.HasValue && record.Type != transactionTypeFilter.Value)
                    continue;

                if (entityIdFilter != null && record.EntityId != entityIdFilter)
                    continue;

                result.Add(record);
            }

            return result;
        }

        /// <summary>
        /// Build a coverage map from active policies.
        /// Maps lotId -> set of owned policy types for that lot.
        /// Only includes active policies.
        /// </summary>
        public static Dictionary<string, HashSet<InsurancePolicyType>> BuildCoverageMap(
            IReadOnlyList<ActiveInsurancePolicy> policies)
        {
            var map = new Dictionary<string, HashSet<InsurancePolicyType>>();

            if (policies == null) return map;

            for (int i = 0; i < policies.Count; i++)
            {
                var policy = policies[i];
                if (!policy.IsActive) continue;

                if (!map.TryGetValue(policy.LotId, out var types))
                {
                    types = new HashSet<InsurancePolicyType>();
                    map[policy.LotId] = types;
                }

                types.Add(policy.PolicyType);
            }

            return map;
        }

        /// <summary>
        /// Check if a policy type is owned on all player lots.
        /// Returns false if the player owns no lots.
        /// </summary>
        private static bool IsFullyCoveredOnAllLots(
            InsurancePolicyType policyType,
            Dictionary<string, HashSet<InsurancePolicyType>> coverageMap,
            IReadOnlyList<string> ownedLotIds)
        {
            if (ownedLotIds == null || ownedLotIds.Count == 0)
                return false;

            if (coverageMap == null)
                return false;

            for (int i = 0; i < ownedLotIds.Count; i++)
            {
                if (!coverageMap.TryGetValue(ownedLotIds[i], out var types))
                    return false;

                if (!types.Contains(policyType))
                    return false;
            }

            return true;
        }

        // ===============================================================
        // UI HELPERS (moved from InsuranceFilterableSubPanelBase)
        // ===============================================================

        /// <summary>
        /// Populate a TMP_Dropdown with player-owned lots.
        /// First option is always "All Lots". Subsequent options match lot display names.
        /// Returns the mapping from dropdown index to lot ID (index 0 = null = All).
        /// </summary>
        public static void PopulateLotDropdown(
            TMP_Dropdown dropdown,
            IReadOnlyList<CityLotDefinition> allLots,
            IReadOnlyDictionary<string, Owner> ownership,
            out List<string> lotIdMapping)
        {
            lotIdMapping = new List<string> { null }; // index 0 = All

            if (dropdown == null) return;

            var options = new List<TMP_Dropdown.OptionData>
            {
                new TMP_Dropdown.OptionData("All Lots")
            };

            if (allLots != null && ownership != null)
            {
                for (int i = 0; i < allLots.Count; i++)
                {
                    var lot = allLots[i];
                    if (ownership.TryGetValue(lot.LotId, out var owner) && owner == Owner.Player)
                    {
                        options.Add(new TMP_Dropdown.OptionData(lot.DisplayName));
                        lotIdMapping.Add(lot.LotId);
                    }
                }
            }

            // Preserve selection if still valid, otherwise reset to All
            int previousSelection = dropdown.value;
            dropdown.ClearOptions();
            dropdown.AddOptions(options);

            if (previousSelection >= 0 && previousSelection < options.Count)
                dropdown.SetValueWithoutNotify(previousSelection);
            else
                dropdown.SetValueWithoutNotify(0);
        }

        /// <summary>
        /// Build a list of eligible lots for purchasing a specific policy type.
        /// Eligible = player-owned and does not already have this policy type.
        /// </summary>
        public static List<Popups.LotOption> BuildEligibleLots(
            IReadOnlyList<CityLotDefinition> allLots,
            IReadOnlyDictionary<string, Owner> ownership,
            InsurancePolicyType policyType,
            Dictionary<string, HashSet<InsurancePolicyType>> coverageMap)
        {
            var result = new List<Popups.LotOption>();

            if (allLots == null || ownership == null) return result;

            for (int i = 0; i < allLots.Count; i++)
            {
                var lot = allLots[i];
                if (!ownership.TryGetValue(lot.LotId, out var owner) || owner != Owner.Player)
                    continue;

                // Skip lots that already have this policy type
                if (coverageMap != null
                    && coverageMap.TryGetValue(lot.LotId, out var types)
                    && types.Contains(policyType))
                    continue;

                result.Add(new Popups.LotOption(lot.LotId, lot.DisplayName));
            }

            return result;
        }

        /// <summary>
        /// Get the background sprite for a given insurance policy type.
        /// </summary>
        public static Sprite GetBackgroundSprite(
            InsurancePolicyType policyType,
            Sprite generalBackground,
            Sprite nonGeneralBackground)
        {
            return policyType switch
            {
                InsurancePolicyType.GeneralProtection => generalBackground,
                InsurancePolicyType.NonGeneralProtection => nonGeneralBackground,
                _ => null
            };
        }
    }
}
