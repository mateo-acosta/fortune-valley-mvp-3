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

        /// <summary>
        /// Execute the full reset: wipe server state, clear local flag,
        /// then tell the tutorial controller to run the intro again.
        /// </summary>
        public void RequestReplay()
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

            GameEvents.RaiseTutorialStartRequested(isReplay: true);
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
