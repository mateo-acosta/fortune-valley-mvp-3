using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Tests.Runtime
{
    /// <summary>
    /// PlayMode coverage for the bootstrapper behaviors that require real
    /// frames or scene transitions: the one-frame-deferred OnSaveRestored
    /// signal, and the DontDestroyOnLoad survival path.
    /// </summary>
    public class GameSaveBootstrapperPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();
            GameEvents.LastLoadedSaveDto = null;
            GameEvents.HasSaveBeenRestored = false;
            GameSaveBootstrapper.ResetExistingForTests();
            DestroyAllBootstrappers();
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.ClearAllSubscriptions();
            GameEvents.LastLoadedSaveDto = null;
            GameEvents.HasSaveBeenRestored = false;
            GameSaveBootstrapper.ResetExistingForTests();
            DestroyAllBootstrappers();
        }

        [UnityTest]
        public IEnumerator OnSaveRestored_FiresOneFrameAfterOnSaveStateLoaded()
        {
            int phase1FrameCount = -1;
            int phase2FrameCount = -1;

            GameEvents.OnSaveStateLoaded += _ => phase1FrameCount = Time.frameCount;
            GameEvents.OnSaveRestored += () => phase2FrameCount = Time.frameCount;

            var go = new GameObject("GameSaveBootstrapper");
            var bootstrapper = go.AddComponent<GameSaveBootstrapper>();
            // Wait one frame so Awake + Start ran.
            yield return null;

            // Apply via the test hook so we don't depend on SendMessage.
            var dto = new GamePlayerStateDTO
            {
                game_mode = "homebase",
                current_day = 1
            };
            bootstrapper.ApplyForTest(JsonUtility.ToJson(dto));

            // Phase 1 fires synchronously inside ApplyForTest.
            Assert.AreNotEqual(-1, phase1FrameCount,
                "OnSaveStateLoaded must have fired synchronously");
            Assert.AreEqual(-1, phase2FrameCount,
                "OnSaveRestored must NOT fire on the same call as Phase 1");

            // Wait one frame for Update to run.
            yield return null;

            Assert.AreNotEqual(-1, phase2FrameCount,
                "OnSaveRestored must fire on the next frame");
            Assert.Greater(phase2FrameCount, phase1FrameCount,
                "Phase 2 must fire AFTER Phase 1");
            Assert.IsTrue(GameEvents.HasSaveBeenRestored,
                "HasSaveBeenRestored catch-up handle must be set");

            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Bootstrapper_SurvivesSceneChange_KeepsLastLoadedSaveDto()
        {
            var go = new GameObject("GameSaveBootstrapper");
            var bootstrapper = go.AddComponent<GameSaveBootstrapper>();
            yield return null; // Awake + Start

            var dto = new GamePlayerStateDTO
            {
                game_mode = "homebase",
                current_day = 42,
                checking_balance = 1234.56f
            };
            bootstrapper.ApplyForTest(JsonUtility.ToJson(dto));
            yield return null;

            int instanceIdBefore = bootstrapper.GetInstanceID();

            // Load a new empty scene additively then unload the original; this
            // mimics a Homebase -> LearningLevel transition without depending
            // on real scene assets being present.
            var emptyScene = SceneManager.CreateScene("FV_Test_EmptyScene");
            SceneManager.SetActiveScene(emptyScene);
            yield return null;

            // Bootstrapper should still exist (DontDestroyOnLoad).
            Assert.IsNotNull(bootstrapper);
            Assert.AreEqual(instanceIdBefore, bootstrapper.GetInstanceID(),
                "Bootstrapper instance must survive scene activation change");
            Assert.IsNotNull(GameEvents.LastLoadedSaveDto,
                "Cached DTO must survive scene change");
            Assert.AreEqual(42, GameEvents.LastLoadedSaveDto.current_day);

            yield return SceneManager.UnloadSceneAsync(emptyScene);
            Object.Destroy(go);
        }

        private static void DestroyAllBootstrappers()
        {
            var bootstrappers = Object.FindObjectsByType<GameSaveBootstrapper>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < bootstrappers.Length; i++)
            {
                Object.Destroy(bootstrappers[i].gameObject);
            }
        }
    }
}
