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
        // Portfolio market value only — used by the Home subpanel single-line graph.
        private List<float> _portfolioValueHistory = new List<float>();

        // Public accessors for graph rendering
        public IReadOnlyList<float> TotalWealthHistory => _totalWealthHistory;
        public IReadOnlyList<float> NetGainHistory => _netGainHistory;
        public IReadOnlyList<float> PortfolioValueHistory => _portfolioValueHistory;
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
            // _currencyManager is optional — only needed for the legacy total-wealth/net-gain
            // histories. The Home subpanel's portfolio-value graph requires only _investmentSystem.
            if (_investmentSystem == null) Debug.LogError("[PortfolioHistoryTracker] _investmentSystem not wired in Inspector.");
        }

        private void HandleGameStart()
        {
            _totalWealthHistory.Clear();
            _netGainHistory.Clear();
            _portfolioValueHistory.Clear();
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
            if (_investmentSystem == null) return;

            float portfolioValue = _investmentSystem.TotalPortfolioValue;
            _portfolioValueHistory.Add(portfolioValue);

            // Legacy series — only recorded when _currencyManager is wired.
            if (_currencyManager != null)
            {
                _totalWealthHistory.Add(_currencyManager.TotalLiquidBalance + portfolioValue);
                _netGainHistory.Add(_investmentSystem.LifetimeTotalGain);
            }

            // Cap each series at max data points independently.
            while (_portfolioValueHistory.Count > _maxDataPoints) _portfolioValueHistory.RemoveAt(0);
            while (_totalWealthHistory.Count > _maxDataPoints) _totalWealthHistory.RemoveAt(0);
            while (_netGainHistory.Count > _maxDataPoints) _netGainHistory.RemoveAt(0);
        }
    }
}
