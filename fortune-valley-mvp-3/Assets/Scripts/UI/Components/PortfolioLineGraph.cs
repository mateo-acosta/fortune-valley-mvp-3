using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FortuneValley.UI.Components
{
    /// <summary>
    /// Draws a line graph on a RawImage texture showing portfolio performance.
    /// Renders two lines: total wealth (green) and net investment gain (yellow).
    /// </summary>
    public class PortfolioLineGraph : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RawImage _graphImage;

        [Header("Dimensions")]
        [SerializeField] private int _textureWidth = 400;
        [SerializeField] private int _textureHeight = 200;

        [Header("Colors")]
        [SerializeField] private Color _backgroundColor = new Color(0.08f, 0.08f, 0.12f);
        [SerializeField] private Color _gridColor = new Color(0.2f, 0.2f, 0.25f);
        [SerializeField] private Color _wealthLineColor = new Color(0.2f, 0.8f, 0.4f);
        [SerializeField] private Color _gainLineColor = new Color(0.9f, 0.8f, 0.2f);
        [SerializeField] private Color _zeroLineColor = new Color(0.4f, 0.4f, 0.4f);

        [Header("Settings")]
        [SerializeField] private int _gridLines = 4;

        private Texture2D _texture;

        private void Awake()
        {
            CreateTexture();
        }

        private void CreateTexture()
        {
            // Match texture resolution to actual display size for crisp rendering
            if (_graphImage != null)
            {
                Rect rect = _graphImage.rectTransform.rect;
                if (rect.width > 0 && rect.height > 0)
                {
                    _textureWidth = Mathf.RoundToInt(rect.width);
                    _textureHeight = Mathf.RoundToInt(rect.height);
                }
            }

            _texture = new Texture2D(_textureWidth, _textureHeight, TextureFormat.RGBA32, false);
            _texture.filterMode = FilterMode.Point; // Crisp pixels, no blurry upscaling
            _texture.wrapMode = TextureWrapMode.Clamp;

            if (_graphImage != null)
                _graphImage.texture = _texture;

            ClearGraph();
        }

        /// <summary>
        /// Clear the graph to background color.
        /// </summary>
        public void ClearGraph()
        {
            if (_texture == null) return;

            Color[] pixels = new Color[_textureWidth * _textureHeight];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = _backgroundColor;

            _texture.SetPixels(pixels);
            _texture.Apply();
        }

        /// <summary>
        /// Redraw the graph with new data.
        /// </summary>
        public void Redraw(IReadOnlyList<float> totalWealth, IReadOnlyList<float> netGain)
        {
            EnsureTextureMatchesDisplay(); // Recreate if RawImage rect changed since Awake
            if (_texture == null) CreateTexture();
            if (totalWealth == null || totalWealth.Count < 2) return;

            // Clear to background
            Color[] pixels = new Color[_textureWidth * _textureHeight];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = _backgroundColor;

            _texture.SetPixels(pixels);

            // Find value ranges for both series
            float minVal = float.MaxValue;
            float maxVal = float.MinValue;

            for (int i = 0; i < totalWealth.Count; i++)
            {
                if (totalWealth[i] < minVal) minVal = totalWealth[i];
                if (totalWealth[i] > maxVal) maxVal = totalWealth[i];
            }

            if (netGain != null)
            {
                for (int i = 0; i < netGain.Count; i++)
                {
                    if (netGain[i] < minVal) minVal = netGain[i];
                    if (netGain[i] > maxVal) maxVal = netGain[i];
                }
            }

            // Add padding to range
            float range = maxVal - minVal;
            if (range < 1f) range = 1f;
            float padding = range * 0.1f;
            minVal -= padding;
            maxVal += padding;
            range = maxVal - minVal;

            // Draw horizontal grid lines
            for (int g = 0; g <= _gridLines; g++)
            {
                int y = Mathf.RoundToInt((float)g / _gridLines * (_textureHeight - 1));
                DrawHorizontalLine(y, _gridColor);
            }

            // Draw zero reference line for net gain
            if (minVal < 0 && maxVal > 0)
            {
                int zeroY = Mathf.RoundToInt((-minVal / range) * (_textureHeight - 1));
                DrawHorizontalLine(zeroY, _zeroLineColor);
            }

            // Draw wealth line (green)
            DrawLine(totalWealth, minVal, range, _wealthLineColor);

            // Draw gain line (yellow)
            if (netGain != null && netGain.Count >= 2)
            {
                DrawLine(netGain, minVal, range, _gainLineColor);
            }

            _texture.Apply();
        }

        /// <summary>
        /// Recreate the texture if the RawImage display area has changed size.
        /// </summary>
        private void EnsureTextureMatchesDisplay()
        {
            if (_graphImage == null) return;
            Rect rect = _graphImage.rectTransform.rect;
            int w = Mathf.Max(64, Mathf.RoundToInt(rect.width));
            int h = Mathf.Max(64, Mathf.RoundToInt(rect.height));
            if (_texture != null && _texture.width == w && _texture.height == h) return;

            // Size changed — recreate texture at new resolution
            if (_texture != null) Destroy(_texture);
            _textureWidth = w;
            _textureHeight = h;
            CreateTexture();
        }

        private void DrawLine(IReadOnlyList<float> data, float minVal, float range, Color color)
        {
            int count = data.Count;
            if (count < 2) return;

            for (int i = 0; i < count - 1; i++)
            {
                // Map data index to X pixel
                float x0 = (float)i / (count - 1) * (_textureWidth - 1);
                float x1 = (float)(i + 1) / (count - 1) * (_textureWidth - 1);

                // Map value to Y pixel
                float y0 = (data[i] - minVal) / range * (_textureHeight - 1);
                float y1 = (data[i + 1] - minVal) / range * (_textureHeight - 1);

                DrawLineSegment(
                    Mathf.RoundToInt(x0), Mathf.RoundToInt(y0),
                    Mathf.RoundToInt(x1), Mathf.RoundToInt(y1),
                    color);
            }
        }

        private void DrawLineSegment(int x0, int y0, int x1, int y1, Color color)
        {
            // Bresenham's line algorithm with thickness
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                // Draw 2px thick point
                SetPixelSafe(x0, y0, color);
                SetPixelSafe(x0, y0 + 1, color);
                SetPixelSafe(x0 + 1, y0, color);

                if (x0 == x1 && y0 == y1) break;

                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        private void DrawHorizontalLine(int y, Color color)
        {
            if (y < 0 || y >= _textureHeight) return;

            for (int x = 0; x < _textureWidth; x++)
            {
                _texture.SetPixel(x, y, color);
            }
        }

        private void SetPixelSafe(int x, int y, Color color)
        {
            if (x >= 0 && x < _textureWidth && y >= 0 && y < _textureHeight)
            {
                _texture.SetPixel(x, y, color);
            }
        }

        private void OnDestroy()
        {
            if (_texture != null)
            {
                Destroy(_texture);
            }
        }
    }
}
