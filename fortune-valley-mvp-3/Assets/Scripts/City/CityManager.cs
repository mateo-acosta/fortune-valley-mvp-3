using System;
using System.Collections.Generic;
using UnityEngine;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Interfaces;

namespace FortuneValley.Core
{
    /// <summary>
    /// Manages city lots and tracks ownership.
    /// Determines win/lose conditions.
    ///
    /// LEARNING DESIGN: Lots are the goal that makes financial decisions matter.
    /// Owning lots provides visual progress and income bonuses.
    /// The race to own lots creates urgency for financial optimization.
    /// </summary>
    public class CityManager : MonoBehaviour, ICityService, ILotRegistry
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        [Header("City Lots")]
        [Tooltip("All lots in the city (5 for POC)")]
        [SerializeField] private List<CityLotDefinition> _allLots;

        [Header("Dependencies")]
        [SerializeField] private CurrencyManager _currencyManager;

        [Header("Starter Assignment")]
        [Tooltip("Lot the player begins with at Tier 2")]
        [SerializeField] private CityLotDefinition _playerStarterLot;

        [Tooltip("Lot the rival begins with at Tier 2")]
        [SerializeField] private CityLotDefinition _rivalStarterLot;

        [Header("Tier Defaults")]
        [Tooltip("Tier a lot is set to when purchased fresh (from None)")]
        [SerializeField] private int _tierOnFreshPurchase = 1;

        [Tooltip("Tier assigned to the starter lots at game start")]
        [SerializeField] private int _tierOnStart = 2;

        [Header("Debug")]
        [SerializeField] private bool _logPurchases = false;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private Dictionary<string, Owner> _lotOwnership = new Dictionary<string, Owner>();
        private Dictionary<string, int> _purchaseTick = new Dictionary<string, int>();
        private Dictionary<string, int> _lotTier = new Dictionary<string, int>();

        // Actual amount the player paid to acquire each lot. Includes the
        // 3x rival markup when the player bought from the rival. Feeds the
        // conservative TotalNetWorth formula (Sum acquisitionCost over owned).
        // Cleared on full game reset; entries removed on lot release.
        private Dictionary<string, float> _acquisitionCost = new Dictionary<string, float>();

        // Soft cap on rival lot ownership so the player is always guaranteed
        // at least (TotalLots - MAX_RIVAL_LOTS) lots available or owned.
        public const int MAX_RIVAL_LOTS = 12;

        // Interface seam -- production mirrors _currencyManager; tests can inject a substitute via reflection.
        private ICurrencyService _currency;

        // Fresh-game render guard. SeedStarterLots raises lot events inline
        // during OnGameStart; swappers that handle OnGameStart after CityManager
        // self-reset the just-seeded paint to "For Sale". This flag queues a
        // one-frame-deferred re-emit (consumed in Update) so starters repaint
        // after every swapper's reset has run, mirroring the proven save-restore
        // Phase 2 (GameSaveBootstrapper._reconcileQueued).
        private bool _reemitOwnedLotsQueued;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// All lot definitions.
        /// </summary>
        public IReadOnlyList<CityLotDefinition> AllLots => _allLots;

        /// <summary>
        /// Read-only view of lot ownership. UI reads this property
        /// instead of calling GetOwner() to respect the UI structural boundary.
        /// </summary>
        public IReadOnlyDictionary<string, Owner> LotOwnership => _lotOwnership;

        /// <summary>
        /// Read-only view of per-lot tier (1..3). Missing key means not-yet-owned.
        /// </summary>
        public IReadOnlyDictionary<string, int> LotTiers => _lotTier;

        /// <summary>
        /// Total number of lots in the city.
        /// </summary>
        public int TotalLots => _allLots.Count;

        /// <summary>
        /// Number of lots owned by player.
        /// </summary>
        public int PlayerLotCount => CountLotsOwnedBy(Owner.Player);

        /// <summary>
        /// Number of lots owned by rival.
        /// </summary>
        public int RivalLotCount => CountLotsOwnedBy(Owner.Rival);

        /// <summary>
        /// Number of lots not yet owned.
        /// </summary>
        public int AvailableLotCount => TotalLots - PlayerLotCount - RivalLotCount;

        /// <summary>
        /// LotId of the player's starter lot (the main restaurant). Null if
        /// no starter lot is configured.
        /// </summary>
        public string PlayerStarterLotId => _playerStarterLot != null ? _playerStarterLot.LotId : null;

        /// <summary>
        /// Total income bonus from player-owned lots per tick.
        /// Sibling: keep in sync with <see cref="EnumeratePlayerLotIncomes"/>.
        /// </summary>
        public float PlayerLotIncomeBonus
        {
            get
            {
                float total = 0f;
                foreach (var lot in _allLots)
                {
                    if (GetOwner(lot.LotId) == Owner.Player)
                    {
                        total += lot.GetIncomeAtTier(GetTier(lot.LotId));
                    }
                }
                return total;
            }
        }

        /// <summary>
        /// Enumerates (lotId, incomePerTick) for each player-owned lot.
        /// Used by the tick hot path so DailyIncomeAccumulator can route per-lot
        /// income into the correct bucket.
        /// Sibling: keep in sync with <see cref="PlayerLotIncomeBonus"/>.
        /// </summary>
        public IEnumerable<(string lotId, float income)> EnumeratePlayerLotIncomes()
        {
            foreach (var lot in _allLots)
            {
                if (GetOwner(lot.LotId) == Owner.Player)
                {
                    yield return (lot.LotId, lot.GetIncomeAtTier(GetTier(lot.LotId)));
                }
            }
        }

        /// <summary>
        /// Total income bonus from rival-owned lots per tick.
        /// </summary>
        public float RivalLotIncomeBonus
        {
            get
            {
                float total = 0f;
                foreach (var lot in _allLots)
                {
                    if (GetOwner(lot.LotId) == Owner.Rival)
                    {
                        total += lot.GetIncomeAtTier(GetTier(lot.LotId));
                    }
                }
                return total;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Awake()
        {
            // Cache the ICurrencyService seam; tests may overwrite via reflection.
            if (_currency == null)
            {
                _currency = _currencyManager;
            }
        }

        private void OnEnable()
        {
            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnTick += HandleTick;
            GameEvents.OnPurchaseLotRequested += HandlePurchaseLotRequested;
            GameEvents.OnLotUpgradeRequested += HandleLotUpgradeRequested;
            SaveRestoreCatchUp.Subscribe(HandleSaveStateLoaded, HandleSaveRestored);
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnTick -= HandleTick;
            GameEvents.OnPurchaseLotRequested -= HandlePurchaseLotRequested;
            GameEvents.OnLotUpgradeRequested -= HandleLotUpgradeRequested;
            SaveRestoreCatchUp.Unsubscribe(HandleSaveStateLoaded, HandleSaveRestored);
        }

        private void Update()
        {
            if (!_reemitOwnedLotsQueued) return;
            _reemitOwnedLotsQueued = false;
            RaiseAllOwnedLotEvents();
        }

        /// <summary>
        /// Phase 2 of save restore. Re-emits per-lot ownership + tier events for
        /// every player- or rival-owned lot so visual subscribers (RestaurantVisualTierSwapper,
        /// BlockController, BuildingCollectButton) paint correctly even if they
        /// enabled after Phase 1 fired. Subscribers' handlers are already idempotent;
        /// receiving these events again on Phase 2 is harmless.
        /// </summary>
        private void HandleSaveRestored()
        {
            RaiseAllOwnedLotEvents();
        }

        private void HandleSaveStateLoaded(GamePlayerStateDTO dto)
        {
            try { Hydrate(dto); }
            catch (Exception e) { Debug.LogError($"[{nameof(CityManager)}] hydrate failed: {e}"); }
        }

        /// <summary>
        /// Intent event handler: UI requested a cash lot purchase.
        /// Loan origination no longer triggers ownership transfer -- the player
        /// must click Buy again after the loan proceeds land in checking.
        /// </summary>
        private void HandlePurchaseLotRequested(string lotId, int tick)
        {
            TryPurchaseLot(lotId, tick);
        }

        private void HandleGameStart()
        {
            // A genuine server restore that actually carried lots owns world
            // state; don't re-seed. But if the restore came back empty (a
            // corrupted/partial row written before starters were ever seeded),
            // fall through and re-seed so the game self-heals instead of
            // showing every block as "For Sale".
            if (GameEvents.SaveStateRestoredFromServer && _lotOwnership.Count > 0) return;
            ResetOwnership();
            SeedStarterLots();
            // Notify UI components of lot count so they can initialize without querying CityManager directly
            GameEvents.RaiseCityInitialized(_allLots.Count);
            // Defer a re-emit one frame so starters survive swapper self-resets
            // that run later in this same OnGameStart dispatch.
            _reemitOwnedLotsQueued = true;
        }

        private void SeedStarterLots()
        {
            if (_playerStarterLot != null)
            {
                _lotOwnership[_playerStarterLot.LotId] = Owner.Player;
                _lotTier[_playerStarterLot.LotId] = _tierOnStart;
                GameEvents.RaiseLotPurchased(_playerStarterLot.LotId, Owner.Player);
                GameEvents.RaiseLotTierChanged(_playerStarterLot.LotId, _tierOnStart);
                GameEvents.RaiseLotOwnershipChanged(_playerStarterLot.LotId, Owner.None, Owner.Player);
            }

            if (_rivalStarterLot != null)
            {
                _lotOwnership[_rivalStarterLot.LotId] = Owner.Rival;
                _lotTier[_rivalStarterLot.LotId] = _tierOnStart;
                GameEvents.RaiseLotPurchased(_rivalStarterLot.LotId, Owner.Rival);
                GameEvents.RaiseLotTierChanged(_rivalStarterLot.LotId, _tierOnStart);
                GameEvents.RaiseLotOwnershipChanged(_rivalStarterLot.LotId, Owner.None, Owner.Rival);
            }
        }

        private void HandleLotUpgradeRequested(string lotId)
        {
            TryUpgradeLot(lotId);
        }

        private void HandleTick(int tickNumber)
        {
            // Lot income is now handled by RestaurantSystem to unify floating text
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Get the owner of a lot.
        /// </summary>
        public Owner GetOwner(string lotId)
        {
            return _lotOwnership.TryGetValue(lotId, out Owner owner) ? owner : Owner.None;
        }

        /// <summary>
        /// Get a lot definition by ID.
        /// </summary>
        public CityLotDefinition GetLot(string lotId)
        {
            return _allLots.Find(lot => lot.LotId == lotId);
        }

        /// <summary>
        /// ILotRegistry: true when a lot with this id exists in the city.
        /// </summary>
        public bool LotExists(string lotId)
        {
            return GetLot(lotId) != null;
        }

        /// <summary>
        /// ILotRegistry: per-tick income for the given lot at the given tier.
        /// Returns 0 if the lot is unknown so callers do not need a null check.
        /// </summary>
        public float GetIncomeAtTier(string lotId, int tier)
        {
            var lot = GetLot(lotId);
            return lot != null ? lot.GetIncomeAtTier(tier) : 0f;
        }

        /// <summary>
        /// Get all lots available for purchase.
        /// </summary>
        public List<CityLotDefinition> GetAvailableLots()
        {
            var available = new List<CityLotDefinition>();
            foreach (var lot in _allLots)
            {
                if (GetOwner(lot.LotId) == Owner.None)
                {
                    available.Add(lot);
                }
            }
            return available;
        }

        /// <summary>
        /// Try to purchase a lot for the player. Resolves cost based on current owner:
        /// None -> BaseCost. Rival -> BaseCost * RivalBuyoutMultiplier (tier still resets to T1).
        /// Player-owned lots cannot be repurchased.
        /// </summary>
        /// <returns>True if purchase succeeded</returns>
        public bool TryPurchaseLot(string lotId, int currentTick)
        {
            var lot = GetLot(lotId);
            if (lot == null)
            {
                Debug.LogWarning($"[CityManager] Lot {lotId} not found");
                return false;
            }

            Owner currentOwner = GetOwner(lotId);
            if (currentOwner == Owner.Player)
            {
                Debug.Log($"[CityManager] Lot {lotId} already owned by player");
                return false;
            }

            float cost = ResolvePurchaseCost(lot, currentOwner);

            if (_currency == null || !_currency.TrySpendChecking(cost, $"Purchase lot: {lot.DisplayName}"))
            {
                Debug.Log($"[CityManager] Cannot afford lot {lotId}. Cost: ${cost:F0}");
                return false;
            }

            // Purchase successful. Tier resets to T1 regardless of prior state.
            _lotTier[lotId] = _tierOnFreshPurchase;
            // Record the actual paid amount (3x markup when buying from rival)
            // for the conservative Total Net Worth formula.
            _acquisitionCost[lotId] = cost;
            SetOwner(lotId, Owner.Player, currentTick);
            GameEvents.RaiseLotTierChanged(lotId, _tierOnFreshPurchase);
            return true;
        }

        /// <summary>
        /// Sum of actual paid amounts across all currently player-owned lots.
        /// Excludes lots returned to "for sale" (entries are cleared on release).
        /// Includes the starter lot once it has been seeded.
        /// Feeds NetWorthService Total Net Worth.
        /// </summary>
        public float OwnedLotsAcquisitionTotal
        {
            get
            {
                float total = 0f;
                foreach (var kv in _acquisitionCost)
                {
                    if (GetOwner(kv.Key) == Owner.Player)
                    {
                        total += kv.Value;
                    }
                }
                return total;
            }
        }

        /// <summary>
        /// True when the rival has reached MAX_RIVAL_LOTS and may not buy more.
        /// RivalAI checks this before each purchase decision.
        /// </summary>
        public bool RivalAtSoftCap => RivalLotCount >= MAX_RIVAL_LOTS;

        /// <summary>
        /// Bankruptcy soft-reset hook. Releases every player-owned non-starter
        /// lot back to "for sale" and forces the starter lot down to T1
        /// dilapidated. Per-lot OnLotOwnershipChanged events are suppressed and
        /// replaced with a single OnLotsBatchReset(string[] lotIds) so subscribers
        /// (UI, AI, persistence) handle the wipe in one pass without frame stutter.
        /// </summary>
        public void BatchResetPlayerLots()
        {
            if (_allLots == null) return;

            string starterId = PlayerStarterLotId;
            var releasedIds = new List<string>();

            foreach (var lot in _allLots)
            {
                string lotId = lot.LotId;
                if (GetOwner(lotId) != Owner.Player) continue;

                if (lotId == starterId)
                {
                    // Starter stays owned but is forced to T1 dilapidated.
                    _lotTier[lotId] = _tierOnFreshPurchase;
                    GameEvents.RaiseLotTierChanged(lotId, _tierOnFreshPurchase);
                }
                else
                {
                    _lotOwnership[lotId] = Owner.None;
                    _lotTier.Remove(lotId);
                    _purchaseTick.Remove(lotId);
                    _acquisitionCost.Remove(lotId);
                    releasedIds.Add(lotId);
                }
            }

            if (releasedIds.Count > 0)
            {
                GameEvents.RaiseLotsBatchReset(releasedIds.ToArray());
            }
        }

        /// <summary>
        /// Compute the purchase price the player owes for a lot given its current owner.
        /// Public for test access and for UI display.
        /// </summary>
        public float ResolvePurchaseCost(CityLotDefinition lot, Owner currentOwner)
        {
            if (lot == null) return 0f;
            return currentOwner == Owner.Rival
                ? lot.BaseCost * lot.RivalBuyoutMultiplier
                : lot.BaseCost;
        }

        /// <summary>
        /// Convenience overload: resolve cost by lotId + current owner from state.
        /// </summary>
        public float ResolvePurchaseCost(string lotId)
        {
            return ResolvePurchaseCost(GetLot(lotId), GetOwner(lotId));
        }

        /// <summary>
        /// Try to upgrade a player-owned lot to the next tier.
        /// Rejects if not player-owned, already at max tier, or insufficient funds.
        /// </summary>
        /// <returns>True if upgrade succeeded</returns>
        public bool TryUpgradeLot(string lotId)
        {
            var lot = GetLot(lotId);
            if (lot == null)
            {
                Debug.LogWarning($"[CityManager] Upgrade rejected: lot {lotId} not found");
                return false;
            }

            if (GetOwner(lotId) != Owner.Player)
            {
                Debug.Log($"[CityManager] Upgrade rejected: {lotId} not player-owned");
                return false;
            }

            int currentTier = GetTier(lotId);
            if (currentTier >= 3)
            {
                Debug.Log($"[CityManager] Upgrade rejected: {lotId} already at max tier");
                return false;
            }

            int nextTier = currentTier + 1;
            float cost = nextTier == 2 ? lot.Tier2UpgradeCost : lot.Tier3UpgradeCost;

            if (_currency == null || !_currency.TrySpendChecking(cost, $"Upgrade {lot.DisplayName} to T{nextTier}"))
            {
                Debug.Log($"[CityManager] Upgrade rejected: cannot afford ${cost:F0} for {lotId}");
                return false;
            }

            _lotTier[lotId] = nextTier;
            GameEvents.RaiseLotTierChanged(lotId, nextTier);
            return true;
        }

        /// <summary>
        /// Try to upgrade a rival-owned lot to the next tier. Sibling to
        /// TryUpgradeLot but: requires Owner.Rival, does NOT touch the player
        /// currency seam (rival uses its own wallet, deducted by RivalAI).
        /// Reports the cost back via the out parameter so the caller can
        /// debit the rival's money. Raises both OnLotTierChanged (for visuals)
        /// and OnRivalUpgradedLot (for decision logging).
        /// </summary>
        /// <returns>True if upgrade succeeded</returns>
        public bool TryRivalUpgradeLot(string lotId, out float costSpent)
        {
            costSpent = 0f;

            var lot = GetLot(lotId);
            if (lot == null)
            {
                return false;
            }

            if (GetOwner(lotId) != Owner.Rival)
            {
                return false;
            }

            int currentTier = GetTier(lotId);
            if (currentTier >= 3)
            {
                return false;
            }

            int nextTier = currentTier + 1;
            costSpent = nextTier == 2 ? lot.Tier2UpgradeCost : lot.Tier3UpgradeCost;

            _lotTier[lotId] = nextTier;
            GameEvents.RaiseLotTierChanged(lotId, nextTier);
            GameEvents.RaiseRivalUpgradedLot(lotId, nextTier);
            return true;
        }

        /// <summary>
        /// Get the current tier of a lot (1..3), or 0 if not yet owned.
        /// </summary>
        public int GetTier(string lotId)
        {
            return _lotTier.TryGetValue(lotId, out int tier) ? tier : 0;
        }

        /// <summary>
        /// Purchase a lot for the rival (no currency check - rival has own economy).
        /// Rival-purchased lots start at T1 like the player's fresh purchases.
        /// Enforces MAX_RIVAL_LOTS soft cap so the player is always guaranteed
        /// at least (TotalLots - MAX_RIVAL_LOTS) lots available or owned.
        /// </summary>
        /// <returns>True if purchase succeeded</returns>
        public bool RivalPurchaseLot(string lotId, int currentTick)
        {
            var lot = GetLot(lotId);
            if (lot == null || GetOwner(lotId) != Owner.None)
            {
                return false;
            }

            // Soft cap: rival cannot exceed MAX_RIVAL_LOTS even if RivalAI requests it.
            if (RivalAtSoftCap)
            {
                return false;
            }

            _lotTier[lotId] = _tierOnFreshPurchase;
            SetOwner(lotId, Owner.Rival, currentTick);
            GameEvents.RaiseLotTierChanged(lotId, _tierOnFreshPurchase);
            return true;
        }

        /// <summary>
        /// Get the tick (day) a lot was purchased, or -1 if not purchased.
        /// Used by LearningReflectionBuilder for end-game insights.
        /// </summary>
        public int GetPurchaseTick(string lotId)
        {
            return _purchaseTick.TryGetValue(lotId, out int tick) ? tick : -1;
        }

        /// <summary>
        /// Get game progress (0 to 1) based on lots owned.
        /// </summary>
        public float GetGameProgress()
        {
            return (float)(PlayerLotCount + RivalLotCount) / TotalLots;
        }

        /// <summary>
        /// Get summary for UI.
        /// </summary>
        public string GetCitySummary()
        {
            return $"City Status:\n" +
                   $"• Your lots: {PlayerLotCount}\n" +
                   $"• Rival's lots: {RivalLotCount}\n" +
                   $"• Available: {AvailableLotCount}\n" +
                   $"• Your lot income: ${PlayerLotIncomeBonus:F0} per tick";
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Restore lot ownership and tiers from a saved DTO.
        /// Sets dictionaries directly (bypasses SetOwner to avoid
        /// win-condition checks during restore). Fires per-lot events so
        /// world-canvas + HUD subscribers refresh; UI components that prefer
        /// "rebuild once at end" listen to OnSaveRestored instead.
        /// ADVISORY: contains loops, but runs once at restore.
        /// Public so EditMode tests can call directly without raising the event.
        /// </summary>
        public void Hydrate(GamePlayerStateDTO dto)
        {
            if (dto == null) return;

            ResetOwnership();

            if (dto.lots_owned != null)
            {
                for (int i = 0; i < dto.lots_owned.Length; i++)
                {
                    _lotOwnership[dto.lots_owned[i]] = Owner.Player;
                    GameEvents.RaiseLotPurchased(dto.lots_owned[i], Owner.Player);
                }
            }

            if (dto.rival_lots_owned != null)
            {
                for (int i = 0; i < dto.rival_lots_owned.Length; i++)
                {
                    _lotOwnership[dto.rival_lots_owned[i]] = Owner.Rival;
                    GameEvents.RaiseLotPurchased(dto.rival_lots_owned[i], Owner.Rival);
                }
            }

            if (dto.franchise_levels != null)
            {
                for (int i = 0; i < dto.franchise_levels.Length; i++)
                {
                    var fl = dto.franchise_levels[i];
                    if (fl == null) continue;
                    _lotTier[fl.lot_id] = fl.tier;
                    GameEvents.RaiseLotTierChanged(fl.lot_id, fl.tier);
                }
            }

            if (dto.acquisition_costs != null)
            {
                for (int i = 0; i < dto.acquisition_costs.Length; i++)
                {
                    var ac = dto.acquisition_costs[i];
                    if (ac == null || string.IsNullOrEmpty(ac.lot_id)) continue;
                    if (!LotExists(ac.lot_id)) continue;
                    _acquisitionCost[ac.lot_id] = ac.cost;
                }
            }
        }

        /// <summary>
        /// Snapshot of per-lot acquisition costs for the autosave DTO. One entry
        /// per lot present in _acquisitionCost regardless of current owner so the
        /// dictionary round-trips faithfully. GameStateDTOBuilder calls this.
        /// </summary>
        public AcquisitionCostEntry[] GetAcquisitionCostEntries()
        {
            if (_acquisitionCost.Count == 0) return new AcquisitionCostEntry[0];
            var entries = new AcquisitionCostEntry[_acquisitionCost.Count];
            int i = 0;
            foreach (var kv in _acquisitionCost)
            {
                entries[i++] = new AcquisitionCostEntry { lot_id = kv.Key, cost = kv.Value };
            }
            return entries;
        }

        /// <summary>
        /// Re-emits OnLotPurchased + OnLotTierChanged for every currently-owned
        /// lot (player and rival). Mirrors Hydrate's event set exactly. Used by
        /// Phase 2 save-restore so visual subscribers that spawned after Phase 1
        /// fired still paint correctly. OnLotOwnershipChanged is intentionally
        /// not re-emitted: live-transition subscribers (DailyIncomeAccumulator
        /// bucket lifecycle) own their own restore path (Hydrate on Phase 1).
        /// </summary>
        public void RaiseAllOwnedLotEvents()
        {
            foreach (var kv in _lotOwnership)
            {
                if (kv.Value == Owner.None) continue;
                GameEvents.RaiseLotPurchased(kv.Key, kv.Value);
                if (_lotTier.TryGetValue(kv.Key, out int tier))
                {
                    GameEvents.RaiseLotTierChanged(kv.Key, tier);
                }
            }
        }

        private void ResetOwnership()
        {
            // Capture prior state so we can notify listeners of Player/Rival -> None
            // transitions (e.g. DailyIncomeAccumulator removing orphan buckets on restart).
            var priorOwners = new Dictionary<string, Owner>(_lotOwnership);

            _lotOwnership.Clear();
            _purchaseTick.Clear();
            _lotTier.Clear();
            _acquisitionCost.Clear();

            foreach (var lot in _allLots)
            {
                _lotOwnership[lot.LotId] = Owner.None;
                if (priorOwners.TryGetValue(lot.LotId, out Owner prev) && prev != Owner.None)
                {
                    GameEvents.RaiseLotOwnershipChanged(lot.LotId, prev, Owner.None);
                }
            }
        }

        private void SetOwner(string lotId, Owner owner, int tick)
        {
            Owner prevOwner = GetOwner(lotId);
            _lotOwnership[lotId] = owner;
            _purchaseTick[lotId] = tick;

            var lot = GetLot(lotId);

            if (_logPurchases)
            {
                Debug.Log($"[CityManager] {owner} purchased {lot.DisplayName} for ${lot.BaseCost:F0}");
            }

            GameEvents.RaiseLotPurchased(lotId, owner);
            GameEvents.RaiseLotOwnershipChanged(lotId, prevOwner, owner);

            // Win/lose-by-lot-count logic removed in the Life Goals revision.
            // The hard end is now retirement (age 65) via LifespanController +
            // RetirementEvaluator. Bankruptcy is a soft mid-life reset.
        }

        private int CountLotsOwnedBy(Owner owner)
        {
            int count = 0;
            foreach (var kvp in _lotOwnership)
            {
                if (kvp.Value == owner)
                    count++;
            }
            return count;
        }
    }
}
