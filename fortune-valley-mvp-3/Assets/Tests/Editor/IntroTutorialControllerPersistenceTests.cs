using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Tutorial;
using FortuneValley.Managers.Notifications;
using FortuneValley.Managers.Tutorial;
using FortuneValley.Tests.Fakes;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Persistence side of IntroTutorialController: verifies that completion
    /// (and skip) writes the tutorial_completed flag to the local KV store
    /// AND mutates+saves the cached player state, so a network failure on
    /// the SaveState POST does not let the tutorial re-run on reload.
    ///
    /// IntroGate's PlayerPrefs-fallback overload is exercised here too so
    /// the local-write-then-server-write contract is end-to-end covered.
    /// </summary>
    [TestFixture]
    public class IntroTutorialControllerPersistenceTests
    {
        private GameObject _go;
        private GameObject _timeGo;
        private GameObject _guidanceGo;
        private GameObject _stateGo;
        private IntroTutorialController _controller;
        private TimeManager _time;
        private GuidanceController _guidance;
        private TutorialTargetRegistry _registry;
        private PlayerStateAccessor _stateAccessor;
        private InMemoryKeyValueStore _store;
        private IntroScriptSO _script;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();
            _store = new InMemoryKeyValueStore();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            if (_timeGo != null) Object.DestroyImmediate(_timeGo);
            if (_guidanceGo != null) Object.DestroyImmediate(_guidanceGo);
            if (_stateGo != null) Object.DestroyImmediate(_stateGo);
            if (_script != null) Object.DestroyImmediate(_script);
            GameEvents.ClearAllSubscriptions();
        }

        private void Build(GamePlayerStateDTO seedState, TutorialStepSO[] steps)
        {
            _timeGo = new GameObject("TimeManager");
            _time = _timeGo.AddComponent<TimeManager>();

            _guidanceGo = new GameObject("GuidanceController");
            _guidance = _guidanceGo.AddComponent<GuidanceController>();
            var bus = new FakeGameEventBus();
            var now = new FakeNowProvider();
            var prefs = new PlayerPrefsDebouncedFlusher(new InMemoryKeyValueStore(), now);
            _guidance.Initialize(bus, new RepeatPolicyFilter(now, prefs));

            _stateGo = new GameObject("PlayerStateAccessor");
            _stateAccessor = _stateGo.AddComponent<PlayerStateAccessor>();
            _stateAccessor.SetCurrent(seedState);

            _go = new GameObject("IntroTutorialController");
            _controller = _go.AddComponent<IntroTutorialController>();
            _registry = _go.AddComponent<TutorialTargetRegistry>();
            _registry.Initialize(staticEntries: null, resolvers: null);

            _script = ScriptableObject.CreateInstance<IntroScriptSO>();
            var stepsField = typeof(IntroScriptSO).GetField("_steps",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            stepsField.SetValue(_script, steps);

            // APIClient is a MonoBehaviour; for tests we leave it null.
            // PersistCompletionFlag tolerates a null APIClient and still
            // writes to the KV store, which is exactly what we cover here.
            _controller.Initialize(_time, _guidance, _registry, _script,
                playerStateAccessor: _stateAccessor,
                apiClient: null,
                keyValueStore: _store);
        }

        private static TutorialStepSO MakeDialog(string text = "x")
        {
            var s = ScriptableObject.CreateInstance<TutorialStepSO>();
            var kindField = typeof(TutorialStepSO).GetField("_kind",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dialogField = typeof(TutorialStepSO).GetField("_dialogText",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            kindField.SetValue(s, TutorialStepKind.Dialog);
            dialogField.SetValue(s, text);
            return s;
        }

        // ===============================================================
        // COMPLETION
        // ===============================================================

        [Test]
        public void Complete_WritesPlayerPrefsFlag_UsingStateGameMode()
        {
            Build(new GamePlayerStateDTO { game_mode = "learning_level_1", tutorial_completed = false },
                  new[] { MakeDialog("only") });

            _controller.HandleTutorialStartRequested();
            _controller.HandleAdvanceRequested();

            string expectedKey = IntroTutorialController.PlayerPrefsKeyPrefix + "learning_level_1";
            Assert.AreEqual(1, _store.GetInt(expectedKey, 0));
            Assert.AreEqual(1, _store.SaveCallCount,
                "Save() must be called so a tab close immediately after completion still persists");
        }

        [Test]
        public void Complete_NullState_FallsBackToHomebaseGameMode()
        {
            Build(seedState: null, new[] { MakeDialog("only") });

            _controller.HandleTutorialStartRequested();
            _controller.HandleAdvanceRequested();

            Assert.AreEqual(1, _store.GetInt(IntroTutorialController.PlayerPrefsKeyPrefix + "homebase", 0));
        }

        [Test]
        public void Complete_MutatesCachedPlayerStateTrue()
        {
            var state = new GamePlayerStateDTO { game_mode = "homebase", tutorial_completed = false };
            Build(state, new[] { MakeDialog("only") });

            _controller.HandleTutorialStartRequested();
            _controller.HandleAdvanceRequested();

            Assert.IsTrue(_stateAccessor.Current.tutorial_completed);
        }

        [Test]
        public void Complete_LastEndWasSkipIsFalse_OnNormalAdvance()
        {
            Build(new GamePlayerStateDTO { game_mode = "homebase" }, new[] { MakeDialog("only") });

            _controller.HandleTutorialStartRequested();
            _controller.HandleAdvanceRequested();

            Assert.IsFalse(_controller.LastEndWasSkip);
        }

        // ===============================================================
        // SKIP
        // ===============================================================

        [Test]
        public void Skip_AlsoPersistsAndMarksLastEndWasSkip()
        {
            var state = new GamePlayerStateDTO { game_mode = "homebase", tutorial_completed = false };
            Build(state, new[] { MakeDialog("one"), MakeDialog("two"), MakeDialog("three") });

            _controller.HandleTutorialStartRequested();
            _controller.HandleAdvanceRequested();   // reveal skip
            _controller.HandleSkipRequested();

            Assert.IsTrue(_stateAccessor.Current.tutorial_completed);
            Assert.AreEqual(1, _store.GetInt(IntroTutorialController.PlayerPrefsKeyPrefix + "homebase", 0));
            Assert.IsTrue(_controller.LastEndWasSkip);
        }

        // ===============================================================
        // INTROGATE FALLBACK
        // ===============================================================

        [Test]
        public void IntroGate_ServerStateOverridesPlayerPrefsFlag_WhenStateSaysIncomplete()
        {
            // Server state is authoritative when delivered. A stale browser-local
            // PlayerPrefs flag (e.g. from a different student on the same shared
            // dev browser) must NOT block the tutorial for a student whose
            // server-side row has tutorial_completed=false.
            // (Persistence revamp Issue 5a: prefs is per-origin, not per-student.)
            var store = new InMemoryKeyValueStore();
            store.SetInt(IntroTutorialController.PlayerPrefsKeyPrefix + "homebase", 1);

            var state = new GamePlayerStateDTO { game_mode = "homebase", tutorial_completed = false };
            Assert.IsTrue(IntroGate.ShouldRunIntro(state, role: "student", keyValueStore: store),
                "Server state non-null + tutorial_completed=false must override prefs");
        }

        [Test]
        public void IntroGate_PlayerPrefsFlagSet_StateNull_FallsBackToPrefs()
        {
            // When state is null (offline / pre-bootstrapper), the local prefs
            // flag is the only source of truth. This is the "I just finished
            // locally, the SaveState POST didn't land" recovery path.
            var store = new InMemoryKeyValueStore();
            store.SetInt(IntroTutorialController.PlayerPrefsKeyPrefix + "homebase", 1);

            Assert.IsFalse(IntroGate.ShouldRunIntro(state: null, role: "student", keyValueStore: store));
        }

        [Test]
        public void IntroGate_PlayerPrefsFlagUnset_RespectsServerState()
        {
            var store = new InMemoryKeyValueStore();
            var state = new GamePlayerStateDTO { game_mode = "homebase", tutorial_completed = false };
            Assert.IsTrue(IntroGate.ShouldRunIntro(state, role: "student", keyValueStore: store));

            state.tutorial_completed = true;
            Assert.IsFalse(IntroGate.ShouldRunIntro(state, role: "student", keyValueStore: store));
        }

        [Test]
        public void IntroGate_TeacherPreview_SkipsRegardlessOfPrefs()
        {
            var store = new InMemoryKeyValueStore();
            store.SetInt(IntroTutorialController.PlayerPrefsKeyPrefix + "homebase", 1);
            var state = new GamePlayerStateDTO { game_mode = "homebase", tutorial_completed = false };
            Assert.IsFalse(IntroGate.ShouldRunIntro(state, IntroGate.TeacherPreviewRole, store));
        }

        [Test]
        public void IntroGate_NullState_NullStore_RunsIntroForStudent()
        {
            Assert.IsTrue(IntroGate.ShouldRunIntro(null, role: "student", keyValueStore: null));
        }

        [Test]
        public void IntroGate_PerGameMode_DrivenByServerStateNotPrefs()
        {
            // Per-mode tutorial completion is now a property of the server-side
            // game_player_states row (one row per (student_id, game_mode)).
            // PlayerPrefs scoping is irrelevant when state is delivered. With
            // both states saying tutorial_completed=false, both should run
            // regardless of which mode's PlayerPrefs flag is set.
            var store = new InMemoryKeyValueStore();
            store.SetInt(IntroTutorialController.PlayerPrefsKeyPrefix + "learning_level_1", 1);

            var homebase = new GamePlayerStateDTO { game_mode = "homebase", tutorial_completed = false };
            var ll1 = new GamePlayerStateDTO { game_mode = "learning_level_1", tutorial_completed = false };

            Assert.IsTrue(IntroGate.ShouldRunIntro(homebase, "student", store));
            Assert.IsTrue(IntroGate.ShouldRunIntro(ll1, "student", store),
                "Server state authoritative: tutorial_completed=false runs the intro even when the per-mode prefs flag is set");
        }
    }
}
