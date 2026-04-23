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
    /// </summary>
    public class BootFlowRouter : MonoBehaviour
    {
        /// <summary>
        /// Pure decision function. Teacher preview short-circuits to
        /// SkipTutorial even if the player state says the tutorial is
        /// incomplete (the role is an evaluator path that bypasses both
        /// the tutorial and the rules carousel). Otherwise we ask
        /// <see cref="IntroGate.ShouldRunIntro"/> whether the tutorial
        /// should run, returning FirstTimeTutorial if yes and
        /// NormalCarousel if no.
        /// </summary>
        public static BootFlow Decide(GamePlayerStateDTO state, string role)
        {
            if (role == IntroGate.TeacherPreviewRole) return BootFlow.SkipTutorial;
            return IntroGate.ShouldRunIntro(state, role)
                ? BootFlow.FirstTimeTutorial
                : BootFlow.NormalCarousel;
        }

        /// <summary>
        /// Production helper: compute and broadcast the decision. Callers
        /// pass the already-loaded state and the role string from the
        /// JSBridge auth layer. GameFlowController subscribes to
        /// <c>GameEvents.OnBootFlowDecided</c> and routes based on the
        /// resulting flow.
        /// </summary>
        public void DecideAndBroadcast(GamePlayerStateDTO state, string role)
        {
            var flow = Decide(state, role);
            GameEvents.RaiseBootFlowDecided(flow);
        }
    }
}
