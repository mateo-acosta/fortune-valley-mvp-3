using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;
using FortuneValley.Domain;
using FortuneValley.Domain.Enums;

namespace FortuneValley.UI.World
{
    /// <summary>
    /// Attached to each WorldSpaceCanvas_Building hovering over a RestaurantVisual.
    /// Displays lot title, tier/status, income, ownership color indicator,
    /// and a context-aware Buy/Manage button that raises LotInfoRequested.
    /// Visibility is hover-driven via OnBlockHoverChanged so the canvas only
    /// shows when the player's mouse is over the matching block. A
    /// self-managed CanvasGroup (auto-added if missing) makes this independent
    /// of any Block-level hover-fade wiring that may have drifted.
    /// </summary>
    public class LotWorldCanvas : MonoBehaviour
    {
        [Header("Lot Binding")]
        [SerializeField] private CityLotDefinition _lot;

        [Header("Tap-to-Collect")]
        [SerializeField] private BuildingCollectButton _collectButton;
        [SerializeField] private Transform _collectAnchor;
        [SerializeField] private IncomeCollectionController _collectionController;
        [SerializeField] private TimeManager _timeManager;

        [Header("Click")]
        [SerializeField] private Button _clickButton;
        [SerializeField] private TextMeshProUGUI _buttonLabel;

        [Header("Live Info Display")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _incomeText;
        [SerializeField] private Image _ownerIndicator;

        [Header("Copy")]
        [SerializeField] private string _forSaleLabel = "For Sale";
        [SerializeField] private string _tierFormat = "Tier {0}";
        [SerializeField] private string _rivalTierFormat = "Rival Tier {0}";
        [SerializeField] private string _buyButtonLabel = "Buy";
        [SerializeField] private string _manageButtonLabel = "Manage";

        [Header("Owner Colors")]
        [SerializeField] private Color _colorForSale = new Color(0.25f, 0.55f, 1f, 1f);
        [SerializeField] private Color _colorPlayer = new Color(0.25f, 0.85f, 0.35f, 1f);
        [SerializeField] private Color _colorRival = new Color(0.9f, 0.25f, 0.25f, 1f);

        [Header("For-Sale Preview Tier")]
        [SerializeField] private int _previewTierWhenForSale = 1;

        [Header("Hover Visibility")]
        [Tooltip("Optional. If unwired, a CanvasGroup is auto-added to this GameObject so the canvas can fade with hover.")]
        [SerializeField] private CanvasGroup _visibilityGroup;

        private Owner _owner = Owner.None;
        private int _tier;

        private void Awake()
        {
            if (_clickButton != null) _clickButton.onClick.AddListener(HandleClicked);

            if (_collectButton != null && _lot != null)
            {
                _collectButton.SetBuildingId(_lot.LotId);
            }

            if (_collectionController != null && _lot != null)
            {
                Transform anchor = _collectAnchor != null ? _collectAnchor : transform;
                _collectionController.RegisterAnchor(_lot.LotId, anchor);
            }

            if (_visibilityGroup == null) _visibilityGroup = GetComponent<CanvasGroup>();
            if (_visibilityGroup == null) _visibilityGroup = gameObject.AddComponent<CanvasGroup>();
            _visibilityGroup.alpha = 0f;
            _visibilityGroup.blocksRaycasts = false;
            _visibilityGroup.interactable = false;
        }

        private void OnEnable()
        {
            GameEvents.OnLotPurchased += HandleLotPurchased;
            GameEvents.OnLotTierChanged += HandleLotTierChanged;
            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnBlockHoverChanged += HandleBlockHoverChanged;
            RefreshDisplay();
        }

        private void OnDisable()
        {
            GameEvents.OnLotPurchased -= HandleLotPurchased;
            GameEvents.OnLotTierChanged -= HandleLotTierChanged;
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnBlockHoverChanged -= HandleBlockHoverChanged;
        }

        private void HandleBlockHoverChanged(string lotId, bool hovered)
        {
            if (_lot == null || lotId != _lot.LotId) return;
            if (_visibilityGroup == null) return;
            _visibilityGroup.alpha = hovered ? 1f : 0f;
            _visibilityGroup.blocksRaycasts = hovered;
            _visibilityGroup.interactable = hovered;
        }

        private void OnDestroy()
        {
            if (_clickButton != null) _clickButton.onClick.RemoveListener(HandleClicked);

            if (_collectionController != null && _lot != null)
            {
                _collectionController.UnregisterAnchor(_lot.LotId);
            }
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

            int displayTier = _owner == Owner.None ? _previewTierWhenForSale : _tier;

            if (_levelText != null)
            {
                if (_owner == Owner.None)
                {
                    _levelText.text = _forSaleLabel;
                }
                else if (_owner == Owner.Rival)
                {
                    _levelText.text = string.Format(_rivalTierFormat, _tier);
                }
                else
                {
                    _levelText.text = string.Format(_tierFormat, _tier);
                }
            }

            if (_incomeText != null)
            {
                float incomePerTick = _lot.GetIncomeAtTier(displayTier);
                int ticksPerDay = _timeManager != null ? _timeManager.EnginePulsesPerTick : 1;
                float incomePerYear = incomePerTick * ticksPerDay * LifespanConstants.TicksPerYear;
                // Unit suffix is hardcoded so a stale prefab-serialized format
                // string cannot reintroduce the dropped "/day" wording.
                _incomeText.text = $"+${incomePerYear:N0}/year";
            }

            if (_ownerIndicator != null)
            {
                if (_owner == Owner.Player) _ownerIndicator.color = _colorPlayer;
                else if (_owner == Owner.Rival) _ownerIndicator.color = _colorRival;
                else _ownerIndicator.color = _colorForSale;
            }

            if (_buttonLabel != null)
            {
                _buttonLabel.text = _owner == Owner.Player ? _manageButtonLabel : _buyButtonLabel;
            }
        }
    }
}
