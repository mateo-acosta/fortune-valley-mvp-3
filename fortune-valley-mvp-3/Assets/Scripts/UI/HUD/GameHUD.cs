using UnityEngine;
using UnityEngine.UI;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;

namespace FortuneValley.UI.HUD
{
    /// <summary>
    /// Main game HUD controller.
    /// Manages the top bar (account displays, day counter, bot progress)
    /// and bottom bar (navigation buttons).
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // REFERENCES - TOP BAR
        // ═══════════════════════════════════════════════════════════════

        [Header("Account Displays")]
        [SerializeField] private AccountDisplay _checkingDisplay;
        [SerializeField] private AccountDisplay _investingDisplay;

        [Header("Day & Speed")]
        [SerializeField] private DaySpeedDisplay _daySpeedDisplay;

        // ═══════════════════════════════════════════════════════════════
        // REFERENCES - BOTTOM BAR
        // ═══════════════════════════════════════════════════════════════

        [Header("Navigation Buttons")]
        [SerializeField] private Button _portfolioButton;
        [SerializeField] private Button _lotsButton;
        [SerializeField] private Button _transferButton;
        [SerializeField] private Button _restaurantButton;

        [Header("Dependencies")]
        [SerializeField] private UIManager _uiManager;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void OnEnable()
        {
            GameEvents.OnGameStart += HandleGameStart;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= HandleGameStart;
        }

        private void Start()
        {
            if (_uiManager == null) Debug.LogError("[GameHUD] _uiManager not wired in Inspector.");

            SetupButtons();
        }

        private void SetupButtons()
        {
            if (_portfolioButton != null)
            {
                _portfolioButton.onClick.AddListener(OnPortfolioClicked);
            }

            if (_lotsButton != null)
            {
                _lotsButton.onClick.AddListener(OnLotsClicked);
            }

            if (_transferButton != null)
            {
                _transferButton.onClick.AddListener(OnTransferClicked);
            }

            if (_restaurantButton != null)
            {
                _restaurantButton.onClick.AddListener(OnRestaurantClicked);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════════

        private void HandleGameStart()
        {
            // Hide the investing display - show a single unified "Balance" label
            if (_investingDisplay != null)
            {
                _investingDisplay.gameObject.SetActive(false);
            }

            if (_checkingDisplay != null)
            {
                _checkingDisplay.SetLabel("Balance");
            }
            // BotProgressBar self-initializes via GameEvents.OnCityInitialized (raised by CityManager)
        }

        // ═══════════════════════════════════════════════════════════════
        // BUTTON CALLBACKS
        // ═══════════════════════════════════════════════════════════════

        private void OnPortfolioClicked()
        {
            _uiManager.TogglePanel(PanelType.Portfolio);
        }

        private void OnLotsClicked()
        {
            _uiManager.TogglePanel(PanelType.Lots);
        }

        private void OnTransferClicked()
        {
            _uiManager.ShowPopup(PopupType.Transfer);
        }

        private void OnRestaurantClicked()
        {
            _uiManager.TogglePanel(PanelType.Restaurant);
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Initialize the HUD with current game state.
        /// Call after CurrencyManager is initialized.
        /// </summary>
        public void Initialize(float checkingBalance, float investingBalance, int currentDay)
        {
            if (_checkingDisplay != null)
            {
                _checkingDisplay.UpdateBalance(checkingBalance, 0);
            }

            if (_investingDisplay != null)
            {
                _investingDisplay.UpdateBalance(investingBalance, 0);
            }

            if (_daySpeedDisplay != null)
            {
                _daySpeedDisplay.UpdateDay(currentDay);
            }
        }
    }
}
