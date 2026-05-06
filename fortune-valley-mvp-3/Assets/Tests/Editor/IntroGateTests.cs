using NUnit.Framework;
using FortuneValley.Domain.Entities;
using FortuneValley.Managers.Tutorial;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class IntroGateTests
    {
        [Test]
        public void NullState_Student_ShouldRunIntro()
        {
            Assert.IsTrue(IntroGate.ShouldRunIntro(state: null, role: "student"));
        }

        [Test]
        public void NullState_TeacherPreview_DoesNotRun()
        {
            Assert.IsFalse(IntroGate.ShouldRunIntro(state: null, role: IntroGate.TeacherPreviewRole));
        }

        [Test]
        public void IncompleteState_Student_ShouldRunIntro()
        {
            var state = new GamePlayerStateDTO { tutorial_completed = false };
            Assert.IsTrue(IntroGate.ShouldRunIntro(state, "student"));
        }

        [Test]
        public void CompleteState_Student_DoesNotRun()
        {
            var state = new GamePlayerStateDTO { tutorial_completed = true };
            Assert.IsFalse(IntroGate.ShouldRunIntro(state, "student"));
        }

        [Test]
        public void CompleteState_TeacherPreview_DoesNotRun()
        {
            var state = new GamePlayerStateDTO { tutorial_completed = true };
            Assert.IsFalse(IntroGate.ShouldRunIntro(state, IntroGate.TeacherPreviewRole));
        }

        [Test]
        public void IncompleteState_TeacherPreview_DoesNotRun()
        {
            // Preview role trumps tutorial_completed either way.
            var state = new GamePlayerStateDTO { tutorial_completed = false };
            Assert.IsFalse(IntroGate.ShouldRunIntro(state, IntroGate.TeacherPreviewRole));
        }

        [Test]
        public void NullRole_WithIncompleteState_RunsIntro()
        {
            var state = new GamePlayerStateDTO { tutorial_completed = false };
            Assert.IsTrue(IntroGate.ShouldRunIntro(state, null));
        }

        [Test]
        public void UnknownRole_TreatedAsStudent()
        {
            var state = new GamePlayerStateDTO { tutorial_completed = false };
            Assert.IsTrue(IntroGate.ShouldRunIntro(state, "teacher"));
        }

        // ─────────────────────────────────────────────────────────────────
        // Server-state-authoritative ordering (Issue 5a in the persistence plan)
        // ─────────────────────────────────────────────────────────────────

        [Test]
        public void StateNonNull_TutorialCompleteTrue_OverridesPrefsFlag()
        {
            // Even if a prior browser-local completion left prefs=1, server
            // state IS the truth and says incomplete -> never run is the
            // wrong call. Here state says complete; prefs has nothing.
            // The point of the test below is the OPPOSITE direction.
            var store = new InMemoryKeyValueStore();
            var state = new GamePlayerStateDTO { game_mode = "homebase", tutorial_completed = true };
            Assert.IsFalse(IntroGate.ShouldRunIntro(state, "student", store));
        }

        [Test]
        public void StateNonNull_TutorialIncomplete_RunsTutorial_EvenIfPrefsSayComplete()
        {
            // The whole point of the gate-ordering fix: a stale browser
            // PlayerPrefs flag must NOT block a fresh student whose server
            // state has tutorial_completed=false.
            var store = new InMemoryKeyValueStore();
            store.SetInt(IntroTutorialController.PlayerPrefsKeyPrefix + "homebase", 1);

            var state = new GamePlayerStateDTO { game_mode = "homebase", tutorial_completed = false };
            Assert.IsTrue(IntroGate.ShouldRunIntro(state, "student", store),
                "Server state must override stale browser prefs");
        }

        [Test]
        public void StateNull_PrefsFlagSet_DoesNotRun()
        {
            // Offline / pre-bootstrapper path: when no server state arrived,
            // the local PlayerPrefs flag is the only source of truth.
            var store = new InMemoryKeyValueStore();
            store.SetInt(IntroTutorialController.PlayerPrefsKeyPrefix + "homebase", 1);

            Assert.IsFalse(IntroGate.ShouldRunIntro(state: null, role: "student", keyValueStore: store));
        }

        [Test]
        public void StateNull_PrefsFlagUnset_RunsTutorial()
        {
            var store = new InMemoryKeyValueStore();
            // No flag set.
            Assert.IsTrue(IntroGate.ShouldRunIntro(state: null, role: "student", keyValueStore: store));
        }

        [Test]
        public void TeacherPreview_AlwaysSkips_RegardlessOfPrefs()
        {
            var store = new InMemoryKeyValueStore();
            store.SetInt(IntroTutorialController.PlayerPrefsKeyPrefix + "homebase", 0);

            Assert.IsFalse(IntroGate.ShouldRunIntro(state: null, role: IntroGate.TeacherPreviewRole, keyValueStore: store));

            var state = new GamePlayerStateDTO { game_mode = "homebase", tutorial_completed = false };
            Assert.IsFalse(IntroGate.ShouldRunIntro(state, IntroGate.TeacherPreviewRole, store));
        }
    }
}
