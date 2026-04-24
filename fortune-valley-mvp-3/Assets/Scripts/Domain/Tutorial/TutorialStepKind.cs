namespace FortuneValley.Domain.Tutorial
{
    /// <summary>
    /// Discrete step kinds authored on a TutorialStepSO. Dialog steps are
    /// advanced by a tap; WaitForX steps gate advancement on a matching
    /// GameEvent firing (see IntroTutorialController's step-to-event map).
    /// </summary>
    public enum TutorialStepKind
    {
        Dialog = 0,
        WaitForRestaurantTap,
        WaitForIncomeCollected,
        WaitForLoanPanelOpened,
        WaitForLoanTaken,
        WaitForLotPurchased,
        WaitForRestaurantUpgraded,
        WaitForLoanShopTabSelected,
        WaitForLotInfoOpened
    }
}
