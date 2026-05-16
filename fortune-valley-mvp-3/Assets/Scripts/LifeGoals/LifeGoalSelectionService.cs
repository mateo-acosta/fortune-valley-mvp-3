using System;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Core
{
    /// <summary>
    /// Holds the player's three locked-in life goals for the current session.
    /// Pure C# (lifecycle owned by GameManager); subscribes to
    /// GameEvents.OnLifeGoalsSelected at construction and unsubscribes on Dispose.
    ///
    /// Persistence: BuildDtoEntries() snapshots the current selection into a
    /// LifeGoalEntry[] for the DTO; HydrateFromDto() restores it on load.
    /// An empty / null DTO field means the player has not picked goals yet,
    /// which the boot flow treats as "force fresh tutorial."
    /// </summary>
    public class LifeGoalSelectionService : IDisposable
    {
        private LifeGoalSelection _selection;
        private bool _disposed;

        public LifeGoalSelectionService()
        {
            GameEvents.OnLifeGoalsSelected += HandleLifeGoalsSelected;
            GameEvents.OnRequestLifeGoalsSnapshot += HandleSnapshotRequest;
        }

        public bool HasSelection => _selection != null;
        public LifeGoalSelection CurrentSelection => _selection;

        /// <summary>
        /// Restore selection from a persisted DTO entry array. Returns true if
        /// a valid selection was hydrated; false if the DTO had no goals
        /// (legacy save) or the entries failed validation.
        /// </summary>
        public bool HydrateFromDto(LifeGoalEntry[] dtoEntries)
        {
            if (dtoEntries == null || dtoEntries.Length != LifeGoalSelection.RequiredEntryCount)
            {
                _selection = null;
                return false;
            }

            if (!LifeGoalSelection.IsValidTierComposition(dtoEntries))
            {
                _selection = null;
                return false;
            }

            _selection = new LifeGoalSelection(dtoEntries);
            return true;
        }

        /// <summary>
        /// Snapshot the current selection for persistence. Returns null when
        /// no selection has been made yet -- caller writes null to the DTO.
        /// </summary>
        public LifeGoalEntry[] BuildDtoEntries()
        {
            if (_selection == null) return null;
            return _selection.Entries;
        }

        public void Dispose()
        {
            if (_disposed) return;
            GameEvents.OnLifeGoalsSelected -= HandleLifeGoalsSelected;
            GameEvents.OnRequestLifeGoalsSnapshot -= HandleSnapshotRequest;
            _disposed = true;
        }

        private void HandleLifeGoalsSelected(LifeGoalSelection selection)
        {
            _selection = selection;
        }

        /// <summary>
        /// Pull-pattern responder. Late subscribers (ProfileWebBridge on panel
        /// open) raise OnRequestLifeGoalsSnapshot; we re-emit OnLifeGoalsSelected
        /// with the current selection so they paint the real goals. No-op when
        /// the player has not picked yet (nothing to replay).
        /// </summary>
        private void HandleSnapshotRequest()
        {
            if (_selection == null) return;
            GameEvents.RaiseLifeGoalsSelected(_selection);
        }
    }
}
