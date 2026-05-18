using System;
using UnityEngine;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Tutorial;

namespace FortuneValley.Core
{
    /// <summary>
    /// Central event bus for loose coupling between systems.
    /// All game systems publish and subscribe through these static events.
    /// </summary>
    public static class GameEvents
    {
        // ═══════════════════════════════════════════════════════════════
        // TIME EVENTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Fired every game tick. Core heartbeat of the simulation.
        /// </summary>
        public static event Action<int> OnTick;

        /// <summary>
        /// Fired when game speed changes (pause, 1x, 2x, etc.)
        /// </summary>
        public static event Action<float> OnGameSpeedChanged;

        // ═══════════════════════════════════════════════════════════════
        // CURRENCY EVENTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Fired when player's total balance changes.
        /// Parameters: new total balance, delta (positive = gained, negative = spent)
        /// </summary>
        public static event Action<float, float> OnCurrencyChanged;

        /// <summary>
        /// Fired when checking account balance changes.
        /// Parameters: new balance, delta
        /// </summary>
        public static event Action<float, float> OnCheckingBalanceChanged;

        /// <summary>
        /// Fired when investing account balance changes.
        /// Parameters: new balance, delta
        /// </summary>
        public static event Action<float, float> OnInvestingBalanceChanged;

        // OnTransfer removed -- single-balance architecture. Re-add for dual accounts.

        /// <summary>
        /// Fired when income is generated (for UI feedback).
        /// Parameters: amount, source description
        /// </summary>
        public static event Action<float, string> OnIncomeGenerated;

        /// <summary>
        /// Fired when income is generated with world position (for visual feedback).
        /// Parameters: amount, world position of income source
        /// </summary>
        public static event Action<float, Vector3> OnIncomeGeneratedWithPosition;

        /// <summary>
        /// Fired when rival earns income, with world position for floating text.
        /// Parameters: amount, world position above rival restaurant
        /// </summary>
        public static event Action<float, Vector3> OnRivalIncomeGeneratedWithPosition;

        // ═══════════════════════════════════════════════════════════════
        // INVESTMENT EVENTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Fired when an investment compounds (grows).
        /// Parameter: the investment that just compounded
        /// </summary>
        public static event Action<ActiveInvestment> OnInvestmentCompounded;

        /// <summary>
        /// Fired when player creates a new investment.
        /// </summary>
        public static event Action<ActiveInvestment> OnInvestmentCreated;

        /// <summary>
        /// Fired when player withdraws an investment.
        /// Parameters: the investment, total payout received
        /// </summary>
        public static event Action<ActiveInvestment, float> OnInvestmentWithdrawn;

        /// <summary>
        /// Fired when a holding is clicked in the Portfolio list.
        /// PortfolioDetailView subscribes to populate the detail panel.
        /// </summary>
        public static event Action<ActiveInvestment> OnPortfolioHoldingSelected;

        /// <summary>
        /// Fired when the user explicitly requests to trade a stock
        /// (e.g., clicking the Trade button in the Portfolio detail view).
        /// InvestingTradeSubPanel subscribes to receive the selection.
        /// </summary>
        public static event Action<InvestmentDefinition> OnTradeRequested;

        // ═══════════════════════════════════════════════════════════════
        // CITY / LOT EVENTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Fired when any lot is purchased.
        /// Parameters: lot ID, new owner
        /// </summary>
        public static event Action<string, Owner> OnLotPurchased;

        /// <summary>
        /// Fired when rival is about to buy a lot (warning for player).
        /// Parameter: lot ID the rival is targeting
        /// </summary>
        public static event Action<string> OnRivalTargetingLot;

        /// <summary>
        /// Fired when rival's target changes (with days until purchase).
        /// Parameters: lot ID, days until rival attempts purchase
        /// </summary>
        public static event Action<string, int> OnRivalTargetChanged;

        /// <summary>
        /// Fired when rival successfully purchases a lot.
        /// Parameter: lot ID that was purchased
        /// </summary>
        public static event Action<string> OnRivalPurchasedLot;

        /// <summary>
        /// Fired when rival successfully upgrades one of its owned lots.
        /// Parameters: lot ID, new tier (2 or 3).
        /// </summary>
        public static event Action<string, int> OnRivalUpgradedLot;

        /// <summary>
        /// Fired when rival's money changes (for balance display).
        /// Parameter: new rival balance
        /// </summary>
        public static event Action<float> OnRivalBalanceChanged;

        // ═══════════════════════════════════════════════════════════════
        // GAME STATE EVENTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Fired when the game ends.
        /// Parameter: the winner (Player or Rival)
        /// </summary>
        public static event Action<Owner> OnGameEnd;

        /// <summary>
        /// Fired when the game ends with full summary data.
        /// Parameters: isPlayerWin, summary data for end screen
        /// </summary>
        public static event Action<bool, GameSummary> OnGameEndWithSummary;

        /// <summary>
        /// Fired when a new game starts.
        /// </summary>
        public static event Action OnGameStart;

        /// <summary>
        /// Fired after game start when city lot data is ready.
        /// Parameter: total number of lots in the city.
        /// Subscribe to initialize UI components that need lot count.
        /// </summary>
        public static event Action<int> OnCityInitialized;

        // ═══════════════════════════════════════════════════════════════
        // RESTAURANT EVENTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Fired when the player clicks the restaurant building in the world.
        /// </summary>
        public static event Action OnRestaurantSelected;

        /// <summary>
        /// Fired when restaurant is upgraded.
        /// Parameter: new level
        /// </summary>
        public static event Action<int> OnRestaurantUpgraded;

        // ═══════════════════════════════════════════════════════════════
        // FLOW CONTROL EVENTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Fired by GameFlowController to tell TitleScreenPanel to show.
        /// </summary>
        public static event Action OnShowTitleScreen;

        /// <summary>
        /// Fired by GameFlowController to tell TitleScreenPanel to hide.
        /// </summary>
        public static event Action OnHideTitleScreen;

        /// <summary>
        /// Fired by GameFlowController to tell RulesCarouselPanel to show.
        /// </summary>
        public static event Action OnShowRulesCarousel;

        /// <summary>
        /// Fired by GameFlowController to tell RulesCarouselPanel to hide.
        /// </summary>
        public static event Action OnHideRulesCarousel;

        /// <summary>
        /// Fired by GameFlowController to start the countdown.
        /// CountdownOverlay subscribes and fires OnCountdownComplete when done.
        /// </summary>
        public static event Action OnStartCountdown;

        /// <summary>
        /// Fired by GameFlowController to show or hide the HUD frames.
        /// Parameter: true = show, false = hide
        /// </summary>
        public static event Action<bool> OnSetHUDVisible;

        /// <summary>
        /// Fired by UIManager.ShowPanel after a panel becomes visible. The
        /// tutorial uses this so a "wait for player to open the loans panel"
        /// step advances when the panel actually opens, not when the HUD
        /// button is clicked.
        /// </summary>
        public static event Action<PanelType> OnPanelOpened;

        /// <summary>
        /// Fired by WebPanel bridges when the HTML close button is clicked.
        /// UIManager subscribes and calls HideCurrentPanel. Avoids a
        /// Managers->UI layer dependency.
        /// </summary>
        public static event Action<PanelType> OnHidePanelRequested;

        // ═══════════════════════════════════════════════════════════════
        // INTENT EVENTS (fired by UI, handled by game systems)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Fired by LotPurchasePopup when the player confirms a lot purchase.
        /// CityManager subscribes and calls TryPurchaseLot.
        /// Parameters: lot ID, current tick
        /// </summary>
        public static event Action<string, int> OnPurchaseLotRequested;

        /// <summary>
        /// Fired by RestaurantUpgradePanel when the player clicks Upgrade.
        /// RestaurantSystem subscribes and calls TryUpgrade.
        /// </summary>
        public static event Action OnUpgradeRestaurantRequested;

        /// <summary>
        /// Fired by PortfolioPanel when the player clicks Buy.
        /// InvestmentSystem subscribes and calls BuyShares.
        /// Parameters: investment definition, share count
        /// </summary>
        public static event Action<InvestmentDefinition, int> OnBuySharesRequested;

        /// <summary>
        /// Fired by PortfolioPanel when the player clicks Sell.
        /// InvestmentSystem subscribes and calls SellShares.
        /// Parameters: active investment, share count
        /// </summary>
        public static event Action<ActiveInvestment, int> OnSellSharesRequested;

        // ═══════════════════════════════════════════════════════════════
        // DAY CYCLE EVENTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Fired by TimeManager when a full day completes (every N ticks).
        /// Parameter: day number (1-based)
        /// </summary>
        public static event Action<int> OnDayEnd;

        // ═══════════════════════════════════════════════════════════════
        // FLOW FEEDBACK EVENTS (fired by UI panels back to GameFlowController)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Fired by TitleScreenPanel when the player presses Start.
        /// </summary>
        public static event Action OnStartRequested;

        /// <summary>
        /// Fired by RulesCarouselPanel when the player finishes all rules slides.
        /// </summary>
        public static event Action OnCarouselComplete;

        /// <summary>
        /// Fired by CountdownOverlay when the countdown animation finishes.
        /// </summary>
        public static event Action OnCountdownComplete;

        /// <summary>
        /// Fired by GameEndPanel when the player clicks "Play Again".
        /// GameFlowController subscribes and restarts without returning to title.
        /// </summary>
        public static event Action OnRestartRequested;

        /// <summary>
        /// Fired by GameEndPanel when the player clicks "Main Menu".
        /// GameFlowController subscribes and returns to the title screen.
        /// </summary>
        public static event Action OnReturnToTitleRequested;

        /// <summary>
        /// Raised by GameOverController on a full "Play Again" restart, just
        /// before ClearAllSubscriptions and the scene reload. The scene-wired
        /// ReplayTutorialService (which holds the APIClient) subscribes and
        /// wipes the server-side player state for the current game mode.
        /// This is the architecture-clean way for the field-less, auto-spawned
        /// GameOverController to trigger a server wipe without referencing
        /// APIClient directly.
        /// </summary>
        public static event Action OnPlayerStateWipeRequested;

        /// <summary>
        /// Fired by BootFlowRouter once it has computed which path a player
        /// should take after clicking Start. GameFlowController subscribes
        /// and routes: FirstTimeTutorial runs the tutorial controller,
        /// NormalCarousel shows the existing rules carousel, SkipTutorial
        /// goes directly to countdown (teacher preview path).
        /// </summary>
        public static event Action<BootFlow> OnBootFlowDecided;

        /// <summary>
        /// Fired by GameFlowController when a FirstTimeTutorial flow has
        /// been decided, or by ReplayTutorialService when the player chose
        /// "Replay tutorial" in settings. The bool payload distinguishes
        /// the two cases: false = first-pass (Skip button never appears),
        /// true = replay (Skip button revealed after step 1, as before).
        /// </summary>
        public static event Action<bool> OnTutorialStartRequested;

        /// <summary>
        /// Fired by IntroTutorialController when its final step completes
        /// (or the Skip button confirms). GameFlowController subscribes and
        /// resumes the normal countdown path.
        /// </summary>
        public static event Action OnTutorialComplete;

        // ═══════════════════════════════════════════════════════════════
        // TUTORIAL UI CONTROL (controller → UI)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Show or hide the tutorial overlay root.</summary>
        public static event Action<bool> OnTutorialOverlayVisibilityChanged;

        /// <summary>Dialog text + character pose for the current step.</summary>
        public static event Action<string, CharacterPose> OnTutorialDialogChanged;

        /// <summary>World-space target to highlight. null clears the highlight.</summary>
        public static event Action<Transform> OnTutorialHighlightTarget;

        /// <summary>
        /// Per-step extra screen offset added to the arrow on top of
        /// TutorialHighlight's global offset. Reset to zero each step.
        /// </summary>
        public static event Action<Vector2> OnTutorialArrowOffsetChanged;

        /// <summary>Block (true) or unblock (false) UI input during tutorial beats.</summary>
        public static event Action<bool> OnTutorialInputBlockChanged;

        /// <summary>Reveal the Skip button after the first dialog scene completes.</summary>
        public static event Action OnTutorialSkipRevealed;

        /// <summary>
        /// Fired when a Dialog step is entered. UI handlers switch the mask
        /// overlay to full-screen dim and show the Next button.
        /// </summary>
        public static event Action OnTutorialDialogModeEntered;

        /// <summary>
        /// Fired when a WaitForX step is entered. Payload is the target's
        /// screen-space Rect (pixels). UI handlers cut a donut hole around
        /// the rect and hide the Next button so only the real game action
        /// can advance the tutorial.
        /// </summary>
        public static event Action<Rect> OnTutorialWaitModeEntered;

        /// <summary>
        /// Fired when a Dialog step that ALSO points at a target is entered
        /// (e.g. "here's the Investing tab" with arrow + donut hole on the
        /// HUD button, but Next still advances). UI handlers cut the donut
        /// AND keep the Next button visible.
        /// </summary>
        public static event Action<Rect> OnTutorialDialogWithHighlightEntered;

        /// <summary>
        /// Fired when the tutorial step wants the dialog frame + character
        /// hidden (in-panel steps) or shown (everything else). Decoupled
        /// from mode entry so each step can independently choose.
        /// </summary>
        public static event Action<bool> OnTutorialDialogVisibilityChanged;

        /// <summary>
        /// Fired by the loan panel's Shop-tab signaler when the player
        /// switches to the Shop subpanel inside Credit &amp; Loans.
        /// Tutorial step `WaitForLoanShopTabSelected` advances on this event.
        /// </summary>
        public static event Action OnLoanShopTabSelected;

        /// <summary>
        /// Tutorial-driven request that UIManager close any open panels and
        /// popups, used when transitioning out of an in-panel step into a
        /// world-space step.
        /// </summary>
        public static event Action OnTutorialClosePanelsRequested;

        /// <summary>
        /// Tutorial-driven gate for world-space hover UIs (BlockHoverController).
        /// True allows the hover canvas to appear even while the tutorial
        /// holds the modal-panel flag; false restores normal suppression.
        /// </summary>
        public static event Action<bool> OnTutorialWorldHoverAllowedChanged;

        // ═══════════════════════════════════════════════════════════════
        // TUTORIAL INPUT (UI → controller)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Player tapped the dialog box's advance area.</summary>
        public static event Action OnTutorialAdvanceRequested;

        /// <summary>Player tapped the Skip Tutorial button.</summary>
        public static event Action OnTutorialSkipRequested;

        // ═══════════════════════════════════════════════════════════════
        // EVENT INVOKERS (called by systems to fire events)
        // ═══════════════════════════════════════════════════════════════

        public static void RaiseTick(int tickNumber) => OnTick?.Invoke(tickNumber);
        public static void RaiseGameSpeedChanged(float speed) => OnGameSpeedChanged?.Invoke(speed);
        public static void RaiseBootFlowDecided(BootFlow flow) => OnBootFlowDecided?.Invoke(flow);
        public static void RaiseTutorialStartRequested(bool isReplay = false) => OnTutorialStartRequested?.Invoke(isReplay);
        public static void RaiseTutorialComplete() => OnTutorialComplete?.Invoke();
        public static void RaiseTutorialOverlayVisibilityChanged(bool visible) => OnTutorialOverlayVisibilityChanged?.Invoke(visible);
        public static void RaiseTutorialDialogChanged(string text, CharacterPose pose) => OnTutorialDialogChanged?.Invoke(text, pose);
        public static void RaiseTutorialHighlightTarget(Transform target) => OnTutorialHighlightTarget?.Invoke(target);
        public static void RaiseTutorialArrowOffsetChanged(Vector2 offset) => OnTutorialArrowOffsetChanged?.Invoke(offset);
        public static void RaiseTutorialInputBlockChanged(bool blocked) => OnTutorialInputBlockChanged?.Invoke(blocked);
        public static void RaiseTutorialSkipRevealed() => OnTutorialSkipRevealed?.Invoke();
        public static void RaiseTutorialDialogModeEntered() => OnTutorialDialogModeEntered?.Invoke();
        public static void RaiseTutorialWaitModeEntered(Rect screenRect) => OnTutorialWaitModeEntered?.Invoke(screenRect);
        public static void RaiseTutorialDialogWithHighlightEntered(Rect screenRect) => OnTutorialDialogWithHighlightEntered?.Invoke(screenRect);
        public static void RaiseTutorialDialogVisibilityChanged(bool visible) => OnTutorialDialogVisibilityChanged?.Invoke(visible);
        public static void RaiseLoanShopTabSelected() => OnLoanShopTabSelected?.Invoke();
        public static void RaiseTutorialClosePanelsRequested() => OnTutorialClosePanelsRequested?.Invoke();
        public static void RaiseTutorialWorldHoverAllowedChanged(bool allowed) => OnTutorialWorldHoverAllowedChanged?.Invoke(allowed);
        public static void RaiseTutorialAdvanceRequested() => OnTutorialAdvanceRequested?.Invoke();
        public static void RaiseTutorialSkipRequested() => OnTutorialSkipRequested?.Invoke();
        public static void RaiseCurrencyChanged(float newBalance, float delta) => OnCurrencyChanged?.Invoke(newBalance, delta);
        public static void RaiseCheckingBalanceChanged(float balance, float delta) => OnCheckingBalanceChanged?.Invoke(balance, delta);
        public static void RaiseInvestingBalanceChanged(float balance, float delta) => OnInvestingBalanceChanged?.Invoke(balance, delta);
        // Transfer removed -- single-balance architecture does not support transfers.
        // Re-add when dual-account model is implemented for loans/insurance.
        public static void RaiseIncomeGenerated(float amount, string source) => OnIncomeGenerated?.Invoke(amount, source);
        public static void RaiseIncomeGeneratedWithPosition(float amount, Vector3 position) => OnIncomeGeneratedWithPosition?.Invoke(amount, position);
        public static void RaiseRivalIncomeWithPosition(float amount, Vector3 position) => OnRivalIncomeGeneratedWithPosition?.Invoke(amount, position);
        public static void RaiseInvestmentCompounded(ActiveInvestment inv) => OnInvestmentCompounded?.Invoke(inv);
        public static void RaiseInvestmentCreated(ActiveInvestment inv) => OnInvestmentCreated?.Invoke(inv);
        public static void RaiseInvestmentWithdrawn(ActiveInvestment inv, float payout) => OnInvestmentWithdrawn?.Invoke(inv, payout);
        public static void RaisePortfolioHoldingSelected(ActiveInvestment inv) => OnPortfolioHoldingSelected?.Invoke(inv);
        public static void RaiseTradeRequested(InvestmentDefinition def) => OnTradeRequested?.Invoke(def);
        public static void RaiseLotPurchased(string lotId, Owner owner) => OnLotPurchased?.Invoke(lotId, owner);
        public static void RaiseRivalTargetingLot(string lotId) => OnRivalTargetingLot?.Invoke(lotId);
        public static void RaiseRivalTargetChanged(string lotId, int daysUntil) => OnRivalTargetChanged?.Invoke(lotId, daysUntil);
        public static void RaiseRivalPurchasedLot(string lotId) => OnRivalPurchasedLot?.Invoke(lotId);
        public static void RaiseRivalUpgradedLot(string lotId, int newTier) => OnRivalUpgradedLot?.Invoke(lotId, newTier);
        public static void RaiseRivalBalanceChanged(float balance) => OnRivalBalanceChanged?.Invoke(balance);
        public static void RaiseGameEnd(Owner winner) => OnGameEnd?.Invoke(winner);
        public static void RaiseGameEndWithSummary(bool isPlayerWin, GameSummary summary) => OnGameEndWithSummary?.Invoke(isPlayerWin, summary);
        public static void RaiseGameStart() => OnGameStart?.Invoke();
        public static void RaiseCityInitialized(int totalLots) => OnCityInitialized?.Invoke(totalLots);
        public static void RaiseRestaurantSelected() => OnRestaurantSelected?.Invoke();
        public static void RaiseRestaurantUpgraded(int level) => OnRestaurantUpgraded?.Invoke(level);
        public static void RaiseShowTitleScreen() => OnShowTitleScreen?.Invoke();
        public static void RaiseHideTitleScreen() => OnHideTitleScreen?.Invoke();
        public static void RaiseShowRulesCarousel() => OnShowRulesCarousel?.Invoke();
        public static void RaiseHideRulesCarousel() => OnHideRulesCarousel?.Invoke();
        public static void RaiseStartCountdown() => OnStartCountdown?.Invoke();
        public static void RaiseSetHUDVisible(bool visible) => OnSetHUDVisible?.Invoke(visible);
        public static void RaisePanelOpened(PanelType panelType) => OnPanelOpened?.Invoke(panelType);
        public static void RaiseHidePanelRequested(PanelType panelType) => OnHidePanelRequested?.Invoke(panelType);
        public static void RaiseStartRequested() => OnStartRequested?.Invoke();
        public static void RaiseCarouselComplete() => OnCarouselComplete?.Invoke();
        public static void RaiseCountdownComplete() => OnCountdownComplete?.Invoke();
        public static void RaiseRestartRequested() => OnRestartRequested?.Invoke();
        public static void RaiseReturnToTitleRequested() => OnReturnToTitleRequested?.Invoke();
        public static void RaisePlayerStateWipeRequested() => OnPlayerStateWipeRequested?.Invoke();

        // Intent event invokers
        public static void RaisePurchaseLotRequested(string lotId, int tick) => OnPurchaseLotRequested?.Invoke(lotId, tick);
        public static void RaiseUpgradeRestaurantRequested() => OnUpgradeRestaurantRequested?.Invoke();
        public static void RaiseBuySharesRequested(InvestmentDefinition def, int qty) => OnBuySharesRequested?.Invoke(def, qty);
        public static void RaiseSellSharesRequested(ActiveInvestment inv, int qty) => OnSellSharesRequested?.Invoke(inv, qty);

        // Credit card events
        public static event Action<float, string> OnCreditCardChargeRequested;
        public static void RaiseCreditCardChargeRequested(float amount, string reason) => OnCreditCardChargeRequested?.Invoke(amount, reason);

        public static event Action<float> OnCreditCardCharged;
        public static void RaiseCreditCardCharged(float amount) => OnCreditCardCharged?.Invoke(amount);

        // statementBalance, minimumPayment, interestCharged -- enriched so popup can self-configure
        public static event Action<float, float, float> OnCreditCardStatementReady;
        public static void RaiseCreditCardStatementReady(float statementBalance, float minimumPayment, float interestCharged)
            => OnCreditCardStatementReady?.Invoke(statementBalance, minimumPayment, interestCharged);

        // balance, delta -- mirrors OnCheckingBalanceChanged / OnInvestingBalanceChanged pattern
        public static event Action<float, float> OnCreditCardBalanceChanged;
        public static void RaiseCreditCardBalanceChanged(float balance, float delta) => OnCreditCardBalanceChanged?.Invoke(balance, delta);

        // Persistence: carries the state-build delegate to AutoSaveController
        public static event Action<System.Func<FortuneValley.Domain.Entities.GamePlayerStateDTO>> OnStateBuildFuncProvided;
        public static void RaiseStateBuildFuncProvided(System.Func<FortuneValley.Domain.Entities.GamePlayerStateDTO> buildFunc)
            => OnStateBuildFuncProvided?.Invoke(buildFunc);

        public static event Action<float> OnCreditCardPaymentRequested;
        public static void RaiseCreditCardPaymentRequested(float amount) => OnCreditCardPaymentRequested?.Invoke(amount);

        public static event Action<float> OnCreditCardPaymentCompleted;
        public static void RaiseCreditCardPaymentCompleted(float amountPaid) => OnCreditCardPaymentCompleted?.Invoke(amountPaid);

        public static event Action<int> OnCreditScoreChanged;
        public static void RaiseCreditScoreChanged(int newScore) => OnCreditScoreChanged?.Invoke(newScore);

        // Transfer intent event (UI fires, CurrencyManager handles)
        public static event Action<AccountType, AccountType, float> OnTransferRequested;
        public static void RaiseTransferRequested(AccountType from, AccountType to, float amount) => OnTransferRequested?.Invoke(from, to, amount);

        // Insurance events
        public static event Action<string, string> OnPurchaseInsuranceRequested;    // lotId, policyId (intent from UI)
        public static void RaisePurchaseInsuranceRequested(string lotId, string policyId) => OnPurchaseInsuranceRequested?.Invoke(lotId, policyId);

        public static event Action<string, InsurancePolicyType> OnCancelInsuranceRequested;  // lotId, policyType (intent from UI)
        public static void RaiseCancelInsuranceRequested(string lotId, InsurancePolicyType type) => OnCancelInsuranceRequested?.Invoke(lotId, type);

        public static event Action<string, string> OnInsurancePurchased;            // lotId, policyId (confirmation)
        public static void RaiseInsurancePurchased(string lotId, string policyId) => OnInsurancePurchased?.Invoke(lotId, policyId);

        public static event Action<string, InsurancePolicyType> OnInsuranceCanceled;  // lotId, policyType (confirmation)
        public static void RaiseInsuranceCanceled(string lotId, InsurancePolicyType type) => OnInsuranceCanceled?.Invoke(lotId, type);

        public static event Action<string, string, float> OnInsurancePremiumCharged;  // lotId, policyId, amount
        public static void RaiseInsurancePremiumCharged(string lotId, string policyId, float amount)
            => OnInsurancePremiumCharged?.Invoke(lotId, policyId, amount);

        // Accident events
        public static event Action<AccidentRollResult> OnAccidentOccurred;           // raw accident trigger
        public static void RaiseAccidentOccurred(AccidentRollResult result) => OnAccidentOccurred?.Invoke(result);

        // lotId, accidentName, totalDamageCost, wasCovered, playerCost
        public static event Action<string, string, float, bool, float> OnAccidentResolved;
        public static void RaiseAccidentResolved(string lotId, string accidentName, float totalDamageCost, bool wasCovered, float playerCost)
            => OnAccidentResolved?.Invoke(lotId, accidentName, totalDamageCost, wasCovered, playerCost);

        // Loan events
        public static event Action<string, float> OnLoanSelectionRequested;         // lotId, price (intent from LotPurchasePopup)
        public static void RaiseLoanSelectionRequested(string lotId, float price) => OnLoanSelectionRequested?.Invoke(lotId, price);

        public static event Action<string, string, float> OnLoanPurchaseRequested;  // loanConfigId, lotId, price (intent from LoanSelectionPopup)
        public static void RaiseLoanPurchaseRequested(string loanConfigId, string lotId, float price) => OnLoanPurchaseRequested?.Invoke(loanConfigId, lotId, price);

        public static event Action<ActiveLoan> OnLoanOriginated;                    // loan created (confirmation)
        public static void RaiseLoanOriginated(ActiveLoan loan) => OnLoanOriginated?.Invoke(loan);

        public static event Action<ActiveLoan, float> OnLoanPaymentMade;            // loan, amount paid
        public static void RaiseLoanPaymentMade(ActiveLoan loan, float amount) => OnLoanPaymentMade?.Invoke(loan, amount);

        public static event Action<ActiveLoan> OnLoanPaymentMissed;                 // loan (checking insufficient)
        public static void RaiseLoanPaymentMissed(ActiveLoan loan) => OnLoanPaymentMissed?.Invoke(loan);

        public static event Action<ActiveLoan> OnLoanPaidOff;                       // loan fully repaid
        public static void RaiseLoanPaidOff(ActiveLoan loan) => OnLoanPaidOff?.Invoke(loan);

        public static event Action<float, float> OnLoanBalanceChanged;              // total outstanding principal, delta
        public static void RaiseLoanBalanceChanged(float total, float delta) => OnLoanBalanceChanged?.Invoke(total, delta);

        // Monthly payment cycle events
        public static event Action<int> OnMonthlyPaymentDayStarted;    // dayNumber
        public static void RaiseMonthlyPaymentDayStarted(int dayNumber) => OnMonthlyPaymentDayStarted?.Invoke(dayNumber);

        public static event Action OnMonthlyPaymentCycleComplete;       // all steps finished
        public static void RaiseMonthlyPaymentCycleComplete() => OnMonthlyPaymentCycleComplete?.Invoke();

        // Day cycle invoker
        public static void RaiseDayEnd(int dayNumber) => OnDayEnd?.Invoke(dayNumber);

        // ═══════════════════════════════════════════════════════════════
        // LOT INFO / TIER EVENTS (world-space click flow)
        // ═══════════════════════════════════════════════════════════════

        // UI intent: open LotInfoPopup for this lot.
        public static event Action<string> OnLotInfoRequested;
        public static void RaiseLotInfoRequested(string lotId) => OnLotInfoRequested?.Invoke(lotId);

        // UI intent: upgrade a player-owned lot to the next tier.
        public static event Action<string> OnLotUpgradeRequested;
        public static void RaiseLotUpgradeRequested(string lotId) => OnLotUpgradeRequested?.Invoke(lotId);

        // Confirmation: a lot's tier changed. lotId, newTier (1..3).
        public static event Action<string, int> OnLotTierChanged;
        public static void RaiseLotTierChanged(string lotId, int newTier) => OnLotTierChanged?.Invoke(lotId, newTier);

        // UI intent: open InsurancePanel pre-filtered to this lot.
        public static event Action<string> OnLotInsuranceRequested;
        public static void RaiseLotInsuranceRequested(string lotId) => OnLotInsuranceRequested?.Invoke(lotId);

        // UI intent: open CreditSystemPanel on Explore tab pre-selected to this lot
        // (fired by LotInfoPopup when player clicks Buy but cannot afford).
        public static event Action<string> OnLotLoanExploreRequested;
        public static void RaiseLotLoanExploreRequested(string lotId) => OnLotLoanExploreRequested?.Invoke(lotId);

        // ═══════════════════════════════════════════════════════════════
        // BLOCK / COSMETIC VARIANT EVENTS
        // ═══════════════════════════════════════════════════════════════

        // Intent: BlockController requests the picker open because a tier just unlocked.
        // Parameters: lotId (the owned lot on the block), tierSlot (0..2, == newTier - 1).
        public static event Action<string, int> OnCosmeticPickerOpened;
        public static void RaiseCosmeticPickerOpened(string lotId, int tierSlot) => OnCosmeticPickerOpened?.Invoke(lotId, tierSlot);

        // Confirmation: player picked a variant. Parameters: lotId, tierSlot, variantId (index into catalog).
        public static event Action<string, int, int> OnCosmeticVariantChosen;
        public static void RaiseCosmeticVariantChosen(string lotId, int tierSlot, int variantId) => OnCosmeticVariantChosen?.Invoke(lotId, tierSlot, variantId);

        // Visual apply: raised by CityManager after a pick is stored (fresh pick or save replay).
        // instant==true on save-replay so BlockController instantiates without the DOTween animation.
        public static event Action<string, int, int, bool> OnCosmeticVariantApplied;
        public static void RaiseCosmeticVariantApplied(string lotId, int tierSlot, int variantId, bool instant) => OnCosmeticVariantApplied?.Invoke(lotId, tierSlot, variantId, instant);

        // Phase 1 of restore: raised by GameSaveBootstrapper after parsing the
        // JSON delivered by the host page (window.FV.loadState round-trip).
        // Each system subscribes and hydrates its own DTO fields.
        public static event Action<GamePlayerStateDTO> OnSaveStateLoaded;
        public static void RaiseSaveStateLoaded(GamePlayerStateDTO dto) => OnSaveStateLoaded?.Invoke(dto);

        // Raised by UIPopup.Show/Hide so world-space interaction systems (e.g. BlockHoverController)
        // can suppress input while a modal popup is open. True = opening, false = closing.
        public static event Action<bool> OnBlockingPanelOpenChanged;
        public static void RaiseBlockingPanelOpenChanged(bool open) => OnBlockingPanelOpenChanged?.Invoke(open);

        // ═══════════════════════════════════════════════════════════════
        // QUESTIONMASTER EVENTS
        // ═══════════════════════════════════════════════════════════════

        // UI intent: start or restart a session.
        public static event Action OnQuestionStartRequested;
        public static void RaiseQuestionStartRequested() => OnQuestionStartRequested?.Invoke();

        // UI intent: override reward tunables for the next session. baseReward, streakMultiplier.
        public static event Action<float, float> OnQuestionRewardConfigOverrideRequested;
        public static void RaiseQuestionRewardConfigOverrideRequested(float baseReward, float streakMultiplier)
            => OnQuestionRewardConfigOverrideRequested?.Invoke(baseReward, streakMultiplier);

        // UI intent: player chose an answer. chosenIndex -1 denotes timeout (fired by manager, not UI).
        public static event Action<int> OnQuestionAnswerSubmitted;
        public static void RaiseQuestionAnswerSubmitted(int chosenIndex) => OnQuestionAnswerSubmitted?.Invoke(chosenIndex);

        public static event Action OnQuestionSessionStarted;
        public static void RaiseQuestionSessionStarted() => OnQuestionSessionStarted?.Invoke();

        // question, currentStreak
        public static event Action<QuestionData, int> OnQuestionPresented;
        public static void RaiseQuestionPresented(QuestionData q, int streak) => OnQuestionPresented?.Invoke(q, streak);

        // remainingSeconds, totalSeconds
        public static event Action<float, float> OnQuestionTimerTick;
        public static void RaiseQuestionTimerTick(float remaining, float total) => OnQuestionTimerTick?.Invoke(remaining, total);

        // question, correct, chosenIndex (-1 on timeout), correctIndex, currentStreak
        // Streak reflects the session-level correct-answer count AFTER this submission
        // resolves (0 if this answer was wrong or timed out; n if this answer continued
        // a streak of length n).
        public static event Action<QuestionData, bool, int, int, int> OnQuestionAnswered;
        public static void RaiseQuestionAnswered(QuestionData question, bool correct, int chosenIndex, int correctIndex, int currentStreak)
            => OnQuestionAnswered?.Invoke(question, correct, chosenIndex, correctIndex, currentStreak);

        // amount, newStreak
        public static event Action<int, int> OnQuestionRewardGranted;
        public static void RaiseQuestionRewardGranted(int amount, int newStreak) => OnQuestionRewardGranted?.Invoke(amount, newStreak);

        public static event Action OnQuestionSessionEnded;
        public static void RaiseQuestionSessionEnded() => OnQuestionSessionEnded?.Invoke();

        // ═══════════════════════════════════════════════════════════════
        // TAP-TO-COLLECT INCOME EVENTS
        // ═══════════════════════════════════════════════════════════════
        // Scale assumption: <=8 player-owned buildings. At larger counts the
        // per-subscriber id-match cost on OnCoinStateChanged becomes worth
        // optimizing (e.g. per-id subscription dictionary).

        // Fired whenever a per-building coin state changes (day locked, tick
        // drained, ready-flipped, collected, ownership changed).
        // Parameters:
        //   buildingId:  the lot or "restaurant" bucket id
        //   dailyPayout: the amount that will be deposited on collect; fixed
        //                for the duration of the current day cycle
        //   progress01:  drain progress in [0,1]. 1 = just locked (overlay
        //                full), 0 = ready (overlay drained, coin revealed)
        //   isReady:     true iff the coin is tappable right now
        public static event Action<string, float, float, bool> OnCoinStateChanged;
        public static void RaiseCoinStateChanged(string buildingId, float dailyPayout, float progress01, bool isReady)
            => OnCoinStateChanged?.Invoke(buildingId, dailyPayout, progress01, isReady);

        // UI -> service request for a re-emit of the current coin state.
        // Raised by BuildingCollectButton in OnEnable so the button seeds
        // correctly even if it enables after the last state-change event fired
        // (scene load, prefab instantiation, hydrate). Expect a burst of
        // queries + responses during scene load (one per coin-reading UI);
        // negligible at POC bucket counts.
        public static event Action<string> OnIncomePendingQuery;
        public static void RaiseIncomePendingQuery(string buildingId) => OnIncomePendingQuery?.Invoke(buildingId);

        // Raised by BlockHoverController when the mouse enters or exits a
        // block's footprint. Lets per-lot UI (e.g. BuildingCollectButton)
        // decide its own visibility without being gated by the hover canvas.
        // Parameters: lotId (null when no BlockController is wired), hovered.
        public static event Action<string, bool> OnBlockHoverChanged;
        public static void RaiseBlockHoverChanged(string lotId, bool hovered)
            => OnBlockHoverChanged?.Invoke(lotId, hovered);

        // UI intent: player tapped collect on a building. Also fired internally
        // by DailyIncomeAccumulator on ownership loss with CollectReason.OwnershipLost.
        public static event Action<string, CollectReason> OnIncomeCollectRequested;
        public static void RaiseIncomeCollectRequested(string buildingId, CollectReason reason)
            => OnIncomeCollectRequested?.Invoke(buildingId, reason);

        // Confirmation: an amount was successfully deposited for a building.
        public static event Action<string, float> OnIncomeCollected;
        public static void RaiseIncomeCollected(string buildingId, float amount)
            => OnIncomeCollected?.Invoke(buildingId, amount);

        // Aggregate daily-income rate across all player-owned income buildings.
        // Raised by DailyIncomeAccumulator (coalesced once per frame in LateUpdate)
        // whenever the rounded total changes. DailyIncomeHud subscribes.
        public static event Action<float> OnTotalDailyIncomeChanged;
        public static void RaiseTotalDailyIncomeChanged(float total)
            => OnTotalDailyIncomeChanged?.Invoke(total);

        // Raised by CityManager when a lot's owner changes. Replaces the need
        // to diff against a cached prev-owner inside each subscriber.
        public static event Action<string, Owner, Owner> OnLotOwnershipChanged;
        public static void RaiseLotOwnershipChanged(string lotId, Owner previousOwner, Owner newOwner)
            => OnLotOwnershipChanged?.Invoke(lotId, previousOwner, newOwner);

        // Generic save-request intent. AutoSaveController subscribes and debounces.
        public static event Action OnSaveRequested;
        public static void RaiseSaveRequested() => OnSaveRequested?.Invoke();

        // ═══════════════════════════════════════════════════════════════
        // LIFE GOALS / NET WORTH / LIFESPAN
        // ═══════════════════════════════════════════════════════════════

        // Fired by GoalSelectionPanelController when the player confirms their
        // 3 picks during the intro tutorial. Tutorial advances on receipt.
        public static event Action<LifeGoalSelection> OnLifeGoalsSelected;
        public static void RaiseLifeGoalsSelected(LifeGoalSelection selection)
            => OnLifeGoalsSelected?.Invoke(selection);

        // Fired by IntroTutorialController to show/hide the GoalSelectionPanel
        // when entering/leaving the WaitForLifeGoalsSelected step. Managers
        // layer cannot reference the UI panel directly, so toggling goes
        // through an event the panel subscribes to.
        public static event Action<bool> OnGoalSelectionPanelRequested;
        public static void RaiseGoalSelectionPanelRequested(bool visible)
            => OnGoalSelectionPanelRequested?.Invoke(visible);

        // Fired by NetWorthService at most once per tick when either Total NW
        // or Liquid NW changes. Parameters: totalNetWorth, liquidNetWorth.
        public static event Action<float, float> OnNetWorthChanged;
        public static void RaiseNetWorthChanged(float totalNetWorth, float liquidNetWorth)
            => OnNetWorthChanged?.Invoke(totalNetWorth, liquidNetWorth);

        // Fired by GoalProgressTracker when a goal threshold is crossed.
        // Sticky -- realized goals stay realized even if NW drops.
        public static event Action<LifeGoalEntry> OnGoalRealized;
        public static void RaiseGoalRealized(LifeGoalEntry entry)
            => OnGoalRealized?.Invoke(entry);

        // Drives the HUD progress slider toward the next-cheapest unrealized goal.
        // Parameters: currentNetWorth, prevRealizedThreshold (lower bound),
        // nextThreshold (upper bound). When all goals realized, this stops firing.
        public static event Action<float, float, float> OnGoalProgressChanged;
        public static void RaiseGoalProgressChanged(float currentNetWorth, float prevThreshold, float nextThreshold)
            => OnGoalProgressChanged?.Invoke(currentNetWorth, prevThreshold, nextThreshold);

        // Fired by GoalProgressTracker exactly once per life when all three goals
        // become realized. Parameter: the highest goal threshold (final tier value).
        // Subscribers use this for the HUD "trophy state" pin (slider full + frozen).
        // Idempotent: tracker tracks last-fired state and will not re-emit.
        public static event Action<float> OnAllGoalsRealized;
        public static void RaiseAllGoalsRealized(float finalThreshold)
            => OnAllGoalsRealized?.Invoke(finalThreshold);

        // Pull-pattern snapshot request. Fresh subscribers (HUD on scene load,
        // save load, etc.) raise this in OnEnable to ask NetWorthService to
        // re-emit OnNetWorthChanged with current cached values, regardless of
        // whether values changed. GoalProgressTracker rides along via the
        // cascaded OnNetWorthChanged and re-fires its own progress events.
        public static event Action OnRequestNetWorthSnapshot;
        public static void RaiseRequestNetWorthSnapshot()
            => OnRequestNetWorthSnapshot?.Invoke();

        // Pull-pattern snapshot request for the locked-in life-goal selection.
        // ProfileWebBridge subscribes to OnLifeGoalsSelected only while the
        // panel is open, so the tutorial's one-shot selection event fires
        // before the bridge is listening. Raising this on panel open asks
        // LifeGoalSelectionService (the live source of truth, owned by
        // GameManager) to re-emit OnLifeGoalsSelected with its current
        // selection so the panel paints the real goals instead of placeholders.
        public static event Action OnRequestLifeGoalsSnapshot;
        public static void RaiseRequestLifeGoalsSnapshot()
            => OnRequestLifeGoalsSnapshot?.Invoke();

        // Fired by LifespanController on each in-game year boundary. Parameter: new age.
        public static event Action<int> OnYearEnd;
        public static void RaiseYearEnd(int age)
            => OnYearEnd?.Invoke(age);

        // Fired exactly once when the player reaches retirement age (65).
        // RetirementEvaluator subscribes and triggers the goal scorecard end-game.
        public static event Action OnRetirementReached;
        public static void RaiseRetirementReached()
            => OnRetirementReached?.Invoke();

        // Fired by RetirementEvaluator once the realized/missed split is computed.
        // GameEndPanel subscribes to render the scorecard.
        public static event Action<GoalScorecard> OnGoalsEvaluated;
        public static void RaiseGoalsEvaluated(GoalScorecard scorecard)
            => OnGoalsEvaluated?.Invoke(scorecard);

        // ═══════════════════════════════════════════════════════════════
        // BANKRUPTCY (soft reset)
        // ═══════════════════════════════════════════════════════════════

        // Fired by InsolvencyMonitor when the 5-cycle threshold is reached.
        // BankruptcyResetService subscribes and orchestrates the soft reset.
        public static event Action OnBankruptcyTriggered;
        public static void RaiseBankruptcyTriggered()
            => OnBankruptcyTriggered?.Invoke();

        // Fired by BankruptcyResetService after all IBankruptcyResettable systems
        // have been reset. UI (BankruptcyPopup), AutoSaveController, HUD listen.
        public static event Action OnSoftBankruptcyReset;
        public static void RaiseSoftBankruptcyReset()
            => OnSoftBankruptcyReset?.Invoke();

        // Fired by CityManager.BatchResetPlayerLots so subscribers can update
        // visuals in one pass instead of receiving N per-lot events. Carries
        // the lot ids that were just released back to "for sale" empty state.
        public static event Action<string[]> OnLotsBatchReset;
        public static void RaiseLotsBatchReset(string[] lotIds)
            => OnLotsBatchReset?.Invoke(lotIds);

        // ═══════════════════════════════════════════════════════════════
        // PERSISTENCE RESTORE (two-phase signal + catch-up handles)
        // ═══════════════════════════════════════════════════════════════

        // Phase 2 of restore: GameSaveBootstrapper queues this for the frame
        // after Apply, so any system that hydrated on Phase 1 (OnSaveStateLoaded)
        // is already done by the time reconcile listeners run. CurrencyManager
        // subscribes to call RefreshInvestingBalance after InvestmentSystem has
        // rebuilt its portfolio.
        public static event Action OnSaveRestored;
        public static void RaiseSaveRestored() => OnSaveRestored?.Invoke();

        // Catch-up handle for Phase 1. Set by GameSaveBootstrapper.Apply and
        // write-throughed by AutoSaveController.PerformSave so cross-scene
        // re-entry sees the latest local state, not the original session-start
        // DTO. Newly-instantiated systems read this in their OnEnable to
        // hydrate without waiting for another network round-trip.
        // Intentionally NOT cleared in ClearAllSubscriptions.
        public static GamePlayerStateDTO LastLoadedSaveDto { get; set; }

        // Catch-up handle for Phase 2. Set when OnSaveRestored fires so
        // late-joining Phase-2 subscribers know the reconcile already happened
        // and can run theirs immediately on OnEnable.
        // Intentionally NOT cleared in ClearAllSubscriptions.
        public static bool HasSaveBeenRestored { get; set; }

        // True only when a genuine server save was restored this session
        // (set by GameSaveBootstrapper.Apply in Phase 1, before OnGameStart).
        // Distinct from LastLoadedSaveDto != null, which is ALSO true after any
        // autosave write-through or when any prior row exists -- neither of
        // which means "a returning player whose hydrated state must be kept."
        // HandleGameStart handlers gate destructive fresh-game resets on this
        // so a fresh player (or replay-tutorial) still seeds defaults even
        // after an autosave has populated LastLoadedSaveDto.
        // Cleared by ReplayTutorialService after a wipe. Intentionally NOT
        // cleared in ClearAllSubscriptions (survives scene reloads).
        public static bool SaveStateRestoredFromServer { get; set; }

        // Set by GameSaveBootstrapper.Apply when the server returns an empty
        // payload for an authenticated user (e.g. brand-new student with no
        // game_player_states row yet). IntroGate reads this so it can stop
        // falling back to the per-browser PlayerPrefs flag, which would
        // otherwise leak tutorial completion across student accounts on a
        // shared browser. Stays false for guests (bridge JS skips SendMessage
        // entirely when unauthenticated) so the existing PlayerPrefs fallback
        // for offline play is preserved.
        // Intentionally NOT cleared in ClearAllSubscriptions.
        public static bool HasServerConfirmedFreshUser { get; set; }

        // Longest correct-answer quiz streak reached this life. QuestionManager
        // bumps it (never lowers it) on a correct answer; GameStateDTOBuilder
        // reads it for autosave; GameManager.HandleSaveStateLoaded hydrates it
        // from the restored save. Lives here (not on QuestionManager) because
        // the quiz runs in an isolated Learning Level scene while the save
        // builder lives in Homebase, so the two never share a scene. Must
        // survive scene reloads, so it is intentionally NOT cleared in
        // ClearAllSubscriptions (same contract as LastLoadedSaveDto).
        public static int BestQuizStreak { get; set; }

        // Latched true by GameFlowController the instant it releases the
        // deferred StartGame() at countdown end, whether release happened
        // because the save round-trip flags flipped OR the bounded timeout
        // elapsed. Monotonic: only ever set true within a session. Closes the
        // cold-boot hydration race where a slow/failed GET let OnGameStart
        // win the countdown and reset every system to fresh defaults.
        // Intentionally NOT cleared in ClearAllSubscriptions (survives scene
        // reloads, same contract as the other persistence statics).
        // Intentionally NOT reset by ReplayTutorialService: a replay happens
        // mid-session when this is already true from the original start, so
        // the replayed countdown must release instantly; resetting it would
        // force a needless full-timeout wait before the replay game begins.
        public static bool StartBarrierReleased { get; set; }

        // Single barrier predicate. Read by GameFlowController (start barrier:
        // hold the destructive OnGameStart path until this is true OR the
        // timeout elapses) and AutoSaveController (autosave barrier: never
        // POST until this is true, so an un-hydrated state can never overwrite
        // the real server row). The flags-only terms also cover bypass paths
        // (_autoStart, a future direct RaiseGameStart) where GameFlowController
        // never set the latch, keeping the autosave barrier a hard backstop.
        public static bool SaveRoundTripResolved =>
            SaveStateRestoredFromServer || HasServerConfirmedFreshUser || StartBarrierReleased;

        // ═══════════════════════════════════════════════════════════════
        // CLEANUP (call when exiting play mode or restarting)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Clears all event subscriptions. Call on game restart or cleanup.
        /// </summary>
        public static void ClearAllSubscriptions()
        {
            OnTick = null;
            OnGameSpeedChanged = null;
            OnCurrencyChanged = null;
            OnCheckingBalanceChanged = null;
            OnInvestingBalanceChanged = null;
            OnIncomeGenerated = null;
            OnIncomeGeneratedWithPosition = null;
            OnRivalIncomeGeneratedWithPosition = null;
            OnInvestmentCompounded = null;
            OnInvestmentCreated = null;
            OnInvestmentWithdrawn = null;
            OnPortfolioHoldingSelected = null;
            OnTradeRequested = null;
            OnLotPurchased = null;
            OnRivalTargetingLot = null;
            OnRivalTargetChanged = null;
            OnRivalPurchasedLot = null;
            OnRivalUpgradedLot = null;
            OnRivalBalanceChanged = null;
            OnGameEnd = null;
            OnGameEndWithSummary = null;
            OnGameStart = null;
            OnCityInitialized = null;
            OnRestaurantSelected = null;
            OnRestaurantUpgraded = null;
            OnShowTitleScreen = null;
            OnHideTitleScreen = null;
            OnShowRulesCarousel = null;
            OnHideRulesCarousel = null;
            OnStartCountdown = null;
            OnSetHUDVisible = null;
            OnPanelOpened = null;
            OnHidePanelRequested = null;
            OnStartRequested = null;
            OnCarouselComplete = null;
            OnCountdownComplete = null;
            OnRestartRequested = null;
            OnReturnToTitleRequested = null;
            OnPlayerStateWipeRequested = null;
            OnBootFlowDecided = null;
            OnTutorialStartRequested = null;
            OnTutorialComplete = null;
            OnTutorialOverlayVisibilityChanged = null;
            OnTutorialDialogChanged = null;
            OnTutorialHighlightTarget = null;
            OnTutorialArrowOffsetChanged = null;
            OnTutorialInputBlockChanged = null;
            OnTutorialSkipRevealed = null;
            OnTutorialDialogModeEntered = null;
            OnTutorialWaitModeEntered = null;
            OnTutorialDialogWithHighlightEntered = null;
            OnTutorialDialogVisibilityChanged = null;
            OnLoanShopTabSelected = null;
            OnTutorialClosePanelsRequested = null;
            OnTutorialWorldHoverAllowedChanged = null;
            OnTutorialAdvanceRequested = null;
            OnTutorialSkipRequested = null;

            // Intent events
            OnPurchaseLotRequested = null;
            OnUpgradeRestaurantRequested = null;
            OnBuySharesRequested = null;
            OnSellSharesRequested = null;

            // Credit card
            OnCreditCardChargeRequested = null;
            OnCreditCardCharged = null;
            OnCreditCardStatementReady = null;
            OnCreditCardBalanceChanged = null;
            OnStateBuildFuncProvided = null;
            OnCreditCardPaymentRequested = null;
            OnCreditCardPaymentCompleted = null;
            OnCreditScoreChanged = null;

            // Transfer
            OnTransferRequested = null;

            // Insurance
            OnPurchaseInsuranceRequested = null;
            OnCancelInsuranceRequested = null;
            OnInsurancePurchased = null;
            OnInsuranceCanceled = null;
            OnInsurancePremiumCharged = null;

            // Accidents
            OnAccidentOccurred = null;
            OnAccidentResolved = null;

            // Loans
            OnLoanSelectionRequested = null;
            OnLoanPurchaseRequested = null;
            OnLoanOriginated = null;
            OnLoanPaymentMade = null;
            OnLoanPaymentMissed = null;
            OnLoanPaidOff = null;
            OnLoanBalanceChanged = null;

            // Monthly payment cycle
            OnMonthlyPaymentDayStarted = null;
            OnMonthlyPaymentCycleComplete = null;

            // Day cycle
            OnDayEnd = null;

            // Lot info / tier
            OnLotInfoRequested = null;
            OnLotUpgradeRequested = null;
            OnLotTierChanged = null;
            OnLotInsuranceRequested = null;
            OnLotLoanExploreRequested = null;

            // Block / cosmetic variants
            OnCosmeticPickerOpened = null;
            OnCosmeticVariantChosen = null;
            OnCosmeticVariantApplied = null;
            OnBlockingPanelOpenChanged = null;

            // Persistence: clear event subscriptions but intentionally NOT
            // LastLoadedSaveDto / HasSaveBeenRestored. Those catch-up handles
            // survive scene reloads so late-joining systems can hydrate.
            OnSaveStateLoaded = null;
            OnSaveRestored = null;

            // QuestionMaster
            OnQuestionStartRequested = null;
            OnQuestionRewardConfigOverrideRequested = null;
            OnQuestionAnswerSubmitted = null;
            OnQuestionSessionStarted = null;
            OnQuestionPresented = null;
            OnQuestionTimerTick = null;
            OnQuestionAnswered = null;
            OnQuestionRewardGranted = null;
            OnQuestionSessionEnded = null;

            // Tap-to-collect income
            OnCoinStateChanged = null;
            OnIncomePendingQuery = null;
            OnBlockHoverChanged = null;
            OnIncomeCollectRequested = null;
            OnIncomeCollected = null;
            OnLotOwnershipChanged = null;
            OnSaveRequested = null;
            OnTotalDailyIncomeChanged = null;

            // Life Goals / Net Worth / Lifespan
            OnLifeGoalsSelected = null;
            OnGoalSelectionPanelRequested = null;
            OnNetWorthChanged = null;
            OnGoalRealized = null;
            OnGoalProgressChanged = null;
            OnAllGoalsRealized = null;
            OnRequestNetWorthSnapshot = null;
            OnRequestLifeGoalsSnapshot = null;
            OnYearEnd = null;
            OnRetirementReached = null;
            OnGoalsEvaluated = null;

            // Bankruptcy
            OnBankruptcyTriggered = null;
            OnSoftBankruptcyReset = null;
            OnLotsBatchReset = null;

            // Same-DTO replay tracking lives next to the persistence events.
            // Clearing here matches the scene-restart contract; LastLoadedSaveDto
            // is preserved per the comment block above.
            SaveRestoreCatchUp.ClearCache();
        }
    }
}
