using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FortuneValley.UI.Components
{
    /// <summary>
    /// View component for a single investment card in the Explore grid.
    /// Holds explicit references to UI elements, eliminating index-based
    /// GetComponentsInChildren lookups.
    ///
    /// Attach to the CardItem prefab and wire fields in the Inspector.
    /// </summary>
    public class CardItemView : MonoBehaviour
    {
        [Header("Background")]
        [SerializeField] private Image _backgroundImage;

        [Header("Text Fields")]
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _priceText;
        [SerializeField] private TMP_Text _changeText;
        [SerializeField] private TMP_Text _riskText;

        public void SetBackground(Sprite sprite)
        {
            if (_backgroundImage != null)
                _backgroundImage.sprite = sprite;
        }

        public void SetName(string text)
        {
            if (_nameText != null)
                _nameText.text = text;
        }

        public void SetPrice(string text)
        {
            if (_priceText != null)
                _priceText.text = text;
        }

        public void SetChange(string text, Color color)
        {
            if (_changeText == null) return;
            _changeText.text = text;
            _changeText.color = color;
        }

        public void SetRisk(string text)
        {
            if (_riskText != null)
                _riskText.text = text;
        }
    }
}
