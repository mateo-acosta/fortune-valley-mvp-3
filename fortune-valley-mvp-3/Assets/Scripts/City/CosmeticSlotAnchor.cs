using UnityEngine;

namespace FortuneValley.City
{
    /// <summary>
    /// Marks a CosmeticSlot Transform with the direction that buildings authored at this slot
    /// should "front" toward (typically the road). The block scene seeder reads this to constrain
    /// the seeded rotation cone. Authored once per slot per corner-orientation prefab.
    /// </summary>
    public class CosmeticSlotAnchor : MonoBehaviour
    {
        [SerializeField] private Vector3 _preferredForwardLocal = Vector3.forward;

        public Vector3 PreferredForwardLocal => _preferredForwardLocal;
    }
}
