namespace FortuneValley.Domain.Entities
{
    /// <summary>
    /// Records when and why a player purchased a specific lot.
    /// </summary>
    [System.Serializable]
    public struct LotPurchaseRecord
    {
        public string LotId;
        public string LotName;
        public float Cost;
        public float IncomeBonus;
        public int PurchasedOnDay;
    }
}
