using System.Collections.Generic;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Core
{
    /// <summary>
    /// Pure C# class that determines which accidents trigger on a given day.
    /// Extracted from AccidentSystem to keep loops and arithmetic
    /// out of MonoBehaviours. Can be used as an instance (caches accident
    /// info) or via static Roll method for direct/test use.
    ///
    /// LEARNING DESIGN: Hybrid frequency model (window + probability)
    /// creates uncertainty that makes insurance feel worthwhile.
    /// Students learn that risk is about probability, not certainty.
    /// </summary>
    public class AccidentRoller
    {
        private const int HashMultiplier = 397;

        private readonly List<AccidentInfo> _cachedAccidentInfos;

        /// <summary>
        /// Create an AccidentRoller with cached accident definitions.
        /// Converts ScriptableObject data to lightweight structs once.
        /// </summary>
        public AccidentRoller(List<AccidentInfo> accidentInfos)
        {
            _cachedAccidentInfos = accidentInfos ?? new List<AccidentInfo>();
        }

        /// <summary>
        /// Roll accidents and invoke the callback for each triggered accident.
        /// Keeps loops out of MonoBehaviours.
        /// </summary>
        public void RollAndNotify(int dayNumber, List<LotInfo> ownedLots,
            System.Action<AccidentRollResult> onAccidentTriggered)
        {
            var results = Roll(dayNumber, ownedLots, _cachedAccidentInfos);
            for (int i = 0; i < results.Count; i++)
            {
                onAccidentTriggered(results[i]);
            }
        }

        /// <summary>
        /// Build AccidentInfo list from AccidentDefinition ScriptableObjects.
        /// Called once at game start to cache lightweight structs.
        /// </summary>
        public static List<AccidentInfo> BuildInfoList(
            IReadOnlyList<AccidentDefinition> definitions)
        {
            var infos = new List<AccidentInfo>();
            if (definitions == null) return infos;

            for (int i = 0; i < definitions.Count; i++)
            {
                var def = definitions[i];
                if (def == null) continue;

                infos.Add(new AccidentInfo(
                    def.AccidentId,
                    def.DisplayName,
                    def.BaseDamageCost,
                    def.WindowIntervalDays,
                    def.RollProbability
                ));
            }
            return infos;
        }

        /// <summary>
        /// Roll accidents for all owned lots on the given day.
        /// Returns a list of accidents that triggered.
        /// Uses deterministic seeding so all students get the same results
        /// on the same in-game day.
        /// </summary>
        public static List<AccidentRollResult> Roll(
            int dayNumber,
            List<LotInfo> ownedLots,
            List<AccidentInfo> accidentDefs)
        {
            var results = new List<AccidentRollResult>();

            if (ownedLots == null || accidentDefs == null) return results;

            for (int lotIdx = 0; lotIdx < ownedLots.Count; lotIdx++)
            {
                var lot = ownedLots[lotIdx];

                for (int accIdx = 0; accIdx < accidentDefs.Count; accIdx++)
                {
                    var accident = accidentDefs[accIdx];

                    // Check if accident window is open today
                    if (accident.WindowIntervalDays <= 0) continue;
                    if (dayNumber % accident.WindowIntervalDays != 0) continue;

                    // Deterministic seed: same day + lot + accident = same result
                    int seed = HashSeed(dayNumber, lot.LotId, accident.AccidentId);
                    var rng = new System.Random(seed);
                    float roll = (float)rng.NextDouble();

                    if (roll < accident.RollProbability)
                    {
                        results.Add(new AccidentRollResult(
                            lot.LotId,
                            accident.AccidentId,
                            accident.DisplayName,
                            accident.BaseDamageCost
                        ));
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Combine day, lot, and accident into a deterministic seed.
        /// </summary>
        private static int HashSeed(int dayNumber, string lotId, string accidentId)
        {
            unchecked
            {
                int hash = dayNumber;
                hash = hash * HashMultiplier + (lotId != null ? lotId.GetHashCode() : 0);
                hash = hash * HashMultiplier + (accidentId != null ? accidentId.GetHashCode() : 0);
                return hash;
            }
        }
    }
}
