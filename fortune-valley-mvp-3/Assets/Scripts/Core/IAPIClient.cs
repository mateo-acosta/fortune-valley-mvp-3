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
    }
}
