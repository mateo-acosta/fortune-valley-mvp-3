using FortuneValley.Domain.Entities;

namespace FortuneValley.Core
{
    /// <summary>
    /// Testable abstraction over APIClient. Lets DecisionLogger, AutoSaveController,
    /// and GameSessionController be exercised without a real Unity runtime.
    /// </summary>
    public interface IAPIClient
    {
        bool CanPersist();
        string GetRole();
        void SaveState(GamePlayerStateDTO state);
        void WipePlayerState(string gameMode);
        void EnqueueDecision(DecisionEventDTO decision);
        void FlushDecisions();

        // Fire-and-forget telemetry event. Forwarded to the Rails telemetry
        // endpoint via the JS bridge; the server captures via Sentry. No-op
        // for unauthenticated sessions.
        void ReportTelemetry(string eventName, string propertiesJson);
    }
}
