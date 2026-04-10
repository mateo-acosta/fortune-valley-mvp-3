namespace FortuneValley.UI
{
    /// <summary>
    /// Formatted display data for an insurance transaction detail view.
    /// All strings are pre-formatted for direct UI binding.
    /// </summary>
    public readonly struct InsuranceTransactionDetails
    {
        public string TypeLabel { get; }
        public string LotId { get; }
        public string Amount { get; }
        public string Description { get; }

        public InsuranceTransactionDetails(
            string typeLabel, string lotId,
            string amount, string description)
        {
            TypeLabel = typeLabel;
            LotId = lotId;
            Amount = amount;
            Description = description;
        }
    }
}
