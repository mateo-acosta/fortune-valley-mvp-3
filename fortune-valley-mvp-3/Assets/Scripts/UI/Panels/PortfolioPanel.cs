using UnityEngine;
using UnityEngine.UI;

namespace FortuneValley.UI.Panels
{
    /// <summary>
    /// UIPanel shell for the InvestingSystemPanel.
    /// Handles Show/Hide/Toggle and the close button.
    /// Content is managed by sub-panel scripts (InvestingHomeSubPanel, etc.).
    /// </summary>
    public class PortfolioPanel : UIPanel
    {
        [Header("Controls")]
        [SerializeField] private Button _closeButton;

        private void Start()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCloseButtonClicked);
        }
    }
}
