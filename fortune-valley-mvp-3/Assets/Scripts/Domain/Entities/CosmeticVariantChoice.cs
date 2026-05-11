using System;

namespace FortuneValley.Domain.Entities
{
    /// <summary>
    /// Per-lot record of which cosmetic neighbor variant the player picked for a given tier slot.
    /// Serialized into GamePlayerStateDTO.cosmetic_variants so picks survive reload.
    /// tier_slot is newTier - 1 (T1 -> 0, T2 -> 1, T3 -> 2). variant_id indexes into the
    /// CosmeticVariantCatalogSO array for that tier.
    /// </summary>
    [Serializable]
    public class CosmeticVariantChoice
    {
        public string lot_id;
        public int tier_slot;
        public int variant_id;
    }
}
