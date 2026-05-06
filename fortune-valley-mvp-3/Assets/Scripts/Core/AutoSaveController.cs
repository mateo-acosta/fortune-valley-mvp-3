using UnityEngine;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Core
{
    /// <summary>
    /// Periodically saves game state to the server via APIClient.
    /// Teardown on game end and tab close is orchestrated by GameSessionController,
    /// which calls FlushFinalSave() directly so ordering is deterministic.
    ///
    /// Life Goals revision: also forces an immediate save on three life-event
    /// boundaries so the HTML status panel does not show stale state to the
    /// student after a meaningful change:
    ///   - OnGoalRealized       (badge flips grey -&gt; colored)
    ///   - OnSoftBankruptcyReset (state wipe; flag flips on)
    ///   - OnGameEnd             (retirement scorecard ready)
    /// </summary>
    public class AutoSaveController : MonoBehaviour
    {
        [SerializeField] private APIClient _apiClient;
        [SerializeField] private int _saveIntervalTicks = 10;

        [Tooltip("Debounce window for OnSaveRequested. Rapid collects collapse into one save.")]
        [SerializeField] private float _saveDebounceSeconds = 0.5f;

        private int _ticksSinceLastSave;
        private System.Func<GamePlayerStateDTO> _buildStateFunc;

        // When >= 0, indicates a pending debounced save scheduled for that
        // unscaledTime. -1 means no pending request.
        private float _pendingSaveAt = -1f;

        private void OnEnable()
        {
            GameEvents.OnTick += HandleTick;
            GameEvents.OnStateBuildFuncProvided += HandleBuildFuncProvided;
            GameEvents.OnSaveRequested += HandleSaveRequested;
            GameEvents.OnGoalRealized += HandleGoalRealized;
            GameEvents.OnSoftBankruptcyReset += HandleSoftBankruptcyReset;
            GameEvents.OnGameEnd += HandleGameEnd;
        }

        private void OnDisable()
        {
            GameEvents.OnTick -= HandleTick;
            GameEvents.OnStateBuildFuncProvided -= HandleBuildFuncProvided;
            GameEvents.OnSaveRequested -= HandleSaveRequested;
            GameEvents.OnGoalRealized -= HandleGoalRealized;
            GameEvents.OnSoftBankruptcyReset -= HandleSoftBankruptcyReset;
            GameEvents.OnGameEnd -= HandleGameEnd;
        }

        private void HandleGoalRealized(LifeGoalEntry entry) => PerformSave();
        private void HandleSoftBankruptcyReset() => PerformSave();
        private void HandleGameEnd(Owner winner) => PerformSave();

        private void Update()
        {
            if (_pendingSaveAt < 0f) return;
            if (Time.unscaledTime < _pendingSaveAt) return;

            _pendingSaveAt = -1f;
            PerformSave();
        }

        private void HandleSaveRequested()
        {
            _pendingSaveAt = Time.unscaledTime + _saveDebounceSeconds;
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
        /// save before decisions flush and the session closes. Flushes any
        /// pending debounced save first so we never lose the last request.
        /// </summary>
        public void FlushFinalSave()
        {
            _pendingSaveAt = -1f;
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
