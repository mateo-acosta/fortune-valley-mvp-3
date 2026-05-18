using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Tests.Common;
using FortuneValley.Tests.Fixtures;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Pins the cross-scene-clobbering fix from Issue 1 P of the persistence
    /// plan: AutoSaveController.PerformSave must write the latest DTO into
    /// GameEvents.LastLoadedSaveDto before pushing to Rails. Without this,
    /// a player who navigates Homebase -> LearningLevel -> Homebase has
    /// their progress overwritten by the bootstrapper's frozen session-start
    /// cache when the Homebase systems re-instantiate.
    /// </summary>
    public class AutoSaveControllerWriteThroughTests : SaveTestsBase
    {
        // Stub IJSBridge that records SaveState payloads without touching the
        // real bridge. Lives only in test code.
        private class RecordingBridge : IJSBridge
        {
            public string LastStateJson;
            public string GetCsrfToken() => "";
            public bool IsSignedIn() => true;
            public string GetRole() => "student";
            public void SaveState(string json) { LastStateJson = json; }
            public void LogDecision(string json) { }
            public void StartSession(string gameMode) { }
            public void EndSession(string sessionId) { }
            public void ShowPanel(string panelId) { }
            public void HidePanel(string panelId) { }
            public void UpdatePanel(string panelId, string json) { }
            public void ShowError(string panelId, string message) { }
            public void ReportEvent(string eventName, string propertiesJson) { }
        }

        [Test]
        public void PerformSave_UpdatesLastLoadedSaveDto_BeforePushingToRails()
        {
            var apiClient = SpawnComponent<APIClient>("APIClient");
            var bridge = new RecordingBridge();
            apiClient.SetBridge(bridge);

            var autoSave = SpawnComponent<AutoSaveController>("AutoSaveController");
            // Wire the apiClient SerializeField via reflection (EditMode skips
            // Inspector wiring).
            typeof(AutoSaveController)
                .GetField("_apiClient", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(autoSave, apiClient);

            // Invoke OnEnable so AutoSaveController subscribes to OnStateBuildFuncProvided.
            InvokeOnEnable(autoSave);

            var fixtureDto = GamePlayerStateDTOFixtures.Default()
                .WithDay(15, 4)
                .WithCheckingBalance(1234.56f)
                .WithLots(new[] { "Lot_Block01" });

            // The autosave barrier refuses to POST until the save round-trip
            // has resolved. Simulate a returning player whose server save was
            // restored so the write-through path under test still runs.
            GameEvents.SaveStateRestoredFromServer = true;

            // Provide a build func via the OnStateBuildFuncProvided event
            // (mirrors the production wiring in GameManager).
            GameEvents.RaiseStateBuildFuncProvided(() => fixtureDto);

            // Trigger PerformSave through OnSaveRequested + the debounce path.
            // Easier: invoke the private method directly via reflection.
            var performSave = typeof(AutoSaveController)
                .GetMethod("PerformSave", BindingFlags.NonPublic | BindingFlags.Instance);
            performSave.Invoke(autoSave, null);

            Assert.AreSame(fixtureDto, GameEvents.LastLoadedSaveDto,
                "Write-through must mirror the saved DTO into the catch-up cache");
            Assert.IsNotNull(bridge.LastStateJson,
                "Bridge must still receive the SaveState push");
        }

        [Test]
        public void PerformSave_WithNullBuildFunc_DoesNotMutateCache()
        {
            var apiClient = SpawnComponent<APIClient>("APIClient");
            var bridge = new RecordingBridge();
            apiClient.SetBridge(bridge);

            var autoSave = SpawnComponent<AutoSaveController>("AutoSaveController");
            typeof(AutoSaveController)
                .GetField("_apiClient", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(autoSave, apiClient);
            InvokeOnEnable(autoSave);

            // No build func wired; PerformSave should early-return.
            var performSave = typeof(AutoSaveController)
                .GetMethod("PerformSave", BindingFlags.NonPublic | BindingFlags.Instance);
            performSave.Invoke(autoSave, null);

            Assert.IsNull(GameEvents.LastLoadedSaveDto);
            Assert.IsNull(bridge.LastStateJson);
        }

        [Test]
        public void PerformSave_WithUnresolvedSaveRoundTrip_DoesNotPush()
        {
            var apiClient = SpawnComponent<APIClient>("APIClient");
            var bridge = new RecordingBridge();
            apiClient.SetBridge(bridge);

            var autoSave = SpawnComponent<AutoSaveController>("AutoSaveController");
            typeof(AutoSaveController)
                .GetField("_apiClient", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(autoSave, apiClient);
            InvokeOnEnable(autoSave);

            // A perfectly valid build func is wired, but the save round-trip
            // has NOT resolved (all three flags false via SaveTestsBase reset).
            // The autosave barrier must block the POST so an un-hydrated state
            // can never overwrite the real server row.
            var fixtureDto = GamePlayerStateDTOFixtures.Default();
            GameEvents.RaiseStateBuildFuncProvided(() => fixtureDto);

            Assert.IsFalse(GameEvents.SaveRoundTripResolved,
                "Precondition: round-trip must be unresolved for this test");

            var performSave = typeof(AutoSaveController)
                .GetMethod("PerformSave", BindingFlags.NonPublic | BindingFlags.Instance);
            performSave.Invoke(autoSave, null);

            Assert.IsNull(bridge.LastStateJson,
                "Autosave barrier must suppress the POST until the round-trip resolves");
            Assert.IsNull(GameEvents.LastLoadedSaveDto,
                "Write-through cache must not be mutated while the barrier holds");
        }
    }
}
