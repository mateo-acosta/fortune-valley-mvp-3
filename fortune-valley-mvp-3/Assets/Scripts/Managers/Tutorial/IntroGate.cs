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
    /// When server state is available we trust it directly; PlayerPrefs is
    /// only consulted as a fallback when state is null (offline / first load
    /// before the bootstrapper has delivered the DTO). This is an Issue 1+5
    /// fix in the persistence revamp plan: PlayerPrefs is per-browser-origin,
    /// not per-student, so reading it before the per-student server state
    /// would let a tutorial completion on one account block the tutorial for
    /// every subsequent student on the same shared browser.
    /// </summary>
    public static class IntroGate
    {
        public const string TeacherPreviewRole = "teacher_preview";

        public static bool ShouldRunIntro(GamePlayerStateDTO state, string role)
            => ShouldRunIntro(state, role, keyValueStore: null);

        public static bool ShouldRunIntro(GamePlayerStateDTO state, string role, IKeyValueStore keyValueStore)
        {
            if (role == TeacherPreviewRole) return false;

            // Server state is authoritative when delivered. Trust it over the
            // browser-local PlayerPrefs flag, which is per-origin and would
            // otherwise leak completion status across student accounts on a
            // shared browser.
            if (state != null) return !state.tutorial_completed;

            // Offline / pre-bootstrapper fallback: use the local PlayerPrefs
            // flag a previous in-browser completion may have written.
            if (keyValueStore != null)
            {
                string key = IntroTutorialController.PlayerPrefsKeyPrefix + "homebase";
                if (keyValueStore.GetInt(key, 0) == 1) return false;
            }

            return true;
        }
    }
}
