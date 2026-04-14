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
                _insuranceTabButton.onClick.AddListener(OnInsuranceTabClicked);
            }

            if (_creditTabButton != null)
            {
                _creditTabButton.onClick.AddListener(OnCreditTabClicked);
            }

            if (_questionMasterButton != null)
            {
                _questionMasterButton.onClick.AddListener(OnQuestionMasterClicked);
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
            _uiManager.ShowPopup(PopupType.Questions);
        }
    }
}
