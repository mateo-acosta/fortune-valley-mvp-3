namespace FortuneValley.Domain.Entities
{
    /// <summary>
    /// Top-level category for a QuestionMaster question. These values are the
    /// canonical per-topic taxonomy that flows from the question bank through
    /// quiz_answer decision events into the Rails per-topic accuracy metrics.
    /// Taxes and Budgeting have no question content yet but are reserved so
    /// the accuracy hash has a stable 5-key shape server-side.
    /// </summary>
    public enum QuestionCategory
    {
        Investing,
        Insurance,
        CreditAndLoans,
        Taxes,
        Budgeting
    }
}
