using UnityEngine;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Core
{
    /// <summary>
    /// Periodically saves game state to the server via APIClient.
    /// Teardown on game end and tab close is orchestrated by GameSessionController,
    /// which calls FlushFinalSave() directly so ordering is deterministic.
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
            GameEvents.OnStateBuildFuncProvided += HandleBuildFuncProvided;
        }

        private void OnDisable()
        {
            GameEvents.OnTick -= HandleTick;
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

        /// <summary>
        /// Invoked by GameSessionController during teardown. Performs a final
        /// save before decisions flush and the session closes.
        /// </summary>
        public void FlushFinalSave()
        {
            PerformSave();
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
