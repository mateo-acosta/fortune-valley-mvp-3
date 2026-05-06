using System;
using System.Collections.Generic;
using FortuneValley.Domain;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Core
{
    /// <summary>
    /// Builds the end-of-life goal scorecard when the player reaches retirement.
    /// Subscribes to GameEvents.OnRetirementReached, snapshots the current
    /// LifeGoalSelection into a GoalScorecard (realized vs missed), and fires
    /// GameEvents.OnGoalsEvaluated. The GameEndPanel renders the scorecard.
    ///
    /// Pure C# (lifecycle owned by GameManager). The bankruptcy_flag value
    /// is supplied via a Func at construction so this stays decoupled from
    /// the bankruptcy system implementation.
    /// </summary>
    public class RetirementEvaluator : IDisposable
    {
        private readonly LifeGoalSelectionService _selectionService;
        private readonly Func<bool> _bankruptcyFlagFunc;
        private bool _disposed;

        public RetirementEvaluator(
            LifeGoalSelectionService selectionService,
            Func<bool> bankruptcyFlagFunc = null)
        {
            _selectionService = selectionService ?? throw new ArgumentNullException(nameof(selectionService));
            _bankruptcyFlagFunc = bankruptcyFlagFunc;
            GameEvents.OnRetirementReached += HandleRetirementReached;
        }

        public void Dispose()
        {
            if (_disposed) return;
            GameEvents.OnRetirementReached -= HandleRetirementReached;
            _disposed = true;
        }

        private void HandleRetirementReached()
        {
            var scorecard = BuildScorecard();
            GameEvents.RaiseGoalsEvaluated(scorecard);
        }

        public GoalScorecard BuildScorecard()
        {
            var scorecard = new GoalScorecard
            {
                retirement_age = LifespanConstants.RetirementAge,
                bankruptcy_flag = _bankruptcyFlagFunc != null && _bankruptcyFlagFunc()
            };

            var selection = _selectionService.CurrentSelection;
            if (selection == null) return scorecard;

            var realized = new List<LifeGoalEntry>();
            var missed = new List<LifeGoalEntry>();
            var entries = selection.Entries;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].realized) realized.Add(entries[i]);
                else missed.Add(entries[i]);
            }

            scorecard.realized = realized.ToArray();
            scorecard.missed = missed.ToArray();
            return scorecard;
        }
    }
}
