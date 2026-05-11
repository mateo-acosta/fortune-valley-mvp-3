using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FortuneValley.UI.Components
{
    /// <summary>
    /// View component for a single transaction card in the History grid.
    /// Different layout from InsuranceCardItemView: shows transaction type,
    /// date, lot, amount, and description.
    ///
    /// Attach to the history card prefab and wire fields in the Inspector.
    /// </summary>
    public class InsuranceHistoryCardView : MonoBehaviour
    {
        [Header("Text Fields")]
        [SerializeField] private TMP_Text _typeLabel;
        [SerializeField] private TMP_Text _dateText;
        [SerializeField] private TMP_Text _lotText;
        [SerializeField] private TMP_Text _amountText;
        [SerializeField] private TMP_Text _descriptionText;

        [Header("Action")]
        [SerializeField] private Button _detailsButton;

        public void SetTypeLabel(string text)
        {
            if (_typeLabel != null)
                _typeLabel.text = text;
        }

        public void SetDate(string text)
        {
            if (_dateText != null)
                _dateText.text = text;
        }

        public void SetLot(string text)
        {
            if (_lotText != null)
                _lotText.text = text;
        }

        public void SetAmount(string text)
        {
            if (_amountText != null)
                _amountText.text = text;
        }

        public void SetDescription(string text)
        {
            if (_descriptionText != null)
                _descriptionText.text = text;
        }

        public void SetDetailsCallback(Action callback)
        {
            if (_detailsButton == null) return;
            _detailsButton.onClick.RemoveAllListeners();
            if (callback != null)
                _detailsButton.onClick.AddListener(() => callback());
        }
    }
}
