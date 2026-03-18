namespace FortuneValley.Domain.Entities
{
    /// <summary>
    /// Records a single sell transaction for game-end recap and Coach Val context.
    /// Captured at time of sell so the data survives after the position is removed.
    /// </summary>
    [System.Serializable]
    public struct SellTransactionRecord
    {
        public string InvestmentName;
        public string Category;
        public int    SharesSold;
        public int    SellDay;
        public float  SellPricePerShare;
        public float  CostBasisPerShare;
        public float  GainOrLoss;
        public float  PercentageReturn;
    }
}
