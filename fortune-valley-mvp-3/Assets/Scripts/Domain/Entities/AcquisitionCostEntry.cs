using System;

namespace FortuneValley.Domain.Entities
{
    /// <summary>
    /// One per player-owned lot, capturing the actual paid amount (including any
    /// rival-buyout markup). Feeds the conservative BusinessAssetValue contribution
    /// of Total Net Worth. Persisted in the autosave DTO so returning players land
    /// with the correct net-worth math on first frame, not zero.
    /// </summary>
    [Serializable]
    public class AcquisitionCostEntry
    {
        public string lot_id;
        public float cost;
    }
}
