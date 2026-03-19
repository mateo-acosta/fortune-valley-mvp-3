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

        /// <summary>
        /// Fired when money is transferred between accounts.
        /// Parameters: amount, from account, to account
        /// </summary>
        public static event Action<float, AccountType, AccountType> OnTransfer;

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
        public static void RaiseTransfer(float amount, AccountType from, AccountType to) => OnTransfer?.Invoke(amount, from, to);
        public static void RaiseIncomeGenerated(float amount, string source) => OnIncomeGenerated?.Invoke(amount, source);
        public static void RaiseIncomeGeneratedWithPosition(float amount, Vector3 position) => OnIncomeGeneratedWithPosition?.Invoke(amount, position);
        public static void RaiseRivalIncomeWithPosition(float amount, Vector3 position) => OnRivalIncomeGeneratedWithPosition?.Invoke(amount, position);
        public static void RaiseInvestmentCompounded(ActiveInvestment inv) => OnInvestmentCompounded?.Invoke(inv);
        public static void RaiseInvestmentCreated(ActiveInvestment inv) => OnInvestmentCreated?.Invoke(inv);
        public static void RaiseInvestmentWithdrawn(ActiveInvestment inv, float payout) => OnInvestmentWithdrawn?.Invoke(inv, payout);
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
            OnTransfer = null;
            OnIncomeGenerated = null;
            OnIncomeGeneratedWithPosition = null;
            OnRivalIncomeGeneratedWithPosition = null;
            OnInvestmentCompounded = null;
            OnInvestmentCreated = null;
            OnInvestmentWithdrawn = null;
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
        }
    }
}
