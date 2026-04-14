using UnityEngine;
using UnityEngine.UI;
using FortuneValley.UI.Panels.Credit;

namespace FortuneValley.UI.Panels
{
    /// <summary>
    /// UIPanel shell for the CreditSystemPanel.
    /// Handles Show/Hide/Toggle and the close button.
    /// Content is managed by sub-panel scripts (CreditHomeSubPanel, etc.).
    /// Also exposes OpenExploreForLot so the "buy but can't afford" flow can
    /// route the player directly to Explore with the lot pre-selected.
    /// </summary>
    public class LoanPanel : UIPanel
    {
        [Header("Controls")]
        [SerializeField] private Button _closeButton;

        [Header("Explore Routing")]
        [Tooltip("Sidebar on this panel. Used to force the Explore tab when opened from a lot buy-click.")]
        [SerializeField] private SidebarController _sidebarController;

        [Tooltip("Index of the Explore sub-panel within the sidebar (0-based).")]
        [SerializeField] private int _exploreTabIndex = 1;

        [Tooltip("Explore sub-panel controller. Receives the pre-selected lot id.")]
        [SerializeField] private CreditExploreSubPanel _creditExploreSubPanel;

        private void Start()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        /// <summary>
        /// Stages the Explore tab and the pre-selected lot. Call this BEFORE showing
        /// the panel so the sidebar's OnEnable picks up the override. The caller
        /// (UIManager) then invokes ShowPanel, which activates this panel normally.
        /// </summary>
        public void PrepareExploreForLot(string lotId)
        {
            if (_creditExploreSubPanel != null)
                _creditExploreSubPanel.SetPendingLotId(lotId);

            if (_sidebarController != null)
                _sidebarController.SetInitialIndexOverride(_exploreTabIndex);
        }

        /// <summary>
        /// Convenience: stage the override and then immediately force the tab.
        /// Safe to call while the panel is already visible (re-entry from another lot).
        /// </summary>
        public void OpenExploreForLot(string lotId)
        {
            PrepareExploreForLot(lotId);

            // If the panel is already active the Start/OnEnable pass has run -- apply now.
            if (gameObject.activeInHierarchy && _sidebarController != null)
                _sidebarController.SwitchTo(_exploreTabIndex);
        }
    }
}
