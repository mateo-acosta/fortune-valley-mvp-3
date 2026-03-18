using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Managers
{
    /// <summary>
    /// Coordinates the full game flow: Title -> Rules -> Play -> Game Over -> Title.
    /// Communicates with UI panels exclusively through GameEvents.
    /// This class does NOT hold typed references to UI panel classes.
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private GameObject _topFrame;
        [SerializeField] private GameObject _bottomFrame;

        [Header("Game End Panel")]
        [Tooltip("The GameEndPanel root GameObject (activated on game over)")]
        [SerializeField] private GameObject _gameEndPanelObject;

        [Header("Game")]
        [SerializeField] private GameManager _gameManager;

        private void OnEnable()
        {
            GameEvents.OnStartRequested += HandleStartRequested;
            GameEvents.OnCarouselComplete += HandleCarouselComplete;
            GameEvents.OnCountdownComplete += HandleCountdownComplete;
            GameEvents.OnGameEndWithSummary += HandleGameEnd;
            GameEvents.OnRestartRequested += RestartGame;
            GameEvents.OnReturnToTitleRequested += ShowTitleScreen;
        }

        private void OnDisable()
        {
            GameEvents.OnStartRequested -= HandleStartRequested;
            GameEvents.OnCarouselComplete -= HandleCarouselComplete;
            GameEvents.OnCountdownComplete -= HandleCountdownComplete;
            GameEvents.OnGameEndWithSummary -= HandleGameEnd;
            GameEvents.OnRestartRequested -= RestartGame;
            GameEvents.OnReturnToTitleRequested -= ShowTitleScreen;
        }

        private void Start()
        {
            ShowTitleScreen();
        }

        /// <summary>
        /// Show the title screen and hide everything else.
        /// </summary>
        public void ShowTitleScreen()
        {
            // Hide HUD
            GameEvents.RaiseSetHUDVisible(false);
            SetHUDVisible(false);

            // Hide any carousel and game end panel
            GameEvents.RaiseHideRulesCarousel();
            if (_gameEndPanelObject != null)
                _gameEndPanelObject.SetActive(false);

            // Return game state to NotStarted (no OnGameStart fired)
            if (_gameManager != null)
                _gameManager.ReturnToTitle();

            // Show title screen
            GameEvents.RaiseShowTitleScreen();
        }

        private void HandleStartRequested()
        {
            // Title -> Rules
            GameEvents.RaiseHideTitleScreen();
            GameEvents.RaiseShowRulesCarousel();
        }

        private void HandleCarouselComplete()
        {
            // Rules -> Countdown -> Gameplay
            GameEvents.RaiseHideRulesCarousel();
            SetHUDVisible(true);
            GameEvents.RaiseSetHUDVisible(true);
            GameEvents.RaiseStartCountdown();
        }

        private void HandleCountdownComplete()
        {
            // Countdown finished - start the game
            _gameManager?.StartGame();
        }

        private void HandleGameEnd(bool isPlayerWin, GameSummary summary)
        {
            // HUD stays visible so player can see final stats.
            // Activate the game end panel.
            if (_gameEndPanelObject != null)
            {
                _gameEndPanelObject.SetActive(true);
            }
        }

        /// <summary>
        /// Restart the game, skipping title and rules. Countdown plays first.
        /// Called by the "Play Again" button on the game end screen.
        /// </summary>
        public void RestartGame()
        {
            if (_gameEndPanelObject != null)
                _gameEndPanelObject.SetActive(false);

            GameEvents.RaiseStartCountdown();
        }

        private void SetHUDVisible(bool visible)
        {
            if (_topFrame != null)
                _topFrame.SetActive(visible);
            if (_bottomFrame != null)
                _bottomFrame.SetActive(visible);
        }
    }
}
