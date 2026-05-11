namespace FortuneValley.Domain.Enums
{
    /// <summary>
    /// Industry sector for stock investments.
    /// Non-stock investments (ETF, Bond, TBill) use None.
    /// </summary>
    public enum Industry
    {
        None,
        Technology,
        Financials,
        Energy,
        ConsumerGoods,
        Healthcare,
        Industrials
    }
}
