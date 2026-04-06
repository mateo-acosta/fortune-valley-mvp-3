namespace FortuneValley.Core
{
    /// <summary>
    /// State machine states for MonthlyPaymentDayController.
    /// </summary>
    public enum PaymentState
    {
        Idle,
        WaitingForCCPayment
    }
}
