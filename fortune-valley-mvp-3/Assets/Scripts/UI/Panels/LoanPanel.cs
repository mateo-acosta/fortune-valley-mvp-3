using UnityEngine;
using UnityEngine.UI;

namespace FortuneValley.UI.Panels
{
    /// <summary>
    /// UIPanel shell for the CreditSystemPanel.
    /// Handles Show/Hide/Toggle and the close button.
    /// Content is managed by sub-panel scripts (CreditHomeSubPanel, etc.).
    /// </summary>
    public class LoanPanel : UIPanel
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
