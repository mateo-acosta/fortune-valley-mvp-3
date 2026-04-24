using UnityEngine;

namespace FortuneValley.Managers.Tutorial
{
    /// <summary>
    /// Exposes a world-space AABB computed from 4 corner Transforms. Attached
    /// to a tutorial target whose true footprint is defined by anchor children
    /// (e.g. a city block's GlowCorner_NW/NE/SE/SW), not the parent's
    /// transform position. <see cref="IntroTutorialController.ResolveScreenRect"/>
    /// checks for this component first so the donut hole lands on the
    /// authored footprint instead of falling back to a renderer or fallback rect.
    /// </summary>
    public class TutorialWorldBoundsAnchors : MonoBehaviour
    {
        [SerializeField] private Transform _cornerNW;
        [SerializeField] private Transform _cornerNE;
        [SerializeField] private Transform _cornerSE;
        [SerializeField] private Transform _cornerSW;

        [Tooltip("Vertical extent of the AABB above the corner anchors. Same idea as " +
                 "BlockHoverController's footprint height -- the corners only define X/Z.")]
        [SerializeField] private float _height = 5f;

        public bool TryGetBounds(out Bounds bounds)
        {
            bounds = default;
            if (_cornerNW == null || _cornerNE == null || _cornerSE == null || _cornerSW == null) return false;

            Vector3 a = _cornerNW.position;
            Vector3 b = _cornerNE.position;
            Vector3 c = _cornerSE.position;
            Vector3 d = _cornerSW.position;

            Vector3 min = Vector3.Min(Vector3.Min(a, b), Vector3.Min(c, d));
            Vector3 max = Vector3.Max(Vector3.Max(a, b), Vector3.Max(c, d));

            Vector3 center = (min + max) * 0.5f;
            center.y = min.y + _height * 0.5f;

            Vector3 size = max - min;
            size.y = _height;

            bounds = new Bounds(center, size);
            return true;
        }
    }
}
