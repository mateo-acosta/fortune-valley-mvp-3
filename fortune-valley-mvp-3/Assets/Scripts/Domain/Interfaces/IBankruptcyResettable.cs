namespace FortuneValley.Domain.Interfaces
{
    /// <summary>
    /// Implemented by any system whose runtime state must be reset when the
    /// player undergoes a soft bankruptcy. The BankruptcyResetService iterates
    /// the registered list and calls OnBankruptcyReset on each.
    ///
    /// Distinct from full game-start initialization: bankruptcy reset preserves
    /// age, selected life goals, and the persistent bankruptcy_flag.
    /// </summary>
    public interface IBankruptcyResettable
    {
        void OnBankruptcyReset();
    }
}
