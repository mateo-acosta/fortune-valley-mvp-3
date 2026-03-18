using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FortuneValley.UI
{
    /// <summary>
    /// Shared UI construction helpers used by both editor builders and runtime scripts.
    /// Keeps UI creation DRY across GameEndPanelBuilder and CoachChatUI.
    /// </summary>
    public static class UIBuilderUtils
    {
        // Cache the default TMP font so we only load it once
        private static TMP_FontAsset _defaultFont;
        private static TMP_FontAsset DefaultFont
        {
            get
            {
                if (_defaultFont == null)
                    _defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                return _defaultFont;
            }
        }

        /// <summary>
        /// Create a bare UI element with RectTransform and CanvasRenderer.
        /// </summary>
        public static GameObject CreateUIElement(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<CanvasRenderer>();
            return go;
        }

        /// <summary>
        /// Stretch a UI element to fill its parent.
        /// </summary>
        public static void StretchToFill(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Set a LayoutElement's preferred height (adds one if missing).
        /// </summary>
        public static void SetPreferredHeight(GameObject go, float height)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
        }

        /// <summary>
        /// Create a TextMeshProUGUI element with standard settings.
        /// </summary>
        public static TextMeshProUGUI CreateTMPText(string name, Transform parent,
            string text, float fontSize, FontStyles style, Color color)
        {
            var go = CreateUIElement(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = DefaultFont;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }

        /// <summary>
        /// Assigns text only if the value has changed, avoiding unnecessary TMP geometry rebuilds.
        /// Safe to call every tick on stable fields (risk label, holdings list, etc.)
        /// </summary>
        public static void SetTextIfChanged(TextMeshProUGUI tmp, string value)
        {
            if (tmp == null || tmp.text == value) return;
            tmp.text = value;
        }

        /// <summary>
        /// Create a Button with Image background and TMP label.
        /// </summary>
        public static Button CreateButton(string name, Transform parent,
            string label, Color bgColor, float width, float height)
        {
            var go = CreateUIElement(name, parent);
            var btnImage = go.AddComponent<Image>();
            btnImage.color = bgColor;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = btnImage;

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;

            var textGo = CreateUIElement("Text", go.transform);
            var textTMP = textGo.AddComponent<TextMeshProUGUI>();
            textTMP.font = DefaultFont;
            textTMP.text = label;
            textTMP.fontSize = 18;
            textTMP.fontStyle = FontStyles.Bold;
            textTMP.color = Color.white;
            textTMP.alignment = TextAlignmentOptions.Center;
            StretchToFill(textGo);

            return btn;
        }
    }
}
