using System.Collections.Generic;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Notifications;
using FortuneValley.Domain.Notifications.Contexts;
using FortuneValley.Managers.Notifications.Builders;

namespace FortuneValley.Managers.Notifications.Dispatchers
{
    /// <summary>
    /// Translates loan-related game events into guidance banner requests
    /// and hands them to <see cref="GuidanceController"/> for filtering,
    /// modal deferral, and suppression handling.
    ///
    /// Also tracks "pending loans" in memory: each OnLoanOriginated records
    /// the loan against its intended lot and the current game tick. On
    /// every OnTick, pending loans older than the configured threshold
    /// without a matching OnLotPurchased fire a "loan held without lot"
    /// nudge once and are removed from the pending set.
    /// </summary>
    public class LoanGuidanceDispatcher : MonoBehaviour
    {
        [Header("Tips")]
        [SerializeField] private GuidanceController _controller;
        [SerializeField] private GuidanceTipSO _loanTakenTip;
        [SerializeField] private GuidanceTipSO _loanHeldWithoutLotTip;

        [Header("Held-without-lot")]
        [Tooltip("Ticks of grace period after a loan originates before the " +
                 "'held without lot' nudge fires. A tick is one in-game minute " +
                 "in the current economy tuning.")]
        [SerializeField] private int _heldThresholdTicks = 5;

        private IBannerMessageBuilder<LoanTakenContext> _takenBuilder;
        private IBannerMessageBuilder<LoanHeldWithoutLotContext> _heldBuilder;

        private readonly Dictionary<string, PendingLoan> _pending = new Dictionary<string, PendingLoan>();

        private struct PendingLoan
        {
            public string LotId;
            public float Principal;
            public int OriginatedTick;
        }

        private void Awake()
        {
            _takenBuilder = new LoanTakenMessageBuilder();
            _heldBuilder = new LoanHeldWithoutLotMessageBuilder();
        }

        private void OnEnable()
        {
            GameEvents.OnLoanOriginated += HandleLoanOriginated;
            GameEvents.OnLotPurchased += HandleLotPurchased;
            GameEvents.OnTick += HandleTick;
        }

        private void OnDisable()
        {
            GameEvents.OnLoanOriginated -= HandleLoanOriginated;
            GameEvents.OnLotPurchased -= HandleLotPurchased;
            GameEvents.OnTick -= HandleTick;
        }

        public void Initialize(
            GuidanceController controller,
            GuidanceTipSO loanTakenTip,
            GuidanceTipSO loanHeldWithoutLotTip,
            int heldThresholdTicks = 5,
            IBannerMessageBuilder<LoanTakenContext> takenBuilder = null,
            IBannerMessageBuilder<LoanHeldWithoutLotContext> heldBuilder = null)
        {
            _controller = controller;
            _loanTakenTip = loanTakenTip;
            _loanHeldWithoutLotTip = loanHeldWithoutLotTip;
            _heldThresholdTicks = heldThresholdTicks;
            _takenBuilder = takenBuilder ?? new LoanTakenMessageBuilder();
            _heldBuilder = heldBuilder ?? new LoanHeldWithoutLotMessageBuilder();
            _pending.Clear();
        }

        public int PendingLoanCount => _pending.Count;

        // ===============================================================
        // LOAN TAKEN
        // ===============================================================

        public void HandleLoanOriginated(ActiveLoan loan)
        {
            if (loan == null) return;

            TrackPending(loan);
            EmitLoanTakenBanner(loan);
        }

        private void TrackPending(ActiveLoan loan)
        {
            // OriginatedTick gets populated lazily by HandleTick; start at 0.
            // If no tick has fired yet, the grace window is measured from the
            // first tick after origination, which is acceptable.
            _pending[loan.LoanId] = new PendingLoan
            {
                LotId = loan.LotId,
                Principal = loan.Principal,
                OriginatedTick = _lastSeenTick
            };
        }

        private void EmitLoanTakenBanner(ActiveLoan loan)
        {
            if (_controller == null || _loanTakenTip == null || _takenBuilder == null) return;

            var context = new LoanTakenContext(
                principal: loan.Principal,
                lotId: loan.LotId,
                termYears: loan.TermYears,
                monthlyPayment: loan.YearlyPayment);

            var (title, message) = _takenBuilder.Build(
                _loanTakenTip.TitleTemplate, _loanTakenTip.MessageTemplate, context);

            var request = new GuidanceBannerRequest(
                title: title,
                message: message,
                severity: _loanTakenTip.Severity,
                targetIntent: _loanTakenTip.TargetIntent,
                targetData: loan.LotId,
                sourceTipId: _loanTakenTip.name);

            _controller.Submit(_loanTakenTip, request);
        }

        // ===============================================================
        // LOT PURCHASED - clears matching pending loans
        // ===============================================================

        public void HandleLotPurchased(string lotId, Owner owner)
        {
            if (owner != Owner.Player || string.IsNullOrEmpty(lotId)) return;

            // Remove any pending loans whose intended lot was just purchased.
            // A rare edge case: multiple loans taken for the same lot all clear
            // on the single purchase. That is the intended behavior.
            var toRemove = new List<string>();
            foreach (var kv in _pending)
            {
                if (kv.Value.LotId == lotId) toRemove.Add(kv.Key);
            }
            foreach (var key in toRemove) _pending.Remove(key);
        }

        // ===============================================================
        // TICK - ages pending loans, fires held-without-lot banner
        // ===============================================================

        private int _lastSeenTick;

        public void HandleTick(int currentTick)
        {
            _lastSeenTick = currentTick;

            if (_pending.Count == 0) return;
            if (_controller == null || _loanHeldWithoutLotTip == null || _heldBuilder == null) return;

            List<string> fired = null;
            foreach (var kv in _pending)
            {
                int age = currentTick - kv.Value.OriginatedTick;
                if (age < _heldThresholdTicks) continue;

                EmitHeldWithoutLotBanner(kv.Key, kv.Value, age);
                fired ??= new List<string>();
                fired.Add(kv.Key);
            }

            if (fired == null) return;
            foreach (var key in fired) _pending.Remove(key);
        }

        private void EmitHeldWithoutLotBanner(string loanId, PendingLoan pending, int ticksAged)
        {
            var context = new LoanHeldWithoutLotContext(
                loanId: loanId,
                lotId: pending.LotId,
                principal: pending.Principal,
                ticksAged: ticksAged);

            var (title, message) = _heldBuilder.Build(
                _loanHeldWithoutLotTip.TitleTemplate, _loanHeldWithoutLotTip.MessageTemplate, context);

            var request = new GuidanceBannerRequest(
                title: title,
                message: message,
                severity: _loanHeldWithoutLotTip.Severity,
                targetIntent: _loanHeldWithoutLotTip.TargetIntent,
                targetData: pending.LotId,
                sourceTipId: _loanHeldWithoutLotTip.name);

            _controller.Submit(_loanHeldWithoutLotTip, request);
        }
    }
}
