using System;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Core
{
    /// <summary>
    /// Watches Total Net Worth and realizes goals when their thresholds are crossed.
    ///
    /// Behavior locked by the Life Goals plan:
    ///  - Sticky: once a goal is realized, it stays realized regardless of NW dropping.
    ///  - Chain animation: when one NW change crosses multiple thresholds, each goal
    ///    fires OnGoalRealized in ascending-threshold order so the HUD can animate
    ///    through them sequentially.
    ///  - Early-return when IsAllRealized() to keep handler cost ~zero for the
    ///    bonus years of life after all goals are reached.
    ///
    /// Pure C#; lifecycle owned by GameManager. Lives in the Core assembly.
    /// </summary>
    public class GoalProgressTracker : IDisposable
    {
        private readonly LifeGoalSelectionService _selectionService;
        private readonly Func<int> _currentDayFunc;
        private bool _disposed;

        public GoalProgressTracker(LifeGoalSelectionService selectionService, Func<int> currentDayFunc)
        {
            _selectionService = selectionService ?? throw new ArgumentNullException(nameof(selectionService));
            _currentDayFunc = currentDayFunc ?? throw new ArgumentNullException(nameof(currentDayFunc));
            GameEvents.OnNetWorthChanged += HandleNetWorthChanged;
        }

        public void Dispose()
        {
            if (_disposed) return;
            GameEvents.OnNetWorthChanged -= HandleNetWorthChanged;
            _disposed = true;
        }

        private void HandleNetWorthChanged(float totalNetWorth, float liquidNetWorth)
        {
            var selection = _selectionService.CurrentSelection;
            if (selection == null) return;
            if (selection.IsAllRealized()) return; // early-return, perf

            int currentDay = _currentDayFunc();
            var entries = selection.Entries;

            // Entries are pre-sorted ascending by threshold (LifeGoalSelection ctor),
            // so iterating in order chain-realizes from cheapest to most expensive
            // when one NW change crosses multiple thresholds.
            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (!entry.realized && totalNetWorth >= entry.threshold)
                {
                    entry.MarkRealized(currentDay);
                    GameEvents.RaiseGoalRealized(entry);
                }
            }

            // Drive HUD slider toward next-cheapest unrealized. When all goals
            // realize on this same change, NextUnrealized() returns null and we
            // do not fire OnGoalProgressChanged -- subscribers should treat the
            // last-fired progress event plus OnGoalRealized as terminal.
            var next = selection.NextUnrealized();
            if (next != null)
            {
                float prev = selection.PreviousRealizedThreshold();
                GameEvents.RaiseGoalProgressChanged(totalNetWorth, prev, next.threshold);
            }
        }
    }
}
