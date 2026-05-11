using UnityEngine;
using UnityEngine.UI;

namespace FortuneValley.UI.Tutorial
{
    /// <summary>
    /// Four-rectangle "donut" dim overlay. Top/Bottom/Left/Right stretch
    /// around a configurable hole, so the non-target portion of the screen
    /// darkens while the target is shown at full brightness. No shader, no
    /// Unity Mask component -- WebGL-safe and fully inspector-driven.
    ///
    /// All four dim children must be RectTransforms with anchor bottom-left
    /// and pivot bottom-left (0,0). ShowFullDim() collapses to a single
    /// full-screen dim. ShowDonut(screenRect) positions the hole around
    /// <paramref name="screenRect"/> (screen pixels) with padding applied.
    /// Hide() fades all four to zero alpha.
    /// </summary>
    public class MaskOverlay : MonoBehaviour
    {
        [Header("Dim rects (anchor and pivot must be bottom-left)")]
        [SerializeField] private RectTransform _top;
        [SerializeField] private RectTransform _bottom;
        [SerializeField] private RectTransform _left;
        [SerializeField] private RectTransform _right;

        [Header("Dim images (for alpha control)")]
        [SerializeField] private Image _topImage;
        [SerializeField] private Image _bottomImage;
        [SerializeField] private Image _leftImage;
        [SerializeField] private Image _rightImage;

        [Header("Container used as the coordinate parent for the hole math.")]
        [Tooltip("Typically the RectTransform that all four dim rects are parented to (stretch-canvas).")]
        [SerializeField] private RectTransform _container;

        [Header("Rounded corners")]
        [Tooltip("Optional circular Images placed at the four inner corners of the donut hole. " +
                 "Each renders a quarter-circle of dim that softens the otherwise 90-degree corner.")]
        [SerializeField] private Image _cornerNW;
        [SerializeField] private Image _cornerNE;
        [SerializeField] private Image _cornerSE;
        [SerializeField] private Image _cornerSW;
        [Tooltip("Radius (pixels) of each rounded corner. Set to 0 to keep sharp corners.")]
        [SerializeField] private float _cornerRadius = 24f;

        [Header("Tuning")]
        [Tooltip("Extra pixels of padding around the target rect before the hole.")]
        [SerializeField] private float _padding = 12f;
        [SerializeField] private float _dimAlphaDialog = 0.7f;
        [SerializeField] private float _dimAlphaWaitForX = 0.5f;

        private Rect? _targetScreenRect;
        private bool _isFullDim;

        private void LateUpdate()
        {
            if (_isFullDim) return;
            if (!_targetScreenRect.HasValue) return;
            ApplyDonut(_targetScreenRect.Value);
        }

        public void ShowFullDim()
        {
            _isFullDim = true;
            _targetScreenRect = null;
            ApplyFullDim();
            SetAlpha(_dimAlphaDialog);
        }

        public void ShowDonut(Rect screenRect)
        {
            _isFullDim = false;
            _targetScreenRect = screenRect;
            ApplyDonut(screenRect);
            SetAlpha(_dimAlphaWaitForX);
        }

        public void Hide()
        {
            _isFullDim = false;
            _targetScreenRect = null;
            SetAlpha(0f);
        }

        private void ApplyFullDim()
        {
            if (_container == null) return;
            Vector2 size = _container.rect.size;
            SetRect(_top, new Rect(0f, 0f, size.x, size.y));
            SetRect(_bottom, Rect.zero);
            SetRect(_left, Rect.zero);
            SetRect(_right, Rect.zero);
            HideCorners();
        }

        private void ApplyDonut(Rect screenRect)
        {
            if (_container == null) return;
            Vector2 size = _container.rect.size;
            Rect containerRect = ScreenRectToContainerRect(screenRect);
            var donut = ComputeDonut(size.x, size.y, containerRect, _padding);
            SetRect(_top, donut.top);
            SetRect(_bottom, donut.bottom);
            SetRect(_left, donut.left);
            SetRect(_right, donut.right);
            ApplyCorners(donut);
        }

        /// <summary>
        /// Position 4 quarter-circle Images at the inner corners of the donut
        /// hole. Each is centered ON the corner so 1/4 of the circle covers
        /// the corner of the hole (rounding it) while the other 3/4 extends
        /// into the dim rects (where it's redundant). Diameter = 2 * radius.
        /// </summary>
        private void ApplyCorners(DonutRects donut)
        {
            float r = _cornerRadius;
            if (r <= 0f) { HideCorners(); return; }

            // Hole bounds derived from the dim edges (independent of pivot).
            float holeXMin = donut.left.xMax;
            float holeXMax = donut.right.xMin;
            float holeYMin = donut.bottom.yMax;
            float holeYMax = donut.top.yMin;

            float d = r * 2f;
            // anchor=pivot=(0,0) on each corner Image, so anchoredPosition
            // is the bottom-left of the rect. Center the circle on the hole
            // corner by offsetting by -r in both axes.
            PositionCorner(_cornerNW, holeXMin - r, holeYMax - r, d);
            PositionCorner(_cornerNE, holeXMax - r, holeYMax - r, d);
            PositionCorner(_cornerSE, holeXMax - r, holeYMin - r, d);
            PositionCorner(_cornerSW, holeXMin - r, holeYMin - r, d);
        }

        private static void PositionCorner(Image img, float xMin, float yMin, float diameter)
        {
            if (img == null) return;
            var rt = img.rectTransform;
            rt.anchoredPosition = new Vector2(xMin, yMin);
            rt.sizeDelta = new Vector2(diameter, diameter);
        }

        private void HideCorners()
        {
            PositionCorner(_cornerNW, 0f, 0f, 0f);
            PositionCorner(_cornerNE, 0f, 0f, 0f);
            PositionCorner(_cornerSE, 0f, 0f, 0f);
            PositionCorner(_cornerSW, 0f, 0f, 0f);
        }

        private Rect ScreenRectToContainerRect(Rect screenRect)
        {
            // Convert two screen-space corners into container-local coords.
            // Camera is null for Screen Space Overlay canvases (what the
            // tutorial overlay uses).
            var canvas = _container.GetComponentInParent<Canvas>();
            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? canvas.worldCamera
                : null;

            Vector2 min, max;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _container, new Vector2(screenRect.xMin, screenRect.yMin), cam, out min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _container, new Vector2(screenRect.xMax, screenRect.yMax), cam, out max);

            // ScreenPointToLocalPointInRectangle returns coords relative to
            // the container's pivot (so (0,0) is the pivot, not the
            // bottom-left). The 4 dim children have anchor+pivot (0,0) so
            // their anchoredPosition is bottom-left-origin. Shift the
            // converted points to match.
            Vector2 size = _container.rect.size;
            Vector2 pivotOffset = new Vector2(size.x * _container.pivot.x, size.y * _container.pivot.y);
            min += pivotOffset;
            max += pivotOffset;

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        /// <summary>
        /// Pure math for the 4-rect donut. Exposed as static for direct
        /// unit testing without needing a MonoBehaviour instance.
        /// Target is given in parent-local (bottom-left origin) coordinates.
        /// </summary>
        public static DonutRects ComputeDonut(float parentWidth, float parentHeight, Rect target, float padding)
        {
            float minX = Mathf.Max(0f, target.xMin - padding);
            float maxX = Mathf.Min(parentWidth, target.xMax + padding);
            float minY = Mathf.Max(0f, target.yMin - padding);
            float maxY = Mathf.Min(parentHeight, target.yMax + padding);

            if (maxX < minX) maxX = minX;
            if (maxY < minY) maxY = minY;

            return new DonutRects
            {
                top = Rect.MinMaxRect(0f, maxY, parentWidth, parentHeight),
                bottom = Rect.MinMaxRect(0f, 0f, parentWidth, minY),
                left = Rect.MinMaxRect(0f, minY, minX, maxY),
                right = Rect.MinMaxRect(maxX, minY, parentWidth, maxY),
            };
        }

        public struct DonutRects
        {
            public Rect top;
            public Rect bottom;
            public Rect left;
            public Rect right;
        }

        private static void SetRect(RectTransform rt, Rect r)
        {
            if (rt == null) return;
            rt.anchoredPosition = new Vector2(r.xMin, r.yMin);
            rt.sizeDelta = new Vector2(Mathf.Max(0f, r.width), Mathf.Max(0f, r.height));
        }

        private void SetAlpha(float alpha)
        {
            ApplyAlpha(_topImage, alpha);
            ApplyAlpha(_bottomImage, alpha);
            ApplyAlpha(_leftImage, alpha);
            ApplyAlpha(_rightImage, alpha);
            // Corner caps share the same alpha so they blend seamlessly with
            // the dim rects. They keep raycastTarget=false so clicks still
            // pass through the hole region.
            ApplyAlpha(_cornerNW, alpha, raycastWhenVisible: false);
            ApplyAlpha(_cornerNE, alpha, raycastWhenVisible: false);
            ApplyAlpha(_cornerSE, alpha, raycastWhenVisible: false);
            ApplyAlpha(_cornerSW, alpha, raycastWhenVisible: false);
        }

        private static void ApplyAlpha(Image img, float alpha, bool raycastWhenVisible = true)
        {
            if (img == null) return;
            Color c = img.color;
            c.a = alpha;
            img.color = c;
            img.raycastTarget = raycastWhenVisible && alpha > 0f;
        }
    }
}
