using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FortuneValley.UI.Components
{
    /// <summary>
    /// View component for a single insurance card in the Home/Explore grids.
    /// Holds explicit references to UI elements, eliminating index-based
    /// GetComponentsInChildren lookups.
    ///
    /// Reused across Home (owned policy cards) and Explore (available policy cards).
    /// Attach to the insurance card prefab and wire fields in the Inspector.
    /// </summary>
    public class InsuranceCardItemView : MonoBehaviour
    {
        [Header("Background")]
        [SerializeField] private Image _backgroundImage;

        [Header("Text Fields")]
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _typeText;
        [SerializeField] private TMP_Text _premiumText;
        [SerializeField] private TMP_Text _detailText;
        [SerializeField] private TMP_Text _statusText;

        [Header("Action")]
        [SerializeField] private Button _actionButton;
        [SerializeField] private TMP_Text _actionButtonLabel;

        [Header("Greyed Out State")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Greyed Out Settings")]
        [SerializeField] private float _greyedOutAlpha = 0.5f;

        public void SetName(string text)
        {
            if (_nameText != null)
                _nameText.text = text;
        }

        public void SetType(string text)
        {
            if (_typeText != null)
                _typeText.text = text;
        }

        public void SetPremium(string text)
        {
            if (_premiumText != null)
                _premiumText.text = text;
        }

        public void SetDetail(string text)
        {
            if (_detailText != null)
                _detailText.text = text;
        }

        public void SetStatus(string text)
        {
            if (_statusText != null)
                _statusText.text = text;
        }

        public void SetBackground(Sprite sprite)
        {
            if (_backgroundImage != null && sprite != null)
                _backgroundImage.sprite = sprite;
        }

        public void SetActionLabel(string text)
        {
            if (_actionButtonLabel != null)
                _actionButtonLabel.text = text;
        }

        public void SetActionCallback(Action callback)
        {
            if (_actionButton == null) return;
            _actionButton.onClick.RemoveAllListeners();
            if (callback != null)
                _actionButton.onClick.AddListener(() => callback());
        }

        /// <summary>
        /// Toggle greyed-out visual state for fully covered policies.
        /// When greyed out, the action button is not interactable.
        /// </summary>
        public void SetGreyedOut(bool greyed)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = greyed ? _greyedOutAlpha : 1f;
                _canvasGroup.interactable = !greyed;
            }

            if (_actionButton != null)
                _actionButton.interactable = !greyed;
        }
    }
}
