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
        void SaveState(GamePlayerStateDTO state);
        void EnqueueDecision(DecisionEventDTO decision);
        void FlushDecisions();
    }
}
