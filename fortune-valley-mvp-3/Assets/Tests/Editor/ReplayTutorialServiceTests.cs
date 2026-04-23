using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Managers.Tutorial;
using FortuneValley.Tests.Fakes;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class ReplayTutorialServiceTests
    {
        /// <summary>
        /// In-memory IAPIClient fake for the replay test. Only WipePlayerState
        /// is exercised; the other methods throw or no-op since they would
        /// require full HTTP / JS-bridge setup.
        /// </summary>
        private class FakeApiClient : APIClient
        {
            public List<string> WipeCallsForGameMode { get; } = new List<string>();
            public new void WipePlayerState(string gameMode) => WipeCallsForGameMode.Add(gameMode);
        }

        private GameObject _serviceGo;
        private GameObject _stateGo;
        private ReplayTutorialService _service;
        private PlayerStateAccessor _stateAccessor;
        private InMemoryKeyValueStore _store;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();
            _store = new InMemoryKeyValueStore();

            _stateGo = new GameObject("PlayerStateAccessor");
            _stateAccessor = _stateGo.AddComponent<PlayerStateAccessor>();

            _serviceGo = new GameObject("ReplayTutorialService");
            _service = _serviceGo.AddComponent<ReplayTutorialService>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_serviceGo != null) Object.DestroyImmediate(_serviceGo);
            if (_stateGo != null) Object.DestroyImmediate(_stateGo);
            GameEvents.ClearAllSubscriptions();
        }

        [Test]
        public void RequestReplay_RaisesTutorialStartRequested()
        {
            _stateAccessor.SetCurrent(new GamePlayerStateDTO { game_mode = "homebase", tutorial_completed = true });
            _service.Initialize(apiClient: null, accessor: _stateAccessor, keyValueStore: _store);

            bool raised = false;
            GameEvents.OnTutorialStartRequested += () => raised = true;

            _service.RequestReplay();

            Assert.IsTrue(raised);
        }

        [Test]
        public void RequestReplay_ClearsPlayerPrefsFlag()
        {
            _stateAccessor.SetCurrent(new GamePlayerStateDTO { game_mode = "homebase" });
            _store.SetInt(IntroTutorialController.PlayerPrefsKeyPrefix + "homebase", 1);

            _service.Initialize(apiClient: null, accessor: _stateAccessor, keyValueStore: _store);
            _service.RequestReplay();

            Assert.AreEqual(0, _store.GetInt(IntroTutorialController.PlayerPrefsKeyPrefix + "homebase", 0));
            Assert.AreEqual(1, _store.SaveCallCount);
        }

        [Test]
        public void RequestReplay_ResetsCachedStateFlag()
        {
            var state = new GamePlayerStateDTO { game_mode = "homebase", tutorial_completed = true };
            _stateAccessor.SetCurrent(state);

            _service.Initialize(apiClient: null, accessor: _stateAccessor, keyValueStore: _store);
            _service.RequestReplay();

            Assert.IsFalse(_stateAccessor.Current.tutorial_completed,
                "Cached state must be reset so IntroGate does not short-circuit");
        }

        [Test]
        public void RequestReplay_NullState_UsesHomebaseGameMode()
        {
            _stateAccessor.SetCurrent(null);
            _store.SetInt(IntroTutorialController.PlayerPrefsKeyPrefix + "homebase", 1);

            _service.Initialize(apiClient: null, accessor: _stateAccessor, keyValueStore: _store);
            _service.RequestReplay();

            Assert.AreEqual(0, _store.GetInt(IntroTutorialController.PlayerPrefsKeyPrefix + "homebase", 0));
        }

        [Test]
        public void RequestReplay_LearningLevel_UsesThatGameModeForPrefs()
        {
            _stateAccessor.SetCurrent(new GamePlayerStateDTO { game_mode = "learning_level_1", tutorial_completed = true });
            _store.SetInt(IntroTutorialController.PlayerPrefsKeyPrefix + "learning_level_1", 1);
            _store.SetInt(IntroTutorialController.PlayerPrefsKeyPrefix + "homebase", 1);

            _service.Initialize(apiClient: null, accessor: _stateAccessor, keyValueStore: _store);
            _service.RequestReplay();

            Assert.AreEqual(0, _store.GetInt(IntroTutorialController.PlayerPrefsKeyPrefix + "learning_level_1", 0));
            Assert.AreEqual(1, _store.GetInt(IntroTutorialController.PlayerPrefsKeyPrefix + "homebase", 0),
                "Replay in learning_level_1 must not clear the homebase flag");
        }
    }
}
