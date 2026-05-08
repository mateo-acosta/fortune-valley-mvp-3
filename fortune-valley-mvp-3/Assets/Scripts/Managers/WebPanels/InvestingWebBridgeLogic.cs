using System.Collections.Generic;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Entities.WebPanels;

namespace FortuneValley.Managers.WebPanels
{
    /// <summary>
    /// Reads investing panel state off the live systems and writes it into
    /// the supplied DTO. Pure C# so EditMode tests can substitute small
    /// fakes for InvestmentSystem / CurrencyManager / PortfolioHistoryTracker /
    /// StockPriceHistoryStore.
    ///
    /// Reuses PortfolioPanelLogic for risk classification (single source of
    /// truth shared with the UGUI panel).
    /// </summary>
    public class InvestingWebBridgeLogic : WebPanelBridgeLogic<InvestingPanelDTO>
    {
        private const int HistoryWindowSize = 30;
        private const string RiskSuffix = " Risk";

        private InvestmentSystem _investmentSystem;
        private CurrencyManager _currencyManager;
        private PortfolioHistoryTracker _historyTracker;
        private StockPriceHistoryStore _priceHistoryStore;

        public void Initialize(
            InvestmentSystem investmentSystem,
            CurrencyManager currencyManager,
            PortfolioHistoryTracker historyTracker,
            StockPriceHistoryStore priceHistoryStore)
        {
            _investmentSystem = investmentSystem;
            _currencyManager = currencyManager;
            _historyTracker = historyTracker;
            _priceHistoryStore = priceHistoryStore;
        }

        public override bool PopulateDTO(InvestingPanelDTO target)
        {
            if (target == null) return false;
            if (_investmentSystem == null || _currencyManager == null) return false;

            float portfolioValue = _investmentSystem.TotalPortfolioValue;

            target.checkingBalance = _currencyManager.CheckingBalance;
            target.investingBalance = portfolioValue;
            target.totalPortfolioValue = portfolioValue;
            target.lifetimeTotalGain = _investmentSystem.LifetimeTotalGain;

            target.riskProfile = StripRiskSuffix(
                PortfolioPanelLogic.GetPortfolioRiskLabel(_investmentSystem.ActiveInvestments));

            FillHistoryWindow(target);
            FillAvailable(target);
            FillHoldings(target);
            return true;
        }

        // ───────────────────── Home tab history ─────────────────────

        private void FillHistoryWindow(InvestingPanelDTO target)
        {
            IReadOnlyList<float> source = _historyTracker != null
                ? _historyTracker.PortfolioValueHistory
                : null;

            int sourceCount = source != null ? source.Count : 0;
            int copyCount = sourceCount < HistoryWindowSize ? sourceCount : HistoryWindowSize;

            if (target.portfolioValueHistory == null || target.portfolioValueHistory.Length != copyCount)
            {
                target.portfolioValueHistory = new float[copyCount];
            }

            int sourceStart = sourceCount - copyCount;
            for (int i = 0; i < copyCount; i++)
            {
                target.portfolioValueHistory[i] = source[sourceStart + i];
            }
        }

        // ───────────────────── Trade + Explore: available investments ─────────────────────

        private void FillAvailable(InvestingPanelDTO target)
        {
            var defs = _investmentSystem.AvailableInvestments;
            int count = defs != null ? defs.Count : 0;

            if (target.available == null || target.available.Length != count)
            {
                target.available = new AvailableInvestmentDTO[count];
            }

            for (int i = 0; i < count; i++)
            {
                if (target.available[i] == null) target.available[i] = new AvailableInvestmentDTO();
                PopulateAvailable(target.available[i], defs[i]);
            }
        }

        private void PopulateAvailable(AvailableInvestmentDTO row, InvestmentDefinition def)
        {
            if (def == null) return;
            row.id = def.name;
            row.name = def.DisplayName;
            row.currentPrice = def.CurrentPrice;
            row.risk = RiskLabel(def.RiskLevel);
            row.category = CategoryLabel(def.Category);
            row.industry = IndustryLabel(def.Industry);

            CopyPriceHistory(row, def);
            row.changePercent = ComputeChangePercent(row.priceHistory, def.CurrentPrice);
        }

        private void CopyPriceHistory(AvailableInvestmentDTO row, InvestmentDefinition def)
        {
            int windowCount = _priceHistoryStore != null
                ? _priceHistoryStore.GetWindowSize(def, HistoryWindowSize)
                : 0;

            if (row.priceHistory == null || row.priceHistory.Length != windowCount)
            {
                row.priceHistory = new float[windowCount];
            }
            if (windowCount > 0)
            {
                _priceHistoryStore.WriteWindowTo(def, row.priceHistory, HistoryWindowSize);
            }
        }

        private static float ComputeChangePercent(float[] priceHistory, float currentPrice)
        {
            if (priceHistory == null || priceHistory.Length == 0) return 0f;
            float first = priceHistory[0];
            if (first <= 0f) return 0f;
            return (currentPrice / first - 1f) * 100f;
        }

        // ───────────────────── Portfolio: holdings ─────────────────────

        private void FillHoldings(InvestingPanelDTO target)
        {
            var holdings = _investmentSystem.ActiveInvestments;
            int count = holdings != null ? holdings.Count : 0;

            if (target.holdings == null || target.holdings.Length != count)
            {
                target.holdings = new ActiveHoldingDTO[count];
            }

            for (int i = 0; i < count; i++)
            {
                if (target.holdings[i] == null) target.holdings[i] = new ActiveHoldingDTO();
                PopulateHolding(target.holdings[i], holdings[i]);
            }
        }

        private void PopulateHolding(ActiveHoldingDTO row, ActiveInvestment inv)
        {
            if (inv == null || inv.Definition == null) return;
            var def = inv.Definition;
            row.id = def.name;
            row.name = def.DisplayName;
            row.shares = inv.NumberOfShares;
            row.currentValue = inv.CurrentValue;
            row.totalGain = inv.TotalGain;
            row.avgCost = inv.AveragePurchasePrice;
            row.currentPrice = def.CurrentPrice;
            row.category = CategoryLabel(def.Category);
            row.industry = IndustryLabel(def.Industry);
            row.risk = RiskLabel(def.RiskLevel);
            CopyHoldingPriceHistory(row, def);
        }

        private void CopyHoldingPriceHistory(ActiveHoldingDTO row, InvestmentDefinition def)
        {
            int windowCount = _priceHistoryStore != null
                ? _priceHistoryStore.GetWindowSize(def, HistoryWindowSize)
                : 0;

            if (row.priceHistory == null || row.priceHistory.Length != windowCount)
            {
                row.priceHistory = new float[windowCount];
            }
            if (windowCount > 0)
            {
                _priceHistoryStore.WriteWindowTo(def, row.priceHistory, HistoryWindowSize);
            }
        }

        // ───────────────────── Enum → label maps (alloc-free, hot-path) ─────────────────────

        // Switch expressions with nameof() so renames break compile rather than
        // silently shifting the iframe's display strings. IL2CPP compiles these
        // to jump tables; zero allocation per call.

        // Public for direct unit testing (matching the StripRiskSuffix precedent below).
        public static string RiskLabel(RiskLevel r) => r switch
        {
            RiskLevel.Low => nameof(RiskLevel.Low),
            RiskLevel.Medium => nameof(RiskLevel.Medium),
            RiskLevel.High => nameof(RiskLevel.High),
            _ => "Unknown",
        };

        public static string CategoryLabel(InvestmentCategory c) => c switch
        {
            InvestmentCategory.Stock => nameof(InvestmentCategory.Stock),
            InvestmentCategory.ETF => nameof(InvestmentCategory.ETF),
            InvestmentCategory.Bond => nameof(InvestmentCategory.Bond),
            InvestmentCategory.TBill => nameof(InvestmentCategory.TBill),
            _ => "Unknown",
        };

        public static string IndustryLabel(Industry i) => i switch
        {
            Industry.None => nameof(Industry.None),
            Industry.Technology => nameof(Industry.Technology),
            Industry.Financials => nameof(Industry.Financials),
            Industry.Energy => nameof(Industry.Energy),
            Industry.ConsumerGoods => nameof(Industry.ConsumerGoods),
            Industry.Healthcare => nameof(Industry.Healthcare),
            Industry.Industrials => nameof(Industry.Industrials),
            _ => "Unknown",
        };

        // ───────────────────── Risk label suffix ─────────────────────

        /// <summary>
        /// PortfolioPanelLogic returns "Low Risk" / "Medium Risk" / "High Risk"
        /// for the UGUI badge format. The HTML mockState uses bare "Low" /
        /// "Medium" / "High" and appends " risk" itself in the badge label,
        /// so we strip the suffix here. "No Holdings" passes through unchanged.
        /// Public for direct unit testing.
        ///
        /// Switch expression maps every known input to a literal so there is
        /// no per-call Substring allocation. Unknown inputs pass through
        /// unchanged to match the original behavior.
        /// </summary>
        public static string StripRiskSuffix(string label) => label switch
        {
            "Low Risk" => "Low",
            "Medium Risk" => "Medium",
            "High Risk" => "High",
            null => null,
            _ => label,
        };
    }
}
