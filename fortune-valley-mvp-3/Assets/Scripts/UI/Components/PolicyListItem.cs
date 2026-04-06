using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FortuneValley.UI.Components
{
    /// <summary>
    /// A single row in the InsurancePanel list.
    /// Shows a lot's name and its General/Non-General coverage status,
    /// plus a Manage button to open the InsuranceSelectionPopup.
    /// </summary>
    public class PolicyListItem : MonoBehaviour
    {
        [Header("Text Elements")]
        [SerializeField] private TextMeshProUGUI _lotNameText;
        [SerializeField] private TextMeshProUGUI _generalStatusText;
        [SerializeField] private TextMeshProUGUI _nonGeneralStatusText;

        [Header("Colors")]
        [SerializeField] private Color _activeColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _inactiveColor = new Color(0.6f, 0.6f, 0.6f);

        [Header("Actions")]
        [SerializeField] private Button _manageButton;

        private string _lotId;
        private Action<string> _onManageClicked;

        // ===============================================================
        // SETUP
        // ===============================================================

        /// <summary>
        /// Configure the row with lot and policy coverage data.
        /// </summary>
        public void Setup(string lotId, string lotName, bool hasGeneral, bool hasNonGeneral,
                          Action<string> onManageClicked)
        {
            _lotId = lotId;
            _onManageClicked = onManageClicked;

            if (_lotNameText != null)
                _lotNameText.text = lotName;

            UpdateCoverageDisplay(hasGeneral, hasNonGeneral);

            if (_manageButton != null)
            {
                _manageButton.onClick.RemoveAllListeners();
                _manageButton.onClick.AddListener(OnManageClicked);
            }
        }

        /// <summary>
        /// Refresh coverage indicators without rebuilding the row.
        /// </summary>
        public void UpdateCoverage(bool hasGeneral, bool hasNonGeneral)
        {
            UpdateCoverageDisplay(hasGeneral, hasNonGeneral);
        }

        // ===============================================================
        // PRIVATE
        // ===============================================================

        private void UpdateCoverageDisplay(bool hasGeneral, bool hasNonGeneral)
        {
            SetCoverageText(_generalStatusText, "General", hasGeneral);
            SetCoverageText(_nonGeneralStatusText, "Non-General", hasNonGeneral);
        }

        private void SetCoverageText(TextMeshProUGUI label, string policyName, bool isActive)
        {
            if (label == null) return;
            label.text = isActive ? $"{policyName}: Active" : $"{policyName}: None";
            label.color = isActive ? _activeColor : _inactiveColor;
        }

        private void OnManageClicked()
        {
            _onManageClicked?.Invoke(_lotId);
        }

        // ===============================================================
        // ACCESSORS
        // ===============================================================

        public string LotId => _lotId;
    }
}
