using System.Collections.Generic;
using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Tracks portfolio value over time for the graph display.
    /// Snapshots total wealth and net investment gain every N ticks.
    /// </summary>
    public class PortfolioHistoryTracker : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private CurrencyManager _currencyManager;
        [SerializeField] private InvestmentSystem _investmentSystem;

        [Header("Settings")]
        [Tooltip("How many ticks between each data snapshot")]
        [SerializeField] private int _snapshotInterval = 1;

        [Tooltip("Maximum data points stored (oldest removed when exceeded)")]
        [SerializeField] private int _maxDataPoints = 200;
        // TECH DEBT: RemoveAt(0) is O(n). Use Queue<float> if maxDataPoints grows >500.

        // History data
        private List<float> _totalWealthHistory = new List<float>();
        private List<float> _netGainHistory = new List<float>();

        // Public accessors for graph rendering
        public IReadOnlyList<float> TotalWealthHistory => _totalWealthHistory;
        public IReadOnlyList<float> NetGainHistory => _netGainHistory;
        public int DataPointCount => _totalWealthHistory.Count;

        private void OnEnable()
        {
            GameEvents.OnTick += HandleTick;
            GameEvents.OnGameStart += HandleGameStart;
        }

        private void OnDisable()
        {
            GameEvents.OnTick -= HandleTick;
            GameEvents.OnGameStart -= HandleGameStart;
        }

        private void Start()
        {
            FindDependencies();
        }

        private void FindDependencies()
        {
            if (_currencyManager == null) Debug.LogError("[PortfolioHistoryTracker] _currencyManager not wired in Inspector.");
            if (_investmentSystem == null) Debug.LogError("[PortfolioHistoryTracker] _investmentSystem not wired in Inspector.");
        }

        private void HandleGameStart()
        {
            _totalWealthHistory.Clear();
            _netGainHistory.Clear();
            // Take initial snapshot
            TakeSnapshot();
        }

        private void HandleTick(int tickNumber)
        {
            if (tickNumber % _snapshotInterval == 0)
            {
                TakeSnapshot();
            }
        }

        private void TakeSnapshot()
        {
            if (_currencyManager == null || _investmentSystem == null)
                return;

            // Total wealth = cash + portfolio value
            float totalWealth = _currencyManager.Balance + _investmentSystem.TotalPortfolioValue;
            float netGain = _investmentSystem.LifetimeTotalGain;

            _totalWealthHistory.Add(totalWealth);
            _netGainHistory.Add(netGain);

            // Cap at max data points
            while (_totalWealthHistory.Count > _maxDataPoints)
            {
                _totalWealthHistory.RemoveAt(0);
                _netGainHistory.RemoveAt(0);
            }
        }
    }
}
