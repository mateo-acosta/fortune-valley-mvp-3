using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Managers
{
    /// <summary>
    /// Main game coordinator. Bootstraps the game and handles high-level state.
    ///
    /// DESIGN NOTE: This is intentionally thin. It coordinates, not controls.
    /// Each system manages itself; GameManager just starts/stops things.
    ///
    /// Life Goals revision: also constructs and owns the Life Goals services
    /// (NetWorthService, LifeGoalSelectionService, GoalProgressTracker,
    /// LifespanController, RetirementEvaluator, InsolvencyMonitor,
    /// BankruptcyResetService) since they are pure-C# (no MonoBehaviour
    /// wiring needed). The IBankruptcyResettable registry is built here too.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ===============================================================
        // SYSTEM REFERENCES
        // ===============================================================

        [Header("Core Systems")]
        [SerializeField] private TimeManager _timeManager;
        [SerializeField] private CurrencyManager _currencyManager;

        [Header("Gameplay Systems")]
        [SerializeField] private RestaurantSystem _restaurantSystem;
        [SerializeField] private InvestmentSystem _investmentSystem;
        [SerializeField] private CityManager _cityManager;
        [SerializeField] private RivalAI _rivalAI;

        [Header("Financial Systems")]
        [SerializeField] private CreditCardSystem _creditCardSystem;
        [SerializeField] private LoanSystem _loanSystem;
        [SerializeField] private InsuranceSystem _insuranceSystem;

        [Header("Income")]
        [SerializeField] private DailyIncomeAccumulator _pendingIncome;

        // State builder for persistence (pure C#, no MonoBehaviour)
        private GameStateDTOBuilder _stateDTOBuilder;

        // Life Goals services (pure C#, owned for the lifetime of GameManager).
        private LifeGoalSelectionService _lifeGoalSelection;
        private NetWorthService _netWorthService;
        private GoalProgressTracker _goalProgressTracker;
        private LifespanController _lifespanController;
        private RetirementEvaluator _retirementEvaluator;
        private InsolvencyMonitor _insolvencyMonitor;
        private BankruptcyResetService _bankruptcyResetService;
        private bool _retirementGameEnd; // distinguishes retirement vs bankruptcy hard-end

        [Header("Auto Start")]
        [Tooltip("Automatically start the game on scene load")]
        [SerializeField] private bool _autoStart = false;

        [Header("Debug")]
        [SerializeField] private bool _logStateChanges = true;

        // ===============================================================
        // RUNTIME STATE
        // ===============================================================

        private GameState _currentState = GameState.NotStarted;

        // ===============================================================
        // PUBLIC ACCESSORS
        // ===============================================================

        public GameState CurrentState => _currentState;
        public bool IsPlaying => _currentState == GameState.Playing;

        // System accessors for UI
        public TimeManager TimeManager => _timeManager;
        public CurrencyManager CurrencyManager => _currencyManager;
        public RestaurantSystem RestaurantSystem => _restaurantSystem;
        public InvestmentSystem InvestmentSystem => _investmentSystem;
        public CityManager CityManager => _cityManager;
        public RivalAI RivalAI => _rivalAI;

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        private void Awake()
        {
            // WebGL perf: cap render rate (vSync is off in QualitySettings) and
            // stop PhysX from running every FixedUpdate for a non-physics game.
            Application.targetFrameRate = 60;
            Physics.simulationMode = SimulationMode.Script;

            ValidateReferences();
            ConstructLifeGoalsServices();
        }

        private void Start()
        {
            if (_autoStart)
            {
                StartGame();
            }
        }

        private void OnEnable()
        {
            GameEvents.OnGameEnd += HandleGameEnd;
            GameEvents.OnSaveStateLoaded += HandleSaveStateLoaded;
            GameEvents.OnRetirementReached += HandleRetirementReached;
        }

        private void OnDisable()
        {
            GameEvents.OnGameEnd -= HandleGameEnd;
            GameEvents.OnSaveStateLoaded -= HandleSaveStateLoaded;
            GameEvents.OnRetirementReached -= HandleRetirementReached;
        }

        private void OnDestroy()
        {
            DisposeLifeGoalsServices();
        }

        // ===============================================================
        // PUBLIC METHODS
        // ===============================================================

        /// <summary>
        /// Start a new game.
        /// </summary>
        public void StartGame()
        {
            if (_currentState == GameState.Playing)
            {
                Debug.LogWarning("[GameManager] Game already in progress");
                return;
            }

            SetState(GameState.Playing);
            WireAutoSave();
            GameEvents.RaiseGameStart();

            if (_logStateChanges)
            {
                Debug.Log("[GameManager] Game started!");
            }
        }

        /// <summary>
        /// Restart the game (reset everything and start fresh).
        /// </summary>
        public void RestartGame()
        {
            if (_logStateChanges)
            {
                Debug.Log("[GameManager] Restarting game...");
            }

            // Systems will reset themselves when they receive OnGameStart
            SetState(GameState.Playing);
            GameEvents.RaiseGameStart();
        }

        /// <summary>
        /// Pause the game.
        /// </summary>
        public void PauseGame()
        {
            if (_currentState != GameState.Playing)
                return;

            SetState(GameState.Paused);
            _timeManager.StopTime();

            if (_logStateChanges)
            {
                Debug.Log("[GameManager] Game paused");
            }
        }

        /// <summary>
        /// Resume a paused game.
        /// </summary>
        public void ResumeGame()
        {
            if (_currentState != GameState.Paused)
                return;

            SetState(GameState.Playing);
            _timeManager.StartTime();

            if (_logStateChanges)
            {
                Debug.Log("[GameManager] Game resumed");
            }
        }

        /// <summary>
        /// Return to title screen state without firing OnGameStart.
        /// Called by GameFlowController when returning to the title screen.
        /// </summary>
        public void ReturnToTitle()
        {
            SetState(GameState.NotStarted);
            _timeManager.StopTime();

            if (_logStateChanges)
            {
                Debug.Log("[GameManager] Returned to title screen");
            }
        }

        /// <summary>
        /// Toggle pause state.
        /// </summary>
        public void TogglePause()
        {
            if (_currentState == GameState.Playing)
                PauseGame();
            else if (_currentState == GameState.Paused)
                ResumeGame();
        }

        /// <summary>
        /// Get a complete game summary for debugging or end-game display.
        /// </summary>
        public string GetGameSummary()
        {
            return $"=== Fortune Valley Summary ===\n\n" +
                   $"Day: {_timeManager.CurrentEnginePulse}\n\n" +
                   $"FINANCES:\n" +
                   $"Checking: ${_currencyManager.CheckingBalance:F0} | Investing: ${_currencyManager.InvestingBalance:F0}\n" +
                   $"{_restaurantSystem.GetPerformanceSummary()}\n\n" +
                   $"INVESTMENTS:\n" +
                   $"{_investmentSystem.GetPortfolioSummary()}\n\n" +
                   $"CITY:\n" +
                   $"{_cityManager.GetCitySummary()}\n\n" +
                   $"RIVAL:\n" +
                   $"{_rivalAI.GetRivalStatus()}";
        }

        // ===============================================================
        // PERSISTENCE WIRING
        // ===============================================================

        /// <summary>
        /// Create the state builder and publish it via event.
        /// AutoSaveController subscribes to OnStateBuildFuncProvided.
        /// </summary>
        private void WireAutoSave()
        {
            _stateDTOBuilder = new GameStateDTOBuilder(
                _timeManager, _currencyManager, _cityManager,
                _restaurantSystem, _creditCardSystem, _loanSystem, _insuranceSystem,
                _investmentSystem,
                _pendingIncome,
                _lifeGoalSelection);

            GameEvents.RaiseStateBuildFuncProvided(_stateDTOBuilder.Build);
        }

        // ===============================================================
        // LIFE GOALS SERVICES
        // ===============================================================

        private void ConstructLifeGoalsServices()
        {
            // 1. Selection service. Subscribes to OnLifeGoalsSelected.
            _lifeGoalSelection = new LifeGoalSelectionService();

            // 2. Bankruptcy reset orchestrator. Constructed before NetWorthService
            //    so its BankruptcyFlag accessor is available to RetirementEvaluator.
            _bankruptcyResetService = new BankruptcyResetService();
            RegisterBankruptcyResettables();
            if (_cityManager != null)
            {
                _bankruptcyResetService.SetBatchLotResetAction(_cityManager.BatchResetPlayerLots);
            }

            // 3. NetWorthService. Composes Liquid + Business contributions from
            //    the wired financial systems via Func delegates.
            _netWorthService = new NetWorthService(
                liquidNetWorth: ComputeLiquidNetWorth,
                businessAssetValue: ComputeBusinessAssetValue);

            // 4. Goal progress tracker. Subscribes to OnNetWorthChanged.
            _goalProgressTracker = new GoalProgressTracker(
                _lifeGoalSelection,
                () => _timeManager != null ? _timeManager.CurrentTickCount : 0);

            // 5. Lifespan controller. Subscribes to OnDayEnd.
            _lifespanController = new LifespanController();

            // 6. Retirement evaluator. Subscribes to OnRetirementReached.
            _retirementEvaluator = new RetirementEvaluator(
                _lifeGoalSelection,
                () => _bankruptcyResetService != null && _bankruptcyResetService.BankruptcyFlag);

            // 7. Insolvency monitor. Subscribes to OnMonthlyPaymentCycleComplete.
            _insolvencyMonitor = new InsolvencyMonitor(
                checking: () => _currencyManager != null ? _currencyManager.CheckingBalance : 0f,
                investing: () => _currencyManager != null ? _currencyManager.InvestingBalance : 0f,
                creditCardDebt: () => _creditCardSystem != null ? _creditCardSystem.CurrentBalance : 0f,
                loanPrincipal: () => (_loanSystem != null && _loanSystem.Portfolio != null)
                    ? _loanSystem.Portfolio.GetTotalOutstandingPrincipal()
                    : 0f);
        }

        private void RegisterBankruptcyResettables()
        {
            if (_bankruptcyResetService == null) return;

            if (_currencyManager != null) _bankruptcyResetService.Register(_currencyManager);
            if (_creditCardSystem != null) _bankruptcyResetService.Register(_creditCardSystem);
            if (_loanSystem != null) _bankruptcyResetService.Register(_loanSystem);
            if (_investmentSystem != null) _bankruptcyResetService.Register(_investmentSystem);
            if (_insuranceSystem != null) _bankruptcyResetService.Register(_insuranceSystem);
            if (_pendingIncome != null) _bankruptcyResetService.Register(_pendingIncome);
        }

        private float ComputeLiquidNetWorth()
        {
            float checking = _currencyManager != null ? _currencyManager.CheckingBalance : 0f;
            float investing = _currencyManager != null ? _currencyManager.InvestingBalance : 0f;
            float loanPrincipal = (_loanSystem != null && _loanSystem.Portfolio != null)
                ? _loanSystem.Portfolio.GetTotalOutstandingPrincipal()
                : 0f;
            float ccBalance = _creditCardSystem != null ? _creditCardSystem.CurrentBalance : 0f;
            return LiquidNetWorthCalculator.Compute(
                checking, investing, loanPrincipal, ccBalance,
                /* ccEnabled */ false);
        }

        private float ComputeBusinessAssetValue()
        {
            // Sum of actual paid amounts for all player-owned lots.
            // Tier-upgrade investment value is not yet ledgered (that lives in a
            // future change to RestaurantSystem); contributes 0 here for now.
            return _cityManager != null ? _cityManager.OwnedLotsAcquisitionTotal : 0f;
        }

        private void DisposeLifeGoalsServices()
        {
            _lifeGoalSelection?.Dispose();
            _netWorthService?.Dispose();
            _goalProgressTracker?.Dispose();
            _lifespanController?.Dispose();
            _retirementEvaluator?.Dispose();
            _insolvencyMonitor?.Dispose();
            _bankruptcyResetService?.Dispose();

            _lifeGoalSelection = null;
            _netWorthService = null;
            _goalProgressTracker = null;
            _lifespanController = null;
            _retirementEvaluator = null;
            _insolvencyMonitor = null;
            _bankruptcyResetService = null;
        }

        private void HandleSaveStateLoaded(GamePlayerStateDTO dto)
        {
            if (dto == null) return;
            if (_lifeGoalSelection != null) _lifeGoalSelection.HydrateFromDto(dto.selected_goals);
            if (_bankruptcyResetService != null) _bankruptcyResetService.HydrateFlag(dto.bankruptcy_flag);
        }

        private void HandleRetirementReached()
        {
            // Retirement is a hard end. Mark it so HandleGameEnd can produce
            // a scorecard-aware GameSummary, then fire the existing game-end
            // pipeline so existing UI/persistence paths keep working.
            _retirementGameEnd = true;
            GameEvents.RaiseGameEnd(Owner.Player);
        }

        // ===============================================================
        // PRIVATE METHODS
        // ===============================================================

        private void HandleGameEnd(Owner winner)
        {
            bool isPlayerWin = winner == Owner.Player;
            SetState(isPlayerWin ? GameState.Won : GameState.Lost);

            // Build game summary for end screen
            GameSummary summary = BuildGameSummary(isPlayerWin);

            // Raise the detailed game end event for the end screen
            GameEvents.RaiseGameEndWithSummary(isPlayerWin, summary);

            if (_logStateChanges)
            {
                string resultText = isPlayerWin
                    ? "Congratulations! You won!"
                    : "Game over. The rival won.";

                Debug.Log($"[GameManager] {resultText}");
                Debug.Log(GetGameSummary());
            }
        }

        /// <summary>
        /// Build a complete game summary for the end screen.
        /// Delegates to GameSummaryBuilder for testable construction.
        /// Lot purchase records are assembled here because they require
        /// concrete CityManager access (AllLots property not on ICityService).
        /// </summary>
        private GameSummary BuildGameSummary(bool isPlayerWin)
        {
            var lotPurchases = BuildLotPurchaseRecords();
            // SellHistory is IReadOnlyList; copy to List for GameSummaryBuilder
            System.Collections.Generic.List<SellTransactionRecord> sellHistory = null;
            if (_investmentSystem != null)
                sellHistory = new System.Collections.Generic.List<SellTransactionRecord>(_investmentSystem.SellHistory);
            int daysPlayed = _timeManager != null ? _timeManager.CurrentEnginePulse : 0;

            // Build scorecard if this game-end is a retirement (or any time
            // the evaluator has selection state available). Returns a struct
            // with empty arrays when no selection -- safe for non-retirement paths.
            GoalScorecard scorecard = null;
            if (_retirementGameEnd && _retirementEvaluator != null)
            {
                scorecard = _retirementEvaluator.BuildScorecard();
            }

            return GameSummaryBuilder.Build(
                isPlayerWin,
                daysPlayed,
                _cityManager,
                _currencyManager,
                _investmentSystem,
                _restaurantSystem,
                lotPurchases,
                sellHistory,
                scorecard);
        }

        private System.Collections.Generic.List<LotPurchaseRecord> BuildLotPurchaseRecords()
        {
            var records = new System.Collections.Generic.List<LotPurchaseRecord>();
            if (_cityManager == null) return records;

            foreach (var lot in _cityManager.AllLots)
            {
                if (_cityManager.GetOwner(lot.LotId) == Owner.Player)
                {
                    records.Add(new LotPurchaseRecord
                    {
                        LotId = lot.LotId,
                        LotName = lot.DisplayName,
                        Cost = lot.BaseCost,
                        IncomeBonus = lot.IncomeBonus,
                        PurchasedOnDay = _cityManager.GetPurchaseTick(lot.LotId)
                    });
                }
            }
            return records;
        }

        private void SetState(GameState newState)
        {
            _currentState = newState;
        }

        private void ValidateReferences()
        {
            bool valid = true;

            if (_timeManager == null) { Debug.LogError("[GameManager] Missing TimeManager reference"); valid = false; }
            if (_currencyManager == null) { Debug.LogError("[GameManager] Missing CurrencyManager reference"); valid = false; }
            if (_restaurantSystem == null) { Debug.LogError("[GameManager] Missing RestaurantSystem reference"); valid = false; }
            if (_investmentSystem == null) { Debug.LogError("[GameManager] Missing InvestmentSystem reference"); valid = false; }
            if (_cityManager == null) { Debug.LogError("[GameManager] Missing CityManager reference"); valid = false; }
            if (_rivalAI == null) { Debug.LogError("[GameManager] Missing RivalAI reference"); valid = false; }

            if (!valid)
            {
                Debug.LogError("[GameManager] Missing references! Wire these in the Unity Editor.");
            }
        }
    }
}
