using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Entities.WebPanels;

namespace FortuneValley.Managers.WebPanels
{
    /// <summary>
    /// Investing panel WebPanelBridge. Subscribes to GameEvents while visible,
    /// builds an InvestingPanelDTO via the logic class, ships it to JS.
    ///
    /// JS-facing SendMessage targets (called via unityInstance.SendMessage):
    ///   RequestBuyShares(string json)   - Phase 5
    ///   RequestSellShares(string json)  - Phase 5
    ///   RequestClose()                  - Phase 4 (fires OnHidePanelRequested)
    ///
    /// Scene GameObject MUST be named exactly "InvestingWebBridge" (see
    /// ObjectName const). Mismatch logs a warning at OnEnable. The matching
    /// JS const lives in the WebGL template's index.html.
    /// </summary>
    public class InvestingWebBridge : WebPanelBridgeBase
    {
        // Issue 4A: keep this in sync with INVESTING_BRIDGE_OBJECT in
        // Assets/WebGLTemplates/FortuneValley/index.html.
        public const string ObjectName = "InvestingWebBridge";

        public override string PanelId => "investing";
        public override string ExpectedObjectName => ObjectName;

        [Header("Dependencies")]
        [SerializeField] private InvestmentSystem _investmentSystem;
        [SerializeField] private CurrencyManager _currencyManager;
        [SerializeField] private PortfolioHistoryTracker _historyTracker;
        [SerializeField] private StockPriceHistoryStore _priceHistoryStore;

        // Cached so per-push allocation stays bounded (Issue 13: tick rate
        // is low so this is leverage on top of an already acceptable cost).
        private readonly InvestingPanelDTO _dto = new InvestingPanelDTO();
        private readonly InvestingWebBridgeLogic _logic = new InvestingWebBridgeLogic();

        protected override void OnEnable()
        {
            base.OnEnable();
            _logic.Initialize(_investmentSystem, _currencyManager, _historyTracker, _priceHistoryStore);
        }

        protected override void Subscribe()
        {
            GameEvents.OnTick += HandleTick;
            GameEvents.OnInvestmentCreated += HandleInvestmentCreated;
            GameEvents.OnInvestmentWithdrawn += HandleInvestmentWithdrawn;
            GameEvents.OnCheckingBalanceChanged += HandleBalanceChanged;
            GameEvents.OnInvestingBalanceChanged += HandleBalanceChanged;
        }

        protected override void Unsubscribe()
        {
            GameEvents.OnTick -= HandleTick;
            GameEvents.OnInvestmentCreated -= HandleInvestmentCreated;
            GameEvents.OnInvestmentWithdrawn -= HandleInvestmentWithdrawn;
            GameEvents.OnCheckingBalanceChanged -= HandleBalanceChanged;
            GameEvents.OnInvestingBalanceChanged -= HandleBalanceChanged;
        }

        protected override string BuildPayloadJson()
        {
            if (!_logic.PopulateDTO(_dto)) return null;
            return JsonUtility.ToJson(_dto);
        }

        // ---------- Event handlers (mark dirty; LateUpdate coalesces the push) ----------

        private void HandleTick(int tickNumber) => MarkDirty();
        private void HandleInvestmentCreated(ActiveInvestment inv) => MarkDirty();
        private void HandleInvestmentWithdrawn(ActiveInvestment inv, float payout) => MarkDirty();
        private void HandleBalanceChanged(float balance, float delta) => MarkDirty();

        // ---------- SendMessage entry points (called from JS) ----------

        public void RequestBuyShares(string json)
        {
            if (!TryParseSharesIntent(json, out SharesIntent intent)) return;
            if (_investmentSystem == null)
            {
                Bridge.ShowError(PanelId, "Game not ready.");
                return;
            }

            InvestmentDefinition def = InvestmentLookup.FindDefinitionByName(_investmentSystem.AvailableInvestments, intent.symbol);
            if (def == null)
            {
                Bridge.ShowError(PanelId, $"Unknown investment: {intent.symbol}");
                return;
            }

            GameEvents.RaiseBuySharesRequested(def, intent.qty);
        }

        public void RequestSellShares(string json)
        {
            if (!TryParseSharesIntent(json, out SharesIntent intent)) return;
            if (_investmentSystem == null)
            {
                Bridge.ShowError(PanelId, "Game not ready.");
                return;
            }

            ActiveInvestment holding = InvestmentLookup.FindHoldingByName(_investmentSystem.ActiveInvestments, intent.symbol);
            if (holding == null)
            {
                Bridge.ShowError(PanelId, $"You don't own any {intent.symbol}.");
                return;
            }

            if (intent.qty > holding.NumberOfShares)
            {
                Bridge.ShowError(PanelId, $"You only own {holding.NumberOfShares} share(s).");
                return;
            }

            GameEvents.RaiseSellSharesRequested(holding, intent.qty);
        }

        // ---------- Intent parsing helpers ----------

        /// <summary>
        /// Parse + validate a SharesIntent. Calls ShowError and returns false
        /// on malformed input or invalid quantity. On success, returns the
        /// hydrated intent via the out parameter.
        /// </summary>
        private bool TryParseSharesIntent(string json, out SharesIntent intent)
        {
            intent = null;
            if (string.IsNullOrEmpty(json))
            {
                Bridge.ShowError(PanelId, "Empty request.");
                return false;
            }

            try { intent = JsonUtility.FromJson<SharesIntent>(json); }
            catch
            {
                Bridge.ShowError(PanelId, "Malformed request.");
                return false;
            }

            if (intent == null || string.IsNullOrEmpty(intent.symbol))
            {
                Bridge.ShowError(PanelId, "Missing symbol.");
                return false;
            }
            if (intent.qty <= 0)
            {
                Bridge.ShowError(PanelId, "Quantity must be positive.");
                return false;
            }
            return true;
        }

        public void RequestClose()
        {
            GameEvents.RaiseHidePanelRequested(PanelType.Portfolio);
        }
    }
}
