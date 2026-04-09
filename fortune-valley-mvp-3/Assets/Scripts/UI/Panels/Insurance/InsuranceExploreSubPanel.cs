using System.Collections.Generic;
using UnityEngine;
using TMPro;
using FortuneValley.Core;

namespace FortuneValley.UI.Panels.Insurance
{
    /// <summary>
    /// Insurance Explore tab: browse available insurance policy types.
    /// Shows coverage details, monthly premiums, and deductibles.
    ///
    /// LEARNING DESIGN: Students compare policy costs vs coverage amounts,
    /// learning to evaluate insurance as a risk management tool.
    /// </summary>
    public class InsuranceExploreSubPanel : SubPanelBase
    {
        // ===============================================================
        // REFERENCES
        // ===============================================================

        [Header("Dependencies")]
        [SerializeField] private InsuranceSystem _insuranceSystem;

        [Header("Card List")]
        [SerializeField] private Transform _cardContainer;
        [SerializeField] private GameObject _insuranceCardPrefab;

        // ===============================================================
        // STATE
        // ===============================================================

        private List<GameObject> _cardInstances = new List<GameObject>();

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        protected override void OnEnable()
        {
            // No dynamic events needed; policy configs don't change at runtime.
            // Refresh on enable is sufficient.
            base.OnEnable();
        }

        // ===============================================================
        // REFRESH
        // ===============================================================

        protected override void Refresh()
        {
            ClearCards();

            if (_insuranceSystem == null) return;
            if (_insuranceCardPrefab == null || _cardContainer == null) return;

            // Property read: available policy configs
            var policies = _insuranceSystem.AvailablePolicies;
            if (policies == null) return;

            for (int i = 0; i < policies.Count; i++)
            {
                SpawnPolicyCard(policies[i]);
            }
        }

        private void SpawnPolicyCard(InsurancePolicyConfig config)
        {
            var card = Instantiate(_insuranceCardPrefab, _cardContainer);
            _cardInstances.Add(card);

            var texts = card.GetComponentsInChildren<TextMeshProUGUI>(true);

            // Expected card layout: Name, Type, Premium, Deductible, Coverage
            if (texts.Length > 0) texts[0].text = config.DisplayName;
            if (texts.Length > 1) texts[1].text = $"Type: {config.PolicyType}";
            if (texts.Length > 2) texts[2].text = $"Premium: ${config.MonthlyPremium:N2}/mo";
            if (texts.Length > 3) texts[3].text = $"Deductible: ${config.Deductible:N2}";
            if (texts.Length > 4) texts[4].text = $"Coverage: {config.CoveragePercent:P0}";
        }

        private void ClearCards()
        {
            for (int i = 0; i < _cardInstances.Count; i++)
            {
                if (_cardInstances[i] != null)
                    Destroy(_cardInstances[i]);
            }
            _cardInstances.Clear();
        }
    }
}
