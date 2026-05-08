using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Entities.WebPanels;

namespace FortuneValley.Managers.WebPanels
{
    /// <summary>
    /// Credit panel WebPanelBridge. Subscribes to GameEvents while visible,
    /// builds a CreditPanelDTO via the logic class, ships it to JS.
    ///
    /// JS-facing SendMessage targets (called via unityInstance.SendMessage):
    ///   RequestApplyForLoan(string json) - parses {loanConfigId, lotId, price}
    ///   RequestClose()                   - fires OnHidePanelRequested
    ///
    /// Scene GameObject MUST be named exactly "CreditWebBridge" (see
    /// ObjectName const). Mismatch logs a warning at OnEnable. The matching
    /// JS const lives in unity_bridge_controller.js (Rails embed).
    /// </summary>
    public class CreditWebBridge : WebPanelBridgeBase
    {
        // Issue 4A: keep this in sync with the BRIDGE_OBJECTS map in
        // alora-finance-main-website/app/javascript/controllers/unity_bridge_controller.js.
        public const string ObjectName = "CreditWebBridge";

        public override string PanelId => "credit";
        public override string ExpectedObjectName => ObjectName;

        [Header("Dependencies")]
        [SerializeField] private LoanSystem _loanSystem;
        [SerializeField] private CreditScoreSystem _creditCardSystem;
        [SerializeField] private CurrencyManager _currencyManager;
        [SerializeField] private CityManager _cityManager;
        [SerializeField] private TransactionLog _transactionLog;
        [SerializeField] private TimeManager _timeManager;

        // Cached DTO + logic to keep per-push allocation bounded.
        private readonly CreditPanelDTO _dto = new CreditPanelDTO();
        private readonly CreditWebBridgeLogic _logic = new CreditWebBridgeLogic();

        protected override void OnEnable()
        {
            base.OnEnable();
            _logic.Initialize(_loanSystem, _creditCardSystem, _currencyManager, _cityManager, _transactionLog, _timeManager);
            // Pre-selection intent must subscribe BEFORE Show because the event
            // fires from the lot click that ALSO opens the panel; if we waited
            // until Subscribe (Show), we would miss the event.
            GameEvents.OnLotLoanExploreRequested += HandleLotLoanExploreRequested;
        }

        protected override void OnDisable()
        {
            GameEvents.OnLotLoanExploreRequested -= HandleLotLoanExploreRequested;
            base.OnDisable();
        }

        private void HandleLotLoanExploreRequested(string lotId)
        {
            _logic.SetSelectedLotId(lotId);
            // Mark dirty so the next Show / LateUpdate push surfaces the pre-select.
            // Safe even when not visible: WebPanelBridgeBase.Show calls PushNow
            // unconditionally on transition to visible.
            MarkDirty();
        }

        protected override void Subscribe()
        {
            GameEvents.OnTick += HandleTick;
            GameEvents.OnLoanOriginated += HandleLoanOriginated;
            GameEvents.OnLoanPaymentMade += HandleLoanPaymentMade;
            GameEvents.OnLoanPaidOff += HandleLoanPaidOff;
            GameEvents.OnCreditScoreChanged += HandleCreditScoreChanged;
            if (FeatureFlags.CreditCardChargesEnabled)
                GameEvents.OnCreditCardBalanceChanged += HandleBalanceChanged;
            GameEvents.OnCheckingBalanceChanged += HandleBalanceChanged;
        }

        protected override void Unsubscribe()
        {
            GameEvents.OnTick -= HandleTick;
            GameEvents.OnLoanOriginated -= HandleLoanOriginated;
            GameEvents.OnLoanPaymentMade -= HandleLoanPaymentMade;
            GameEvents.OnLoanPaidOff -= HandleLoanPaidOff;
            GameEvents.OnCreditScoreChanged -= HandleCreditScoreChanged;
            if (FeatureFlags.CreditCardChargesEnabled)
                GameEvents.OnCreditCardBalanceChanged -= HandleBalanceChanged;
            GameEvents.OnCheckingBalanceChanged -= HandleBalanceChanged;
        }

        protected override string BuildPayloadJson()
        {
            if (!_logic.PopulateDTO(_dto)) return null;
            return JsonUtility.ToJson(_dto);
        }

        // ---------- Event handlers (mark dirty; LateUpdate coalesces the push) ----------

        private void HandleTick(int tickNumber) => MarkDirty();
        private void HandleLoanOriginated(ActiveLoan loan) => MarkDirty();
        private void HandleLoanPaymentMade(ActiveLoan loan, float amount) => MarkDirty();
        private void HandleLoanPaidOff(ActiveLoan loan) => MarkDirty();
        private void HandleCreditScoreChanged(int newScore) => MarkDirty();
        private void HandleBalanceChanged(float balance, float delta) => MarkDirty();

        // ---------- SendMessage entry points (called from JS) ----------

        public void RequestApplyForLoan(string json)
        {
            if (!TryParseApplyIntent(json, out ApplyForLoanIntent intent)) return;
            if (_loanSystem == null)
            {
                Bridge.ShowError(PanelId, "Game not ready.");
                return;
            }
            GameEvents.RaiseLoanPurchaseRequested(intent.loanConfigId, intent.lotId, intent.price);
        }

        public void RequestClose()
        {
            GameEvents.RaiseHidePanelRequested(PanelType.Loan);
        }

        // ---------- Intent parsing helpers ----------

        private bool TryParseApplyIntent(string json, out ApplyForLoanIntent intent)
        {
            intent = null;
            if (string.IsNullOrEmpty(json))
            {
                Bridge.ShowError(PanelId, "Empty request.");
                return false;
            }

            try { intent = JsonUtility.FromJson<ApplyForLoanIntent>(json); }
            catch
            {
                Bridge.ShowError(PanelId, "Malformed request.");
                return false;
            }

            if (intent == null || string.IsNullOrEmpty(intent.loanConfigId))
            {
                Bridge.ShowError(PanelId, "Missing loan selection.");
                return false;
            }
            if (string.IsNullOrEmpty(intent.lotId))
            {
                Bridge.ShowError(PanelId, "Missing lot selection.");
                return false;
            }
            if (intent.price <= 0f)
            {
                Bridge.ShowError(PanelId, "Invalid lot price.");
                return false;
            }
            return true;
        }
    }
}
