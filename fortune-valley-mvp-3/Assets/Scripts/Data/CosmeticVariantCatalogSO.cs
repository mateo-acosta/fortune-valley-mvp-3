using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Shared catalog of cosmetic neighbor variants, grouped by the tier that unlocks them.
    /// One catalog is authored per project and referenced by the CosmeticVariantPickerPopup.
    /// Each tier array should hold exactly 3 variants so the picker can display 3 cards.
    /// </summary>
    [CreateAssetMenu(fileName = "CosmeticVariantCatalog", menuName = "Fortune Valley/Cosmetic Variant Catalog")]
    public class CosmeticVariantCatalogSO : ScriptableObject
    {
        [SerializeField] private CosmeticVariantSO[] _tier1Variants;
        [SerializeField] private CosmeticVariantSO[] _tier2Variants;
        [SerializeField] private CosmeticVariantSO[] _tier3Variants;

        public CosmeticVariantSO[] GetVariantsForTier(int tier)
        {
            if (tier == 1) return _tier1Variants;
            if (tier == 2) return _tier2Variants;
            if (tier == 3) return _tier3Variants;
            return null;
        }

        public CosmeticVariantSO GetVariant(int tier, int variantIndex)
        {
            var variants = GetVariantsForTier(tier);
            if (variants == null) return null;
            if (variantIndex < 0 || variantIndex >= variants.Length) return null;
            return variants[variantIndex];
        }
    }
}
