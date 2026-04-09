namespace FortuneValley.Core
{
    /// <summary>
    /// Immutable record of a single financial transaction.
    /// Stored in TransactionHistory's circular buffer.
    /// </summary>
    public readonly struct TransactionRecord
    {
        public TransactionType Type { get; }
        public string Description { get; }
        public float Amount { get; }
        public int Tick { get; }

        public TransactionRecord(TransactionType type, string description, float amount, int tick)
        {
            Type = type;
            Description = description;
            Amount = amount;
            Tick = tick;
        }
    }
}
