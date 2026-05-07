using System;

namespace FortuneValley.Domain.Entities.WebPanels
{
    /// <summary>
    /// One player-owned lot row for the Properties tab on the iframe.
    /// yearly_income is the per-day income at the lot's current tier
    /// scaled by LifespanConstants.DaysPerYear (multiplied server-side
    /// so the iframe can render the value as-is).
    /// </summary>
    [Serializable]
    public class ProfileRestaurantRowDTO
    {
        public string lot_id;           // matches CityLotDefinition.LotId
        public string lot_name;         // CityLotDefinition.DisplayName
        public int tier;                // 1, 2, 3
        public float yearly_income;     // per-day income * DaysPerYear
    }
}
