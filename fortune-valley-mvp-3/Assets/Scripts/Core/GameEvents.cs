using System;
using UnityEngine;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Entities;

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

        // ═══════════════════════════════════════════════════════════════
        // EVENT INVOKERS (called by systems to fire events)
        // ═══════════════════════════════════════════════════════════════

        public static void RaiseTick(int tickNumber) => OnTick?.Invoke(tickNumber);
        public static void RaiseGameSpeedChanged(float speed) => OnGameSpeedChanged?.Invoke(speed);
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
        public static void RaiseStartRequested() => OnStartRequested?.Invoke();
        public static void RaiseCarouselComplete() => OnCarouselComplete?.Invoke();
        public static void RaiseCountdownComplete() => OnCountdownComplete?.Invoke();
        public static void RaiseRestartRequested() => OnRestartRequested?.Invoke();
        public static void RaiseReturnToTitleRequested() => OnReturnToTitleRequested?.Invoke();

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

        // Save replay: raised by GameSaveBootstrapper when the host page delivers saved state.
        // CityManager subscribes to apply cosmetic variants (and future: tiers, ownership, finances).
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
            OnStartRequested = null;
            OnCarouselComplete = null;
            OnCountdownComplete = null;
            OnRestartRequested = null;
            OnReturnToTitleRequested = null;

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
            OnSaveStateLoaded = null;
            OnBlockingPanelOpenChanged = null;

            // QuestionMaster
            OnQuestionStartRequested = null;
            OnQuestionAnswerSubmitted = null;
            OnQuestionSessionStarted = null;
            OnQuestionPresented = null;
            OnQuestionTimerTick = null;
            OnQuestionAnswered = null;
            OnQuestionRewardGranted = null;
            OnQuestionSessionEnded = null;
        }
    }
}
