using System;
using FortuneValley.Domain;

namespace FortuneValley.Core
{
    /// <summary>
    /// Tracks the player's in-game age and fires lifespan boundary events.
    ///
    /// Responsibilities:
    ///  - Subscribe to GameEvents.OnDayEnd.
    ///  - Convert current day to age via LifespanConstants.AgeFromDay.
    ///  - Fire OnYearEnd(age) every time the year increments.
    ///  - Fire OnRetirementReached exactly once when age &gt;= RetirementAge.
    ///
    /// Day-end ordering (codified in the plan):
    ///   1. Billing cycle
    ///   2. Insolvency check
    ///   3. Lifespan tick      &lt;- this controller
    ///   4. Goal realization check
    ///
    /// Subscription order in GameManager.Awake controls runtime ordering;
    /// LifespanController must be wired AFTER InsolvencyMonitor so insolvency
    /// runs before age advances to the retirement boundary.
    /// </summary>
    public class LifespanController : IDisposable
    {
        private int _lastObservedAge;
        private bool _retirementFired;
        private bool _disposed;

        public LifespanController()
        {
            _lastObservedAge = LifespanConstants.StartingAge;
            GameEvents.OnDayEnd += HandleDayEnd;
        }

        public int CurrentAge => _lastObservedAge;
        public bool HasRetired => _retirementFired;

        public void Dispose()
        {
            if (_disposed) return;
            GameEvents.OnDayEnd -= HandleDayEnd;
            _disposed = true;
        }

        /// <summary>
        /// Reset retirement / age state. Used by GameManager on a fresh game start
        /// (not on bankruptcy soft-reset, which preserves age per the plan).
        /// </summary>
        public void ResetForNewGame()
        {
            _lastObservedAge = LifespanConstants.StartingAge;
            _retirementFired = false;
        }

        private void HandleDayEnd(int currentDay)
        {
            int newAge = LifespanConstants.AgeFromTick(currentDay);

            if (newAge > _lastObservedAge)
            {
                _lastObservedAge = newAge;
                GameEvents.RaiseYearEnd(newAge);
            }

            if (!_retirementFired && newAge >= LifespanConstants.RetirementAge)
            {
                _retirementFired = true;
                GameEvents.RaiseRetirementReached();
            }
        }
    }
}
