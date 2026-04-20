using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Orchestrates the server-side session lifecycle from Unity. Subscribes to
    /// GameEvents.OnGameStart so empty scene loads do not create orphan session
    /// rows, and to GameEvents.OnGameEnd to tear down deterministically in order:
    /// final state save, decision flush, session end.
    ///
    /// Session id flow: when Unity calls Bridge.StartSession(gameMode), the Rails
    /// side (window.FV.startSession) posts /api/game/session/start, caches the
    /// session_id, and SendMessages it back to the "DecisionLogger" GameObject's
    /// SetSessionId method. EndSession here passes an empty string so Rails uses
    /// its cached id server-side.
    /// </summary>
    public class GameSessionController : MonoBehaviour
    {
        [SerializeField] private APIClient _apiClient;
        [SerializeField] private DecisionLogger _decisionLogger;
        [SerializeField] private AutoSaveController _autoSaveController;
        [SerializeField] private string _gameMode = "homebase";

        private IJSBridge _bridge;
        private bool _sessionStarted;

        private IJSBridge Bridge
        {
            get
            {
                if (_bridge == null) _bridge = new StaticJSBridge();
                return _bridge;
            }
        }

        // Test hook: substitute an IJSBridge mock for PlayMode tests.
        public void SetBridge(IJSBridge bridge) { _bridge = bridge; }

        private void OnEnable()
        {
            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnGameEnd += HandleGameEnd;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnGameEnd -= HandleGameEnd;
        }

        private void HandleGameStart()
        {
            if (_apiClient == null || !_apiClient.CanPersist()) return;
            if (_sessionStarted) return;
            _sessionStarted = true;

            // game_mode is synchronous: ensures decisions logged before the async
            // session_id arrives still carry the correct mode.
            if (_decisionLogger != null)
            {
                _decisionLogger.SetGameMode(_gameMode);
            }

            Bridge.StartSession(_gameMode);
        }

        private void HandleGameEnd(FortuneValley.Domain.Enums.Owner winner)
        {
            Teardown();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            // WebGL: fires on tab blur. Treat as soft teardown.
            if (pauseStatus) Teardown();
        }

        private void OnApplicationQuit()
        {
            Teardown();
        }

        private void Teardown()
        {
            if (!_sessionStarted) return;
            _sessionStarted = false;

            // Order is the contract: final save must happen before decisions flush
            // (state may reference recent decisions), decisions must flush before
            // session closes (so rows carry the real session_id), and EndSession
            // is last so ended_at reflects actual end-of-play.
            if (_autoSaveController != null) _autoSaveController.FlushFinalSave();
            if (_apiClient != null) _apiClient.FlushDecisions();
            // Empty string: Rails uses its cached sessionId internally.
            Bridge.EndSession(string.Empty);
        }
    }
}
