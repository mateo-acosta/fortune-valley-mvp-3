using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;

namespace FortuneValley.UI.Popups
{
    /// <summary>
    /// Simple lot picker popup for insurance purchasing.
    /// Shows eligible lots (those without this policy type already).
    /// Player selects a lot and confirms to purchase.
    ///
    /// LEARNING DESIGN: Requiring a deliberate lot selection makes students
    /// think about which properties need coverage most.
    /// </summary>
    public class LotSelectionPopup : UIPopup
    {
        // ===============================================================
        // REFERENCES
        // ===============================================================

        [Header("UI Elements")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _selectedLotText;
        [SerializeField] private Transform _lotListContainer;
        [SerializeField] private GameObject _lotItemPrefab;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        [Header("Visual State")]
        [SerializeField] private Color _normalColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField] private Color _selectedColor = new Color(0.3f, 0.7f, 1f, 1f);

        // ===============================================================
        // STATE
        // ===============================================================

        private string _policyId;
        private string _selectedLotId;
        private List<GameObject> _lotItems = new List<GameObject>();
        private List<Button> _lotButtons = new List<Button>();

        // ===============================================================
        // CONFIGURE
        // ===============================================================

        /// <summary>
        /// Set up the popup with a policy and list of eligible lots.
        /// </summary>
        public void Configure(string policyId, string policyDisplayName, List<LotOption> eligibleLots)
        {
            _policyId = policyId;
            _selectedLotId = null;

            if (_titleText != null)
                _titleText.text = $"Select a lot for {policyDisplayName}";

            if (_selectedLotText != null)
                _selectedLotText.text = "Select a lot below";

            // Clear existing lot items
            ClearLotItems();

            // Create lot buttons
            if (_lotItemPrefab != null && _lotListContainer != null && eligibleLots != null)
            {
                for (int i = 0; i < eligibleLots.Count; i++)
                {
                    var lotOption = eligibleLots[i];
                    var item = Instantiate(_lotItemPrefab, _lotListContainer);
                    _lotItems.Add(item);

                    // Set lot name text
                    var text = item.GetComponentInChildren<TMP_Text>();
                    if (text != null) text.text = lotOption.LotName;

                    // Wire button
                    var button = item.GetComponent<Button>();
                    if (button != null)
                    {
                        _lotButtons.Add(button);
                        var capturedId = lotOption.LotId;
                        var capturedName = lotOption.LotName;
                        var capturedIndex = _lotButtons.Count - 1;
                        button.onClick.AddListener(() => SelectLot(capturedId, capturedName, capturedIndex));
                    }
                }
            }

            // Disable confirm until a lot is selected
            if (_confirmButton != null)
                _confirmButton.interactable = false;

            // Wire buttons
            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveAllListeners();
                _confirmButton.onClick.AddListener(HandleConfirm);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveAllListeners();
                _cancelButton.onClick.AddListener(HandleCancel);
            }
        }

        // ===============================================================
        // SELECTION
        // ===============================================================

        private void SelectLot(string lotId, string lotName, int buttonIndex)
        {
            _selectedLotId = lotId;

            if (_selectedLotText != null)
                _selectedLotText.text = lotName;

            if (_confirmButton != null)
                _confirmButton.interactable = true;

            // Update visual state on all buttons
            for (int i = 0; i < _lotButtons.Count; i++)
            {
                var graphic = _lotButtons[i].targetGraphic as Graphic;
                if (graphic != null)
                    graphic.color = (i == buttonIndex) ? _selectedColor : _normalColor;
            }
        }

        // ===============================================================
        // CONFIRM / CANCEL
        // ===============================================================

        private void HandleConfirm()
        {
            if (string.IsNullOrEmpty(_selectedLotId) || string.IsNullOrEmpty(_policyId))
                return;

            GameEvents.RaisePurchaseInsuranceRequested(_selectedLotId, _policyId);
            OnConfirmClicked(); // closes popup via UIPopup base
        }

        private void HandleCancel()
        {
            OnCancelClicked(); // closes popup via UIPopup base
        }

        // ===============================================================
        // CLEANUP
        // ===============================================================

        private void ClearLotItems()
        {
            for (int i = 0; i < _lotItems.Count; i++)
            {
                if (_lotItems[i] != null)
                    Destroy(_lotItems[i]);
            }
            _lotItems.Clear();
            _lotButtons.Clear();
        }

        protected override void OnHide()
        {
            ClearLotItems();
            _selectedLotId = null;
            base.OnHide();
        }
    }
}
