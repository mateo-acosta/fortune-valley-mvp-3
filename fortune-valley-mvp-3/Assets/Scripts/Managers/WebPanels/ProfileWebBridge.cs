using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Entities.WebPanels;

namespace FortuneValley.Managers.WebPanels
{
    /// <summary>
    /// PlayerProfile panel WebPanelBridge. Subscribes to GameEvents while visible,
    /// builds a ProfilePanelDTO via the logic class, ships it to JS.
    ///
    /// JS-facing SendMessage targets (called via unityInstance.SendMessage):
    ///   RequestClose()           - fires OnHidePanelRequested(PanelType.Profile)
    ///   RequestReplayTutorial()  - fires OnTutorialStartRequested(true)
    ///
    /// Scene GameObject MUST be named exactly "ProfileWebBridge" (see ObjectName
    /// const). Mismatch logs a warning at OnEnable. The matching JS const lives
    /// in WebGLTemplates/FortuneValley/index.html in the BRIDGE_OBJECTS map.
    ///
    /// NetWorthService and LifeGoalSelectionService are pure-C# (lifecycle owned
    /// by GameManager) and therefore cannot be SerializeField'd. The bridge
    /// instead listens on GameEvents.OnNetWorthChanged + OnLifeGoalsSelected
    /// for snapshots, and on Show() raises OnRequestNetWorthSnapshot to seed
    /// the cache for fresh subscriptions (matches the LifeGoalsHud pattern).
    /// </summary>
    public class ProfileWebBridge : WebPanelBridgeBase
    {
        // Keep this in sync with the BRIDGE_OBJECTS map in
        // Assets/WebGLTemplates/FortuneValley/index.html.
        public const string ObjectName = "ProfileWebBridge";

        public override string PanelId => "profile";
        public override string ExpectedObjectName => ObjectName;

        [Header("Dependencies")]
        [SerializeField] private LoanSystem _loanSystem;
        [SerializeField] private CurrencyManager _currencyManager;
        [SerializeField] private CityManager _cityManager;
        [SerializeField] private TimeManager _timeManager;
        [SerializeField] private CreditScoreSystem _creditCardSystem;
        [SerializeField] private QuestionManager _questionManager;
        [SerializeField] private RestaurantSystem _restaurantSystem;
        [SerializeField] private InvestmentSystem _investmentSystem;

        // Cached DTO + logic to keep per-push allocation bounded.
        private readonly ProfilePanelDTO _dto = new ProfilePanelDTO();
        private readonly ProfileWebBridgeLogic _logic = new ProfileWebBridgeLogic();

        protected override void OnEnable()
        {
            base.OnEnable();
            _logic.Initialize(_loanSystem, _currencyManager, _cityManager, _timeManager, _creditCardSystem, _questionManager, _restaurantSystem, _investmentSystem);

            // Catch-up from save: BankruptcyResetService is pure C# (owned by
            // GameManager) and the bankruptcy flag is sticky. The selected
            // life goals are likewise loaded into LifeGoalSelectionService;
            // if a save was hydrated before this component instantiated,
            // we reseed both from the cached DTO so the first push reflects
            // restored state.
            var saveDto = GameEvents.LastLoadedSaveDto;
            if (saveDto != null)
            {
                if (saveDto.bankruptcy_flag)
                {
                    _logic.SetBankruptcyFlag(true);
                }
                if (saveDto.selected_goals != null
                    && saveDto.selected_goals.Length == LifeGoalSelection.RequiredEntryCount
                    && LifeGoalSelection.IsValidTierComposition(saveDto.selected_goals))
                {
                    _logic.SetSelection(new LifeGoalSelection(saveDto.selected_goals));
                }
            }
        }

        protected override void Subscribe()
        {
            GameEvents.OnTick += HandleTick;
            GameEvents.OnNetWorthChanged += HandleNetWorthChanged;
            GameEvents.OnLifeGoalsSelected += HandleLifeGoalsSelected;
            GameEvents.OnGoalRealized += HandleGoalRealized;
            GameEvents.OnYearEnd += HandleYearEnd;
            GameEvents.OnLoanOriginated += HandleLoanOriginated;
            GameEvents.OnLoanPaymentMade += HandleLoanPaymentMade;
            GameEvents.OnLoanPaidOff += HandleLoanPaidOff;
            GameEvents.OnLotPurchased += HandleLotPurchased;
            GameEvents.OnLotTierChanged += HandleLotTierChanged;
            GameEvents.OnLotOwnershipChanged += HandleLotOwnershipChanged;
            GameEvents.OnCreditScoreChanged += HandleCreditScoreChanged;
            // Activity tab: streak rides OnQuestionAnswered + OnQuestionRewardGranted
            // (post-submission streak); lifetime restaurant earnings rides
            // OnIncomeCollected.
            GameEvents.OnQuestionAnswered += HandleQuestionAnswered;
            GameEvents.OnQuestionRewardGranted += HandleQuestionRewardGranted;
            GameEvents.OnIncomeCollected += HandleIncomeCollected;
            // Bankruptcy chip: sticky-true after first OnSoftBankruptcyReset.
            GameEvents.OnSoftBankruptcyReset += HandleSoftBankruptcyReset;
            // Investment story numbers: react to all create/sell/compound events.
            GameEvents.OnInvestmentCreated += HandleInvestmentChanged;
            GameEvents.OnInvestmentWithdrawn += HandleInvestmentWithdrawn;
            GameEvents.OnInvestmentCompounded += HandleInvestmentChanged;

            // Pull-pattern seed: ask NetWorthService to re-emit current cached
            // values immediately. The cascaded OnNetWorthChanged populates the
            // bridge cache before the first PushNow() runs.
            GameEvents.RaiseRequestNetWorthSnapshot();

            // Same pull-pattern for the locked-in life goals. The tutorial's
            // OnLifeGoalsSelected fired before this bridge ever subscribed
            // (Subscribe runs on panel open, not scene load) and on a fresh
            // game there is no save to catch up from. LifeGoalSelectionService
            // re-emits OnLifeGoalsSelected synchronously here so HandleLifeGoalsSelected
            // seeds _logic before the first PushNow() builds the DTO.
            GameEvents.RaiseRequestLifeGoalsSnapshot();
        }

        protected override void Unsubscribe()
        {
            GameEvents.OnTick -= HandleTick;
            GameEvents.OnNetWorthChanged -= HandleNetWorthChanged;
            GameEvents.OnLifeGoalsSelected -= HandleLifeGoalsSelected;
            GameEvents.OnGoalRealized -= HandleGoalRealized;
            GameEvents.OnYearEnd -= HandleYearEnd;
            GameEvents.OnLoanOriginated -= HandleLoanOriginated;
            GameEvents.OnLoanPaymentMade -= HandleLoanPaymentMade;
            GameEvents.OnLoanPaidOff -= HandleLoanPaidOff;
            GameEvents.OnLotPurchased -= HandleLotPurchased;
            GameEvents.OnLotTierChanged -= HandleLotTierChanged;
            GameEvents.OnLotOwnershipChanged -= HandleLotOwnershipChanged;
            GameEvents.OnCreditScoreChanged -= HandleCreditScoreChanged;
            GameEvents.OnQuestionAnswered -= HandleQuestionAnswered;
            GameEvents.OnQuestionRewardGranted -= HandleQuestionRewardGranted;
            GameEvents.OnIncomeCollected -= HandleIncomeCollected;
            GameEvents.OnSoftBankruptcyReset -= HandleSoftBankruptcyReset;
            GameEvents.OnInvestmentCreated -= HandleInvestmentChanged;
            GameEvents.OnInvestmentWithdrawn -= HandleInvestmentWithdrawn;
            GameEvents.OnInvestmentCompounded -= HandleInvestmentChanged;
        }

        protected override string BuildPayloadJson()
        {
            if (!_logic.PopulateDTO(_dto)) return null;
            string json = JsonUtility.ToJson(_dto);
            // One-shot: clear bloom flags after the push so the iframe only
            // animates the realize moment once. Matches the data-just-realized
            // contract on the panel side.
            _logic.ClearJustRealizedFlags();
            return json;
        }

        // ---------- Event handlers (mark dirty; LateUpdate coalesces the push) ----------

        private void HandleTick(int tickNumber) => MarkDirty();

        private void HandleNetWorthChanged(float total, float liquid)
        {
            _logic.SetNetWorthSnapshot(total, liquid);
            MarkDirty();
        }

        private void HandleLifeGoalsSelected(LifeGoalSelection selection)
        {
            _logic.SetSelection(selection);
            MarkDirty();
        }

        private void HandleGoalRealized(LifeGoalEntry entry)
        {
            if (entry != null) _logic.MarkJustRealized(entry.goal_id);
            MarkDirty();
        }

        private void HandleYearEnd(int age) => MarkDirty();
        private void HandleLoanOriginated(ActiveLoan loan) => MarkDirty();
        private void HandleLoanPaymentMade(ActiveLoan loan, float amount) => MarkDirty();
        private void HandleLoanPaidOff(ActiveLoan loan) => MarkDirty();
        private void HandleLotPurchased(string lotId, Owner owner) => MarkDirty();
        private void HandleLotTierChanged(string lotId, int newTier) => MarkDirty();
        private void HandleLotOwnershipChanged(string lotId, Owner prev, Owner next) => MarkDirty();
        private void HandleCreditScoreChanged(int newScore) => MarkDirty();
        private void HandleQuestionAnswered(QuestionData q, bool correct, int chosen, int correctIdx, int streak) => MarkDirty();
        private void HandleQuestionRewardGranted(int amount, int newStreak) => MarkDirty();
        private void HandleIncomeCollected(string buildingId, float amount) => MarkDirty();
        private void HandleSoftBankruptcyReset()
        {
            _logic.SetBankruptcyFlag(true);
            MarkDirty();
        }
        private void HandleInvestmentChanged(ActiveInvestment inv) => MarkDirty();
        private void HandleInvestmentWithdrawn(ActiveInvestment inv, float payout) => MarkDirty();

        // ---------- SendMessage entry points (called from JS) ----------

        public void RequestClose()
        {
            GameEvents.RaiseHidePanelRequested(PanelType.Profile);
        }

        public void RequestReplayTutorial()
        {
            // Replay flag = true so the Skip button is revealed after step 1
            // (matches ReplayTutorialService behavior).
            GameEvents.RaiseTutorialStartRequested(true);
        }
    }
}
