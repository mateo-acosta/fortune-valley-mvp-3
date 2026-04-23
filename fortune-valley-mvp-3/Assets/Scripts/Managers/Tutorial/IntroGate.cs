using FortuneValley.Domain.Entities;

namespace FortuneValley.Managers.Tutorial
{
    /// <summary>
    /// Pure predicate: given the player's persisted state and their role,
    /// should the onboarding tutorial run on this session? No Unity
    /// dependencies so the decision is EditMode-testable.
    ///
    /// Teacher-preview sessions always skip the tutorial; the preview role
    /// is an evaluator path, not a student path. A null state means the
    /// player has no server-side record yet (first-ever load) which
    /// unambiguously maps to running the tutorial.
    /// </summary>
    public static class IntroGate
    {
        public const string TeacherPreviewRole = "teacher_preview";

        public static bool ShouldRunIntro(GamePlayerStateDTO state, string role)
        {
            if (role == TeacherPreviewRole) return false;
            if (state == null) return true;
            return !state.tutorial_completed;
        }
    }
}
