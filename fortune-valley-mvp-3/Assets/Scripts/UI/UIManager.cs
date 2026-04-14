using System.Collections.Generic;
using UnityEngine;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;
using FortuneValley.UI.Popups;
using FortuneValley.UI.Panels;

namespace FortuneValley.UI
{
    /// <summary>
    /// Manages all UI panels and popups in the game.
    /// Provides centralized control for showing/hiding UI elements.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // REFERENCES
        // ═══════════════════════════════════════════════════════════════

        [Header("Panel References")]
        [Tooltip("Portfolio panel showing investment holdings")]
        [SerializeField] private UIPanel _portfolioPanel;

        [Tooltip("Lots panel showing city lots")]
        [SerializeField] private UIPanel _lotsPanel;

        [Tooltip("Restaurant panel for upgrades")]
        [SerializeField] private UIPanel _restaurantPanel;

        [Tooltip("Insurance management panel (lot-first view)")]
        [SerializeField] private UIPanel _insurancePanel;

        [Tooltip("Loan management panel (read-only)")]
        [SerializeField] private LoanPanel _loanPanel;

        [Header("Popup References")]
        [Tooltip("Lot purchase confirmation popup")]
        [SerializeField] private UIPopup _lotPurchasePopup;

        [Tooltip("Buy investment popup")]
        [SerializeField] private UIPopup _buyInvestmentPopup;

        [Tooltip("Sell investment popup")]
        [SerializeField] private UIPopup _sellInvestmentPopup;

        [Tooltip("Transfer between accounts popup")]
        [SerializeField] private UIPopup _transferPopup;

        [Tooltip("Monthly credit card statement popup")]
        [SerializeField] private UIPopup _creditCardStatementPopup;

        [Tooltip("Accident resolution report popup")]
        [SerializeField] private UIPopup _accidentReportPopup;

        [Tooltip("Per-lot insurance policy selection popup")]
        [SerializeField] private UIPopup _insuranceSelectionPopup;

        [Tooltip("Loan product selection popup")]
        [SerializeField] private UIPopup _loanSelectionPopup;

        [Tooltip("Insurance policy detail popup")]
        [SerializeField] private UIPopup _insuranceDetailPopup;

        [Tooltip("Lot selection popup for insurance purchase")]
        [SerializeField] private UIPopup _insuranceLotSelectionPopup;

        [Tooltip("Lot info popup opened by world-space LotWorldCanvas click")]
        [SerializeField] private LotInfoPopup _lotInfoPopup;

        [Tooltip("QuestionMaster popup opened by the HUD button")]
        [SerializeField] private UIPopup _questionsPopup;

        [Header("Overlay")]
        [Tooltip("Dark overlay behind popups")]
        [SerializeField] private GameObject _popupOverlay;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private UIPanel _currentPanel;
        private Stack<UIPopup> _popupStack = new Stack<UIPopup>();

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Awake()
        {
            // Subscribe to close events from each panel and popup.
            // When a panel fires OnCloseRequested, UIManager hides it.
            if (_portfolioPanel != null) _portfolioPanel.OnCloseRequested += HandlePanelCloseRequested;
            if (_lotsPanel != null) _lotsPanel.OnCloseRequested += HandlePanelCloseRequested;
            if (_restaurantPanel != null) _restaurantPanel.OnCloseRequested += HandlePanelCloseRequested;
            if (_lotPurchasePopup != null) _lotPurchasePopup.OnCloseRequested += HandlePopupCloseRequested;
            if (_buyInvestmentPopup != null) _buyInvestmentPopup.OnCloseRequested += HandlePopupCloseRequested;
            if (_sellInvestmentPopup != null) _sellInvestmentPopup.OnCloseRequested += HandlePopupCloseRequested;
            if (_transferPopup != null) _transferPopup.OnCloseRequested += HandlePopupCloseRequested;
            if (_creditCardStatementPopup != null) _creditCardStatementPopup.OnCloseRequested += HandlePopupCloseRequested;
            if (_accidentReportPopup != null) _accidentReportPopup.OnCloseRequested += HandlePopupCloseRequested;
            if (_insuranceSelectionPopup != null) _insuranceSelectionPopup.OnCloseRequested += HandlePopupCloseRequested;
            if (_loanSelectionPopup != null) _loanSelectionPopup.OnCloseRequested += HandlePopupCloseRequested;
            if (_insuranceDetailPopup != null) _insuranceDetailPopup.OnCloseRequested += HandlePopupCloseRequested;
            if (_insuranceLotSelectionPopup != null) _insuranceLotSelectionPopup.OnCloseRequested += HandlePopupCloseRequested;
            if (_insurancePanel != null) _insurancePanel.OnCloseRequested += HandlePanelCloseRequested;
            if (_loanPanel != null) _loanPanel.OnCloseRequested += HandlePanelCloseRequested;
            if (_lotInfoPopup != null) _lotInfoPopup.OnCloseRequested += HandlePopupCloseRequested;
            if (_questionsPopup != null) _questionsPopup.OnCloseRequested += HandlePopupCloseRequested;

            GameEvents.OnLotInfoRequested += HandleLotInfoRequested;
            GameEvents.OnLotInsuranceRequested += HandleLotInsuranceRequested;
            GameEvents.OnLotLoanExploreRequested += HandleLotLoanExploreRequested;

            HideAllPanels();
            HideAllPopups();
        }

        private void OnDestroy()
        {
            if (_portfolioPanel != null) _portfolioPanel.OnCloseRequested -= HandlePanelCloseRequested;
            if (_lotsPanel != null) _lotsPanel.OnCloseRequested -= HandlePanelCloseRequested;
            if (_restaurantPanel != null) _restaurantPanel.OnCloseRequested -= HandlePanelCloseRequested;
            if (_lotPurchasePopup != null) _lotPurchasePopup.OnCloseRequested -= HandlePopupCloseRequested;
            if (_buyInvestmentPopup != null) _buyInvestmentPopup.OnCloseRequested -= HandlePopupCloseRequested;
            if (_sellInvestmentPopup != null) _sellInvestmentPopup.OnCloseRequested -= HandlePopupCloseRequested;
            if (_transferPopup != null) _transferPopup.OnCloseRequested -= HandlePopupCloseRequested;
            if (_creditCardStatementPopup != null) _creditCardStatementPopup.OnCloseRequested -= HandlePopupCloseRequested;
            if (_accidentReportPopup != null) _accidentReportPopup.OnCloseRequested -= HandlePopupCloseRequested;
            if (_insuranceSelectionPopup != null) _insuranceSelectionPopup.OnCloseRequested -= HandlePopupCloseRequested;
            if (_loanSelectionPopup != null) _loanSelectionPopup.OnCloseRequested -= HandlePopupCloseRequested;
            if (_insuranceDetailPopup != null) _insuranceDetailPopup.OnCloseRequested -= HandlePopupCloseRequested;
            if (_insuranceLotSelectionPopup != null) _insuranceLotSelectionPopup.OnCloseRequested -= HandlePopupCloseRequested;
            if (_insurancePanel != null) _insurancePanel.OnCloseRequested -= HandlePanelCloseRequested;
            if (_loanPanel != null) _loanPanel.OnCloseRequested -= HandlePanelCloseRequested;
            if (_lotInfoPopup != null) _lotInfoPopup.OnCloseRequested -= HandlePopupCloseRequested;
            if (_questionsPopup != null) _questionsPopup.OnCloseRequested -= HandlePopupCloseRequested;

            GameEvents.OnLotInfoRequested -= HandleLotInfoRequested;
            GameEvents.OnLotInsuranceRequested -= HandleLotInsuranceRequested;
            GameEvents.OnLotLoanExploreRequested -= HandleLotLoanExploreRequested;
        }

        private void HandleLotInfoRequested(string lotId)
        {
            if (_lotInfoPopup == null) return;
            _lotInfoPopup.ConfigureForLotId(lotId);
            ShowPopup(_lotInfoPopup);
        }

        private void HandleLotInsuranceRequested(string lotId)
        {
            // Pre-filter hook: open the panel. Sub-panels can subscribe to OnLotInsuranceRequested
            // separately to apply a lot-specific filter. HUD-button entry stays unchanged.
            ShowPanel(PanelType.Insurance);
        }

        private void HandleLotLoanExploreRequested(string lotId)
        {
            if (_loanPanel == null) return;
            // Stage pre-selection BEFORE the panel activates so the sidebar's OnEnable
            // picks up the Explore-tab override instead of resetting to Home.
            _loanPanel.PrepareExploreForLot(lotId);
            ShowPanel(PanelType.Loan);
            // Re-entry safety: if the panel was already visible, the activation hook
            // didn't fire. Force the tab and pre-selection now.
            _loanPanel.OpenExploreForLot(lotId);
        }

        // ═══════════════════════════════════════════════════════════════
        // PANEL MANAGEMENT
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Show a specific panel by type.
        /// </summary>
        public void ShowPanel(PanelType panelType)
        {
            // Hide current panel first
            if (_currentPanel != null)
            {
                _currentPanel.Hide();
            }

            UIPanel panel = GetPanel(panelType);
            if (panel != null)
            {
                panel.Show();
                _currentPanel = panel;
            }
        }

        /// <summary>
        /// Hide the currently open panel.
        /// </summary>
        public void HideCurrentPanel()
        {
            if (_currentPanel != null)
            {
                _currentPanel.Hide();
                _currentPanel = null;
            }
        }

        /// <summary>
        /// Toggle a panel (show if hidden, hide if shown).
        /// </summary>
        public void TogglePanel(PanelType panelType)
        {
            UIPanel panel = GetPanel(panelType);
            if (panel == null) return;

            if (_currentPanel == panel)
            {
                HideCurrentPanel();
            }
            else
            {
                ShowPanel(panelType);
            }
        }

        /// <summary>
        /// Hide all panels.
        /// </summary>
        public void HideAllPanels()
        {
            if (_portfolioPanel != null) _portfolioPanel.Hide();
            if (_lotsPanel != null) _lotsPanel.Hide();
            if (_restaurantPanel != null) _restaurantPanel.Hide();
            if (_insurancePanel != null) _insurancePanel.Hide();
            if (_loanPanel != null) _loanPanel.Hide();
            _currentPanel = null;
        }

        private UIPanel GetPanel(PanelType type)
        {
            return type switch
            {
                PanelType.Portfolio => _portfolioPanel,
                PanelType.Lots => _lotsPanel,
                PanelType.Restaurant => _restaurantPanel,
                PanelType.Insurance => _insurancePanel,
                PanelType.Loan => _loanPanel,
                _ => null
            };
        }

        // ═══════════════════════════════════════════════════════════════
        // POPUP MANAGEMENT
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Show a popup by type.
        /// </summary>
        public void ShowPopup(PopupType popupType)
        {
            UIPopup popup = GetPopup(popupType);
            if (popup != null)
            {
                ShowPopup(popup);
            }
        }

        /// <summary>
        /// Show a specific popup instance.
        /// </summary>
        public void ShowPopup(UIPopup popup)
        {
            if (popup == null) return;

            // Show overlay if this is the first popup
            if (_popupStack.Count == 0 && _popupOverlay != null)
            {
                _popupOverlay.SetActive(true);
            }

            _popupStack.Push(popup);
            popup.Show();
        }

        /// <summary>
        /// Hide the topmost popup.
        /// </summary>
        public void HideTopPopup()
        {
            if (_popupStack.Count > 0)
            {
                UIPopup popup = _popupStack.Pop();
                popup.Hide();

                // Hide overlay if no more popups
                if (_popupStack.Count == 0 && _popupOverlay != null)
                {
                    _popupOverlay.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Hide a specific popup.
        /// </summary>
        public void HidePopup(UIPopup popup)
        {
            if (popup == null) return;

            popup.Hide();

            // Rebuild stack without this popup
            var tempStack = new Stack<UIPopup>();
            while (_popupStack.Count > 0)
            {
                var p = _popupStack.Pop();
                if (p != popup)
                {
                    tempStack.Push(p);
                }
            }

            while (tempStack.Count > 0)
            {
                _popupStack.Push(tempStack.Pop());
            }

            // Hide overlay if no more popups
            if (_popupStack.Count == 0 && _popupOverlay != null)
            {
                _popupOverlay.SetActive(false);
            }
        }

        /// <summary>
        /// Hide all popups.
        /// </summary>
        public void HideAllPopups()
        {
            while (_popupStack.Count > 0)
            {
                _popupStack.Pop().Hide();
            }

            // Also hide any popups that might not be in stack
            if (_lotPurchasePopup != null) _lotPurchasePopup.Hide();
            if (_buyInvestmentPopup != null) _buyInvestmentPopup.Hide();
            if (_sellInvestmentPopup != null) _sellInvestmentPopup.Hide();
            if (_transferPopup != null) _transferPopup.Hide();
            if (_creditCardStatementPopup != null) _creditCardStatementPopup.Hide();
            if (_accidentReportPopup != null) _accidentReportPopup.Hide();
            if (_insuranceSelectionPopup != null) _insuranceSelectionPopup.Hide();
            if (_loanSelectionPopup != null) _loanSelectionPopup.Hide();
            if (_insuranceDetailPopup != null) _insuranceDetailPopup.Hide();
            if (_insuranceLotSelectionPopup != null) _insuranceLotSelectionPopup.Hide();
            if (_lotInfoPopup != null) _lotInfoPopup.Hide();
            if (_questionsPopup != null) _questionsPopup.Hide();

            if (_popupOverlay != null)
            {
                _popupOverlay.SetActive(false);
            }
        }

        private UIPopup GetPopup(PopupType type)
        {
            return type switch
            {
                PopupType.LotPurchase => _lotPurchasePopup,
                PopupType.BuyInvestment => _buyInvestmentPopup,
                PopupType.SellInvestment => _sellInvestmentPopup,
                PopupType.Transfer => _transferPopup,
                PopupType.CreditCardStatement => _creditCardStatementPopup,
                PopupType.AccidentReport => _accidentReportPopup,
                PopupType.InsuranceSelection => _insuranceSelectionPopup,
                PopupType.LoanSelection => _loanSelectionPopup,
                PopupType.InsuranceDetail => _insuranceDetailPopup,
                PopupType.LotSelection => _insuranceLotSelectionPopup,
                PopupType.LotInfo => _lotInfoPopup,
                PopupType.Questions => _questionsPopup,
                _ => null
            };
        }

        private void HandlePanelCloseRequested(UIPanel panel)
        {
            HideCurrentPanel();
        }

        private void HandlePopupCloseRequested(UIPopup popup)
        {
            HidePopup(popup);
        }

        // ═══════════════════════════════════════════════════════════════
        // CONVENIENCE ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Check if any popup is currently open.
        /// </summary>
        public bool IsPopupOpen => _popupStack.Count > 0;

        /// <summary>
        /// Check if any panel is currently open.
        /// </summary>
        public bool IsPanelOpen => _currentPanel != null;

        /// <summary>
        /// Get the lot purchase popup for configuration.
        /// </summary>
        public UIPopup LotPurchasePopup => _lotPurchasePopup;

        /// <summary>
        /// Get the buy investment popup for configuration.
        /// </summary>
        public UIPopup BuyInvestmentPopup => _buyInvestmentPopup;

        /// <summary>
        /// Get the sell investment popup for configuration.
        /// </summary>
        public UIPopup SellInvestmentPopup => _sellInvestmentPopup;

        /// <summary>
        /// Get the transfer popup for configuration.
        /// </summary>
        public UIPopup TransferPopup => _transferPopup;

        /// <summary>
        /// Get the insurance selection popup for configuration by InsurancePanel.
        /// </summary>
        public UIPopup InsuranceSelectionPopup => _insuranceSelectionPopup;

        /// <summary>
        /// Get the loan selection popup for configuration by LotPurchasePopup.
        /// </summary>
        public UIPopup LoanSelectionPopup => _loanSelectionPopup;

        /// <summary>
        /// Insurance detail popup for policy/transaction information.
        /// </summary>
        public UIPopup InsuranceDetailPopup => _insuranceDetailPopup;

        /// <summary>
        /// Lot selection popup for insurance purchasing.
        /// </summary>
        public UIPopup InsuranceLotSelectionPopup => _insuranceLotSelectionPopup;
    }
}
