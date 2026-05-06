using System;

namespace FortuneValley.Core
{
    /// <summary>
    /// Thin composer that combines per-system contributions into Total and Liquid
    /// Net Worth values, debounces recomputation to at most once per tick, and
    /// fires GameEvents.OnNetWorthChanged when either value changes.
    ///
    /// Each owning system exposes its own contribution as a Func&lt;float&gt; so this
    /// service stays decoupled from concrete system types and easy to unit-test
    /// with mock contributions.
    ///
    /// Conservative formula (locked in life_goals_design memory):
    ///   LiquidNW = Checking + Investing - CC_debt - Outstanding_loan_principal
    ///   TotalNW  = LiquidNW + Sum(lot acquisitionCost) + Sum(paid tier upgrade costs)
    /// </summary>
    public class NetWorthService : IDisposable
    {
        private const float ChangeEpsilon = 0.01f;

        private readonly Func<float> _liquidNetWorthFunc;
        private readonly Func<float> _businessAssetValueFunc;

        private bool _dirty = true;
        private bool _disposed;
        private float _lastTotal;
        private float _lastLiquid;
        private bool _hasFiredOnce;

        public NetWorthService(Func<float> liquidNetWorth, Func<float> businessAssetValue = null)
        {
            _liquidNetWorthFunc = liquidNetWorth ?? throw new ArgumentNullException(nameof(liquidNetWorth));
            _businessAssetValueFunc = businessAssetValue;

            GameEvents.OnCheckingBalanceChanged += HandleBalance;
            GameEvents.OnInvestingBalanceChanged += HandleBalance;
            GameEvents.OnCreditCardBalanceChanged += HandleBalance;
            GameEvents.OnLotPurchased += HandleLotPurchased;
            GameEvents.OnLotOwnershipChanged += HandleLotOwnership;
            GameEvents.OnLotTierChanged += HandleLotTier;
            GameEvents.OnLoanPaymentMade += HandleLoanPayment;
            GameEvents.OnLoanOriginated += HandleLoanOriginated;
            GameEvents.OnLoanPaidOff += HandleLoanPaidOff;
            GameEvents.OnTick += HandleTick;
        }

        public float LiquidNetWorth => _liquidNetWorthFunc != null ? _liquidNetWorthFunc() : 0f;

        public float BusinessAssetValue => _businessAssetValueFunc != null ? _businessAssetValueFunc() : 0f;

        public float TotalNetWorth => LiquidNetWorth + BusinessAssetValue;

        /// <summary>
        /// Force a recompute and fire OnNetWorthChanged if values changed beyond epsilon.
        /// Tests call this directly. Production code lets HandleTick drive it.
        /// </summary>
        public void Pump()
        {
            if (!_dirty) return;
            _dirty = false;

            float total = TotalNetWorth;
            float liquid = LiquidNetWorth;

            if (!_hasFiredOnce ||
                Math.Abs(total - _lastTotal) > ChangeEpsilon ||
                Math.Abs(liquid - _lastLiquid) > ChangeEpsilon)
            {
                _lastTotal = total;
                _lastLiquid = liquid;
                _hasFiredOnce = true;
                GameEvents.RaiseNetWorthChanged(total, liquid);
            }
        }

        /// <summary>
        /// Mark dirty. Subscribers can also call this externally if they observe
        /// a state change that the service does not subscribe to directly
        /// (e.g. a tier upgrade ledger update once Step 14 wires acquisitionCost).
        /// </summary>
        public void MarkDirty()
        {
            _dirty = true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            GameEvents.OnCheckingBalanceChanged -= HandleBalance;
            GameEvents.OnInvestingBalanceChanged -= HandleBalance;
            GameEvents.OnCreditCardBalanceChanged -= HandleBalance;
            GameEvents.OnLotPurchased -= HandleLotPurchased;
            GameEvents.OnLotOwnershipChanged -= HandleLotOwnership;
            GameEvents.OnLotTierChanged -= HandleLotTier;
            GameEvents.OnLoanPaymentMade -= HandleLoanPayment;
            GameEvents.OnLoanOriginated -= HandleLoanOriginated;
            GameEvents.OnLoanPaidOff -= HandleLoanPaidOff;
            GameEvents.OnTick -= HandleTick;
            _disposed = true;
        }

        private void HandleBalance(float balance, float delta) => MarkDirty();
        private void HandleLotPurchased(string lotId, FortuneValley.Domain.Enums.Owner owner) => MarkDirty();
        private void HandleLotOwnership(string lotId, FortuneValley.Domain.Enums.Owner prev, FortuneValley.Domain.Enums.Owner next) => MarkDirty();
        private void HandleLotTier(string lotId, int tier) => MarkDirty();
        private void HandleLoanPayment(FortuneValley.Domain.Entities.ActiveLoan loan, float amount) => MarkDirty();
        private void HandleLoanOriginated(FortuneValley.Domain.Entities.ActiveLoan loan) => MarkDirty();
        private void HandleLoanPaidOff(FortuneValley.Domain.Entities.ActiveLoan loan) => MarkDirty();
        private void HandleTick(int tick) => Pump();
    }
}
