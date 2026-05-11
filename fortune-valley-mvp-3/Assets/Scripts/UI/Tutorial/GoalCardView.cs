using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;

namespace FortuneValley.UI.Tutorial
{
    /// <summary>
    /// Visual for a single Life Goal card inside GoalSelectionPanel. Stays
    /// dumb: receives a LifeGoalSO and a click callback at Bind time;
    /// renders icon, display name, threshold, description; toggles a
    /// "selected" highlight when SetSelected is called by the controller.
    /// </summary>
    public class GoalCardView : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private TextMeshProUGUI _displayNameText;
        [SerializeField] private TextMeshProUGUI _thresholdText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Button _button;

        [Header("Selection Highlight")]
        [Tooltip("Optional. GameObject to enable/disable to show 'this is the picked card for its tier'.")]
        [SerializeField] private GameObject _selectedHighlight;

        private LifeGoalSO _goal;
        private Action<LifeGoalSO> _clickCallback;

        public LifeGoalSO Goal => _goal;

        public void Bind(LifeGoalSO goal, Action<LifeGoalSO> clickCallback)
        {
            _goal = goal;
            _clickCallback = clickCallback;

            if (_displayNameText != null) _displayNameText.text = goal.DisplayName;
            if (_thresholdText != null) _thresholdText.text = "$" + goal.NetWorthThreshold.ToString("N0");
            if (_descriptionText != null) _descriptionText.text = goal.Description;
            if (_iconImage != null && goal.Icon != null) _iconImage.sprite = goal.Icon;

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(HandleClick);
            }

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (_selectedHighlight != null) _selectedHighlight.SetActive(selected);
        }

        private void HandleClick()
        {
            _clickCallback?.Invoke(_goal);
        }
    }
}
