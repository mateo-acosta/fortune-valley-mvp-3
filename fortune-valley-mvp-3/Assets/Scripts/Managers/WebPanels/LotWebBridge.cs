using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Entities.WebPanels;

namespace FortuneValley.Managers.WebPanels
{
    /// <summary>
    /// Lot detail panel WebPanelBridge. Replaces the legacy LotInfoPopup
    /// surface. Subscribes to GameEvents while visible, builds a
    /// LotPanelDTO via the logic class, ships it to JS.
    ///
    /// JS-facing SendMessage targets (called via unityInstance.SendMessage):
    ///   RequestPurchaseLot(string json)  - parses LotIntent, fires OnPurchaseLotRequested
    ///   RequestExploreLoan(string json)  - parses LotIntent, fires OnLotLoanExploreRequested
    ///   RequestUpgradeLot(string json)   - parses LotIntent, fires OnLotUpgradeRequested (with debounce)
    ///   RequestInsureLot(string json)    - parses LotIntent, fires OnLotInsuranceRequested (FeatureFlag-gated)
    ///   RequestClose()                   - fires OnHidePanelRequested(PanelType.Lots)
    ///
    /// Scene GameObject MUST be named exactly "LotWebBridge" (see
    /// ObjectName const). Mismatch logs a warning at OnEnable. The matching
    /// JS const lives in Assets/WebGLTemplates/FortuneValley/index.html
    /// in the BRIDGE_OBJECTS map.
    /// </summary>
    public class LotWebBridge : WebPanelBridgeBase
    {
        // Keep this in sync with the BRIDGE_OBJECTS map in
        // Assets/WebGLTemplates/FortuneValley/index.html.
        public const string ObjectName = "LotWebBridge";

        public override string PanelId => "lot";
        public override string ExpectedObjectName => ObjectName;

        [Header("Dependencies")]
        [SerializeField] private CityManager _cityManager;
        [SerializeField] private CurrencyManager _currencyManager;
        [SerializeField] private TimeManager _timeManager;

        // Cached DTO + logic to keep per-push allocation bounded.
        private readonly LotPanelDTO _dto = new LotPanelDTO();
        private readonly LotWebBridgeLogic _logic = new LotWebBridgeLogic();

        protected override void OnEnable()
        {
            base.OnEnable();
            _logic.Initialize(_cityManager, _currencyManager, _timeManager);
        }

        /// <summary>
        /// Seeds the active lot context. Called by UIManager on
        /// OnLotInfoRequested before Show() so the first push targets
        /// the right lot.
        /// </summary>
        public void ConfigureForLotId(string lotId)
        {
            _logic.SetActiveLotId(lotId);
            MarkDirty();
        }

        protected override void Subscribe()
        {
            GameEvents.OnCheckingBalanceChanged += HandleBalanceChanged;
            GameEvents.OnLotTierChanged += HandleLotTierChanged;
            GameEvents.OnLotPurchased += HandleLotPurchased;
        }

        protected override void Unsubscribe()
        {
            GameEvents.OnCheckingBalanceChanged -= HandleBalanceChanged;
            GameEvents.OnLotTierChanged -= HandleLotTierChanged;
            GameEvents.OnLotPurchased -= HandleLotPurchased;
        }

        protected override string BuildPayloadJson()
        {
            if (!_logic.PopulateDTO(_dto)) return null;
            return JsonUtility.ToJson(_dto);
        }

        // ---------- Event handlers (mark dirty; LateUpdate coalesces the push) ----------

        private void HandleBalanceChanged(float balance, float delta) => MarkDirty();

        private void HandleLotTierChanged(string lotId, int newTier)
        {
            // Successful upgrade releases the debounce only when the
            // event matches the lot we're showing. Mirrors LotInfoPopup.
            if (lotId == _logic.ActiveLotId) _logic.SetUpgradePending(false);
            MarkDirty();
        }

        private void HandleLotPurchased(string lotId, Owner owner) => MarkDirty();

        // ---------- SendMessage entry points (called from JS) ----------

        public void RequestPurchaseLot(string json)
        {
            if (!TryParseLotIntent(json, out LotIntent intent)) return;
            // Cash-buy path. Mirrors LotInfoPopup.HandleBuyClicked when affordable.
            // CurrencyManager remains the cost authority; a stale UI intent
            // for a lot the player cannot actually afford gets rejected
            // downstream by CityManager.TryPurchaseLot.
            GameEvents.RaisePurchaseLotRequested(intent.lotId, intent.loanId);
            GameEvents.RaiseHidePanelRequested(PanelType.Lots);
        }

        public void RequestExploreLoan(string json)
        {
            if (!TryParseLotIntent(json, out LotIntent intent)) return;
            // Loan-explore path. Mirrors LotInfoPopup.HandleBuyClicked when
            // the player cannot afford. The credit panel opens with this
            // lot pre-selected on its Explore tab.
            GameEvents.RaiseLotLoanExploreRequested(intent.lotId);
            GameEvents.RaiseHidePanelRequested(PanelType.Lots);
        }

        public void RequestUpgradeLot(string json)
        {
            if (!TryParseLotIntent(json, out LotIntent intent)) return;
            if (_logic.IsUpgradePending) return;
            _logic.SetUpgradePending(true);
            // Push state immediately so the panel sees the pending flag
            // even before LateUpdate's next coalesced push.
            MarkDirty();
            GameEvents.RaiseLotUpgradeRequested(intent.lotId);
        }

        public void RequestInsureLot(string json)
        {
            if (!FeatureFlags.InsuranceEnabled) return;
            if (!TryParseLotIntent(json, out LotIntent intent)) return;
            GameEvents.RaiseLotInsuranceRequested(intent.lotId);
            GameEvents.RaiseHidePanelRequested(PanelType.Lots);
        }

        public void RequestClose()
        {
            GameEvents.RaiseHidePanelRequested(PanelType.Lots);
        }

        // ---------- Intent parsing helpers ----------

        private bool TryParseLotIntent(string json, out LotIntent intent)
        {
            intent = null;
            if (string.IsNullOrEmpty(json))
            {
                Bridge.ShowError(PanelId, "Empty request.");
                return false;
            }

            try { intent = JsonUtility.FromJson<LotIntent>(json); }
            catch
            {
                Bridge.ShowError(PanelId, "Malformed request.");
                return false;
            }

            if (intent == null || string.IsNullOrEmpty(intent.lotId))
            {
                Bridge.ShowError(PanelId, "Missing lot id.");
                return false;
            }
            return true;
        }
    }
}
