using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;

namespace FortuneValley.UI.World
{
    /// <summary>
    /// Attached to each WorldSpaceCanvas_Building hovering over a RestaurantVisual.
    /// On button click, raises LotInfoRequested so UIManager can open the screen-space popup.
    /// Also displays live lot info (name, tier, income) on its own canvas children.
    /// </summary>
    public class LotWorldCanvas : MonoBehaviour
    {
        [Header("Lot Binding")]
        [SerializeField] private CityLotDefinition _lot;

        [Header("Click")]
        [SerializeField] private Button _clickButton;

        [Header("Live Info Display")]
        [Tooltip("Displays the lot's DisplayName")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [Tooltip("Displays 'For Sale' when unowned, 'Tier N' when owned")]
        [SerializeField] private TextMeshProUGUI _levelText;
        [Tooltip("Displays income when player-owned; blank otherwise")]
        [SerializeField] private TextMeshProUGUI _incomeText;

        [Header("Copy")]
        [SerializeField] private string _forSaleLabel = "For Sale";
        [SerializeField] private string _tierFormat = "Tier {0}";
        [SerializeField] private string _incomeFormat = "+${0:N0}/day";
        [SerializeField] private string _rivalLabel = "Rival";

        private Owner _owner = Owner.None;
        private int _tier;

        private void Awake()
        {
            if (_clickButton != null) _clickButton.onClick.AddListener(HandleClicked);
        }

        private void OnEnable()
        {
            GameEvents.OnLotPurchased += HandleLotPurchased;
            GameEvents.OnLotTierChanged += HandleLotTierChanged;
            GameEvents.OnGameStart += HandleGameStart;
            RefreshDisplay();
        }

        private void OnDisable()
        {
            GameEvents.OnLotPurchased -= HandleLotPurchased;
            GameEvents.OnLotTierChanged -= HandleLotTierChanged;
            GameEvents.OnGameStart -= HandleGameStart;
        }

        private void OnDestroy()
        {
            if (_clickButton != null) _clickButton.onClick.RemoveListener(HandleClicked);
        }

        private void HandleClicked()
        {
            if (_lot == null) return;
            GameEvents.RaiseLotInfoRequested(_lot.LotId);
        }

        private void HandleLotPurchased(string lotId, Owner owner)
        {
            if (_lot == null || lotId != _lot.LotId) return;
            _owner = owner;
            RefreshDisplay();
        }

        private void HandleLotTierChanged(string lotId, int newTier)
        {
            if (_lot == null || lotId != _lot.LotId) return;
            _tier = newTier;
            RefreshDisplay();
        }

        private void HandleGameStart()
        {
            _owner = Owner.None;
            _tier = 0;
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (_lot == null) return;

            if (_titleText != null) _titleText.text = _lot.DisplayName;

            if (_levelText != null)
            {
                if (_owner == Owner.None || _tier <= 0)
                {
                    _levelText.text = _forSaleLabel;
                }
                else if (_owner == Owner.Rival)
                {
                    _levelText.text = _rivalLabel + " " + string.Format(_tierFormat, _tier);
                }
                else
                {
                    _levelText.text = string.Format(_tierFormat, _tier);
                }
            }

            if (_incomeText != null)
            {
                _incomeText.text = _owner == Owner.Player && _lot.IncomeBonus > 0f
                    ? string.Format(_incomeFormat, _lot.IncomeBonus)
                    : string.Empty;
            }
        }
    }
}
