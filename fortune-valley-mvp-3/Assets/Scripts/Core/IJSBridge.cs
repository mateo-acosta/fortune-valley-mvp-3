namespace FortuneValley.Core
{
    /// <summary>
    /// Testable abstraction over the static JSBridge extern calls. Production code
    /// uses StaticJSBridge, which delegates to the WebGL extern methods. Tests substitute
    /// a NSubstitute mock.
    /// </summary>
    public interface IJSBridge
    {
        string GetCsrfToken();
        bool IsSignedIn();
        string GetRole();
        void SaveState(string json);
        void LogDecision(string json);
        void StartSession(string gameMode);
        void EndSession(string sessionId);

        // Web panel overlay surface (HTML iframe panels above the Unity canvas).
        void ShowPanel(string panelId);
        void HidePanel(string panelId);
        void UpdatePanel(string panelId, string json);
        void ShowError(string panelId, string message);
    }
}
