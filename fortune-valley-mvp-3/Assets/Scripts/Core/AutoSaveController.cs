using UnityEngine;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Core
{
    /// <summary>
    /// Periodically saves game state to the server via APIClient.
    /// Also flushes on game end and fires a save on application quit/pause (tab close).
    /// Wiring: GameManager or GameFlowController assigns the state-building callback.
    /// </summary>
    public class AutoSaveController : MonoBehaviour
    {
        [SerializeField] private APIClient _apiClient;
        [SerializeField] private int _saveIntervalTicks = 10;

        private int _ticksSinceLastSave;
        private System.Func<GamePlayerStateDTO> _buildStateFunc;

        private void OnEnable()
        {
            GameEvents.OnTick += HandleTick;
            GameEvents.OnGameEnd += HandleGameEnd;
            GameEvents.OnStateBuildFuncProvided += HandleBuildFuncProvided;
        }

        private void OnDisable()
        {
            GameEvents.OnTick -= HandleTick;
            GameEvents.OnGameEnd -= HandleGameEnd;
            GameEvents.OnStateBuildFuncProvided -= HandleBuildFuncProvided;
        }

        private void HandleBuildFuncProvided(System.Func<GamePlayerStateDTO> buildFunc)
        {
            _buildStateFunc = buildFunc;
        }

        private void HandleTick(int tickNumber)
        {
            _ticksSinceLastSave++;
            if (_ticksSinceLastSave >= _saveIntervalTicks)
            {
                _ticksSinceLastSave = 0;
                PerformSave();
            }
        }

        private void HandleGameEnd(FortuneValley.Domain.Enums.Owner winner)
        {
            // Final save + flush on game end
            PerformSave();
            if (_apiClient != null)
            {
                _apiClient.FlushDecisions();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            // On WebGL, this fires when the tab loses focus
            if (pauseStatus)
            {
                PerformSave();
                if (_apiClient != null)
                {
                    _apiClient.FlushDecisions();
                }
            }
        }

        private void PerformSave()
        {
            if (_apiClient == null || !_apiClient.CanPersist()) return;
            if (_buildStateFunc == null) return;

            GamePlayerStateDTO state = _buildStateFunc();
            if (state != null)
            {
                _apiClient.SaveState(state);
            }
        }
    }
}
