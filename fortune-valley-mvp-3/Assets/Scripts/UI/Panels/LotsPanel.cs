using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;
using FortuneValley.UI.Components;
using FortuneValley.UI.Popups;

namespace FortuneValley.UI.Panels
{
    /// <summary>
    /// Panel showing all city lots in a list view.
    /// Allows filtering and sorting, and opens purchase popup on selection.
    /// </summary>
    public class LotsPanel : UIPanel
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        [Header("List Container")]
        [SerializeField] private Transform _listContainer;
        [SerializeField] private LotListItem _lotItemPrefab;

        [Header("Filter Controls")]
        [SerializeField] private Toggle _showAllToggle;
        [SerializeField] private Toggle _showAvailableToggle;
        [SerializeField] private Toggle _showOwnedToggle;

        [Header("Sort Controls")]
        [SerializeField] private TMP_Dropdown _sortDropdown;

        [Header("Summary")]
        [SerializeField] private TextMeshProUGUI _summaryText;

        [Header("References")]
        [SerializeField] private CityManager _cityManager;
        [SerializeField] private UIManager _uiManager;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private List<LotListItem> _listItems = new List<LotListItem>();
        private LotsFilterMode _currentFilter = LotsFilterMode.All;
        private LotsSortMode _currentSort = LotsSortMode.Price;
        private int _currentTick;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Start()
        {
            if (_cityManager == null) Debug.LogError("[LotsPanel] _cityManager not wired in Inspector.");
            if (_uiManager == null) Debug.LogError("[LotsPanel] _uiManager not wired in Inspector.");

            SetupControls();
        }

        private void OnEnable()
        {
            GameEvents.OnLotPurchased += HandleLotPurchased;
            GameEvents.OnTick += HandleTick;
        }

        private void OnDisable()
        {
            GameEvents.OnLotPurchased -= HandleLotPurchased;
            GameEvents.OnTick -= HandleTick;
        }

        private void HandleTick(int tick)
        {
            _currentTick = tick;
        }

        private void HandleLotPurchased(string lotId, Owner owner)
        {
            // Refresh list when ownership changes
            if (IsVisible)
            {
                RefreshList();
            }
        }

        private void SetupControls()
        {
            // Filter toggles
            if (_showAllToggle != null)
            {
                _showAllToggle.onValueChanged.AddListener(isOn => {
                    if (isOn) SetFilter(LotsFilterMode.All);
                });
            }

            if (_showAvailableToggle != null)
            {
                _showAvailableToggle.onValueChanged.AddListener(isOn => {
                    if (isOn) SetFilter(LotsFilterMode.Available);
                });
            }

            if (_showOwnedToggle != null)
            {
                _showOwnedToggle.onValueChanged.AddListener(isOn => {
                    if (isOn) SetFilter(LotsFilterMode.Owned);
                });
            }

            // Sort dropdown
            if (_sortDropdown != null)
            {
                _sortDropdown.ClearOptions();
                _sortDropdown.AddOptions(new List<string> { "Price", "Income", "Name" });
                _sortDropdown.onValueChanged.AddListener(index => {
                    SetSort((LotsSortMode)index);
                });
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PANEL OVERRIDES
        // ═══════════════════════════════════════════════════════════════

        protected override void OnShow()
        {
            RefreshList();
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Refresh the lot list.
        /// </summary>
        public void RefreshList()
        {
            if (_cityManager == null) return;

            // Clear existing items
            ClearList();

            // Get and filter lots
            var lots = GetFilteredLots();

            // Sort lots
            SortLots(lots);

            // Create list items
            foreach (var lot in lots)
            {
                CreateListItem(lot);
            }

            // Update summary
            UpdateSummary();
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════

        private void SetFilter(LotsFilterMode filter)
        {
            _currentFilter = filter;
            if (IsVisible) RefreshList();
        }

        private void SetSort(LotsSortMode sort)
        {
            _currentSort = sort;
            if (IsVisible) RefreshList();
        }

        private List<CityLotDefinition> GetFilteredLots()
        {
            var allLots = _cityManager.AllLots;
            var filtered = new List<CityLotDefinition>();

            foreach (var lot in allLots)
            {
                Owner owner = _cityManager.GetOwner(lot.LotId);

                switch (_currentFilter)
                {
                    case LotsFilterMode.All:
                        filtered.Add(lot);
                        break;
                    case LotsFilterMode.Available:
                        if (owner == Owner.None)
                            filtered.Add(lot);
                        break;
                    case LotsFilterMode.Owned:
                        if (owner == Owner.Player)
                            filtered.Add(lot);
                        break;
                }
            }

            return filtered;
        }

        private void SortLots(List<CityLotDefinition> lots)
        {
            switch (_currentSort)
            {
                case LotsSortMode.Price:
                    lots.Sort((a, b) => a.BaseCost.CompareTo(b.BaseCost));
                    break;
                case LotsSortMode.Income:
                    lots.Sort((a, b) => b.IncomeBonus.CompareTo(a.IncomeBonus));
                    break;
                case LotsSortMode.Name:
                    lots.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName));
                    break;
            }
        }

        private void CreateListItem(CityLotDefinition lot)
        {
            if (_lotItemPrefab == null || _listContainer == null) return;

            LotListItem item = Instantiate(_lotItemPrefab, _listContainer);
            Owner owner = _cityManager.GetOwner(lot.LotId);

            item.Setup(lot, owner, OnLotItemClicked);
            _listItems.Add(item);
        }

        private void ClearList()
        {
            foreach (var item in _listItems)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }
            _listItems.Clear();
        }

        private void UpdateSummary()
        {
            if (_summaryText == null || _cityManager == null) return;

            _summaryText.text = $"Lots: {_cityManager.PlayerLotCount} owned / " +
                               $"{_cityManager.AvailableLotCount} available / " +
                               $"{_cityManager.RivalLotCount} rival";
        }

        private void OnLotItemClicked(CityLotDefinition lot)
        {
            if (lot == null) return;

            Owner owner = _cityManager.GetOwner(lot.LotId);

            if (owner == Owner.None)
            {
                // Open purchase popup for available lots
                var popup = _uiManager.LotPurchasePopup as LotPurchasePopup;
                if (popup != null)
                {
                    if (popup.IsVisible)
                    {
                        popup.ConfigureForLot(lot, _currentTick);
                        return;
                    }

                    popup.ConfigureForLot(lot, _currentTick);
                    _uiManager.ShowPopup(popup);
                }
            }
            else
            {
                // Could show info panel for owned lots
                UnityEngine.Debug.Log($"[LotsPanel] {lot.DisplayName} is owned by {owner}");
            }
        }
    }
}
