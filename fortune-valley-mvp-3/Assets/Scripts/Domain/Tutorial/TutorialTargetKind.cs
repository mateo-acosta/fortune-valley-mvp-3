namespace FortuneValley.Domain.Tutorial
{
    /// <summary>
    /// Named scene-object targets a tutorial step can point at. Resolved at
    /// runtime by TutorialTargetRegistry via its serialized static targets or
    /// a registered ITutorialTargetResolver (used for dynamic targets like
    /// "whichever For Sale lot is still available").
    /// </summary>
    public enum TutorialTargetKind
    {
        None = 0,
        PlayerStarterRestaurant,
        NextAvailableForSaleLot,
        LoanPanelButton,
        RestaurantUpgradeButton,
        TopBarMoneyDisplay
    }
}
