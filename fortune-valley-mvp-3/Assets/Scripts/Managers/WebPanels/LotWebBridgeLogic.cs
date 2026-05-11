using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Entities.WebPanels;

namespace FortuneValley.Managers.WebPanels
{
    /// <summary>
    /// Reads lot detail panel state off the live systems and writes it
    /// into the supplied LotPanelDTO. Pure C# so EditMode tests can
    /// substitute fakes for the system references.
    ///
    /// Mirrors LotInfoPopup.cs decision logic: cost authority stays in
    /// CityManager, but the displayed cost (rival buyout multiplier) is
    /// resolved here for the panel readout.
    ///
    /// All income figures shipped to the panel are pre-converted to
    /// per-year using EnginePulsesPerTick * TicksPerYear so the iframe
    /// never sees engine-internal units.
    /// </summary>
    public class LotWebBridgeLogic : WebPanelBridgeLogic<LotPanelDTO>
    {
        private CityManager _cityManager;
        private CurrencyManager _currencyManager;
        private TimeManager _timeManager;

        // The active lot being viewed. Set by ConfigureForLotId before Show.
        private string _activeLotId;

        // Set true on RequestUpgradeLot, cleared on OnLotTierChanged for the
        // active lot. Mirrors LotInfoPopup._upgradePending so a double-click
        // during the round-trip is suppressed.
        private bool _upgradePending;

        public void Initialize(CityManager cityManager, CurrencyManager currencyManager, TimeManager timeManager)
        {
            _cityManager = cityManager;
            _currencyManager = currencyManager;
            _timeManager = timeManager;
        }

        public void SetActiveLotId(string lotId)
        {
            _activeLotId = lotId;
            _upgradePending = false;
        }

        public string ActiveLotId => _activeLotId;

        public void SetUpgradePending(bool pending) { _upgradePending = pending; }
        public bool IsUpgradePending => _upgradePending;

        public override bool PopulateDTO(LotPanelDTO target)
        {
            if (target == null) return false;
            if (_cityManager == null || _currencyManager == null) return false;
            if (string.IsNullOrEmpty(_activeLotId)) return false;

            CityLotDefinition lot = ResolveLot(_activeLotId);
            if (lot == null) return false;

            // Identity
            target.lotId = lot.LotId;
            target.displayName = lot.DisplayName;
            target.description = lot.Description;

            // Ownership + tier
            Owner owner = ResolveOwner(lot.LotId);
            int tier = ResolveTier(lot.LotId);
            target.owner = OwnerToWire(owner);
            target.tier = owner == Owner.Player ? tier : 0;

            // Costs
            target.baseCost = lot.BaseCost;
            target.rivalMultiplier = lot.RivalBuyoutMultiplier;
            target.resolvedCost = owner == Owner.Rival
                ? lot.BaseCost * lot.RivalBuyoutMultiplier
                : lot.BaseCost;
            target.tier2Cost = lot.Tier2UpgradeCost;
            target.tier3Cost = lot.Tier3UpgradeCost;

            // Income (yearly).
            int ticksPerDay = _timeManager != null ? _timeManager.EnginePulsesPerTick : 1;
            int yearMultiplier = ticksPerDay * LifespanConstants.TicksPerYear;
            target.incomePerYear = lot.IncomeBonus * yearMultiplier;

            if (owner == Owner.Player)
            {
                target.incomeAtCurrentTierPerYear = lot.GetIncomeAtTier(tier) * yearMultiplier;
                target.incomeAtNextTierPerYear = tier < 3
                    ? lot.GetIncomeAtTier(tier + 1) * yearMultiplier
                    : 0f;
            }
            else
            {
                target.incomeAtCurrentTierPerYear = 0f;
                target.incomeAtNextTierPerYear = 0f;
            }

            // Payback (whole years, rounded up). 0 means "under a year".
            // -1 sentinel means no income; panel hides the payback row.
            float yearlyForPayback = owner == Owner.Player
                ? target.incomeAtCurrentTierPerYear
                : target.incomePerYear;
            if (yearlyForPayback <= 0f) target.paybackYears = -1;
            else target.paybackYears = Mathf.Max(0, Mathf.CeilToInt(target.resolvedCost / yearlyForPayback));

            // Player wallet
            target.checkingBalance = _currencyManager.CheckingBalance;

            // Flags
            target.insuranceEnabled = FeatureFlags.InsuranceEnabled;
            target.upgradePending = _upgradePending;
            target.isMaxTier = owner == Owner.Player && tier >= 3;

            return true;
        }

        private CityLotDefinition ResolveLot(string lotId)
        {
            var all = _cityManager.AllLots;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && all[i].LotId == lotId) return all[i];
            }
            return null;
        }

        private Owner ResolveOwner(string lotId)
        {
            var dict = _cityManager.LotOwnership;
            if (dict != null && dict.TryGetValue(lotId, out var o)) return o;
            return Owner.None;
        }

        private int ResolveTier(string lotId)
        {
            var dict = _cityManager.LotTiers;
            if (dict != null && dict.TryGetValue(lotId, out var t)) return t;
            return 0;
        }

        private static string OwnerToWire(Owner owner)
        {
            if (owner == Owner.Player) return "player";
            if (owner == Owner.Rival) return "rival";
            return "none";
        }
    }
}
