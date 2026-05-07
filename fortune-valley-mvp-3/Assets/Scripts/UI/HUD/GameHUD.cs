using UnityEngine;
using UnityEngine.UI;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;

namespace FortuneValley.UI.HUD
{
    /// <summary>
    /// Bottom-bar navigation controller for the Homebase HUD.
    /// Wires tab buttons to UIManager panel/popup toggles.
    /// Account balance displays (Checking / Investing / Credit) are NOT managed here —
    /// each AccountDisplay self-subscribes to its GameEvents balance event.
    /// DaySpeedDisplay and BotProgressBar likewise self-initialize.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [Header("Financial System Tabs")]
        [Tooltip("Investing tab button (opens Portfolio panel)")]
        [SerializeField] private Button _investingTabButton;
        [Tooltip("Insurance tab button (opens Insurance panel)")]
        [SerializeField] private Button _insuranceTabButton;
        [Tooltip("Credit tab button (opens Loan panel)")]
        [SerializeField] private Button _creditTabButton;

        [Header("QuestionMaster")]
        [Tooltip("Opens the QuestionMaster popup")]
        [SerializeField] private Button _questionMasterButton;

        [Header("Player Profile")]
        [Tooltip("Player avatar / profile button. Opens the read-only PlayerProfile panel.")]
        [SerializeField] private Button _profileButton;

        [Header("Dependencies")]
        [Tooltip("HomebaseSceneManager. Required — receives TogglePanel / ShowPopup calls from tab buttons.")]
        [SerializeField] private UIManager _uiManager;

        private void Start()
        {
            if (_uiManager == null) Debug.LogError("[GameHUD] _uiManager not wired in Inspector.");

            SetupButtons();
        }

        private void SetupButtons()
        {
            if (_investingTabButton != null)
            {
                _investingTabButton.onClick.AddListener(OnInvestingTabClicked);
            }

            if (_insuranceTabButton != null)
            {
                // POC: insurance disabled. Force the tab hidden in code so a
                // reverted scene/prefab edit can't re-expose it.
                if (!FeatureFlags.InsuranceEnabled)
                {
                    _insuranceTabButton.gameObject.SetActive(false);
                }
                else
                {
                    _insuranceTabButton.onClick.AddListener(OnInsuranceTabClicked);
                }
            }

            if (_creditTabButton != null)
            {
                _creditTabButton.onClick.AddListener(OnCreditTabClicked);
            }

            if (_questionMasterButton != null)
            {
                _questionMasterButton.onClick.AddListener(OnQuestionMasterClicked);
            }

            if (_profileButton != null)
            {
                _profileButton.onClick.AddListener(OnProfileButtonClicked);
            }
        }

        private void OnInvestingTabClicked()
        {
            _uiManager.TogglePanel(PanelType.Portfolio);
        }

        private void OnInsuranceTabClicked()
        {
            _uiManager.TogglePanel(PanelType.Insurance);
        }

        private void OnCreditTabClicked()
        {
            _uiManager.TogglePanel(PanelType.Loan);
        }

        private void OnQuestionMasterClicked()
        {
            // Route through the web-bridge panel pathway (matches Investing/Credit).
            // UIManager.GetWebBridge falls through to null when the bridge isn't wired,
            // so a missing bridge just opens nothing -- no crash, no legacy popup.
            _uiManager.TogglePanel(PanelType.QuestionMaster);
        }

        private void OnProfileButtonClicked()
        {
            _uiManager.TogglePanel(PanelType.Profile);
        }
    }
}
