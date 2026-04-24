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
        TopBarMoneyDisplay,

        // Inside-panel targets: resolved to UI Transforms that live inside a
        // panel the player has just opened. Used by later tutorial steps after
        // the panel itself is confirmed visible.
        LoanPanelFirstLoanCard,
        LotsPanelFirstAvailableLot,
        RestaurantPanelUpgradeButton,

        // Sidebar tabs and panel roots. LoanPanelRoot is the CreditSystemPanel
        // RectTransform used as the "mask-around-this" target so the full panel
        // stays bright while the donut dims everything outside of it.
        LoanShopTabButton,
        LoanPanelRoot,

        // Buy-lot flow: a fixed lot wired in the registry (always Block5 for
        // tutorial determinism), plus the LotInfoPopup root and its Buy
        // button for the second sub-step.
        TutorialFirstLot,
        LotInfoPopupBuyButton,
        LotInfoPopupRoot,

        // Closing tour: HUD buttons and the rival's starter lot. Pointed at
        // by Dialog steps that explain features without requiring interaction.
        InvestingHudButton,
        InsuranceHudButton,
        QuestionBonusButton,
        RivalRestaurantLot
    }
}
