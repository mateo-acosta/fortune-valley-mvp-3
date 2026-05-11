using System;
using UnityEngine;
using UnityEngine.UI;

namespace FortuneValley.UI.Components
{
    /// <summary>
    /// Reusable controller for a single row of filter buttons.
    /// Domain-agnostic: only tracks which button index is selected.
    /// The consuming sub-panel maps indices to domain meaning.
    ///
    /// Wire the _filterButtons array in the Inspector in the order
    /// that matches your index-to-domain mapping. Index 0 is "All."
    /// </summary>
    public class FilterRowController : MonoBehaviour
    {
        [Header("Buttons (order = index, 0 = All)")]
        [SerializeField] private Button[] _filterButtons;

        [Header("Behavior")]
        [Tooltip("When false, clicking the selected button does nothing (always one selected). Use for time filters.")]
        [SerializeField] private bool _allowDeselect = true;

        [Header("Visual State")]
        [SerializeField] private Color _normalColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField] private Color _selectedColor = new Color(0.3f, 0.7f, 1f, 1f);

        private int _selectedIndex;

        /// <summary>
        /// Currently selected button index. 0 = "All" (no filter).
        /// </summary>
        public int SelectedIndex => _selectedIndex;

        /// <summary>
        /// Fired when the selected index changes. Passes the new index.
        /// </summary>
        public event Action<int> OnSelectionChanged;

        private void Awake()
        {
            if (_filterButtons == null) return;

            for (int i = 0; i < _filterButtons.Length; i++)
            {
                if (_filterButtons[i] == null) continue;
                int capturedIndex = i;
                _filterButtons[i].onClick.AddListener(() => HandleButtonClicked(capturedIndex));
            }

            // Start with index 0 selected
            _selectedIndex = 0;
            ApplyVisualState();
        }

        private void HandleButtonClicked(int index)
        {
            // Clicking the already-selected button
            if (index == _selectedIndex && index != 0)
            {
                if (!_allowDeselect) return; // Time filters: always keep one selected
                _selectedIndex = 0; // Category filters: deselect to All
            }
            else
            {
                _selectedIndex = index;
            }

            ApplyVisualState();
            OnSelectionChanged?.Invoke(_selectedIndex);
        }

        /// <summary>
        /// Programmatically select a button index. Fires OnSelectionChanged.
        /// Used by sub-panels for auto-narrow behavior.
        /// </summary>
        public void Select(int index)
        {
            if (index < 0 || _filterButtons == null || index >= _filterButtons.Length)
                return;

            _selectedIndex = index;
            ApplyVisualState();
            OnSelectionChanged?.Invoke(_selectedIndex);
        }

        /// <summary>
        /// Reset to index 0 (All) without firing OnSelectionChanged.
        /// Used when hiding this filter row so the caller controls the refresh.
        /// </summary>
        public void ResetToAll()
        {
            _selectedIndex = 0;
            ApplyVisualState();
        }

        private void ApplyVisualState()
        {
            if (_filterButtons == null) return;

            for (int i = 0; i < _filterButtons.Length; i++)
            {
                if (_filterButtons[i] == null) continue;
                var graphic = _filterButtons[i].targetGraphic as Graphic;
                if (graphic != null)
                    graphic.color = (i == _selectedIndex) ? _selectedColor : _normalColor;
            }
        }
    }
}
