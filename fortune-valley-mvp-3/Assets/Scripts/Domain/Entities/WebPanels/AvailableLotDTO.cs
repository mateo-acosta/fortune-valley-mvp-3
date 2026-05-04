using System;

namespace FortuneValley.Domain.Entities.WebPanels
{
    /// <summary>
    /// One row in the iframe's available-lots dropdown (Explore tab).
    /// Only includes lots the player can purchase: not owned by player
    /// or rival, and not the starter lot.
    /// </summary>
    [Serializable]
    public class AvailableLotDTO
    {
        public string id;       // CityLotDefinition.LotId
        public string name;     // CityLotDefinition.DisplayName
        public float price;     // CityLotDefinition.BaseCost
    }
}
