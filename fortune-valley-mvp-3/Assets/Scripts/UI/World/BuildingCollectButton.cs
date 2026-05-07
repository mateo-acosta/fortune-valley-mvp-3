using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;
using FortuneValley.Domain;
using FortuneValley.Domain.Enums;

namespace FortuneValley.UI.World
{
    /// <summary>
    /// World-space coin button that sits above an income-producing building.
    /// Non-interactive indicator under the automatic end-of-day deposit model:
    /// hidden by default, briefly fades in for a punch + color flash when its
    /// building's income lands at day-end, then fades back out. Hover reveals
    /// a static "+$X/year" rate readout for any owned building.
    /// </summary>
    public class BuildingCollectButton : MonoBehaviour
    {
        [Header("Binding")]
        [SerializeField] private string _buildingId;
        [SerializeField] private CityManager _cityManager;
        [SerializeField] private TimeManager _timeManager;

        [Header("Visual")]
        [SerializeField] private Image _fillImage;
        [SerializeField] private Image _coinTintImage;
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _amountLabel;
        [SerializeField] private string _flashAmountFormat = "+${0:N0}";

        [Header("Preview")]
        [Tooltip("Tier used when computing the potential rate shown on unowned lots.")]
        [SerializeField] private int _previewTier = 1;

        [Header("Flash")]
        [SerializeField] private float _flashScale = 1.2f;
        [SerializeField] private float _flashDuration = 0.35f;
        [SerializeField] private float _fadeInDuration = 0.1f;
        [SerializeField] private float _holdDuration = 0.3f;
        [SerializeField] private float _fadeOutDuration = 0.4f;
        [SerializeField] private Color _flashColor = new Color(1f, 0.95f, 0.3f, 1f);

        [Header("Visibility")]
        [Tooltip("CanvasGroup controlling alpha. Hidden by default; revealed on hover or briefly during the day-end flash.")]
        [SerializeField] private CanvasGroup _visibilityGroup;

        private Vector3 _baseScale;
        private float _lastKnownDailyRate;
        private bool _isHovered;
        private bool _isFlashing;
        private bool _subscribed;
        private CoinFlashSequencer _flashSequencer;

        public string BuildingId => _buildingId;

        public void SetBuildingId(string buildingId)
        {
            _buildingId = buildingId;
            if (_subscribed) RequestStateSeed();
        }

        private void Awake()
        {
            _baseScale = transform.localScale;
            if (_button != null)
            {
                _button.interactable = false;
            }
            if (_fillImage != null && _fillImage.gameObject.activeSelf)
            {
                _fillImage.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnCoinStateChanged += HandleCoinStateChanged;
            GameEvents.OnLotOwnershipChanged += HandleLotOwnershipChanged;
            GameEvents.OnBlockHoverChanged += HandleBlockHoverChanged;
            GameEvents.OnIncomeCollected += HandleIncomeCollected;
            _subscribed = true;

            if (_visibilityGroup != null)
            {
                _visibilityGroup.alpha = 0f;
                _visibilityGroup.blocksRaycasts = false;
                _visibilityGroup.interactable = false;
            }

            _flashSequencer = new CoinFlashSequencer(
                transform,
                _baseScale,
                _visibilityGroup,
                _coinTintImage,
                _flashColor,
                _flashScale,
                _flashDuration,
                _fadeInDuration,
                _holdDuration,
                _fadeOutDuration);

            ApplyVisibility();
            RequestStateSeed();
        }

        private void OnDisable()
        {
            GameEvents.OnCoinStateChanged -= HandleCoinStateChanged;
            GameEvents.OnLotOwnershipChanged -= HandleLotOwnershipChanged;
            GameEvents.OnBlockHoverChanged -= HandleBlockHoverChanged;
            GameEvents.OnIncomeCollected -= HandleIncomeCollected;
            _subscribed = false;
            _isHovered = false;
            _isFlashing = false;

            if (_flashSequencer != null) _flashSequencer.Kill();
            transform.DOKill();
            if (_visibilityGroup != null) _visibilityGroup.DOKill();
            if (_coinTintImage != null) _coinTintImage.DOKill();
            transform.localScale = _baseScale;
        }

        private void HandleCoinStateChanged(string id, float dailyPayout, float progress01, bool isReady)
        {
            if (id != _buildingId) return;

            _lastKnownDailyRate = dailyPayout;
            if (_isFlashing) return;

            if (_amountLabel != null)
            {
                // Unit suffix is hardcoded so a stale prefab-serialized format
                // string cannot reintroduce the dropped "/day" wording.
                _amountLabel.text = $"+${Mathf.FloorToInt(dailyPayout * LifespanConstants.TicksPerYear):N0}/year";
            }
        }

        private void HandleIncomeCollected(string buildingId, float amount)
        {
            if (buildingId != _buildingId) return;
            if (!isActiveAndEnabled) return;

            if (_amountLabel != null)
            {
                _amountLabel.text = CoinLabelFormatter.FormatDeposit(amount, _flashAmountFormat);
            }

            _isFlashing = true;
            _flashSequencer.Play(
                stayVisibleAfter: () => _isHovered,
                onComplete: () => _isFlashing = false);
        }

        private void HandleBlockHoverChanged(string lotId, bool hovered)
        {
            if (lotId != _buildingId) return;
            _isHovered = hovered;
            if (hovered && !_isFlashing && _amountLabel != null)
            {
                _amountLabel.text = $"+${Mathf.FloorToInt(_lastKnownDailyRate * LifespanConstants.TicksPerYear):N0}/year";
            }
            ApplyVisibility();
        }

        /// <summary>
        /// Player-owned/restaurant: visible while hovered. Unowned/rival: visible
        /// while hovered (teaser). Flash sequence overrides alpha temporarily via
        /// its own tween; on flash end the sequencer respects current hover state.
        /// </summary>
        private void ApplyVisibility()
        {
            if (_visibilityGroup == null) return;
            if (_isFlashing) return;

            _visibilityGroup.alpha = _isHovered ? 1f : 0f;
            _visibilityGroup.blocksRaycasts = false;
            _visibilityGroup.interactable = false;
        }

        private void HandleLotOwnershipChanged(string lotId, Owner previousOwner, Owner newOwner)
        {
            if (_buildingId == DailyIncomeAccumulator.RestaurantBuildingId) return;
            if (lotId != _buildingId) return;

            if (newOwner != Owner.Player)
            {
                ShowPotentialRate();
            }
            else
            {
                RequestStateSeed();
            }
            ApplyVisibility();
        }

        private void RequestStateSeed()
        {
            if (string.IsNullOrEmpty(_buildingId)) return;

            // Player-owned: ask the accumulator to re-emit the coin state.
            // Unowned or rival: fall back to the potential-rate teaser.
            if (_cityManager != null
                && _buildingId != DailyIncomeAccumulator.RestaurantBuildingId
                && _cityManager.GetOwner(_buildingId) != Owner.Player)
            {
                ShowPotentialRate();
                return;
            }

            GameEvents.RaiseIncomePendingQuery(_buildingId);
        }

        /// <summary>
        /// Unowned/rival lots show the yearly rate the player would unlock by
        /// buying. No state in the accumulator to query. The internal
        /// _lastKnownDailyRate stays in daily units for consistency with
        /// OnCoinStateChanged; the label scales to per-year at format time.
        /// </summary>
        private void ShowPotentialRate()
        {
            if (_amountLabel == null || _cityManager == null || _timeManager == null) return;
            var lot = _cityManager.GetLot(_buildingId);
            if (lot == null) return;

            int dailyPotential = Mathf.FloorToInt(lot.GetIncomeAtTier(_previewTier) * _timeManager.EnginePulsesPerTick);
            _lastKnownDailyRate = dailyPotential;
            _amountLabel.text = $"+${dailyPotential * LifespanConstants.TicksPerYear:N0}/year";
        }
    }
}
