using FortuneValley.Domain.Entities;

namespace FortuneValley.Tests.Fixtures
{
    /// <summary>
    /// Composable builders for GamePlayerStateDTO test fixtures. Tests start
    /// from <see cref="Default"/> and chain With*() helpers to layer on the
    /// fields they care about. One place to update when the DTO grows a new
    /// field; keeps per-system Hydrate tests symmetric.
    /// </summary>
    public static class GamePlayerStateDTOFixtures
    {
        /// <summary>
        /// A baseline non-null DTO with game_mode="homebase" and zeroed
        /// numeric fields. Mirrors the fresh-state shape used by the API
        /// client's WipePlayerState path.
        /// </summary>
        public static GamePlayerStateDTO Default(string gameMode = "homebase")
        {
            return new GamePlayerStateDTO
            {
                game_mode = gameMode,
                current_day = 0,
                current_tick = 0,
                checking_balance = 0f,
                credit_balance = 0f,
                investment_balance = 0f,
                credit_score = 650,
                budget_variance_streak = 0,
                tax_liability_ytd = 0f,
                monthly_income = 0f,
                lots_owned = new string[0],
                rival_lots_owned = new string[0],
                learning_levels_completed = new string[0],
                investment_holdings = new InvestmentHoldingDTO[0],
                active_loans = new ActiveLoanDTO[0],
                insurance_policies = new ActiveInsurancePolicyDTO[0],
                franchise_levels = new FranchiseLevelDTO[0],
                consecutive_insolvent_months = 0,
                bankruptcy_flag = false,
                restaurant_level = 1,
                tutorial_completed = false,
                schema_version = 1
            };
        }

        public static GamePlayerStateDTO WithDay(this GamePlayerStateDTO dto, int day, int tick = 0)
        {
            dto.current_day = day;
            dto.current_tick = tick;
            return dto;
        }

        public static GamePlayerStateDTO WithCheckingBalance(this GamePlayerStateDTO dto, float balance)
        {
            dto.checking_balance = balance;
            return dto;
        }

        public static GamePlayerStateDTO WithCreditState(this GamePlayerStateDTO dto, float creditBalance, int creditScore)
        {
            dto.credit_balance = creditBalance;
            dto.credit_score = creditScore;
            return dto;
        }

        public static GamePlayerStateDTO WithRestaurantLevel(this GamePlayerStateDTO dto, int level)
        {
            dto.restaurant_level = level;
            return dto;
        }

        public static GamePlayerStateDTO WithTutorialCompleted(this GamePlayerStateDTO dto, bool completed = true)
        {
            dto.tutorial_completed = completed;
            return dto;
        }

        public static GamePlayerStateDTO WithLots(this GamePlayerStateDTO dto, string[] playerLots, string[] rivalLots = null)
        {
            dto.lots_owned = playerLots ?? new string[0];
            dto.rival_lots_owned = rivalLots ?? new string[0];
            return dto;
        }

        public static GamePlayerStateDTO WithFranchiseTiers(this GamePlayerStateDTO dto, params (string lot_id, int tier)[] tiers)
        {
            var arr = new FranchiseLevelDTO[tiers.Length];
            for (int i = 0; i < tiers.Length; i++)
            {
                arr[i] = new FranchiseLevelDTO { lot_id = tiers[i].lot_id, tier = tiers[i].tier };
            }
            dto.franchise_levels = arr;
            return dto;
        }

        public static GamePlayerStateDTO WithInvestmentHoldings(this GamePlayerStateDTO dto, params InvestmentHoldingDTO[] holdings)
        {
            dto.investment_holdings = holdings ?? new InvestmentHoldingDTO[0];
            return dto;
        }

        public static GamePlayerStateDTO WithLoans(this GamePlayerStateDTO dto, params ActiveLoanDTO[] loans)
        {
            dto.active_loans = loans ?? new ActiveLoanDTO[0];
            return dto;
        }

        public static GamePlayerStateDTO WithInsurancePolicies(this GamePlayerStateDTO dto, params ActiveInsurancePolicyDTO[] policies)
        {
            dto.insurance_policies = policies ?? new ActiveInsurancePolicyDTO[0];
            return dto;
        }
    }
}
