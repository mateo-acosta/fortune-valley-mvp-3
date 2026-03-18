using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;

namespace FortuneValley.UI.Components
{
    /// <summary>
    /// A single row in the lots list showing lot info and status.
    /// </summary>
    public class LotListItem : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // REFERENCES
        // ═══════════════════════════════════════════════════════════════

        [Header("Text Elements")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private TextMeshProUGUI _incomeText;
        [SerializeField] private TextMeshProUGUI _statusText;

        [Header("Visual Elements")]
        [SerializeField] private Image _statusIcon;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Button _button;

        [Header("Colors")]
        [SerializeField] private Color _availableColor = new Color(0.2f, 0.7f, 0.2f);
        [SerializeField] private Color _playerOwnedColor = new Color(0.2f, 0.4f, 0.9f);
        [SerializeField] private Color _rivalOwnedColor = new Color(0.8f, 0.2f, 0.2f);

        [SerializeField] private Color _availableBackgroundColor = new Color(0.9f, 1f, 0.9f);
        [SerializeField] private Color _ownedBackgroundColor = new Color(0.9f, 0.9f, 1f);
        [SerializeField] private Color _rivalBackgroundColor = new Color(1f, 0.9f, 0.9f);

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private CityLotDefinition _lot;
        private Owner _owner;
        private Action<CityLotDefinition> _onClick;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Set up the list item with lot data.
        /// </summary>
        public void Setup(CityLotDefinition lot, Owner owner, Action<CityLotDefinition> onClick)
        {
            _lot = lot;
            _owner = owner;
            _onClick = onClick;

            UpdateDisplay();
            SetupButton();
        }

        /// <summary>
        /// Update ownership status.
        /// </summary>
        public void SetOwner(Owner owner)
        {
            _owner = owner;
            UpdateDisplay();
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════

        private void UpdateDisplay()
        {
            if (_lot == null) return;

            // Name
            if (_nameText != null)
            {
                _nameText.text = _lot.DisplayName;
            }

            // Price
            if (_priceText != null)
            {
                _priceText.text = $"${_lot.BaseCost:N0}";
            }

            // Income
            if (_incomeText != null)
            {
                if (_lot.IncomeBonus > 0)
                {
                    _incomeText.text = $"+${_lot.IncomeBonus:N0}/day";
                }
                else
                {
                    _incomeText.text = "";
                }
            }

            // Status
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            string statusText;
            Color statusColor;
            Color bgColor;

            switch (_owner)
            {
                case Owner.Player:
                    statusText = "Owned";
                    statusColor = _playerOwnedColor;
                    bgColor = _ownedBackgroundColor;
                    break;
                case Owner.Rival:
                    statusText = "Rival";
                    statusColor = _rivalOwnedColor;
                    bgColor = _rivalBackgroundColor;
                    break;
                default:
                    statusText = "Available";
                    statusColor = _availableColor;
                    bgColor = _availableBackgroundColor;
                    break;
            }

            if (_statusText != null)
            {
                _statusText.text = statusText;
                _statusText.color = statusColor;
            }

            if (_statusIcon != null)
            {
                _statusIcon.color = statusColor;
            }

            if (_backgroundImage != null)
            {
                _backgroundImage.color = bgColor;
            }
        }

        private void SetupButton()
        {
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(OnClick);
            }
        }

        private void OnClick()
        {
            _onClick?.Invoke(_lot);
        }

        // ═══════════════════════════════════════════════════════════════
        // ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public CityLotDefinition Lot => _lot;
        public Owner Owner => _owner;
    }
}
