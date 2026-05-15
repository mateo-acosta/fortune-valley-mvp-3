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
            if (GameEvents.LastLoadedSaveDto != null) return;
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
        /// LEGACY: allocates two lists per call. Hot-path callers should use
        /// GetWindowSize + WriteWindowTo instead.
        /// </summary>
        public IReadOnlyList<float> GetWindow(InvestmentDefinition def, int windowSize = 30)
        {
            if (def == null || !_history.TryGetValue(def, out var list))
                return new List<float>();

            int start = Mathf.Max(0, list.Count - windowSize);
            return new List<float>(list.GetRange(start, list.Count - start));
        }

        /// <summary>
        /// Returns the actual number of entries the next WriteWindowTo call will
        /// produce for <paramref name="def"/> with the given window size.
        /// Pair with WriteWindowTo to allocate a properly-sized destination
        /// buffer once per size change instead of per push.
        /// </summary>
        public int GetWindowSize(InvestmentDefinition def, int windowSize = 30)
        {
            if (def == null || !_history.TryGetValue(def, out var list)) return 0;
            return Mathf.Min(windowSize, list.Count);
        }

        /// <summary>
        /// Writes the last <paramref name="windowSize"/> price entries for
        /// <paramref name="def"/> into <paramref name="dest"/>, oldest-first.
        /// Writes up to dest.Length values. No allocations. Hot-path-safe.
        /// No-op if dest is null or def is unknown.
        /// </summary>
        public void WriteWindowTo(InvestmentDefinition def, float[] dest, int windowSize = 30)
        {
            if (def == null || dest == null || !_history.TryGetValue(def, out var list)) return;
            int start = Mathf.Max(0, list.Count - windowSize);
            int writeCount = Mathf.Min(list.Count - start, dest.Length);
            for (int i = 0; i < writeCount; i++) dest[i] = list[start + i];
        }
    }
}
