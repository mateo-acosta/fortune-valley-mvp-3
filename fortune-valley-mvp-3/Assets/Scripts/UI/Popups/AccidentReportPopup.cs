using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;
using FortuneValley.UI;

namespace FortuneValley.UI.Popups
{
    /// <summary>
    /// Display-only popup showing accident results.
    /// Subscribes to OnAccidentResolved (not OnAccidentOccurred).
    /// InsuranceSystem handles resolution; this popup only displays the outcome.
    ///
    /// LEARNING DESIGN: Shows students exactly what they paid and whether
    /// insurance saved them money, reinforcing the value of coverage.
    /// </summary>
    public class AccidentReportPopup : UIPopup
    {
        // ===============================================================
        // REFERENCES
        // ===============================================================

        [Header("Accident Display")]
        [SerializeField] private TextMeshProUGUI _accidentNameText;
        [SerializeField] private TextMeshProUGUI _damageCostText;
        [SerializeField] private TextMeshProUGUI _coverageStatusText;
        [SerializeField] private TextMeshProUGUI _playerCostText;

        [Header("Actions")]
        [SerializeField] private Button _dismissButton;

        [Header("Dependencies")]
        [SerializeField] private UIManager _uiManager;

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        private void Start()
        {
            if (_dismissButton != null)
                _dismissButton.onClick.AddListener(OnCancelClicked);
        }

        private void OnEnable()
        {
            GameEvents.OnAccidentResolved += HandleAccidentResolved;
        }

        private void OnDisable()
        {
            GameEvents.OnAccidentResolved -= HandleAccidentResolved;
        }

        // ===============================================================
        // EVENT HANDLERS
        // ===============================================================

        private void HandleAccidentResolved(string lotId, string accidentName, float totalDamageCost, bool wasCovered, float playerCost)
        {
            Configure(accidentName, totalDamageCost, wasCovered, playerCost);
            if (_uiManager != null)
                _uiManager.ShowPopup(this);
        }

        // ===============================================================
        // CONFIGURATION
        // ===============================================================

        /// <summary>
        /// Configure the popup with accident resolution data.
        /// Called by the controller before Show().
        /// </summary>
        public void Configure(string accidentName, float damageCost, bool wasCovered, float playerCost)
        {
            if (_accidentNameText != null)
                _accidentNameText.text = accidentName;

            if (_damageCostText != null)
                _damageCostText.text = $"Damage Cost: ${damageCost:N2}";

            if (_coverageStatusText != null)
            {
                _coverageStatusText.text = wasCovered
                    ? "Status: COVERED by insurance"
                    : "Status: NOT COVERED";
            }

            if (_playerCostText != null)
            {
                string label = wasCovered ? "Your Deductible" : "Your Cost (Full)";
                _playerCostText.text = $"{label}: ${playerCost:N2}";
            }
        }
    }
}
