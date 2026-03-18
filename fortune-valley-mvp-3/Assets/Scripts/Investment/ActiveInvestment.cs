using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Runtime data for an active investment (shares) held by the player.
    /// This is NOT a MonoBehaviour - it's pure data managed by InvestmentSystem.
    ///
    /// LEARNING NOTE: All values are exposed explicitly so students can see
    /// exactly how their investment is performing and WHY.
    ///
    /// SHARE-BASED SYSTEM: Player owns N shares at fluctuating prices.
    /// Value changes based on current market price, not compounding.
    /// </summary>
    [System.Serializable]
    public class ActiveInvestment
    {
        // ═══════════════════════════════════════════════════════════════
        // INVESTMENT IDENTITY
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Unique ID for this investment instance.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Reference to the investment type definition (ScriptableObject).
        /// </summary>
        public InvestmentDefinition Definition { get; private set; }

        // ═══════════════════════════════════════════════════════════════
        // SHARE TRACKING
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Number of shares owned.
        /// </summary>
        public int NumberOfShares { get; private set; }

        /// <summary>
        /// Average price paid per share (cost basis).
        /// Used to calculate gain/loss accurately when buying at different prices.
        /// </summary>
        public float AveragePurchasePrice { get; private set; }

        /// <summary>
        /// Total cost basis (what the player paid for all shares).
        /// </summary>
        public float TotalCostBasis => NumberOfShares * AveragePurchasePrice;

        /// <summary>
        /// Alias for TotalCostBasis (backward compatibility).
        /// </summary>
        public float Principal => TotalCostBasis;

        /// <summary>
        /// Current value based on current share price.
        /// </summary>
        public float CurrentValue => NumberOfShares * Definition.CurrentPrice;

        /// <summary>
        /// Total gain or loss (CurrentValue - TotalCostBasis).
        /// Positive = profit, negative = loss.
        /// </summary>
        public float TotalGain => CurrentValue - TotalCostBasis;

        /// <summary>
        /// Percentage return: (CurrentValue / TotalCostBasis - 1) * 100
        /// </summary>
        public float PercentageReturn => TotalCostBasis > 0 ? (CurrentValue / TotalCostBasis - 1f) * 100f : 0f;

        // ═══════════════════════════════════════════════════════════════
        // TIME TRACKING
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Tick when investment was created.
        /// </summary>
        public int CreatedAtTick { get; private set; }

        /// <summary>
        /// Number of ticks this investment has been held.
        /// </summary>
        public int TicksHeld { get; private set; }

        /// <summary>
        /// Legacy field kept for compatibility.
        /// </summary>
        public int LastCompoundTick { get; private set; }

        /// <summary>
        /// Legacy field kept for compatibility.
        /// </summary>
        public int CompoundCount { get; private set; }

        /// <summary>
        /// The gain from the most recent price change (for visual feedback).
        /// </summary>
        public float LastCompoundGain { get; private set; }

        // ═══════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════════

        public ActiveInvestment(InvestmentDefinition definition, int shares, float pricePerShare, int currentTick)
        {
            Id = System.Guid.NewGuid().ToString();
            Definition = definition;
            NumberOfShares = shares;
            AveragePurchasePrice = pricePerShare;
            CreatedAtTick = currentTick;
            LastCompoundTick = currentTick;
            TicksHeld = 0;
            CompoundCount = 0;
        }

        // Legacy constructor for backward compatibility
        public ActiveInvestment(InvestmentDefinition definition, float principal, int currentTick)
        {
            Id = System.Guid.NewGuid().ToString();
            Definition = definition;
            // Convert principal to shares at current price
            float price = definition.CurrentPrice;
            NumberOfShares = Mathf.FloorToInt(principal / price);
            AveragePurchasePrice = price;
            CreatedAtTick = currentTick;
            LastCompoundTick = currentTick;
            TicksHeld = 0;
            CompoundCount = 0;
        }

        // ═══════════════════════════════════════════════════════════════
        // METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Add more shares to this position (averaging the cost basis).
        /// </summary>
        public void AddShares(int count, float pricePerShare)
        {
            if (count <= 0)
                return;

            // Calculate new average purchase price (weighted average)
            float totalOldCost = NumberOfShares * AveragePurchasePrice;
            float newCost = count * pricePerShare;
            int newTotalShares = NumberOfShares + count;

            AveragePurchasePrice = (totalOldCost + newCost) / newTotalShares;
            NumberOfShares = newTotalShares;
        }

        /// <summary>
        /// Remove shares from this position (for partial sells).
        /// Returns the actual number of shares removed (capped at owned).
        /// </summary>
        public int RemoveShares(int count)
        {
            if (count <= 0)
                return 0;

            int actual = Mathf.Min(count, NumberOfShares);
            NumberOfShares -= actual;
            return actual;
        }

        /// <summary>
        /// Called each tick to update time held.
        /// </summary>
        public void IncrementTicksHeld()
        {
            TicksHeld++;
        }

        /// <summary>
        /// Legacy method - no longer applies compounding (price fluctuation replaces it).
        /// Returns false always since we use price-based value changes now.
        /// </summary>
        public bool TryCompound(int currentTick)
        {
            // Price fluctuation now happens in InvestmentDefinition.UpdatePrice()
            // This method is kept for backward compatibility but does nothing
            return false;
        }

        /// <summary>
        /// Get a plain-language explanation of this investment's performance.
        /// LEARNING NOTE: This helps students articulate what happened.
        /// </summary>
        public string GetPerformanceExplanation()
        {
            string gainOrLoss = TotalGain >= 0 ? "gained" : "lost";
            string absGain = Mathf.Abs(TotalGain).ToString("F2");

            return $"Your {NumberOfShares} shares (bought at avg ${AveragePurchasePrice:F2}) " +
                   $"have {gainOrLoss} ${absGain} ({PercentageReturn:F1}%) " +
                   $"after {TicksHeld} days. Current price: ${Definition.CurrentPrice:F2}/share.";
        }
    }
}
