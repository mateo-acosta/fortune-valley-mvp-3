using System.Collections.Generic;
using UnityEngine;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Core
{
    /// <summary>
    /// Manages all player investments.
    ///
    /// LEARNING DESIGN: This is the core teaching system. Students must:
    /// 1. See their money grow over time (compound interest)
    /// 2. Understand risk/reward tradeoffs (different investment types)
    /// 3. Experience the time value of money (earlier investments grow more)
    ///
    /// All values are explicit and trackable to support learning reflection.
    /// </summary>
    public class InvestmentSystem : MonoBehaviour, IInvestmentService
    {
        // ═══════════════════════════════════════════════════════════════
        // DEPENDENCIES
        // ═══════════════════════════════════════════════════════════════

        [Header("Dependencies")]
        [Tooltip("Reference to currency manager")]
        [SerializeField] private CurrencyManager _currencyManager;

        [Tooltip("Reference to time manager (for current tick)")]
        [SerializeField] private TimeManager _timeManager;

        [Header("Available Investments")]
        [Tooltip("Investment types players can choose from")]
        [SerializeField] private List<InvestmentDefinition> _availableInvestments;

        [Header("Debug")]
        [SerializeField] private bool _logCompounding = false;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private List<ActiveInvestment> _activeInvestments = new List<ActiveInvestment>();

        // Lifetime tracking — survives sell-offs so game-end analysis is accurate
        private float _lifetimeRealizedGains;
        private int _lifetimeTotalInvestmentsMade;
        private float _lifetimeTotalPrincipalInvested;
        private float _peakPortfolioValue;

        // Per-game sell transaction log (capped at 20 for Coach Val context size)
        private List<SellTransactionRecord> _sellTransactions = new List<SellTransactionRecord>();

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// All currently active investments.
        /// </summary>
        public IReadOnlyList<ActiveInvestment> ActiveInvestments => _activeInvestments;

        /// <summary>
        /// All investment types available to the player.
        /// </summary>
        public IReadOnlyList<InvestmentDefinition> AvailableInvestments => _availableInvestments;

        /// <summary>
        /// Total value of all active investments.
        /// </summary>
        public float TotalPortfolioValue
        {
            get
            {
                float total = 0f;
                foreach (var inv in _activeInvestments)
                {
                    total += inv.CurrentValue;
                }
                return total;
            }
        }

        /// <summary>
        /// Total amount originally invested (sum of all principals).
        /// </summary>
        public float TotalPrincipal
        {
            get
            {
                float total = 0f;
                foreach (var inv in _activeInvestments)
                {
                    total += inv.Principal;
                }
                return total;
            }
        }

        /// <summary>
        /// Total gain/loss across all investments.
        /// </summary>
        public float TotalGain => TotalPortfolioValue - TotalPrincipal;

        /// <summary>
        /// Lifetime total gain = realized gains from sold investments + unrealized gains on current holdings.
        /// This never drops when selling at a profit.
        /// </summary>
        public float LifetimeTotalGain => _lifetimeRealizedGains + TotalGain;

        /// <summary>
        /// Total number of buy operations across the entire game.
        /// </summary>
        public int LifetimeTotalInvestmentsMade => _lifetimeTotalInvestmentsMade;

        /// <summary>
        /// Sum of all money ever invested (every buy adds to this).
        /// </summary>
        public float LifetimeTotalPrincipalInvested => _lifetimeTotalPrincipalInvested;

        /// <summary>
        /// Highest portfolio value reached during this game.
        /// </summary>
        public float PeakPortfolioValue => _peakPortfolioValue;

        /// <summary>
        /// All sell transactions recorded this game, in order.
        /// Capped at 20 entries so Coach Val's context stays predictable.
        /// </summary>
        public IReadOnlyList<SellTransactionRecord> SellHistory => _sellTransactions;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void OnEnable()
        {
            GameEvents.OnTick += HandleTick;
            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnBuySharesRequested += HandleBuySharesRequested;
            GameEvents.OnSellSharesRequested += HandleSellSharesRequested;
        }

        private void OnDisable()
        {
            GameEvents.OnTick -= HandleTick;
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnBuySharesRequested -= HandleBuySharesRequested;
            GameEvents.OnSellSharesRequested -= HandleSellSharesRequested;
        }

        /// <summary>
        /// Intent event handler: UI requested a share purchase.
        /// </summary>
        private void HandleBuySharesRequested(InvestmentDefinition def, int qty)
        {
            BuyShares(def, qty);
        }

        /// <summary>
        /// Intent event handler: UI requested a share sale.
        /// </summary>
        private void HandleSellSharesRequested(ActiveInvestment inv, int qty)
        {
            SellShares(inv, qty);
        }

        private void HandleGameStart()
        {
            _activeInvestments.Clear();
            _lifetimeRealizedGains = 0f;
            _lifetimeTotalInvestmentsMade = 0;
            _lifetimeTotalPrincipalInvested = 0f;
            _peakPortfolioValue = 0f;
            _sellTransactions.Clear();
            InitializePrices();
        }

        private void HandleTick(int tickNumber)
        {
            UpdatePrices();
            UpdateAllInvestments(tickNumber);

            // Track peak portfolio value for game-end analysis
            float currentValue = TotalPortfolioValue;
            if (currentValue > _peakPortfolioValue)
                _peakPortfolioValue = currentValue;
        }

        /// <summary>
        /// Initialize all investment prices at game start.
        /// </summary>
        private void InitializePrices()
        {
            foreach (var def in _availableInvestments)
            {
                def.InitializePrice();
            }
        }

        /// <summary>
        /// Update all investment prices each tick based on volatility.
        /// </summary>
        private void UpdatePrices()
        {
            foreach (var def in _availableInvestments)
            {
                def.UpdatePrice();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Buy shares of an investment type.
        /// If player already owns shares of this type, adds to existing position.
        /// </summary>
        /// <param name="definition">Type of investment</param>
        /// <param name="shareCount">Number of shares to buy</param>
        /// <returns>The investment (new or updated), or null if failed</returns>
        public ActiveInvestment BuyShares(InvestmentDefinition definition, int shareCount)
        {
            if (shareCount <= 0)
            {
                Debug.Log("[InvestmentSystem] Share count must be positive");
                return null;
            }

            float pricePerShare = definition.CurrentPrice;
            float totalCost = shareCount * pricePerShare;

            // Try to spend the money
            if (!_currencyManager.TrySpend(totalCost, $"Buy {shareCount} shares of {definition.DisplayName}"))
            {
                Debug.Log($"[InvestmentSystem] Cannot afford ${totalCost:F0} for {shareCount} shares");
                return null;
            }

            // Track lifetime buy operations
            _lifetimeTotalInvestmentsMade++;
            _lifetimeTotalPrincipalInvested += totalCost;

            // Check if player already has this investment type - consolidate if so
            var existing = _activeInvestments.Find(inv => inv.Definition == definition);
            if (existing != null)
            {
                existing.AddShares(shareCount, pricePerShare);
                Debug.Log($"[InvestmentSystem] Added {shareCount} shares to {definition.DisplayName}. " +
                         $"Total: {existing.NumberOfShares} shares");
                return existing;
            }

            // Create new investment
            var investment = new ActiveInvestment(definition, shareCount, pricePerShare, _timeManager.CurrentTick);
            _activeInvestments.Add(investment);

            GameEvents.RaiseInvestmentCreated(investment);

            Debug.Log($"[InvestmentSystem] Bought {shareCount} shares of {definition.DisplayName} at ${pricePerShare:F2}/share");

            return investment;
        }

        /// <summary>
        /// Legacy method - creates investment by amount (converted to shares).
        /// </summary>
        public ActiveInvestment CreateInvestment(InvestmentDefinition definition, float amount)
        {
            float pricePerShare = definition.CurrentPrice;
            int shareCount = Mathf.FloorToInt(amount / pricePerShare);

            if (shareCount <= 0)
            {
                Debug.Log($"[InvestmentSystem] Amount ${amount:F0} not enough for 1 share at ${pricePerShare:F2}");
                return null;
            }

            return BuyShares(definition, shareCount);
        }

        /// <summary>
        /// Sell a specific number of shares from an investment (partial sell).
        /// Removes the investment entirely if all shares are sold.
        /// </summary>
        /// <param name="investment">The investment to sell from</param>
        /// <param name="shareCount">Number of shares to sell</param>
        /// <returns>Amount received, or 0 if failed</returns>
        public float SellShares(ActiveInvestment investment, int shareCount)
        {
            if (shareCount <= 0 || !_activeInvestments.Contains(investment))
                return 0f;

            // Sell all if requested amount >= owned
            if (shareCount >= investment.NumberOfShares)
                return SellAllShares(investment);

            float pricePerShare = investment.Definition.CurrentPrice;
            RecordRealizedGain(investment, shareCount, pricePerShare);
            int removed = investment.RemoveShares(shareCount);
            float payout = removed * pricePerShare;

            _currencyManager.Add(payout, $"Sold {removed} shares of {investment.Definition.DisplayName}");

            Debug.Log($"[InvestmentSystem] Partial sell: {removed} shares of {investment.Definition.DisplayName}. " +
                     $"Payout: ${payout:F2}. Remaining: {investment.NumberOfShares} shares");

            return payout;
        }

        /// <summary>
        /// Sell all shares of an investment (cash out).
        /// </summary>
        /// <param name="investment">The investment to sell</param>
        /// <returns>Amount received (current value), or 0 if failed</returns>
        public float SellAllShares(ActiveInvestment investment)
        {
            if (!_activeInvestments.Contains(investment))
            {
                Debug.LogWarning("[InvestmentSystem] Investment not found");
                return 0f;
            }

            float pricePerShare = investment.Definition.CurrentPrice;
            RecordRealizedGain(investment, investment.NumberOfShares, pricePerShare);
            float payout = investment.CurrentValue;
            _activeInvestments.Remove(investment);

            // Add the money back to balance
            _currencyManager.Add(payout, $"Sold {investment.NumberOfShares} shares of {investment.Definition.DisplayName}");

            GameEvents.RaiseInvestmentWithdrawn(investment, payout);

            Debug.Log($"[InvestmentSystem] Sold {investment.NumberOfShares} shares of {investment.Definition.DisplayName}. " +
                     $"Payout: ${payout:F2}, Gain: ${investment.TotalGain:F2} ({investment.PercentageReturn:F1}%)");

            return payout;
        }

        /// <summary>
        /// Legacy method - alias for SellAllShares.
        /// </summary>
        public float WithdrawInvestment(ActiveInvestment investment)
        {
            return SellAllShares(investment);
        }

        /// <summary>
        /// Get investment by ID.
        /// </summary>
        public ActiveInvestment GetInvestment(string id)
        {
            return _activeInvestments.Find(inv => inv.Id == id);
        }

        /// <summary>
        /// Get a portfolio summary for students.
        /// </summary>
        public string GetPortfolioSummary()
        {
            if (_activeInvestments.Count == 0)
            {
                return "You have no active investments.\n" +
                       "Investing allows your money to grow over time through compound interest!";
            }

            string summary = $"Portfolio: {_activeInvestments.Count} investment(s)\n" +
                            $"Total invested: ${TotalPrincipal:F0}\n" +
                            $"Current value: ${TotalPortfolioValue:F0}\n" +
                            $"Total gain/loss: ${TotalGain:F0} ({GetTotalPercentageReturn():F1}%)\n\n";

            foreach (var inv in _activeInvestments)
            {
                summary += $"• {inv.Definition.DisplayName}: ${inv.CurrentValue:F0} " +
                          $"({(inv.TotalGain >= 0 ? "+" : "")}{inv.TotalGain:F0})\n";
            }

            return summary;
        }

        /// <summary>
        /// Get comparison text to help students understand investment vs saving.
        /// </summary>
        public string GetInvestmentVsSavingComparison(float amount, InvestmentDefinition definition, int ticks)
        {
            float projectedValue = definition.ProjectValue(amount, ticks);
            float projectedGain = projectedValue - amount;

            return $"If you invest ${amount:F0} in {definition.DisplayName}:\n" +
                   $"• After {ticks} days: ~${projectedValue:F0}\n" +
                   $"• Potential gain: ~${projectedGain:F0}\n\n" +
                   $"If you keep ${amount:F0} in your wallet:\n" +
                   $"• After {ticks} days: ${amount:F0}\n" +
                   $"• Gain: $0\n\n" +
                   $"The trade-off: Invested money is locked up and can't buy lots immediately.";
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════

        private void UpdateAllInvestments(int currentTick)
        {
            foreach (var investment in _activeInvestments)
            {
                // Update time held
                investment.IncrementTicksHeld();

                // Try to compound
                if (investment.TryCompound(currentTick))
                {
                    if (_logCompounding)
                    {
                        Debug.Log($"[InvestmentSystem] {investment.Definition.DisplayName} compounded! " +
                                 $"Value: ${investment.CurrentValue:F2}, Gain: ${investment.TotalGain:F2}");
                    }

                    GameEvents.RaiseInvestmentCompounded(investment);
                }
            }
        }

        private float GetTotalPercentageReturn()
        {
            if (TotalPrincipal <= 0)
                return 0f;

            return (TotalPortfolioValue / TotalPrincipal - 1f) * 100f;
        }

        /// <summary>
        /// Record realized gain/loss when selling shares.
        /// This accumulates over the game so game-end analysis reflects total performance.
        /// Also appends a SellTransactionRecord so the recap and Coach Val see the specific trade.
        /// </summary>
        private void RecordRealizedGain(ActiveInvestment inv, int sharesSold, float sellPrice)
        {
            float gainPerShare = sellPrice - inv.AveragePurchasePrice;
            _lifetimeRealizedGains += gainPerShare * sharesSold;

            // Cap at 20 records to keep Coach Val's context predictable
            if (_sellTransactions.Count < 20)
            {
                _sellTransactions.Add(new SellTransactionRecord
                {
                    InvestmentName    = inv.Definition.DisplayName,
                    Category          = inv.Definition.Category.ToString(),
                    SharesSold        = sharesSold,
                    SellDay           = _timeManager.CurrentTick,
                    SellPricePerShare = sellPrice,
                    CostBasisPerShare = inv.AveragePurchasePrice,
                    GainOrLoss        = gainPerShare * sharesSold,
                    // Same formula as ActiveInvestment.PercentageReturn — per-trade return
                    PercentageReturn  = inv.AveragePurchasePrice > 0
                                        ? (sellPrice / inv.AveragePurchasePrice - 1f) * 100f
                                        : 0f
                });
            }
        }
    }
}
