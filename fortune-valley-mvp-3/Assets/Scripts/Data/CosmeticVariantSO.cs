using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// One cosmetic neighbor building option the player can pick when a tier unlocks
    /// on their block. Purely visual — no economic effect. Three of these are offered
    /// per tier slot via the CosmeticVariantPickerPopup; the chosen prefab is instantiated
    /// at the matching cosmetic slot anchor on the block.
    /// </summary>
    [CreateAssetMenu(fileName = "CosmeticVariant", menuName = "Fortune Valley/Cosmetic Variant")]
    public class CosmeticVariantSO : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;
        [SerializeField] private GameObject _prefab;

        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public GameObject Prefab => _prefab;
    }
}
