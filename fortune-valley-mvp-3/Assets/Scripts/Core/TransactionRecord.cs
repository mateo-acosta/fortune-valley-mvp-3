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

        /// <summary>
        /// Generic secondary identifier for the entity this transaction relates to.
        /// Insurance uses lotId, investments could use instrumentId, loans loanId.
        /// Null for records that don't need a secondary identifier.
        /// </summary>
        public string EntityId { get; }

        public TransactionRecord(TransactionType type, string description, float amount, int tick)
        {
            Type = type;
            Description = description;
            Amount = amount;
            Tick = tick;
            EntityId = null;
        }

        public TransactionRecord(TransactionType type, string description, float amount, int tick, string entityId)
        {
            Type = type;
            Description = description;
            Amount = amount;
            Tick = tick;
            EntityId = entityId;
        }
    }
}
