using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;

namespace FortuneValley.UI.World
{
    /// <summary>
    /// World-space coin button that sits above an income-producing building.
    /// Displays the locked DailyPayout amount (constant for the day), a radial
    /// drain overlay that shrinks with each tick, pulses when ready, and
    /// deposits the locked amount on tap via OnIncomeCollectRequested.
    ///
    /// Subscription-race safety: on OnEnable the button raises
    /// OnIncomePendingQuery so PendingIncomeService re-emits the current
    /// coin state for this building. That seeds the visual even when this
    /// component enables after the last OnCoinStateChanged fired.
    /// </summary>
    public class BuildingCollectButton : MonoBehaviour
    {
        [Header("Binding")]
        [SerializeField] private string _buildingId;
        [SerializeField] private CityManager _cityManager;
        [SerializeField] private TimeManager _timeManager;

        [Header("Visual")]
        [SerializeField] private Image _fillImage;
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _amountLabel;
        [SerializeField] private string _amountFormat = "+${0:N0}/day";

        [Header("Preview")]
        [Tooltip("Tier used when computing the potential rate shown on unowned lots.")]
        [SerializeField] private int _previewTier = 1;

        [Header("Pulse")]
        [SerializeField] private float _pulseScale = 1.1f;
        [SerializeField] private float _pulseDuration = 0.4f;

        [Header("Persistent Visibility")]
        [Tooltip("Optional. When set, the coin shows whenever the bucket is ready OR the player is hovering this lot, independent of the building's hover canvas. Leave null to inherit visibility from an ancestor (pre-split behavior).")]
        [SerializeField] private CanvasGroup _visibilityGroup;

        private Tween _pulseTween;
        private Vector3 _baseScale;
        private int _lastDisplayedAmount = int.MinValue;
        private bool _lastReady;
        private bool _subscribed;
        private bool _isHovered;

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
                _button.onClick.AddListener(HandleClicked);
                _button.interactable = false;
            }
        }

        private void OnEnable()
        {
            GameEvents.OnCoinStateChanged += HandleCoinStateChanged;
            GameEvents.OnLotOwnershipChanged += HandleLotOwnershipChanged;
            GameEvents.OnBlockHoverChanged += HandleBlockHoverChanged;
            _subscribed = true;
            ApplyVisibility();
            RequestStateSeed();
        }

        private void OnDisable()
        {
            GameEvents.OnCoinStateChanged -= HandleCoinStateChanged;
            GameEvents.OnLotOwnershipChanged -= HandleLotOwnershipChanged;
            GameEvents.OnBlockHoverChanged -= HandleBlockHoverChanged;
            _subscribed = false;
            _isHovered = false;

            transform.DOKill();
            transform.localScale = _baseScale;
            _pulseTween = null;
            _lastReady = false;
        }

        private void HandleClicked()
        {
            if (string.IsNullOrEmpty(_buildingId)) return;
            if (_button != null) _button.interactable = false;
            GameEvents.RaiseIncomeCollectRequested(_buildingId, CollectReason.PlayerTap);
        }

        private void HandleCoinStateChanged(string id, float dailyPayout, float progress01, bool isReady)
        {
            if (id != _buildingId) return;

            if (_fillImage != null)
            {
                _fillImage.fillAmount = Mathf.Clamp01(progress01);
            }

            if (_amountLabel != null)
            {
                int rounded = Mathf.FloorToInt(dailyPayout);
                if (rounded != _lastDisplayedAmount)
                {
                    _amountLabel.text = string.Format(_amountFormat, rounded);
                    _lastDisplayedAmount = rounded;
                }
            }

            if (isReady != _lastReady)
            {
                if (isReady) StartPulse();
                else StopPulse();
                if (_button != null) _button.interactable = isReady;
                _lastReady = isReady;
                ApplyVisibility();
            }
        }

        private void HandleBlockHoverChanged(string lotId, bool hovered)
        {
            if (lotId != _buildingId) return;
            _isHovered = hovered;
            ApplyVisibility();
        }

        /// <summary>
        /// Drives _visibilityGroup alpha from (isReady || isHovered) for
        /// player-owned lots, (isHovered) for rival/unowned teaser. No-op
        /// when _visibilityGroup isn't wired (pre-split prefabs inherit
        /// visibility from the hover-gated ancestor canvas).
        /// </summary>
        private void ApplyVisibility()
        {
            if (_visibilityGroup == null) return;

            bool ownedOrRestaurant = _buildingId == PendingIncomeService.RestaurantBuildingId
                || (_cityManager != null && _cityManager.GetOwner(_buildingId) == Owner.Player);

            bool visible = ownedOrRestaurant ? (_lastReady || _isHovered) : _isHovered;
            _visibilityGroup.alpha = visible ? 1f : 0f;
            _visibilityGroup.blocksRaycasts = visible && _lastReady;
            _visibilityGroup.interactable = visible && _lastReady;
        }

        private void HandleLotOwnershipChanged(string lotId, Owner previousOwner, Owner newOwner)
        {
            if (_buildingId == PendingIncomeService.RestaurantBuildingId) return;
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

            // Player-owned: ask the service to re-emit the coin state.
            // Unowned or rival: fall back to the potential-rate teaser.
            if (_cityManager != null
                && _buildingId != PendingIncomeService.RestaurantBuildingId
                && _cityManager.GetOwner(_buildingId) != Owner.Player)
            {
                ShowPotentialRate();
                return;
            }

            GameEvents.RaiseIncomePendingQuery(_buildingId);
        }

        /// <summary>
        /// Unowned/rival lots show a fully-shaded coin + the daily rate the
        /// player would unlock by buying. No state in the service to query.
        /// </summary>
        private void ShowPotentialRate()
        {
            if (_fillImage != null) _fillImage.fillAmount = 1f;
            if (_button != null) _button.interactable = false;
            StopPulse();
            _lastReady = false;

            if (_amountLabel == null || _cityManager == null || _timeManager == null) return;
            var lot = _cityManager.GetLot(_buildingId);
            if (lot == null) return;

            int potential = Mathf.FloorToInt(lot.GetIncomeAtTier(_previewTier) * _timeManager.TicksPerDay);
            if (potential == _lastDisplayedAmount) return;

            _amountLabel.text = string.Format(_amountFormat, potential);
            _lastDisplayedAmount = potential;
        }

        private void StartPulse()
        {
            StopPulse();
            _pulseTween = transform.DOScale(_baseScale * _pulseScale, _pulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        private void StopPulse()
        {
            if (_pulseTween != null)
            {
                _pulseTween.Kill();
                _pulseTween = null;
            }
            transform.localScale = _baseScale;
        }
    }
}
