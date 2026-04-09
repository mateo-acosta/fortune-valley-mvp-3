namespace FortuneValley.Core
{
    /// <summary>
    /// Categories of financial transactions recorded by TransactionHistory.
    /// Used by History sub-panels to filter which transactions to display.
    /// </summary>
    public enum TransactionType
    {
        LoanOriginated,
        LoanPayment,
        LoanPaidOff,
        LoanPaymentMissed,
        CreditCardCharge,
        CreditCardPayment,
        InsurancePurchased,
        InsuranceCanceled,
        AccidentResolved,
        InvestmentBought,
        InvestmentSold
    }
}
