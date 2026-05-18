using FortuneValley.Core;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Minimal IJSBridge stub for PlayMode barrier tests. Records SaveState
    /// pushes (payload + count) and reports a signed-in student so
    /// APIClient.CanPersist() returns true, isolating the assertion to the
    /// autosave barrier rather than the persistence-permission guard.
    /// </summary>
    internal sealed class RecordingJSBridgeLocal : IJSBridge
    {
        public string LastStateJson { get; private set; }
        public int SaveStateCallCount { get; private set; }

        public string GetCsrfToken() => "";
        public bool IsSignedIn() => true;
        public string GetRole() => "student";

        public void SaveState(string json)
        {
            LastStateJson = json;
            SaveStateCallCount++;
        }

        public void LogDecision(string json) { }
        public void StartSession(string gameMode) { }
        public void EndSession(string sessionId) { }
        public void ShowPanel(string panelId) { }
        public void HidePanel(string panelId) { }
        public void UpdatePanel(string panelId, string json) { }
        public void ShowError(string panelId, string message) { }
        public void ReportEvent(string eventName, string propertiesJson) { }
    }
}
