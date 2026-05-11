using System;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Domain.Entities
{
    /// <summary>
    /// The player's three locked-in life goals (one per tier).
    /// Runtime container; persistence is via the entry array on GamePlayerStateDTO.
    ///
    /// Sorts entries by ascending threshold to support the HUD's
    /// "next-cheapest unrealized" tracking pattern.
    /// </summary>
    public class LifeGoalSelection
    {
        public const int RequiredEntryCount = 3;

        private readonly LifeGoalEntry[] _entries;

        public LifeGoalSelection(LifeGoalEntry[] entries)
        {
            if (entries == null || entries.Length != RequiredEntryCount)
            {
                throw new ArgumentException(
                    $"LifeGoalSelection requires exactly {RequiredEntryCount} entries.");
            }

            _entries = new LifeGoalEntry[RequiredEntryCount];
            Array.Copy(entries, _entries, RequiredEntryCount);
            SortByThresholdAscending();
        }

        public LifeGoalEntry[] Entries => _entries;

        public bool IsAllRealized()
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                if (!_entries[i].realized) return false;
            }
            return true;
        }

        /// <summary>
        /// Returns the cheapest unrealized entry, or null if all goals are realized.
        /// Caller uses this to drive the HUD progress slider target.
        /// </summary>
        public LifeGoalEntry NextUnrealized()
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                if (!_entries[i].realized) return _entries[i];
            }
            return null;
        }

        /// <summary>
        /// Returns the highest threshold below currentNetWorth that has already been
        /// realized (or 0 if none). Used by the HUD slider to compute the lower
        /// bound when filling toward the next goal.
        /// </summary>
        public float PreviousRealizedThreshold()
        {
            float best = 0f;
            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].realized && _entries[i].threshold > best)
                {
                    best = _entries[i].threshold;
                }
            }
            return best;
        }

        /// <summary>
        /// Validates the one-per-tier rule. Returns true if entries cover
        /// Starter, Mid, and Ambitious exactly once each.
        /// </summary>
        public static bool IsValidTierComposition(LifeGoalEntry[] entries)
        {
            if (entries == null || entries.Length != RequiredEntryCount) return false;

            bool hasStarter = false;
            bool hasMid = false;
            bool hasAmbitious = false;

            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] == null) return false;
                switch (entries[i].tier)
                {
                    case LifeGoalTier.Starter:
                        if (hasStarter) return false;
                        hasStarter = true;
                        break;
                    case LifeGoalTier.Mid:
                        if (hasMid) return false;
                        hasMid = true;
                        break;
                    case LifeGoalTier.Ambitious:
                        if (hasAmbitious) return false;
                        hasAmbitious = true;
                        break;
                }
            }

            return hasStarter && hasMid && hasAmbitious;
        }

        private void SortByThresholdAscending()
        {
            Array.Sort(_entries, CompareByThreshold);
        }

        private static int CompareByThreshold(LifeGoalEntry a, LifeGoalEntry b)
        {
            return a.threshold.CompareTo(b.threshold);
        }
    }
}
