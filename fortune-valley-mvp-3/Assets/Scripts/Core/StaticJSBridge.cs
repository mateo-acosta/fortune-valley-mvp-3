namespace FortuneValley.Core
{
    /// <summary>
    /// Production IJSBridge that forwards to the static JSBridge extern wrappers.
    /// Used by APIClient and GameSessionController at runtime. Tests substitute
    /// a different IJSBridge via NSubstitute instead.
    /// </summary>
    public class StaticJSBridge : IJSBridge
    {
        public string GetCsrfToken() => JSBridge.GetCsrfToken();
        public bool IsSignedIn() => JSBridge.IsSignedIn();
        public string GetRole() => JSBridge.GetRole();
        public void SaveState(string json) => JSBridge.SaveState(json);
        public void LogDecision(string json) => JSBridge.LogDecision(json);
        public void StartSession(string gameMode) => JSBridge.StartSession(gameMode);
        public void EndSession(string sessionId) => JSBridge.EndSession(sessionId);
        public void ShowPanel(string panelId) => JSBridge.ShowPanel(panelId);
        public void HidePanel(string panelId) => JSBridge.HidePanel(panelId);
        public void UpdatePanel(string panelId, string json) => JSBridge.UpdatePanel(panelId, json);
        public void ShowError(string panelId, string message) => JSBridge.ShowError(panelId, message);
    }
}
