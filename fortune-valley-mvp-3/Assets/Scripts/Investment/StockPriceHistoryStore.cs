using System.Collections.Generic;
using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Tracks per-investment price history for the stock graph.
    /// Pre-populates 30 days of simulated history at game start so the
    /// Invest tab graph is never empty on day 1.
    /// </summary>
    public class StockPriceHistoryStore : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private InvestmentSystem _investmentSystem;

        private Dictionary<InvestmentDefinition, List<float>> _history = new();

        private const int MaxHistory = 200;
        // TECH DEBT: RemoveAt(0) is O(n). Use Queue<float> if MaxHistory grows >500.

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void OnEnable()
        {
            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnTick      += HandleTick;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnTick      -= HandleTick;
        }

        private void Start()
        {
            FindDependencies();
        }

        private void FindDependencies()
        {
            if (_investmentSystem == null) Debug.LogError("[StockPriceHistoryStore] _investmentSystem not wired in Inspector.");
        }

        // ═══════════════════════════════════════════════════════════════
        // EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════════

        private void HandleGameStart()
        {
            _history.Clear();
            if (_investmentSystem == null) return;

            // Pre-populate 30 "days" of simulated history for each investment
            foreach (var def in _investmentSystem.AvailableInvestments)
            {
                // Use the display name hash as a stable, per-investment seed
                int seed = def.DisplayName.GetHashCode();
                float[] preHistory = def.SimulateHistory(30, seed);
                _history[def] = new List<float>(preHistory);
            }
        }

        private void HandleTick(int tickNumber)
        {
            if (_investmentSystem == null) return;

            foreach (var def in _investmentSystem.AvailableInvestments)
            {
                if (!_history.TryGetValue(def, out var list))
                {
                    list = new List<float>();
                    _history[def] = list;
                }

                list.Add(def.CurrentPrice);

                // Keep within cap
                if (list.Count > MaxHistory)
                    list.RemoveAt(0);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC API
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns the last <paramref name="windowSize"/> price entries for <paramref name="def"/>.
        /// Returns an empty list (never null) if the definition is unknown.
        /// TECH DEBT: returns a new List copy per call — accept O(n) for ≤30 entries at 1 Hz.
        /// </summary>
        public IReadOnlyList<float> GetWindow(InvestmentDefinition def, int windowSize = 30)
        {
            if (def == null || !_history.TryGetValue(def, out var list))
                return new List<float>();

            int start = Mathf.Max(0, list.Count - windowSize);
            return new List<float>(list.GetRange(start, list.Count - start));
        }
    }
}
