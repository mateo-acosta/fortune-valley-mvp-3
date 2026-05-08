using System.Collections.Generic;
using FortuneValley.Core;
using FortuneValley.Domain;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Entities.WebPanels;

namespace FortuneValley.Managers.WebPanels
{
    /// <summary>
    /// Reads the player's profile state off the live systems and writes it
    /// into a ProfilePanelDTO for the PlayerProfile iframe.
    ///
    /// Inputs:
    ///   - LoanSystem: per-loan rows + total outstanding principal
    ///   - CurrencyManager: cash + investing balances
    ///   - CityManager: player-owned restaurants and per-tier income
    ///   - TimeManager: current day -> age
    ///   - LifeGoalSelection cache + last NW snapshot pushed in by the
    ///     bridge MonoBehaviour (NetWorthService is pure C# and therefore
    ///     not a SerializeField; the bridge subscribes to OnNetWorthChanged
    ///     and OnLifeGoalsSelected and forwards the latest values via Snapshot).
    ///
    /// Pure C#; EditMode-testable without spinning up a scene.
    /// </summary>
    public class ProfileWebBridgeLogic : WebPanelBridgeLogic<ProfilePanelDTO>
    {
        private LoanSystem _loanSystem;
        private CurrencyManager _currencyManager;
        private CityManager _cityManager;
        private TimeManager _timeManager;
        private CreditScoreSystem _creditCardSystem;
        private QuestionManager _questionManager;
        private RestaurantSystem _restaurantSystem;
        private InvestmentSystem _investmentSystem;

        // Sticky bankruptcy flag — once set true (first OnSoftBankruptcyReset
        // or hydrate from save with bankruptcy_flag=true) it stays true for
        // the lifetime of the session per the soft-bankruptcy design spec.
        private bool _bankruptcyFlag;

        public void SetBankruptcyFlag(bool value)
        {
            // Sticky: only allow false -> true transitions.
            if (value) _bankruptcyFlag = true;
        }

        // Cached values pushed in from the MonoBehaviour bridge after each
        // NetWorthService / LifeGoalSelection event.
        private float _totalNetWorth;
        private float _liquidNetWorth;
        private LifeGoalSelection _selection;

        // Per-goal one-shot bloom flags. The bridge sets a flag when
        // OnGoalRealized fires; PopulateDTO copies it into the DTO and
        // ClearJustRealizedFlags wipes it after the push.
        private readonly HashSet<string> _justRealizedGoalIds = new HashSet<string>();

        public void Initialize(
            LoanSystem loanSystem,
            CurrencyManager currencyManager,
            CityManager cityManager,
            TimeManager timeManager,
            CreditScoreSystem creditCardSystem = null,
            QuestionManager questionManager = null,
            RestaurantSystem restaurantSystem = null,
            InvestmentSystem investmentSystem = null)
        {
            _loanSystem = loanSystem;
            _currencyManager = currencyManager;
            _cityManager = cityManager;
            _timeManager = timeManager;
            _creditCardSystem = creditCardSystem;
            _questionManager = questionManager;
            _restaurantSystem = restaurantSystem;
            _investmentSystem = investmentSystem;
        }

        public void SetNetWorthSnapshot(float total, float liquid)
        {
            _totalNetWorth = total;
            _liquidNetWorth = liquid;
        }

        public void SetSelection(LifeGoalSelection selection)
        {
            _selection = selection;
        }

        public void MarkJustRealized(string goalId)
        {
            if (!string.IsNullOrEmpty(goalId)) _justRealizedGoalIds.Add(goalId);
        }

        public void ClearJustRealizedFlags()
        {
            _justRealizedGoalIds.Clear();
        }

        public override bool PopulateDTO(ProfilePanelDTO target)
        {
            if (target == null) return false;
            // Currency + city + time are required to render anything meaningful.
            if (_currencyManager == null || _cityManager == null || _timeManager == null) return false;

            int day = _timeManager.CurrentTickCount;
            target.current_day = day;
            target.current_age = LifespanConstants.AgeFromTick(day);
            target.retirement_age = LifespanConstants.RetirementAge;

            target.total_net_worth = _totalNetWorth;
            target.liquid_net_worth = _liquidNetWorth;
            target.cash_in_checking = _currencyManager.CheckingBalance;
            target.investment_value = _currencyManager.InvestingBalance;
            target.loans_total = _loanSystem != null ? _loanSystem.TotalOutstandingPrincipal : 0f;
            target.yearly_loan_payments = _loanSystem != null
                ? _loanSystem.TotalYearlyDebt
                : 0f;

            FillRestaurants(target);
            // Restaurant assets value = total net worth - liquid net worth.
            // NetWorthService formula: TotalNW = LiquidNW + BusinessAssetValue.
            // Negative results clamp to 0 in case of tiny float drift.
            float businessAssets = target.total_net_worth - target.liquid_net_worth;
            target.restaurant_assets_value = businessAssets > 0f ? businessAssets : 0f;

            // Vitals: credit score (300..850; defaults to 0 if no CC system).
            target.credit_score = _creditCardSystem != null ? _creditCardSystem.CreditScore : 0;

            // Activity tab: quiz streak + lifetime restaurant earnings.
            target.current_quiz_streak = _questionManager != null ? _questionManager.CurrentStreak : 0;
            target.lifetime_restaurant_earnings = _restaurantSystem != null ? _restaurantSystem.TotalEarned : 0f;

            // Header bankruptcy chip: cached sticky flag.
            target.has_bankruptcy_flag = _bankruptcyFlag;

            // Investments breakdown for the Finances tab story numbers.
            // investment_value is already populated above from CurrencyManager.
            // InvestmentSystem owns the lifetime-tracking fields.
            target.investment_principal = _investmentSystem != null
                ? _investmentSystem.LifetimeTotalPrincipalInvested
                : 0f;
            target.investment_growth = _investmentSystem != null
                ? _investmentSystem.LifetimeTotalGain
                : 0f;
            target.investment_count = _investmentSystem != null && _investmentSystem.ActiveInvestments != null
                ? _investmentSystem.ActiveInvestments.Count
                : 0;

            // DTI ratio: yearly debt / yearly income, clamped 0..1. Filled
            // AFTER FillRestaurants so target.yearly_restaurant_income is
            // populated. If income is zero, DTI is treated as 0 (no income
            // to evaluate against rather than divide-by-zero infinity).
            float yearlyDebt = target.yearly_loan_payments;
            float yearlyIncome = target.yearly_restaurant_income;
            float dti = yearlyIncome > 0f ? yearlyDebt / yearlyIncome : 0f;
            if (dti < 0f) dti = 0f;
            if (dti > 1f) dti = 1f;
            target.dti_ratio = dti;

            FillGoals(target);
            FillActiveLoans(target);
            return true;
        }

        // ───────────────────────── restaurants ─────────────────────────

        private void FillRestaurants(ProfilePanelDTO target)
        {
            var allLots = _cityManager.AllLots;
            int playerCount = 0;
            float yearlyTotal = 0f;

            if (allLots != null)
            {
                for (int i = 0; i < allLots.Count; i++)
                {
                    if (allLots[i] == null) continue;
                    if (_cityManager.GetOwner(allLots[i].LotId) == Owner.Player) playerCount++;
                }
            }

            if (target.restaurants == null || target.restaurants.Length != playerCount)
            {
                target.restaurants = new ProfileRestaurantRowDTO[playerCount];
            }

            int idx = 0;
            if (allLots != null)
            {
                for (int i = 0; i < allLots.Count; i++)
                {
                    var lot = allLots[i];
                    if (lot == null) continue;
                    if (_cityManager.GetOwner(lot.LotId) != Owner.Player) continue;
                    if (target.restaurants[idx] == null) target.restaurants[idx] = new ProfileRestaurantRowDTO();

                    int tier = _cityManager.GetTier(lot.LotId);
                    // GetIncomeAtTier returns per-engine-pulse income (the
                    // 0.4s atomic). To convert to per-year:
                    //   perPulse * EnginePulsesPerTick * TicksPerYear
                    //   = perPulse * 10 * 30 = perPulse * 300
                    // RestaurantUpgradePanel + BuildingCollectButton + the
                    // HUD income readouts all use this same chain; matches
                    // existing rendering convention.
                    float perPulse = _cityManager.GetIncomeAtTier(lot.LotId, tier);
                    int pulsesPerTick = _timeManager.EnginePulsesPerTick > 0
                        ? _timeManager.EnginePulsesPerTick
                        : 1;
                    float perYear = perPulse * pulsesPerTick * LifespanConstants.TicksPerYear;

                    target.restaurants[idx].lot_id = lot.LotId;
                    target.restaurants[idx].lot_name = string.IsNullOrEmpty(lot.DisplayName) ? lot.LotId : lot.DisplayName;
                    target.restaurants[idx].tier = tier;
                    target.restaurants[idx].yearly_income = perYear;
                    yearlyTotal += perYear;
                    idx++;
                }
            }

            target.yearly_restaurant_income = yearlyTotal;
        }

        // ───────────────────────── goals ─────────────────────────

        private void FillGoals(ProfilePanelDTO target)
        {
            if (_selection == null)
            {
                target.selected_goals = target.selected_goals ?? new ProfileGoalRowDTO[0];
                return;
            }

            var entries = _selection.Entries;
            int count = entries != null ? entries.Length : 0;

            if (target.selected_goals == null || target.selected_goals.Length != count)
            {
                target.selected_goals = new ProfileGoalRowDTO[count];
            }

            int day = _timeManager.CurrentTickCount;
            for (int i = 0; i < count; i++)
            {
                var entry = entries[i];
                if (target.selected_goals[i] == null) target.selected_goals[i] = new ProfileGoalRowDTO();
                var row = target.selected_goals[i];
                row.goal_id = entry != null ? entry.goal_id : null;
                row.tier = entry != null ? (int)entry.tier : 0;
                row.threshold = entry != null ? entry.threshold : 0f;
                row.realized = entry != null && entry.realized;
                row.realized_age = (entry != null && entry.realized && entry.realized_at_day >= 0)
                    ? LifespanConstants.AgeFromTick(entry.realized_at_day)
                    : -1;
                row.just_realized = entry != null && _justRealizedGoalIds.Contains(entry.goal_id);
            }
        }

        // ───────────────────────── active loans ─────────────────────────

        private void FillActiveLoans(ProfilePanelDTO target)
        {
            if (_loanSystem == null)
            {
                target.active_loans = target.active_loans ?? new ActiveLoanRowDTO[0];
                return;
            }

            var portfolio = _loanSystem.Portfolio;
            var loans = portfolio != null ? portfolio.AllLoans : null;
            int count = 0;
            if (loans != null)
            {
                for (int i = 0; i < loans.Count; i++)
                {
                    if (loans[i] != null && loans[i].IsActive) count++;
                }
            }

            if (target.active_loans == null || target.active_loans.Length != count)
            {
                target.active_loans = new ActiveLoanRowDTO[count];
            }

            int idx = 0;
            if (loans != null)
            {
                for (int i = 0; i < loans.Count; i++)
                {
                    var loan = loans[i];
                    if (loan == null || !loan.IsActive) continue;
                    if (target.active_loans[idx] == null) target.active_loans[idx] = new ActiveLoanRowDTO();
                    PopulateActiveLoan(target.active_loans[idx], loan);
                    idx++;
                }
            }
        }

        private void PopulateActiveLoan(ActiveLoanRowDTO row, ActiveLoan loan)
        {
            row.id = loan.LoanId;
            row.lotName = ResolveLotName(loan.LotId);
            row.balance = loan.RemainingBalance;
            row.originalPrincipal = loan.Principal;
            row.monthlyPayment = loan.YearlyPayment;
            row.monthsPaid = loan.PaymentsMade;
            row.termMonths = loan.TermYears;
        }

        private string ResolveLotName(string lotId)
        {
            if (string.IsNullOrEmpty(lotId) || _cityManager == null) return lotId;
            var def = _cityManager.GetLot(lotId);
            return (def != null && !string.IsNullOrEmpty(def.DisplayName)) ? def.DisplayName : lotId;
        }
    }
}
