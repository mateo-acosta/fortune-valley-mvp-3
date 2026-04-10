using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FortuneValley.UI.Components
{
    /// <summary>
    /// View component for a single holding item in the Portfolio list.
    /// Holds explicit references to UI elements, eliminating index-based lookups.
    ///
    /// Attach to the Stock_in_Verticall_Scroll prefab and wire fields in the Inspector.
    /// </summary>
    public class HoldingListItemView : MonoBehaviour
    {
        [Header("Text Fields")]
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _valueText;

        [Header("Icon")]
        [SerializeField] private Image _iconImage;

        public void SetName(string text)
        {
            if (_nameText != null)
                _nameText.text = text;
        }

        public void SetValue(string text, Color color)
        {
            if (_valueText == null) return;
            _valueText.text = text;
            _valueText.color = color;
        }

        public void SetIcon(Sprite sprite)
        {
            if (_iconImage != null)
                _iconImage.sprite = sprite;
        }
    }
}
