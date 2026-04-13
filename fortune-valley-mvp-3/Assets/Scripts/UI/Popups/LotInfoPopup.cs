using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;
using FortuneValley.UI.Effects;

namespace FortuneValley.UI.Popups
{
    /// <summary>
    /// Single popup opened by the world-space LotWorldCanvas. Button set switches by ownership:
    /// None   -> Buy (BaseCost) + Finance
    /// Player -> Upgrade (hidden at T3) + Insurance
    /// Rival  -> Buy (BaseCost * RivalBuyoutMultiplier)
    /// </summary>
    public class LotInfoPopup : UIPopup
    {
        [Header("Layout Roots")]
        [Tooltip("Sub-panel shown when the player owns the lot (Upgrade / Insurance).")]
        [SerializeField] private GameObject _ownedLayoutRoot;
        [Tooltip("Sub-panel shown when the lot is unowned or rival-owned (Buy / Finance).")]
        [SerializeField] private GameObject _unownedLayoutRoot;

        [Header("Owned Panel Texts")]
        [Tooltip("Lot name text inside OwnedLotDetailPanel")]
        [SerializeField] private TextMeshProUGUI _lotNameText;
        [Tooltip("Lot description text inside OwnedLotDetailPanel")]
        [SerializeField] private TextMeshProUGUI _lotDescriptionText;
        [Tooltip("Income bonus text inside OwnedLotDetailPanel")]
        [SerializeField] private TextMeshProUGUI _incomeBonusText;
        [Tooltip("Checking balance text inside OwnedLotDetailPanel")]
        [SerializeField] private TextMeshProUGUI _balanceText;

        [Header("Unowned Panel Texts")]
        [Tooltip("Lot name text inside Non-OwnedLotDetailPanel")]
        [SerializeField] private TextMeshProUGUI _lotNameTextUnowned;
        [Tooltip("Lot description text inside Non-OwnedLotDetailPanel")]
        [SerializeField] private TextMeshProUGUI _lotDescriptionTextUnowned;
        [Tooltip("Income bonus text inside Non-OwnedLotDetailPanel")]
        [SerializeField] private TextMeshProUGUI _incomeBonusTextUnowned;
        [Tooltip("Checking balance text inside Non-OwnedLotDetailPanel")]
        [SerializeField] private TextMeshProUGUI _balanceTextUnowned;

        [Header("Owned-Only Stats")]
        [SerializeField] private TextMeshProUGUI _tierText;

        [Header("Unowned-Only Stats")]
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private TextMeshProUGUI _roiText;
        [SerializeField] private TextMeshProUGUI _affordabilityText;

        [Header("Buttons")]
        [SerializeField] private Button _buyButton;
        [SerializeField] private Button _financeButton;
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private Button _insuranceButton;
        [Tooltip("Close button inside OwnedLotDetailPanel")]
        [SerializeField] private Button _closeButton;
        [Tooltip("Close button inside Non-OwnedLotDetailPanel")]
        [SerializeField] private Button _closeButtonUnowned;
        [SerializeField] private TextMeshProUGUI _buyButtonText;
        [SerializeField] private TextMeshProUGUI _upgradeButtonText;

        [Header("Dependencies")]
        [SerializeField] private CityManager _cityManager;
        [SerializeField] private CurrencyManager _currencyManager;

        [Header("Effects")]
        [SerializeField] private ConfettiBurst _upgradeConfetti;

        [Header("Colors")]
        [SerializeField] private Color _canAffordColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _cannotAffordColor = new Color(0.8f, 0.2f, 0.2f);

        [Header("Copy")]
        [SerializeField] private string _buyLabel = "Buy";
        [SerializeField] private string _buyFromRivalLabel = "Buy from Rival";
        [SerializeField] private string _upgradeLabel = "Upgrade to T{0}";
        [SerializeField] private string _maxTierLabel = "Max Tier";
        [SerializeField] private string _tierDisplayFormat = "Tier {0}";
        [SerializeField] private string _notOwnedTierLabel = "Unowned";
        [SerializeField] private string _rivalOwnedTierLabel = "Owned by Rival";

        private CityLotDefinition _currentLot;
        private string _pendingLotId;
        private Owner _currentOwner;
        private int _currentTier;
        private float _currentResolvedCost;
        private bool _upgradePending; // Issue 8A: block re-click during confetti

        private void Awake()
        {
            if (_buyButton != null) _buyButton.onClick.AddListener(HandleBuyClicked);
            if (_financeButton != null) _financeButton.onClick.AddListener(HandleFinanceClicked);
            if (_upgradeButton != null) _upgradeButton.onClick.AddListener(HandleUpgradeClicked);
            if (_insuranceButton != null) _insuranceButton.onClick.AddListener(HandleInsuranceClicked);
            if (_closeButton != null) _closeButton.onClick.AddListener(HandleCloseClicked);
            if (_closeButtonUnowned != null) _closeButtonUnowned.onClick.AddListener(HandleCloseClicked);
        }

        /// <summary>
        /// Called by UIManager before ShowPopup to seed the lot context.
        /// String-only to keep UIManager free of Core-typed method-call arguments.
        /// </summary>
        public void ConfigureForLotId(string lotId)
        {
            _pendingLotId = lotId;
        }

        protected override void OnShow()
        {
            base.OnShow();
            GameEvents.OnCheckingBalanceChanged += HandleBalanceChanged;
            GameEvents.OnLotTierChanged += HandleLotTierChanged;
            GameEvents.OnLotPurchased += HandleLotPurchased;
            _upgradePending = false;
            ResolveLotFromPendingId();
            RefreshOwnershipAndTier();
            UpdateDisplay();
        }

        private void ResolveLotFromPendingId()
        {
            _currentLot = null;
            if (string.IsNullOrEmpty(_pendingLotId) || _cityManager == null) return;
            var all = _cityManager.AllLots;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && all[i].LotId == _pendingLotId)
                {
                    _currentLot = all[i];
                    return;
                }
            }
        }

        protected override void OnHide()
        {
            base.OnHide();
            GameEvents.OnCheckingBalanceChanged -= HandleBalanceChanged;
            GameEvents.OnLotTierChanged -= HandleLotTierChanged;
            GameEvents.OnLotPurchased -= HandleLotPurchased;
            _currentLot = null;
            _upgradePending = false;
        }

        private void HandleBalanceChanged(float balance, float delta)
        {
            if (_currentLot == null) return;
            UpdateAffordability();
        }

        private void HandleLotTierChanged(string lotId, int newTier)
        {
            if (_currentLot == null || lotId != _currentLot.LotId) return;
            _currentTier = newTier;
            _upgradePending = false;
            if (_upgradeConfetti != null) _upgradeConfetti.Play();
            UpdateDisplay();
        }

        private void HandleLotPurchased(string lotId, Owner owner)
        {
            if (_currentLot == null || lotId != _currentLot.LotId) return;
            _currentOwner = owner;
            UpdateDisplay();
        }

        private void RefreshOwnershipAndTier()
        {
            if (_currentLot == null || _cityManager == null)
            {
                _currentOwner = Owner.None;
                _currentTier = 0;
                return;
            }
            var ownership = _cityManager.LotOwnership;
            _currentOwner = ownership != null && ownership.TryGetValue(_currentLot.LotId, out var o) ? o : Owner.None;
            var tiers = _cityManager.LotTiers;
            _currentTier = tiers != null && tiers.TryGetValue(_currentLot.LotId, out var t) ? t : 0;
        }

        private void UpdateDisplay()
        {
            if (_currentLot == null)
            {
                Debug.LogWarning($"[LotInfoPopup] UpdateDisplay called with null _currentLot (pendingLotId='{_pendingLotId}')");
                return;
            }

            SetTextPair(_lotNameText, _lotNameTextUnowned, _currentLot.DisplayName);
            SetTextPair(_lotDescriptionText, _lotDescriptionTextUnowned, _currentLot.Description);

            string incomeCopy = _currentLot.IncomeBonus > 0f
                ? $"Income: +${_currentLot.IncomeBonus:N0}/day"
                : "No income bonus";
            SetTextPair(_incomeBonusText, _incomeBonusTextUnowned, incomeCopy);

            UpdateTierDisplay();
            UpdateCostDisplay();
            UpdateRoiDisplay();
            UpdateButtons();
            UpdateAffordability();
        }

        private static void SetTextPair(TextMeshProUGUI a, TextMeshProUGUI b, string value)
        {
            if (a != null) a.text = value;
            if (b != null) b.text = value;
        }

        private void UpdateTierDisplay()
        {
            if (_tierText == null) return;
            if (_currentOwner == Owner.Player) _tierText.text = string.Format(_tierDisplayFormat, _currentTier);
            else if (_currentOwner == Owner.Rival) _tierText.text = _rivalOwnedTierLabel;
            else _tierText.text = _notOwnedTierLabel;
        }

        private void UpdateCostDisplay()
        {
            // Resolve cost inline per Issue 3A semantics (cost authority stays in CityManager for the
            // actual purchase; UI computes the display value only).
            _currentResolvedCost = _currentOwner == Owner.Rival
                ? _currentLot.BaseCost * _currentLot.RivalBuyoutMultiplier
                : _currentLot.BaseCost;

            if (_costText != null)
            {
                if (_currentOwner == Owner.Player)
                {
                    int nextTier = _currentTier + 1;
                    if (_currentTier >= 3)
                    {
                        _costText.text = "";
                    }
                    else
                    {
                        float upCost = nextTier == 2 ? _currentLot.Tier2UpgradeCost : _currentLot.Tier3UpgradeCost;
                        _costText.text = $"Upgrade cost: ${upCost:N0}";
                    }
                }
                else
                {
                    _costText.text = $"Cost: ${_currentResolvedCost:N0}";
                }
            }
        }

        private void UpdateRoiDisplay()
        {
            if (_roiText == null) return;
            if (_currentOwner == Owner.Player)
            {
                _roiText.text = "";
                return;
            }
            if (_currentLot.IncomeBonus <= 0f)
            {
                _roiText.text = "";
                return;
            }
            int daysToPayback = Mathf.CeilToInt(_currentResolvedCost / _currentLot.IncomeBonus);
            _roiText.text = $"Payback: ~{daysToPayback} days";
        }

        private void UpdateButtons()
        {
            // Toggle which sub-layout is visible. Individual button fields live inside their layout,
            // so hiding the layout hides them.
            bool playerOwned = _currentOwner == Owner.Player;
            SetActive(_ownedLayoutRoot, playerOwned);
            SetActive(_unownedLayoutRoot, !playerOwned);

            SetActive(_buyButton, _currentOwner != Owner.Player);
            SetActive(_financeButton, _currentOwner == Owner.None);
            bool canUpgrade = playerOwned && _currentTier < 3;
            SetActive(_upgradeButton, canUpgrade);
            SetActive(_insuranceButton, playerOwned);

            if (_buyButtonText != null)
            {
                _buyButtonText.text = _currentOwner == Owner.Rival ? _buyFromRivalLabel : _buyLabel;
            }
            if (_upgradeButtonText != null)
            {
                _upgradeButtonText.text = _currentTier >= 3
                    ? _maxTierLabel
                    : string.Format(_upgradeLabel, _currentTier + 1);
            }

            // Debounce upgrade during pending event round-trip (Issue 8A).
            if (_upgradeButton != null) _upgradeButton.interactable = canUpgrade && !_upgradePending;
        }

        private void UpdateAffordability()
        {
            if (_currentLot == null || _currencyManager == null) return;
            float balance = _currencyManager.CheckingBalance;

            string balanceCopy = $"Your Checking: ${balance:N0}";
            SetTextPair(_balanceText, _balanceTextUnowned, balanceCopy);

            float cost = 0f;
            bool showAffordability = false;
            if (_currentOwner == Owner.Player && _currentTier < 3)
            {
                cost = _currentTier + 1 == 2 ? _currentLot.Tier2UpgradeCost : _currentLot.Tier3UpgradeCost;
                showAffordability = true;
            }
            else if (_currentOwner != Owner.Player)
            {
                cost = _currentResolvedCost;
                showAffordability = true;
            }

            bool canAfford = balance >= cost;

            if (_affordabilityText != null)
            {
                if (!showAffordability)
                {
                    _affordabilityText.text = "";
                }
                else if (canAfford)
                {
                    _affordabilityText.text = "You can afford this!";
                    _affordabilityText.color = _canAffordColor;
                }
                else
                {
                    _affordabilityText.text = $"Need ${cost - balance:N0} more";
                    _affordabilityText.color = _cannotAffordColor;
                }
            }

            if (_buyButton != null) _buyButton.interactable = (_currentOwner != Owner.Player) && canAfford;
            // Upgrade interactivity also respects affordability + debounce.
            if (_upgradeButton != null && _currentOwner == Owner.Player)
            {
                _upgradeButton.interactable = _currentTier < 3 && canAfford && !_upgradePending;
            }
        }

        // ── click handlers ──

        private void HandleBuyClicked()
        {
            if (_currentLot == null) return;
            GameEvents.RaisePurchaseLotRequested(_currentLot.LotId, 0);
            HandleCloseClicked();
        }

        private void HandleFinanceClicked()
        {
            if (_currentLot == null) return;
            GameEvents.RaiseLoanSelectionRequested(_currentLot.LotId, _currentLot.BaseCost);
            HandleCloseClicked();
        }

        private void HandleUpgradeClicked()
        {
            if (_currentLot == null || _upgradePending) return;
            _upgradePending = true;
            if (_upgradeButton != null) _upgradeButton.interactable = false;
            GameEvents.RaiseLotUpgradeRequested(_currentLot.LotId);
        }

        private void HandleInsuranceClicked()
        {
            if (_currentLot == null) return;
            GameEvents.RaiseLotInsuranceRequested(_currentLot.LotId);
            HandleCloseClicked();
        }

        private void HandleCloseClicked()
        {
            OnCancelClicked();
        }

        private static void SetActive(Button btn, bool value)
        {
            if (btn != null && btn.gameObject.activeSelf != value) btn.gameObject.SetActive(value);
        }

        private static void SetActive(GameObject go, bool value)
        {
            if (go != null && go.activeSelf != value) go.SetActive(value);
        }
    }
}
