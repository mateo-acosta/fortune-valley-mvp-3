using UnityEngine;
using System.Collections.Generic;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Core
{
    /// <summary>
    /// Checks for accidents at the end of each day.
    /// Delegates rolling logic to AccidentRoller (pure C#).
    /// Fires OnAccidentOccurred for each triggered accident;
    /// InsuranceSystem handles resolution.
    ///
    /// LEARNING DESIGN: Accidents create unpredictable losses that
    /// motivate students to consider insurance as a risk tool.
    /// </summary>
    public class AccidentSystem : MonoBehaviour
    {
        // ===============================================================
        // CONFIGURATION
        // ===============================================================

        [Header("Accident Definitions")]
        [Tooltip("All possible accident types")]
        [SerializeField] private List<AccidentDefinition> _accidentDefinitions;

        [Header("Debug")]
        [SerializeField] private bool _logRolls;

        // ===============================================================
        // RUNTIME STATE
        // ===============================================================

        // Pure C# class holds cached accident info and handles rolling
        private AccidentRoller _roller;

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        private void OnEnable()
        {
            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnDayEnd += HandleDayEnd;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnDayEnd -= HandleDayEnd;
        }

        private void HandleGameStart()
        {
            // Build info list and create roller (loops in pure C#)
            var infos = AccidentRoller.BuildInfoList(_accidentDefinitions);
            _roller = new AccidentRoller(infos);
        }

        // ===============================================================
        // DAY END
        // ===============================================================

        private void HandleDayEnd(int dayNumber)
        {
            if (_roller == null) return;

            // Get owned lots from CityManager via a helper
            var ownedLots = GetOwnedLots();
            if (ownedLots.Count == 0) return;

            // Delegate rolling and event firing to pure C# class
            _roller.RollAndNotify(dayNumber, ownedLots, HandleAccidentTriggered);
        }

        private void HandleAccidentTriggered(AccidentRollResult result)
        {
            if (_logRolls)
                Debug.Log($"[AccidentSystem] Accident triggered: {result.AccidentName} on lot {result.LotId}");

            GameEvents.RaiseAccidentOccurred(result);
        }

        // ===============================================================
        // HELPERS
        // ===============================================================

        /// <summary>
        /// Get list of player-owned lot IDs.
        /// Reads from CityManager via GameEvents or a cached list.
        /// For now, returns lots that have been purchased.
        /// TODO: Subscribe to OnLotPurchased to maintain a cached list.
        /// </summary>
        private List<LotInfo> GetOwnedLots()
        {
            // This will be wired to CityManager's lot data in Phase 5/6.
            // For now, return empty -- no accidents without owned lots.
            return new List<LotInfo>();
        }
    }
}
