using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.UI;

// Allow the Editor test assembly to access the internal GraphLayout class.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("FortuneValley.Tests.Editor")]

namespace FortuneValley.UI.Components
{
    /// <summary>
    /// Reusable UI Graphic that renders a line graph using VertexHelper quads.
    /// Integrates with Canvas batching — no Texture2D pixel-painting (WebGL safe).
    /// Used for both the portfolio overview graph and per-stock price history.
    /// Overview tab: draws two lines (total wealth + net gain).
    /// Invest tab: draws one line (stock price history).
    /// </summary>
    public class LineGraphGraphic : Graphic
    {
        // ═══════════════════════════════════════════════════════════════
        // INSPECTOR
        // ═══════════════════════════════════════════════════════════════

        [SerializeField] private Color _lineColor = Color.red;                               // primary line (total wealth)
        [SerializeField] private Color _secondaryLineColor = new Color(0.9f, 0.8f, 0.2f);   // secondary line (net gain)
        [SerializeField] private float _lineWidth = 2.5f;
        [SerializeField] private Color _gridLineColor = new Color(0f, 0f, 0f, 0.08f);
        [SerializeField] private int _gridLineCount = 4;
        [SerializeField] private Color _labelColor = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        // Dark grey at 85% — readable on the light-grey placeholder background.
        // Inspector-tweakable if the background colour ever changes.

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC CONSTANTS — consumed by PortfolioPanel to inset the graph
        // so axis labels don't clip outside the container
        // ═══════════════════════════════════════════════════════════════

        public const float YLabelWidth  = 50f;
        public const float XLabelHeight = 20f;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private List<float> _data          = new List<float>();
        private List<float> _secondaryData = new List<float>();
        private List<float> _combinedData  = new List<float>(); // cached union of both series
        private int _startDayLabel;

        // Y-range computed once in SetData, reused in OnPopulateMesh + UpdateAxisLabels
        // to keep the label values in sync with the drawn grid lines.
        private float _paddedMin;
        private float _paddedMax;

        // Axis labels created lazily on the first SetData() call (rect is valid by then).
        private bool _labelsCreated;
        private TextMeshProUGUI[] _xLabels;       // 5 X-axis tick labels
        private TextMeshProUGUI[] _yLabels;       // 4 Y-axis tick labels (one per grid line)
        private TextMeshProUGUI _collectingLabel; // shown when data < 2

        // Font for all axis labels — set by PortfolioPanel before first SetData call
        private TMP_FontAsset _labelFont;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC API
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Called by PortfolioPanel to apply a consistent font to all axis labels.</summary>
        public void SetLabelFont(TMP_FontAsset font) { _labelFont = font; }

        /// <summary>
        /// Single-series overload — used by the Invest tab stock graph.
        /// Delegates to the dual-series overload with no secondary data.
        /// </summary>
        public void SetData(IReadOnlyList<float> data, int startDayLabel)
        {
            SetData(data, null, startDayLabel);
        }

        /// <summary>
        /// Dual-series overload — used by the Overview tab.
        /// Primary (red) = total wealth. Secondary (yellow) = net investment gain.
        /// Call order is intentional:
        ///   1. Copy both data series.
        ///   2. Rebuild _combinedData and cache Y-range.
        ///   3. UpdateAxisLabels (needs cached range).
        ///   4. SetVerticesDirty once (OnPopulateMesh uses cached range).
        /// </summary>
        public void SetData(IReadOnlyList<float> data, IReadOnlyList<float> secondary, int startDayLabel)
        {
            // Lazy label creation — only after layout is established
            if (!_labelsCreated)
                CreateAxisLabels();

            bool hasEnoughData = data != null && data.Count >= 2;

            if (!hasEnoughData)
            {
                _data.Clear();
                _secondaryData.Clear();
                _combinedData.Clear();
                SetLabelsVisible(false);
                if (_collectingLabel != null) _collectingLabel.gameObject.SetActive(true);
                SetVerticesDirty();
                return;
            }

            // 1. Copy both series (caller retains ownership of the source lists)
            GraphLayout.CopyToList(data, _data);
            SetSecondaryData(secondary);
            _startDayLabel = startDayLabel;

            // 2. Rebuild combined cache and Y-range so both lines share the same scale
            _combinedData.Clear();
            _combinedData.AddRange(_data);
            if (_secondaryData.Count >= 2) _combinedData.AddRange(_secondaryData);
            (_paddedMin, _paddedMax) = GraphLayout.ComputeYRange(_combinedData);

            // 3. Labels use cached range — must happen after step 2
            if (_collectingLabel != null) _collectingLabel.gameObject.SetActive(false);
            SetLabelsVisible(true);
            UpdateAxisLabels();

            // 4. Deferred mesh rebuild uses cached range — must happen after step 2
            SetVerticesDirty();
        }

        // ═══════════════════════════════════════════════════════════════
        // MESH GENERATION
        // ═══════════════════════════════════════════════════════════════

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_data == null || _data.Count < 2) return;

            Rect rect = rectTransform.rect;
            // Use cached range — computed in SetData so labels always match drawn lines
            float pMin = _paddedMin;
            float pMax = _paddedMax;

            // Draw horizontal grid lines for educational readability
            for (int g = 0; g < _gridLineCount; g++)
            {
                float t = (g + 1f) / (_gridLineCount + 1f);
                float y = Mathf.Lerp(rect.yMin, rect.yMax, t);
                AddHorizontalQuad(vh, rect.xMin, rect.xMax, y, 0.5f, _gridLineColor);
            }

            // Primary line (red) — total wealth
            for (int i = 0; i < _data.Count - 1; i++)
            {
                Vector2 p0 = GraphLayout.MapPoint(_data[i],     i,     _data.Count, rect, pMin, pMax);
                Vector2 p1 = GraphLayout.MapPoint(_data[i + 1], i + 1, _data.Count, rect, pMin, pMax);
                AddLineSegment(vh, p0, p1, _lineWidth, _lineColor);
            }

            // Secondary line (yellow) — net investment gain; only when enough data points
            if (_secondaryData != null && _secondaryData.Count >= 2)
            {
                for (int i = 0; i < _secondaryData.Count - 1; i++)
                {
                    Vector2 p0 = GraphLayout.MapPoint(_secondaryData[i],     i,     _secondaryData.Count, rect, pMin, pMax);
                    Vector2 p1 = GraphLayout.MapPoint(_secondaryData[i + 1], i + 1, _secondaryData.Count, rect, pMin, pMax);
                    AddLineSegment(vh, p0, p1, _lineWidth, _secondaryLineColor);
                }
            }
        }

        /// <summary>Thin horizontal quad for a grid line.</summary>
        private static void AddHorizontalQuad(VertexHelper vh, float xMin, float xMax,
            float y, float halfThick, Color color)
        {
            int idx = vh.currentVertCount;
            UIVertex v = UIVertex.simpleVert;
            v.color = color;

            v.position = new Vector3(xMin, y - halfThick, 0); vh.AddVert(v);
            v.position = new Vector3(xMin, y + halfThick, 0); vh.AddVert(v);
            v.position = new Vector3(xMax, y + halfThick, 0); vh.AddVert(v);
            v.position = new Vector3(xMax, y - halfThick, 0); vh.AddVert(v);

            vh.AddTriangle(idx, idx + 1, idx + 2);
            vh.AddTriangle(idx, idx + 2, idx + 3);
        }

        /// <summary>Line segment as a quad rotated along the segment direction.</summary>
        private static void AddLineSegment(VertexHelper vh, Vector2 p0, Vector2 p1,
            float width, Color color)
        {
            Vector2 dir    = (p1 - p0).normalized;
            Vector2 normal = new Vector2(-dir.y, dir.x) * (width * 0.5f);

            int idx = vh.currentVertCount;
            UIVertex v = UIVertex.simpleVert;
            v.color = color;

            v.position = new Vector3(p0.x - normal.x, p0.y - normal.y, 0); vh.AddVert(v);
            v.position = new Vector3(p0.x + normal.x, p0.y + normal.y, 0); vh.AddVert(v);
            v.position = new Vector3(p1.x + normal.x, p1.y + normal.y, 0); vh.AddVert(v);
            v.position = new Vector3(p1.x - normal.x, p1.y - normal.y, 0); vh.AddVert(v);

            vh.AddTriangle(idx, idx + 1, idx + 2);
            vh.AddTriangle(idx, idx + 2, idx + 3);
        }

        // ═══════════════════════════════════════════════════════════════
        // AXIS LABELS (lazy creation — called once after layout)
        // ═══════════════════════════════════════════════════════════════

        private void CreateAxisLabels()
        {
            _labelsCreated = true;
            CreateCollectingLabel();
            CreateValueLabels();  // X/Y tick labels
            CreateTitleLabels();  // "($)" and "Day" axis titles
        }

        /// <summary>"Collecting data…" placeholder shown before 2 data points arrive.</summary>
        private void CreateCollectingLabel()
        {
            var rt = MakeLabel("CollectingLabel");
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(200f, 30f);
            _collectingLabel = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (_labelFont != null) _collectingLabel.font = _labelFont;
            _collectingLabel.text = "Collecting data\u2026";
            _collectingLabel.fontSize = 12;
            _collectingLabel.color = new Color(1f, 1f, 1f, 0.6f);
            _collectingLabel.alignment = TextAlignmentOptions.Center;
            rt.gameObject.SetActive(false);
        }

        /// <summary>Create X-axis (5 labels) and Y-axis (4 labels) tick labels.</summary>
        private void CreateValueLabels()
        {
            // 5 X-axis labels positioned below the rect
            _xLabels = new TextMeshProUGUI[5];
            for (int i = 0; i < 5; i++)
            {
                var rt = MakeLabel($"XLabel_{i}");
                rt.anchorMin = new Vector2(0.5f, 0.5f); // center anchor — anchoredPosition is in graph local space
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(60f, 20f);
                rt.pivot = new Vector2(0.5f, 1f); // top-center: label hangs below the positioned point
                var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
                if (_labelFont != null) tmp.font = _labelFont;
                tmp.fontSize = 9;
                tmp.color = _labelColor;
                tmp.alignment = TextAlignmentOptions.Center;
                _xLabels[i] = tmp;
                rt.gameObject.SetActive(false);
            }

            // 4 Y-axis labels positioned to the left of the rect
            _yLabels = new TextMeshProUGUI[4];
            for (int i = 0; i < 4; i++)
            {
                var rt = MakeLabel($"YLabel_{i}");
                rt.anchorMin = new Vector2(0.5f, 0.5f); // center anchor — anchoredPosition is in graph local space
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(50f, 20f);
                rt.pivot = new Vector2(1f, 0.5f); // right-center: label extends left of the positioned point
                var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
                if (_labelFont != null) tmp.font = _labelFont;
                tmp.fontSize = 9;
                tmp.color = _labelColor;
                tmp.alignment = TextAlignmentOptions.Right;
                _yLabels[i] = tmp;
                rt.gameObject.SetActive(false);
            }
        }

        /// <summary>Create "($)" (Y-axis) and "Day" (X-axis) title labels.</summary>
        private void CreateTitleLabels()
        {
            // Compute positions in graph local space (same coordinate space UpdateAxisLabels uses).
            // Center anchor (0.5, 0.5) means anchoredPosition IS the position in parent local space.
            Rect rect = rectTransform.rect;

            // Y-axis title "($)" — rotated 90°, top-left corner ABOVE the graph.
            // Placed above rect.yMax so it does not overlap any Y tick label.
            var yTitle = MakeLabel("YAxisTitle");
            yTitle.anchorMin = new Vector2(0.5f, 0.5f);
            yTitle.anchorMax = new Vector2(0.5f, 0.5f);
            yTitle.sizeDelta = new Vector2(40f, 14f);
            yTitle.pivot = new Vector2(0.5f, 0.5f);
            yTitle.anchoredPosition = new Vector2(rect.xMin - 60f, 0f);
            yTitle.localEulerAngles = new Vector3(0f, 0f, 90f);
            var yTmp = yTitle.gameObject.AddComponent<TextMeshProUGUI>();
            if (_labelFont != null) yTmp.font = _labelFont;
            yTmp.text = "($)";
            yTmp.fontSize = 9;
            yTmp.color = _labelColor;
            yTmp.alignment = TextAlignmentOptions.Center;

            // X-axis title "Day" — centred horizontally, below all X tick labels.
            // X tick labels end at ~rect.yMin - 24; placing centre at yMin - 30 avoids overlap.
            var xTitle = MakeLabel("XAxisTitle");
            xTitle.anchorMin = new Vector2(0.5f, 0.5f);
            xTitle.anchorMax = new Vector2(0.5f, 0.5f);
            xTitle.sizeDelta = new Vector2(30f, 12f);
            xTitle.pivot = new Vector2(0.5f, 0.5f);
            xTitle.anchoredPosition = new Vector2(0f, rect.yMin - 30f);
            var xTmp = xTitle.gameObject.AddComponent<TextMeshProUGUI>();
            if (_labelFont != null) xTmp.font = _labelFont;
            xTmp.text = "Day";
            xTmp.fontSize = 9;
            xTmp.color = _labelColor;
            xTmp.alignment = TextAlignmentOptions.Center;
        }

        /// <summary>Create a named RectTransform child. Shared by all label factories.</summary>
        private RectTransform MakeLabel(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            return go.GetComponent<RectTransform>();
        }

        private void UpdateAxisLabels()
        {
            if (_data == null || _data.Count < 2) return;

            Rect rect = rectTransform.rect;
            int count = _data.Count;
            // Use cached range — same values used in OnPopulateMesh so labels align with grid lines
            float paddedMin = _paddedMin;
            float paddedMax = _paddedMax;

            // X labels: 5 evenly spaced across the window (may show negative day numbers)
            if (_xLabels != null)
            {
                for (int i = 0; i < _xLabels.Length; i++)
                {
                    if (_xLabels[i] == null) continue;

                    float frac     = i / (float)(_xLabels.Length - 1);
                    int   dayIndex = Mathf.RoundToInt(frac * (count - 1));
                    int   dayLabel = _startDayLabel + dayIndex;

                    UIBuilderUtils.SetTextIfChanged(_xLabels[i], $"Day {dayLabel}");

                    var rt = _xLabels[i].GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(Mathf.Lerp(rect.xMin, rect.xMax, frac), rect.yMin - 4f);
                    _xLabels[i].gameObject.SetActive(true);
                }
            }

            // Y labels: one per grid line, matching the horizontal lines drawn in OnPopulateMesh
            if (_yLabels != null)
            {
                for (int g = 0; g < _gridLineCount && g < _yLabels.Length; g++)
                {
                    if (_yLabels[g] == null) continue;

                    float t     = (g + 1f) / (_gridLineCount + 1f);
                    float value = Mathf.Lerp(paddedMin, paddedMax, t);
                    float yPos  = Mathf.Lerp(rect.yMin, rect.yMax, t);

                    UIBuilderUtils.SetTextIfChanged(_yLabels[g], $"${Mathf.RoundToInt(value)}");

                    var rt = _yLabels[g].GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(rect.xMin - 4f, yPos);
                    _yLabels[g].gameObject.SetActive(true);
                }
            }
        }

        private void SetLabelsVisible(bool visible)
        {
            if (_xLabels != null)
                foreach (var l in _xLabels)
                    if (l != null) l.gameObject.SetActive(visible);

            if (_yLabels != null)
                foreach (var l in _yLabels)
                    if (l != null) l.gameObject.SetActive(visible);
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Copy secondary data into _secondaryData.
        /// Does NOT call SetVerticesDirty — the calling SetData overload handles that once.
        /// </summary>
        private void SetSecondaryData(IReadOnlyList<float> data)
        {
            GraphLayout.CopyToList(data, _secondaryData);
        }

        // ═══════════════════════════════════════════════════════════════
        // INNER STATIC CLASS — pure math, testable without Canvas setup
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Pure layout helpers with no MonoBehaviour dependency.
        /// Marked internal so unit tests can reach it via InternalsVisibleTo.
        /// </summary>
        internal static class GraphLayout
        {
            /// <summary>
            /// Clear target and copy all elements from source. Handles null source.
            /// Indexed loop avoids IEnumerator allocation on interface types.
            /// </summary>
            internal static void CopyToList(IReadOnlyList<float> source, List<float> target)
            {
                target.Clear();
                if (source == null) return;
                for (int i = 0; i < source.Count; i++)
                    target.Add(source[i]);
            }

            /// <summary>
            /// Compute padded Y range for the graph.
            /// Uses a minimum range of 1f to prevent degenerate display on flat data.
            /// Padding: 8% above and below the actual min/max.
            /// Pass the combined primary+secondary list so both lines share the same scale.
            /// </summary>
            internal static (float paddedMin, float paddedMax) ComputeYRange(IReadOnlyList<float> data)
            {
                float min = float.MaxValue;
                float max = float.MinValue;
                foreach (float v in data)
                {
                    if (v < min) min = v;
                    if (v > max) max = v;
                }

                float range = max - min;
                if (range < 1f) range = 1f; // floor avoids divide-by-zero on flat data

                float paddedMin = min - range * 0.08f;
                float paddedMax = max + range * 0.08f;
                return (paddedMin, paddedMax);
            }

            /// <summary>
            /// Map a value + index to a 2D canvas point inside rect.
            /// index 0 → left edge, index count-1 → right edge.
            /// value == paddedMin → bottom of rect, value == paddedMax → top.
            /// </summary>
            internal static Vector2 MapPoint(float value, int index, int count, Rect rect,
                float paddedMin, float paddedMax)
            {
                float xFrac = count <= 1 ? 0f : (float)index / (count - 1);
                float yFrac = (paddedMax - paddedMin) > 0f
                    ? (value - paddedMin) / (paddedMax - paddedMin)
                    : 0.5f;

                float x = Mathf.Lerp(rect.xMin, rect.xMax, xFrac);
                float y = Mathf.Lerp(rect.yMin, rect.yMax, yFrac);
                return new Vector2(x, y);
            }
        }
    }
}
