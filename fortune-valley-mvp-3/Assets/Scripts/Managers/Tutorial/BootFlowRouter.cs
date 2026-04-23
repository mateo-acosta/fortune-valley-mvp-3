using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Tutorial;

namespace FortuneValley.Managers.Tutorial
{
    /// <summary>
    /// Single decision point for "what flow does this player get when they
    /// click Start?". Wrapping this in its own MonoBehaviour (and keeping
    /// the decision function static) pulls future boot branches out of
    /// GameFlowController and makes the rule set easy to extend: a new
    /// BootFlow enum value plus one new case in <see cref="Decide"/>.
    ///
    /// Production wiring: subscribe to <c>GameEvents.OnStartRequested</c>
    /// in <see cref="OnEnable"/>; on fire, pull player state from
    /// <see cref="PlayerStateAccessor"/> and role from the API client's
    /// JS-bridge role accessor, then broadcast via
    /// <c>GameEvents.OnBootFlowDecided</c>.
    /// </summary>
    public class BootFlowRouter : MonoBehaviour
    {
        [SerializeField] private PlayerStateAccessor _stateAccessor;
        [SerializeField] private APIClient _apiClient;

        private void OnEnable()
        {
            GameEvents.OnStartRequested += HandleStartRequested;
        }

        private void OnDisable()
        {
            GameEvents.OnStartRequested -= HandleStartRequested;
        }

        private void HandleStartRequested()
        {
            GamePlayerStateDTO state = _stateAccessor != null ? _stateAccessor.Current : null;
            string role = _apiClient != null ? _apiClient.GetRole() : null;
            DecideAndBroadcast(state, role);
        }

        /// <summary>
        /// Pure decision function. Teacher preview short-circuits to
        /// SkipTutorial even if the player state says the tutorial is
        /// incomplete. Otherwise we ask <see cref="IntroGate.ShouldRunIntro"/>
        /// whether the tutorial should run, returning FirstTimeTutorial
        /// if yes and NormalCarousel if no.
        /// </summary>
        public static BootFlow Decide(GamePlayerStateDTO state, string role)
        {
            if (role == IntroGate.TeacherPreviewRole) return BootFlow.SkipTutorial;
            return IntroGate.ShouldRunIntro(state, role)
                ? BootFlow.FirstTimeTutorial
                : BootFlow.NormalCarousel;
        }

        /// <summary>
        /// Compute and broadcast the decision. Callers pass the already-loaded
        /// state and the role string from the auth layer.
        /// </summary>
        public void DecideAndBroadcast(GamePlayerStateDTO state, string role)
        {
            var flow = Decide(state, role);
            GameEvents.RaiseBootFlowDecided(flow);
        }
    }
}
