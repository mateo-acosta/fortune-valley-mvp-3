using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Managers.Tutorial
{
    /// <summary>
    /// For scenes without a Title → Start-button flow (like the current
    /// Homebase direct-boot path), this component checks IntroGate after a
    /// short startup delay and raises <c>GameEvents.OnTutorialStartRequested</c>
    /// if the tutorial should run. Use INSTEAD of <c>BootFlowRouter</c> when
    /// no <c>OnStartRequested</c> event ever fires in the scene.
    ///
    /// The startup delay gives APIClient time to receive the initial state
    /// load from the JS bridge before IntroGate evaluates it. If the state
    /// is still null at check time, IntroGate treats the player as
    /// first-time and runs the tutorial — which is correct for a brand-new
    /// player, but a returning player with a slow network would also see
    /// the tutorial once. The PlayerPrefs fallback flag (written by
    /// IntroTutorialController on prior completion) guards against that.
    /// </summary>
    public class SceneBootTutorialTrigger : MonoBehaviour
    {
        [SerializeField] private PlayerStateAccessor _stateAccessor;
        [SerializeField] private APIClient _apiClient;

        [Tooltip("Seconds to wait after scene start before evaluating IntroGate. " +
                 "Lets the JS bridge deliver the loaded state via OnSaveStateLoaded.")]
        [SerializeField] private float _startupDelaySeconds = 1.0f;

        private float _remainingDelay;
        private bool _fired;

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

            if (IntroGate.ShouldRunIntro(state, role, new PlayerPrefsKeyValueStore()))
            {
                GameEvents.RaiseTutorialStartRequested();
            }
        }
    }
}
