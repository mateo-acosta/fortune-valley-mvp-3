using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Managers;

namespace FortuneValley.Tests.Runtime
{
    /// <summary>
    /// PlayMode regression coverage for the cold-boot hydration race
    /// (HANDOFF_hydration_race_autosave_overwrite.md). Two barriers:
    ///   1. GameFlowController must NOT release StartGame until the save
    ///      round-trip resolves OR the bounded timeout elapses.
    ///   2. AutoSaveController.PerformSave must NOT POST until the same
    ///      condition holds, so an un-hydrated fresh-default state can never
    ///      overwrite a returning player's real server row.
    /// The start barrier is exercised in isolation by wiring
    /// GameFlowController with a null GameManager (ReleaseStart's
    /// _gameManager?.StartGame() is null-safe) and asserting the precise
    /// contract: GameEvents.StartBarrierReleased flips true exactly when the
    /// barrier releases, which is 1:1 with the StartGame call.
    /// </summary>
    public class HydrationRaceBarrierPlayModeTests
    {
        private readonly List<GameObject> _tracked = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            ResetPersistenceStatics();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _tracked.Count; i++)
            {
                if (_tracked[i] != null) Object.DestroyImmediate(_tracked[i]);
            }
            _tracked.Clear();
            ResetPersistenceStatics();
        }

        private static void ResetPersistenceStatics()
        {
            GameEvents.ClearAllSubscriptions();
            GameEvents.LastLoadedSaveDto = null;
            GameEvents.HasSaveBeenRestored = false;
            GameEvents.SaveStateRestoredFromServer = false;
            GameEvents.HasServerConfirmedFreshUser = false;
            GameEvents.StartBarrierReleased = false;
            GameSaveBootstrapper.ResetExistingForTests();
        }

        private GameFlowController SpawnFlowController(float timeoutSeconds)
        {
            var go = new GameObject("GameFlowController");
            _tracked.Add(go);
            var controller = go.AddComponent<GameFlowController>();
            typeof(GameFlowController)
                .GetField("_saveRoundTripTimeoutSeconds",
                    BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(controller, timeoutSeconds);
            return controller;
        }

        // ─────────────────────────────────────────────────────────────────
        // START BARRIER
        // ─────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator StartBarrier_HoldsWhenUnresolved_ThenReleasesOnRestoreFlag()
        {
            // Large timeout so only the flag flip can release within the test.
            SpawnFlowController(timeoutSeconds: 600f);
            yield return null; // OnEnable + Start ran (subscriptions live)

            GameEvents.RaiseCountdownComplete();

            // Cold boot: GET has not resolved. The barrier must hold.
            yield return null;
            yield return null;
            Assert.IsFalse(GameEvents.StartBarrierReleased,
                "Start barrier must hold while the save round-trip is unresolved");

            // Server save arrives (Phase 1).
            GameEvents.SaveStateRestoredFromServer = true;
            yield return null; // Update() observes resolution
            yield return null;

            Assert.IsTrue(GameEvents.StartBarrierReleased,
                "Start barrier must release once the server save is restored");
        }

        [UnityTest]
        public IEnumerator StartBarrier_TimesOut_WhenRoundTripNeverResolves()
        {
            SpawnFlowController(timeoutSeconds: 0.1f);
            yield return null;

            GameEvents.RaiseCountdownComplete();

            // Same frame: deadline just set, not yet elapsed, no flags.
            Assert.IsFalse(GameEvents.StartBarrierReleased,
                "Barrier should not release before the timeout elapses");

            // Wait past the bounded timeout (the dev safety valve).
            yield return new WaitForSecondsRealtime(0.25f);
            yield return null;
            yield return null;

            Assert.IsTrue(GameEvents.StartBarrierReleased,
                "Barrier must release via timeout so the game cannot soft-lock");
            Assert.IsFalse(GameEvents.SaveStateRestoredFromServer,
                "Released purely by timeout, not by a restore flag");
            Assert.IsFalse(GameEvents.HasServerConfirmedFreshUser,
                "Released purely by timeout, not by a fresh-user flag");
        }

        [UnityTest]
        public IEnumerator StartBarrier_ReleasesImmediately_ForServerConfirmedFreshUser()
        {
            // Brand-new student: empty payload set the fresh-user flag before
            // the countdown finished. Fresh players must not be blocked.
            GameEvents.HasServerConfirmedFreshUser = true;
            SpawnFlowController(timeoutSeconds: 600f);
            yield return null;

            GameEvents.RaiseCountdownComplete();
            yield return null;

            Assert.IsTrue(GameEvents.StartBarrierReleased,
                "Fresh-user path must release the start barrier immediately");
        }

        // ─────────────────────────────────────────────────────────────────
        // AUTOSAVE BARRIER
        // ─────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator AutosaveBarrier_BlocksPostUntilResolved_ThenAllows()
        {
            var apiGo = new GameObject("APIClient");
            _tracked.Add(apiGo);
            var apiClient = apiGo.AddComponent<APIClient>();
            var bridge = new RecordingJSBridgeLocal();
            apiClient.SetBridge(bridge);

            var saveGo = new GameObject("AutoSaveController");
            _tracked.Add(saveGo);
            var autoSave = saveGo.AddComponent<AutoSaveController>();
            typeof(AutoSaveController)
                .GetField("_apiClient", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(autoSave, apiClient);
            yield return null; // OnEnable subscribed (OnTick / OnStateBuildFuncProvided)

            var dto = new GamePlayerStateDTO { game_mode = "homebase" };
            GameEvents.RaiseStateBuildFuncProvided(() => dto);

            int saveIntervalTicks = (int)typeof(AutoSaveController)
                .GetField("_saveIntervalTicks", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(autoSave);

            // Unresolved cold boot: ticks must NOT produce a POST.
            for (int i = 1; i <= saveIntervalTicks; i++) GameEvents.RaiseTick(i);

            Assert.AreEqual(0, bridge.SaveStateCallCount,
                "Autosave barrier must suppress the POST while unresolved");
            Assert.IsNull(GameEvents.LastLoadedSaveDto,
                "Write-through cache must stay null while the barrier holds");

            // Save round-trip resolves; autosave is now permitted.
            GameEvents.SaveStateRestoredFromServer = true;
            for (int i = 1; i <= saveIntervalTicks; i++) GameEvents.RaiseTick(i);

            Assert.GreaterOrEqual(bridge.SaveStateCallCount, 1,
                "Autosave must POST once the round-trip has resolved");
            Assert.AreSame(dto, GameEvents.LastLoadedSaveDto,
                "Write-through must mirror the saved DTO after the barrier opens");
        }
    }
}
