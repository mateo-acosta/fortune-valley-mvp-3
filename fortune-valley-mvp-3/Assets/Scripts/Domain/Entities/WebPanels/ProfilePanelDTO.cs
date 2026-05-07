using System;

namespace FortuneValley.Domain.Entities.WebPanels
{
    /// <summary>
    /// Wire payload from Unity to the PlayerProfile iframe.
    /// JsonUtility-serialized; field names match what PlayerProfile.html's
    /// FV.profile.update(payload) handler expects.
    ///
    /// All monetary values are absolute (positive), in dollars.
    /// All "yearly" values are pre-scaled server-side (per-day * TicksPerYear,
    /// per-month * 12) so the iframe renders them without further math.
    ///
    /// active_loans reuses the existing ActiveLoanRowDTO (with monthlyPayment);
    /// the iframe is responsible for displaying it as a yearly figure (× 12).
    /// </summary>
    [Serializable]
    public class ProfilePanelDTO
    {
        // Lifespan
        public int current_age;
        public int retirement_age;
        public int current_day;

        // Net worth equation (live)
        public float total_net_worth;
        public float liquid_net_worth;
        public float cash_in_checking;
        public float investment_value;
        public float restaurant_assets_value;
        public float loans_total;

        // Income overview (yearly)
        public float yearly_loan_payments;
        public float yearly_restaurant_income;

        // Goals
        public ProfileGoalRowDTO[] selected_goals;

        // Properties (player-owned restaurants only)
        public ProfileRestaurantRowDTO[] restaurants;

        // Active loans (reused row DTO; monthlyPayment * 12 = yearly on iframe side)
        public ActiveLoanRowDTO[] active_loans;
    }
}
