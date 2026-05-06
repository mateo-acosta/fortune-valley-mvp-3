using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Managers.Tutorial
{
    /// <summary>
    /// For scenes without a Title → Start-button flow (like the current
    /// Homebase direct-boot path), this component checks IntroGate when the
    /// save state arrives (or after a timeout if it doesn't) and raises
    /// <c>GameEvents.OnTutorialStartRequested</c> if the tutorial should run.
    /// Use INSTEAD of <c>BootFlowRouter</c> when no <c>OnStartRequested</c>
    /// event ever fires in the scene.
    ///
    /// Wakes up on either signal, whichever comes first:
    ///  1. <c>GameEvents.OnSaveStateLoaded</c> fires (or the catch-up handle
    ///     <c>GameEvents.LastLoadedSaveDto</c> is already populated when this
    ///     component enables) — happy path: server state is authoritative.
    ///  2. The timeout elapses — offline or first-time student with no DB row.
    ///     IntroGate falls back to the PlayerPrefs flag in that case.
    /// </summary>
    public class SceneBootTutorialTrigger : MonoBehaviour
    {
        [SerializeField] private PlayerStateAccessor _stateAccessor;
        [SerializeField] private APIClient _apiClient;

        [Tooltip("Timeout (seconds) after scene start before IntroGate evaluates without a save. " +
                 "OnSaveStateLoaded fires sooner in the happy path.")]
        [SerializeField] private float _startupDelaySeconds = 2.0f;

        private float _remainingDelay;
        private bool _fired;

        private void OnEnable()
        {
            GameEvents.OnSaveStateLoaded += HandleStateArrived;

            // Catch-up: state may have arrived before this component enabled.
            if (GameEvents.LastLoadedSaveDto != null)
            {
                HandleStateArrived(GameEvents.LastLoadedSaveDto);
            }
        }

        private void OnDisable()
        {
            GameEvents.OnSaveStateLoaded -= HandleStateArrived;
        }

        private void Start()
        {
            _remainingDelay = _startupDelaySeconds;
        }

        private void Update()
        {
            if (_fired) return;
            _remainingDelay -= Time.unscaledDeltaTime;
            if (_remainingDelay > 0f) return;
            EvaluateAndFire();
        }

        private void HandleStateArrived(GamePlayerStateDTO _)
        {
            if (_fired) return;
            EvaluateAndFire();
        }

        /// <summary>
        /// Test hook: run the evaluation immediately instead of waiting
        /// for the delay Update loop.
        /// </summary>
        public void EvaluateNow() => EvaluateAndFire();

        private void EvaluateAndFire()
        {
            if (_fired) return;
            _fired = true;

            GamePlayerStateDTO state = _stateAccessor != null ? _stateAccessor.Current : null;
            string role = _apiClient != null ? _apiClient.GetRole() : null;
            var prefs = new PlayerPrefsKeyValueStore();

            bool shouldRun = IntroGate.ShouldRunIntro(state, role, prefs);

            string gameMode = state != null && !string.IsNullOrEmpty(state.game_mode) ? state.game_mode : "homebase";
            int prefsFlag = prefs.GetInt(IntroTutorialController.PlayerPrefsKeyPrefix + gameMode, 0);

            Debug.Log($"[SceneBootTutorialTrigger] fire decision: shouldRun={shouldRun} " +
                      $"state={(state == null ? "null" : "present")} " +
                      $"state.tutorial_completed={(state != null ? state.tutorial_completed.ToString() : "n/a")} " +
                      $"role='{role}' " +
                      $"prefsKey='{IntroTutorialController.PlayerPrefsKeyPrefix + gameMode}' " +
                      $"prefsFlag={prefsFlag} " +
                      $"(stateAccessor null? {_stateAccessor == null}, apiClient null? {_apiClient == null})");

            if (shouldRun)
            {
                Debug.Log("[SceneBootTutorialTrigger] Raising OnTutorialStartRequested.");
                GameEvents.RaiseTutorialStartRequested();
            }
            else
            {
                Debug.Log("[SceneBootTutorialTrigger] Tutorial suppressed. " +
                          "If you expected it to run, clear PlayerPrefs (Edit > Clear All PlayerPrefs) " +
                          "or delete the key above.");
            }
        }
    }
}
