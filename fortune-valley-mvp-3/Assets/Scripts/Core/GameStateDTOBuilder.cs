using System.Collections.Generic;
using FortuneValley.Domain;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Core
{
    /// <summary>
    /// Pure C# class that builds a GamePlayerStateDTO from system state.
    /// Extracted from GameManager to keep loops and collections out of MonoBehaviours.
    /// </summary>
    public class GameStateDTOBuilder
    {
        private readonly TimeManager _timeManager;
        private readonly CurrencyManager _currencyManager;
        private readonly CityManager _cityManager;
        private readonly RestaurantSystem _restaurantSystem;
        private readonly CreditScoreSystem _creditCardSystem;
        private readonly LoanSystem _loanSystem;
        private readonly InsuranceSystem _insuranceSystem;
        private readonly InvestmentSystem _investmentSystem;
        private readonly DailyIncomeAccumulator _pendingIncome;
        private readonly LifeGoalSelectionService _lifeGoalSelection;

        public GameStateDTOBuilder(
            TimeManager timeManager,
            CurrencyManager currencyManager,
            CityManager cityManager,
            RestaurantSystem restaurantSystem,
            CreditScoreSystem creditCardSystem,
            LoanSystem loanSystem,
            InsuranceSystem insuranceSystem,
            InvestmentSystem investmentSystem,
            DailyIncomeAccumulator pendingIncome,
            LifeGoalSelectionService lifeGoalSelection = null)
        {
            _timeManager = timeManager;
            _currencyManager = currencyManager;
            _cityManager = cityManager;
            _restaurantSystem = restaurantSystem;
            _creditCardSystem = creditCardSystem;
            _loanSystem = loanSystem;
            _insuranceSystem = insuranceSystem;
            _investmentSystem = investmentSystem;
            _pendingIncome = pendingIncome;
            _lifeGoalSelection = lifeGoalSelection;
        }

        /// <summary>
        /// Build a complete state snapshot from all systems.
        /// </summary>
        public GamePlayerStateDTO Build()
        {
            int currentTickCount = _timeManager != null ? _timeManager.CurrentTickCount : 0;
            int currentEnginePulse = _timeManager != null ? _timeManager.CurrentEnginePulse : 0;
            float yearlyIncome = ComputeMonthlyIncome();

            var dto = new GamePlayerStateDTO
            {
                game_mode = "homebase",
                // Legacy fields (Stage 0a alias chain). Removed in Stage 0c.
                current_day = currentTickCount,
                current_tick = currentEnginePulse,
                monthly_income = yearlyIncome,
                // New "tick" naming, written in parallel.
                current_tick_count = currentTickCount,
                current_engine_pulse = currentEnginePulse,
                yearly_income = yearlyIncome,
                checking_balance = _currencyManager != null ? _currencyManager.CheckingBalance : 0f,
                investment_balance = _currencyManager != null ? _currencyManager.InvestingBalance : 0f,
                credit_balance = _creditCardSystem != null ? _creditCardSystem.CurrentBalance : 0f,
                credit_score = _creditCardSystem != null ? _creditCardSystem.CreditScore : 0,
                restaurant_level = _restaurantSystem != null ? _restaurantSystem.CurrentLevel : 1,
                current_age = LifespanConstants.AgeFromTick(currentTickCount),
                liquid_net_worth = ComputeLiquidNetWorth(),
                total_net_worth = ComputeLiquidNetWorth(),
                selected_goals = _lifeGoalSelection != null ? _lifeGoalSelection.BuildDtoEntries() : null
            };

            BuildLotOwnership(dto);
            BuildFranchiseLevels(dto);
            BuildActiveLoans(dto);
            BuildInsurancePolicies(dto);
            BuildInvestmentHoldings(dto);

            if (_pendingIncome != null)
            {
                _pendingIncome.Snapshot(dto);
            }

            return dto;
        }

        // Liquid Net Worth = Checking + Investing - CC debt - outstanding loan principal.
        // Conservative formula matches the Life Goals design spec.
        // Total Net Worth currently equals Liquid until lot acquisitionCost +
        // restaurant tier upgrade ledger are wired (Steps 3 + 14 of the plan).
        private float ComputeLiquidNetWorth()
        {
            float liquid = 0f;
            if (_currencyManager != null)
            {
                liquid += _currencyManager.CheckingBalance + _currencyManager.InvestingBalance;
            }
            if (_creditCardSystem != null)
            {
                liquid -= _creditCardSystem.CurrentBalance;
            }
            liquid -= ComputeOutstandingLoanPrincipal();
            return liquid;
        }

        private float ComputeOutstandingLoanPrincipal()
        {
            if (_loanSystem == null || _loanSystem.Portfolio == null) return 0f;
            return _loanSystem.Portfolio.GetTotalOutstandingPrincipal();
        }

        // Same formula as MonthlyPaymentDayController so dashboard DTI/liquidity
        // ratios line up with what the student sees on their payment day popup.
        private float ComputeMonthlyIncome()
        {
            if (_restaurantSystem == null || _timeManager == null || _creditCardSystem == null) return 0f;
            return DtiCalculator.ComputeMonthlyIncome(
                _restaurantSystem.TotalIncomePerTick,
                _timeManager.EnginePulsesPerTick,
                _creditCardSystem.BillingCycleTicks);
        }

        private void BuildLotOwnership(GamePlayerStateDTO dto)
        {
            if (_cityManager == null) return;

            var playerLots = new List<string>();
            var rivalLots = new List<string>();
            var ownership = _cityManager.LotOwnership;

            foreach (var kvp in ownership)
            {
                if (kvp.Value == Owner.Player) playerLots.Add(kvp.Key);
                else if (kvp.Value == Owner.Rival) rivalLots.Add(kvp.Key);
            }

            dto.lots_owned = playerLots.ToArray();
            dto.rival_lots_owned = rivalLots.ToArray();
        }

        private void BuildActiveLoans(GamePlayerStateDTO dto)
        {
            if (_loanSystem == null) return;

            var loans = _loanSystem.Portfolio.AllLoans;
            var loanDtos = new List<ActiveLoanDTO>();

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
                    // Legacy fields (Stage 0a alias chain). Removed in Stage 0c.
                    monthly_payment = loan.YearlyPayment,
                    term_months = loan.TermYears,
                    start_day = loan.StartTick,
                    // New tick-vocabulary fields, written in parallel.
                    yearly_payment = loan.YearlyPayment,
                    term_ticks = loan.TermYears,
                    start_tick = loan.StartTick,
                    payments_made = loan.PaymentsMade,
                    apr = loan.APR,
                    down_payment = loan.DownPayment
                });
            }

            dto.active_loans = loanDtos.ToArray();
        }

        private void BuildInsurancePolicies(GamePlayerStateDTO dto)
        {
            if (_insuranceSystem == null || _insuranceSystem.Portfolio == null) return;

            var policies = _insuranceSystem.Portfolio.AllPolicies;
            var policyDtos = new List<ActiveInsurancePolicyDTO>();

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

        private void BuildFranchiseLevels(GamePlayerStateDTO dto)
        {
            if (_cityManager == null) return;

            var tiers = _cityManager.LotTiers;
            var dtos = new List<FranchiseLevelDTO>();

            foreach (var kvp in tiers)
            {
                dtos.Add(new FranchiseLevelDTO
                {
                    lot_id = kvp.Key,
                    tier = kvp.Value
                });
            }

            dto.franchise_levels = dtos.ToArray();
        }

        private void BuildInvestmentHoldings(GamePlayerStateDTO dto)
        {
            if (_investmentSystem == null) return;

            var holdings = _investmentSystem.ActiveInvestments;
            var dtos = new List<InvestmentHoldingDTO>();

            for (int i = 0; i < holdings.Count; i++)
            {
                var h = holdings[i];
                if (h == null || h.Definition == null) continue;
                dtos.Add(new InvestmentHoldingDTO
                {
                    // DisplayName is the user-facing label; asset name is the stable instrument_id Rails expects.
                    name = h.Definition.DisplayName,
                    instrument_id = h.Definition.name,
                    shares = h.NumberOfShares,
                    avg_price = h.AveragePurchasePrice,
                    current_value = h.CurrentValue
                });
            }

            dto.investment_holdings = dtos.ToArray();
        }
    }
}
