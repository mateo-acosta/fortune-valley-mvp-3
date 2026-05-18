using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Tutorial;

namespace FortuneValley.Managers
{
    /// <summary>
    /// Coordinates the full game flow: Title -> (Tutorial | Rules | Skip) ->
    /// Play -> Game Over -> Title. Communicates with UI panels exclusively
    /// through GameEvents. This class does NOT hold typed references to UI
    /// panel classes.
    ///
    /// The Start click no longer routes directly into the rules carousel;
    /// BootFlowRouter decides whether a first-time tutorial runs, the
    /// normal rules carousel plays, or the player (teacher preview) skips
    /// straight into countdown. Each BootFlow value has its own explicit
    /// handler method so adding future flows is a single switch-case, not
    /// a nested if tree.
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private GameObject _topFrame;
        [SerializeField] private GameObject _bottomFrame;

        [Header("Game")]
        [SerializeField] private GameManager _gameManager;

        [Header("Hydration Barrier")]
        [Tooltip("Bounded dev safety valve. If the server save round-trip has " +
                 "not resolved this many unscaled seconds after the countdown " +
                 "ends, start the game anyway so a permanently-asleep backend " +
                 "cannot soft-lock the game forever.")]
        [SerializeField] private float _saveRoundTripTimeoutSeconds = 20f;

        // Set true once the countdown has finished and we are holding StartGame
        // until the save round-trip resolves (or the timeout elapses).
        private bool _awaitingSaveRoundTrip;
        // Time.unscaledTime deadline after which we release StartGame even if
        // the round-trip never resolved.
        private float _startDeadlineUnscaled;

        private void OnEnable()
        {
            GameEvents.OnBootFlowDecided += HandleBootFlowDecided;
            GameEvents.OnCarouselComplete += HandleCarouselComplete;
            GameEvents.OnCountdownComplete += HandleCountdownComplete;
            GameEvents.OnTutorialComplete += HandleTutorialComplete;
            GameEvents.OnRestartRequested += RestartGame;
            GameEvents.OnReturnToTitleRequested += ShowTitleScreen;
        }

        private void OnDisable()
        {
            GameEvents.OnBootFlowDecided -= HandleBootFlowDecided;
            GameEvents.OnCarouselComplete -= HandleCarouselComplete;
            GameEvents.OnCountdownComplete -= HandleCountdownComplete;
            GameEvents.OnTutorialComplete -= HandleTutorialComplete;
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

            // Hide any carousel. The game-end panel hides itself via its
            // own CanvasGroup and stays active in the scene, so it never
            // misses the game-over event.
            GameEvents.RaiseHideRulesCarousel();

            // Return game state to NotStarted (no OnGameStart fired)
            if (_gameManager != null)
                _gameManager.ReturnToTitle();

            // Show title screen
            GameEvents.RaiseShowTitleScreen();
        }

        // ═══════════════════════════════════════════════════════════════
        // BOOT FLOW ROUTING
        // ═══════════════════════════════════════════════════════════════

        private void HandleBootFlowDecided(BootFlow flow)
        {
            switch (flow)
            {
                case BootFlow.FirstTimeTutorial: HandleFirstTimeTutorialFlow(); return;
                case BootFlow.NormalCarousel:    HandleNormalCarouselFlow();    return;
                case BootFlow.SkipTutorial:      HandleSkipTutorialFlow();      return;
            }
        }

        private void HandleFirstTimeTutorialFlow()
        {
            // Hide the title screen and hand off to the tutorial controller
            // (IntroTutorialController subscribes to OnTutorialStartRequested
            // and drives the scripted sequence; the HUD stays hidden until
            // OnTutorialComplete fires).
            GameEvents.RaiseHideTitleScreen();
            GameEvents.RaiseTutorialStartRequested();
        }

        private void HandleNormalCarouselFlow()
        {
            // Existing returning-player path: Title -> Rules carousel.
            GameEvents.RaiseHideTitleScreen();
            GameEvents.RaiseShowRulesCarousel();
        }

        private void HandleSkipTutorialFlow()
        {
            // Teacher-preview path: skip the rules carousel entirely and go
            // straight to countdown. HUD on, gameplay systems boot next.
            GameEvents.RaiseHideTitleScreen();
            SetHUDVisible(true);
            GameEvents.RaiseSetHUDVisible(true);
            GameEvents.RaiseStartCountdown();
        }

        // ═══════════════════════════════════════════════════════════════
        // POST-FLOW STAGES (unchanged)
        // ═══════════════════════════════════════════════════════════════

        private void HandleCarouselComplete()
        {
            // Rules -> Countdown -> Gameplay
            GameEvents.RaiseHideRulesCarousel();
            SetHUDVisible(true);
            GameEvents.RaiseSetHUDVisible(true);
            GameEvents.RaiseStartCountdown();
        }

        private void HandleTutorialComplete()
        {
            // Tutorial finished: resume the normal countdown path so
            // gameplay systems boot through the same entry point.
            SetHUDVisible(true);
            GameEvents.RaiseSetHUDVisible(true);
            GameEvents.RaiseStartCountdown();
        }

        private void HandleCountdownComplete()
        {
            // Start barrier: do NOT run the destructive OnGameStart path until
            // the server save round-trip has resolved. On a cold dev boot the
            // GET /api/game/state is slow or fails while the Fly machine and
            // Postgres wake; without this wait OnGameStart wins the fixed
            // countdown, every system resets to fresh defaults, and the
            // autosave then overwrites the real server row permanently.
            if (GameEvents.SaveRoundTripResolved)
            {
                ReleaseStart();
                return;
            }

            _awaitingSaveRoundTrip = true;
            _startDeadlineUnscaled = Time.unscaledTime + _saveRoundTripTimeoutSeconds;
        }

        private void Update()
        {
            if (!_awaitingSaveRoundTrip)
                return;

            if (GameEvents.SaveRoundTripResolved || Time.unscaledTime >= _startDeadlineUnscaled)
            {
                _awaitingSaveRoundTrip = false;
                ReleaseStart();
            }
        }

        private void ReleaseStart()
        {
            // Latch BEFORE StartGame so AutoSaveController sees the round-trip
            // as resolved before the first OnTick can drive a save.
            GameEvents.StartBarrierReleased = true;
            _gameManager?.StartGame();
        }

        /// <summary>
        /// Restart the game, skipping title and rules. Countdown plays first.
        /// Called by the "Play Again" button on the game end screen.
        /// </summary>
        public void RestartGame()
        {
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
