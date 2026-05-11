using System;
using UnityEngine;
using UnityEngine.UI;
using FortuneValley.Core;

namespace FortuneValley.UI.Tutorial
{
    /// <summary>
    /// Modal confirmation for the Replay Tutorial settings button. Extends
    /// <see cref="UIPopup"/> so it participates in the existing popup stack
    /// (blocking-panel raycasts flip while it is open, meaning the guidance
    /// system defers banners until it closes). Confirm fires
    /// <see cref="OnReplayConfirmed"/>; the settings UI glue subscribes to
    /// trigger the wipe + tutorial restart.
    /// </summary>
    public class ReplayConfirmPopup : UIPopup
    {
        [Header("Buttons")]
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        public event Action OnReplayConfirmed;

        protected override void OnShow()
        {
            base.OnShow();
            if (_confirmButton != null) _confirmButton.onClick.AddListener(HandleConfirm);
            if (_cancelButton != null) _cancelButton.onClick.AddListener(HandleCancel);
        }

        protected override void OnHide()
        {
            if (_confirmButton != null) _confirmButton.onClick.RemoveListener(HandleConfirm);
            if (_cancelButton != null) _cancelButton.onClick.RemoveListener(HandleCancel);
            base.OnHide();
        }

        private void HandleConfirm()
        {
            OnReplayConfirmed?.Invoke();
            OnConfirmClicked();
        }

        private void HandleCancel() => OnCancelClicked();
    }
}
