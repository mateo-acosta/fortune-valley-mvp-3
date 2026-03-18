using System;
using UnityEngine;

namespace FortuneValley.UI
{
    /// <summary>
    /// Base class for all UI panels.
    /// Panels are full-screen or large UI areas that can be toggled.
    /// </summary>
    public abstract class UIPanel : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // REFERENCES
        // ═══════════════════════════════════════════════════════════════

        [Header("Panel Settings")]
        [Tooltip("The root GameObject to show/hide")]
        [SerializeField] protected GameObject _panelRoot;

        // ═══════════════════════════════════════════════════════════════
        // STATE
        // ═══════════════════════════════════════════════════════════════

        public bool IsVisible { get; protected set; }

        /// <summary>
        /// Fired when this panel requests to be closed (from its own close button).
        /// UIManager subscribes to this at startup.
        /// </summary>
        public event Action<UIPanel> OnCloseRequested;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Show this panel.
        /// </summary>
        public virtual void Show()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(true);
            }
            else
            {
                gameObject.SetActive(true);
            }

            IsVisible = true;
            OnShow();
        }

        /// <summary>
        /// Hide this panel.
        /// </summary>
        public virtual void Hide()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }

            IsVisible = false;
            OnHide();
        }

        /// <summary>
        /// Toggle this panel's visibility.
        /// </summary>
        public void Toggle()
        {
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // VIRTUAL METHODS (override in subclasses)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Called when the panel is shown. Override to refresh data.
        /// </summary>
        protected virtual void OnShow() { }

        /// <summary>
        /// Called when the panel is hidden. Override to cleanup.
        /// </summary>
        protected virtual void OnHide() { }

        // ═══════════════════════════════════════════════════════════════
        // UI CALLBACKS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Called when close/back button is pressed.
        /// </summary>
        public void OnCloseButtonClicked()
        {
            OnCloseRequested?.Invoke(this);
        }
    }
}
