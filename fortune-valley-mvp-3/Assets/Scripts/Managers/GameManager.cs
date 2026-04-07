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

        [Header("Persistence")]
        [SerializeField] private AutoSaveController _autoSaveController;
        [SerializeField] private DecisionLogger _decisionLogger;

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
            ValidateReferences();
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
        }

        private void OnDisable()
        {
            GameEvents.OnGameEnd -= HandleGameEnd;
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
                   $"Day: {_timeManager.CurrentTick}\n\n" +
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
        /// Wire the AutoSaveController with a lambda that builds the current state.
        /// Called once at game start.
        /// </summary>
        private void WireAutoSave()
        {
            if (_autoSaveController == null) return;

            _autoSaveController.SetStateBuildFunc(BuildStateDTO);
        }

        private GamePlayerStateDTO BuildStateDTO()
        {
            var dto = new GamePlayerStateDTO
            {
                game_mode = "homebase",
                current_day = _timeManager != null ? _timeManager.CurrentDay : 0,
                current_tick = _timeManager != null ? _timeManager.CurrentTick : 0,
                checking_balance = _currencyManager != null ? _currencyManager.CheckingBalance : 0f,
                investment_balance = _currencyManager != null ? _currencyManager.InvestingBalance : 0f,
                credit_balance = _creditCardSystem != null ? _creditCardSystem.CurrentBalance : 0f,
                credit_score = _creditCardSystem != null ? _creditCardSystem.CreditScore : 0,
                restaurant_level = _restaurantSystem != null ? _restaurantSystem.CurrentLevel : 1
            };

            // Lots owned
            if (_cityManager != null)
            {
                var playerLots = new System.Collections.Generic.List<string>();
                var rivalLots = new System.Collections.Generic.List<string>();
                var ownership = _cityManager.LotOwnership;

                foreach (var kvp in ownership)
                {
                    if (kvp.Value == Owner.Player) playerLots.Add(kvp.Key);
                    else if (kvp.Value == Owner.Rival) rivalLots.Add(kvp.Key);
                }

                dto.lots_owned = playerLots.ToArray();
                dto.rival_lots_owned = rivalLots.ToArray();
            }

            // Active loans
            if (_loanSystem != null)
            {
                var loans = _loanSystem.Portfolio.AllLoans;
                var loanDtos = new System.Collections.Generic.List<ActiveLoanDTO>();
                for (int i = 0; i < loans.Count; i++)
                {
                    var loan = loans[i];
                    if (!loan.IsActive) continue;
                    loanDtos.Add(new ActiveLoanDTO
                    {
                        loan_id = loan.LoanId,
                        lot_id = loan.LotId,
                        principal = loan.Principal,
                        remaining_balance = loan.RemainingBalance,
                        monthly_payment = loan.MonthlyPayment,
                        payments_made = loan.PaymentsMade,
                        term_months = loan.TermMonths,
                        apr = loan.APR,
                        down_payment = loan.DownPayment,
                        start_day = loan.StartDay
                    });
                }
                dto.active_loans = loanDtos.ToArray();
            }

            // Insurance policies
            if (_insuranceSystem != null && _insuranceSystem.Portfolio != null)
            {
                var policies = _insuranceSystem.Portfolio.AllPolicies;
                var policyDtos = new System.Collections.Generic.List<ActiveInsurancePolicyDTO>();
                for (int i = 0; i < policies.Count; i++)
                {
                    var p = policies[i];
                    if (!p.IsActive) continue;
                    policyDtos.Add(new ActiveInsurancePolicyDTO
                    {
                        policy_id = p.PolicyId,
                        lot_id = p.LotId,
                        policy_type = p.PolicyType.ToString(),
                        monthly_premium = p.MonthlyPremium,
                        deductible = p.Deductible,
                        start_day = p.StartDay
                    });
                }
                dto.insurance_policies = policyDtos.ToArray();
            }

            return dto;
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
            int daysPlayed = _timeManager != null ? _timeManager.CurrentTick : 0;

            return GameSummaryBuilder.Build(
                isPlayerWin,
                daysPlayed,
                _cityManager,
                _currencyManager,
                _investmentSystem,
                _restaurantSystem,
                lotPurchases,
                sellHistory);
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

            // Financial systems are optional in non-Homebase scenes
            if (_autoSaveController == null) Debug.LogWarning("[GameManager] AutoSaveController not wired. State will not be saved.");
            if (_decisionLogger == null) Debug.LogWarning("[GameManager] DecisionLogger not wired. Decisions will not be logged.");

            if (!valid)
            {
                Debug.LogError("[GameManager] Missing references! Wire these in the Unity Editor.");
            }
        }
    }
}
