using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Managers.Tutorial
{
    /// <summary>
    /// Glue between the settings-level Replay Tutorial button and the
    /// tutorial flow. On <see cref="RequestReplay"/> the service wipes the
    /// server-side player state for the current game mode, clears the
    /// local PlayerPrefs fallback flag, and raises
    /// <c>GameEvents.OnTutorialStartRequested</c> so the existing
    /// IntroTutorialController handler takes over. The actual popup
    /// confirmation UI lives in ReplayConfirmPopup; this service runs
    /// AFTER the player has already confirmed.
    /// </summary>
    public class ReplayTutorialService : MonoBehaviour
    {
        [SerializeField] private APIClient _apiClient;
        [SerializeField] private PlayerStateAccessor _playerStateAccessor;

        private IKeyValueStore _keyValueStore;

        public void Initialize(APIClient apiClient, PlayerStateAccessor accessor, IKeyValueStore keyValueStore = null)
        {
            _apiClient = apiClient;
            _playerStateAccessor = accessor;
            _keyValueStore = keyValueStore;
        }

        private void OnEnable()
        {
            // Play Again path: GameOverController raises this while this
            // scene component is still alive (before ClearAllSubscriptions
            // and the scene reload), so we are the listener that performs
            // the actual server wipe.
            GameEvents.OnPlayerStateWipeRequested += HandleWipeRequested;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerStateWipeRequested -= HandleWipeRequested;
        }

        /// <summary>
        /// Execute the full reset: wipe server state, clear local flag,
        /// then tell the tutorial controller to run the intro again.
        /// </summary>
        public void RequestReplay()
        {
            WipeServerAndLocalTutorialFlags();

            // Clear the cached save DTO so HandleGameStart gates fall through
            // and destructive resets run for the replay-tutorial flow.
            GameEvents.LastLoadedSaveDto = null;
            GameEvents.HasSaveBeenRestored = false;
            GameEvents.SaveStateRestoredFromServer = false;

            GameEvents.RaiseTutorialStartRequested(isReplay: true);
        }

        /// <summary>
        /// Full "Play Again" restart handler. Only the server wipe + local
        /// tutorial-flag clear happen here; GameOverController owns the
        /// GameEvents static clears so their ordering relative to the scene
        /// reload stays deterministic, and the scene reload itself re-runs
        /// the intro tutorial (so no RaiseTutorialStartRequested -- this
        /// scene is about to unload).
        /// </summary>
        private void HandleWipeRequested()
        {
            WipeServerAndLocalTutorialFlags();
        }

        /// <summary>
        /// Wipe the server-side player state for the current game mode and
        /// clear the local PlayerPrefs / cached-state tutorial-completed
        /// flags so the next boot treats this as a fresh user (intro tutorial
        /// and goal selection re-run). The old game's full history is kept
        /// server-side in the append-only game_state_snapshots table.
        /// </summary>
        private void WipeServerAndLocalTutorialFlags()
        {
            string gameMode = ResolveGameMode();

            if (_apiClient != null) _apiClient.WipePlayerState(gameMode);

            var store = _keyValueStore ?? PlayerPrefsStore;
            store.SetInt(IntroTutorialController.PlayerPrefsKeyPrefix + gameMode, 0);
            store.Save();

            // Clear the cached state so IntroGate no longer reports it as complete.
            if (_playerStateAccessor != null)
            {
                var state = _playerStateAccessor.Current;
                if (state != null) state.tutorial_completed = false;
            }
        }

        private string ResolveGameMode()
        {
            var state = _playerStateAccessor != null ? _playerStateAccessor.Current : null;
            if (state != null && !string.IsNullOrEmpty(state.game_mode)) return state.game_mode;
            return "homebase";
        }

        private static IKeyValueStore _playerPrefsStore;
        private static IKeyValueStore PlayerPrefsStore =>
            _playerPrefsStore ??= new PlayerPrefsKeyValueStore();
    }
}
