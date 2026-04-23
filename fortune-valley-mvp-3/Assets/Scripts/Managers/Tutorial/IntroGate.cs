using FortuneValley.Core;
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
    ///
    /// A PlayerPrefs fallback covers the window where the tutorial has
    /// completed locally but a network failure prevented the SaveState
    /// POST from landing. The IntroTutorialController writes PlayerPrefs
    /// BEFORE calling SaveState, so a successful completion that failed to
    /// persist server-side still suppresses re-runs on reload. The
    /// overload that takes a null store skips this check and is retained
    /// for existing callers and simple EditMode tests.
    /// </summary>
    public static class IntroGate
    {
        public const string TeacherPreviewRole = "teacher_preview";

        public static bool ShouldRunIntro(GamePlayerStateDTO state, string role)
            => ShouldRunIntro(state, role, keyValueStore: null);

        public static bool ShouldRunIntro(GamePlayerStateDTO state, string role, IKeyValueStore keyValueStore)
        {
            if (role == TeacherPreviewRole) return false;

            if (state != null && state.tutorial_completed) return false;

            if (keyValueStore != null)
            {
                string gameMode = state != null && !string.IsNullOrEmpty(state.game_mode)
                    ? state.game_mode
                    : "homebase";
                string key = IntroTutorialController.PlayerPrefsKeyPrefix + gameMode;
                if (keyValueStore.GetInt(key, 0) == 1) return false;
            }

            return state == null || !state.tutorial_completed;
        }
    }
}
