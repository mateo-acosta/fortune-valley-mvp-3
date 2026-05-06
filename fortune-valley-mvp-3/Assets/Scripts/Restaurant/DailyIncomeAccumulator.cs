using System;
using System.Collections.Generic;
using UnityEngine;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Interfaces;

namespace FortuneValley.Core
{
    /// <summary>
    /// Per-building income accumulator that pays out automatically at day-end.
    ///
    /// Each tick, every accumulator's DailyPayout grows by its CachedPerTickRate
    /// (lazily filled from ComputeDayRate when RateDirty). On OnDayEnd, every
    /// accumulator is force-collected through the existing
    /// IncomeCollectionController pipeline (RaiseIncomeCollectRequested with
    /// CollectReason.DayEnd), which deposits to checking, fires the floating
    /// "+$X" feedback, raises OnIncomeCollected (the trigger for the per-building
    /// flash animation on BuildingCollectButton), and asks AutoSaveController to
    /// save (debounced, so multiple buckets in one day-end collapse into one save).
    ///
    /// Mid-day lot purchases and tier upgrades pro-rate naturally: per-tick
    /// accumulation only counts ticks since the bucket existed at the current
    /// rate, and rate changes invalidate the cache via MarkAllRatesDirty.
    ///
    /// Migration: legacy saves (Clash-of-Clans tap-to-collect model) carry a
    /// non-zero pending DailyPayout into the new bucket. The first OnDayEnd
    /// after load deposits that carried amount alongside the new day's
    /// accumulation in a single deposit. No special migration code needed.
    /// </summary>
    public class DailyIncomeAccumulator : MonoBehaviour, IBankruptcyResettable
    {
        public const string RestaurantBuildingId = "restaurant";

        [Header("Dependencies")]
        [SerializeField] private CityManager _cityManager;
        [SerializeField] private RestaurantSystem _restaurantSystem;
        [SerializeField] private TimeManager _timeManager;

        private ILotRegistry _lotRegistryOverride;
        private ITickClock _tickClockOverride;

        private readonly Dictionary<string, IncomeAccumulator> _accumulators = new Dictionary<string, IncomeAccumulator>();
        private readonly HashSet<string> _warnedIds = new HashSet<string>();
        private readonly List<string> _tickKeysScratch = new List<string>();

        private bool _totalDailyDirty = true;
        private float _lastTotalDaily = -1f;

        public IReadOnlyDictionary<string, IncomeAccumulator> Accumulators => _accumulators;

        private ILotRegistry LotRegistry => _lotRegistryOverride ?? _cityManager;
        private ITickClock Clock => _tickClockOverride ?? _timeManager;

        /// <summary>
        /// Test seam: inject stub dependencies before driving the service.
        /// Production leaves this alone and falls through to SerializeField refs.
        /// </summary>
        public void Initialize(ILotRegistry lotRegistry, ITickClock tickClock)
        {
            _lotRegistryOverride = lotRegistry;
            _tickClockOverride = tickClock;
        }

        private void OnEnable()
        {
            GameEvents.OnTick += HandleTick;
            GameEvents.OnDayEnd += HandleDayEnd;
            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnLotOwnershipChanged += HandleLotOwnershipChanged;
            GameEvents.OnLotTierChanged += HandleLotTierChanged;
            GameEvents.OnRestaurantUpgraded += HandleRestaurantUpgraded;
            GameEvents.OnSaveStateLoaded += HandleSaveStateLoaded;
            GameEvents.OnIncomePendingQuery += HandleQuery;
        }

        private void OnDisable()
        {
            GameEvents.OnTick -= HandleTick;
            GameEvents.OnDayEnd -= HandleDayEnd;
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnLotOwnershipChanged -= HandleLotOwnershipChanged;
            GameEvents.OnLotTierChanged -= HandleLotTierChanged;
            GameEvents.OnRestaurantUpgraded -= HandleRestaurantUpgraded;
            GameEvents.OnSaveStateLoaded -= HandleSaveStateLoaded;
            GameEvents.OnIncomePendingQuery -= HandleQuery;
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC API
        // ═══════════════════════════════════════════════════════════════

        public void EnsureBucket(string buildingId)
        {
            if (string.IsNullOrEmpty(buildingId)) return;
            if (_accumulators.ContainsKey(buildingId)) return;
            if (!ShouldHaveBucket(buildingId)) return;

            _accumulators[buildingId] = new IncomeAccumulator();
            StartNewDay(buildingId);
            _totalDailyDirty = true;
        }

        /// <summary>
        /// Resets the bucket for a fresh day. With automatic deposit, there is
        /// no countdown to arm; this just zeroes accumulation and republishes
        /// state. Called from EnsureBucket and after a day-end collect (via
        /// TryCollect for the OwnershipLost path).
        /// </summary>
        public void StartNewDay(string buildingId)
        {
            if (!_accumulators.TryGetValue(buildingId, out var acc))
            {
                WarnOnce(buildingId, nameof(StartNewDay));
                return;
            }

            acc.DailyPayout = 0f;
            acc.IsReady = false;
            acc.TicksRemaining = 0;
            acc.RateDirty = true;
            WriteAndRaise(buildingId, acc);
        }

        public bool TryCollect(string buildingId, out float amount)
        {
            amount = 0f;
            if (!_accumulators.TryGetValue(buildingId, out var acc))
            {
                WarnOnce(buildingId, nameof(TryCollect));
                return false;
            }
            if (!acc.IsReady || acc.DailyPayout <= 0f) return false;

            amount = acc.DailyPayout;
            acc.IsReady = false;
            acc.DailyPayout = 0f;
            acc.TicksRemaining = 0;
            WriteAndRaise(buildingId, acc);
            return true;
        }

        /// <summary>
        /// Ownership-loss path. If the bucket has accumulated income, leave it
        /// ready so IncomeCollectionController.HandleCollectRequested pays the
        /// final coin via the standard pipeline. Otherwise zero out (mid-day
        /// progress with no balance is forfeited cleanly).
        /// </summary>
        public void PrepareLostLotCollect(string buildingId)
        {
            if (!_accumulators.TryGetValue(buildingId, out var acc))
            {
                WarnOnce(buildingId, nameof(PrepareLostLotCollect));
                return;
            }
            if (acc.DailyPayout > 0f)
            {
                acc.IsReady = true;
                WriteAndRaise(buildingId, acc);
                return;
            }

            acc.DailyPayout = 0f;
            acc.TicksRemaining = 0;
            acc.IsReady = false;
            WriteAndRaise(buildingId, acc);
        }

        public void RemoveBucket(string buildingId)
        {
            if (_accumulators.Remove(buildingId))
            {
                GameEvents.RaiseCoinStateChanged(buildingId, 0f, 0f, false);
                _totalDailyDirty = true;
            }
        }

        public void Snapshot(GamePlayerStateDTO dto)
        {
            if (dto == null) return;
            var entries = new PendingIncomeEntryDTO[_accumulators.Count];
            int i = 0;
            foreach (var kvp in _accumulators)
            {
                entries[i++] = new PendingIncomeEntryDTO
                {
                    building_id = kvp.Key,
                    daily_payout = kvp.Value.DailyPayout,
                    ticks_remaining = kvp.Value.TicksRemaining,
                    is_ready = kvp.Value.IsReady,
                };
            }
            dto.pending_incomes = entries;
            if (dto.schema_version < 1) dto.schema_version = 1;
        }

        /// <summary>
        /// Restores accumulators from a save. Legacy carry-over: any non-zero
        /// daily_payout from old saves becomes the starting DailyPayout, and
        /// the next OnDayEnd deposits it alongside that day's fresh accumulation
        /// in a single event. RateDirty is set so the per-tick rate recomputes
        /// against the current world state.
        /// </summary>
        public void Hydrate(GamePlayerStateDTO dto)
        {
            _accumulators.Clear();
            _warnedIds.Clear();

            if (dto == null)
            {
                _totalDailyDirty = true;
                return;
            }

            bool isLegacy = dto.schema_version < 1;

            if (dto.pending_incomes != null)
            {
                var dropped = new List<string>();
                foreach (var entry in dto.pending_incomes)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.building_id)) continue;

                    if (!ShouldHaveBucket(entry.building_id))
                    {
                        dropped.Add(entry.building_id);
                        continue;
                    }

                    if (isLegacy)
                    {
                        _accumulators[entry.building_id] = new IncomeAccumulator();
                    }
                    else
                    {
                        _accumulators[entry.building_id] = new IncomeAccumulator
                        {
                            DailyPayout = entry.daily_payout,
                            TicksRemaining = Math.Max(0, entry.ticks_remaining),
                            IsReady = entry.is_ready,
                            RateDirty = true,
                        };
                    }
                }

                if (dropped.Count > 0)
                {
                    Debug.LogWarning($"[DailyIncomeAccumulator] Dropped {dropped.Count} orphan bucket(s) during hydrate: {string.Join(", ", dropped)}");
                }
            }

            if (isLegacy)
            {
                var ids = new List<string>(_accumulators.Keys);
                foreach (var id in ids) StartNewDay(id);
            }
            else
            {
                var ids = new List<string>(_accumulators.Keys);
                foreach (var id in ids)
                {
                    WriteAndRaise(id, _accumulators[id]);
                }
            }

            _totalDailyDirty = true;
        }

        /// <summary>
        /// Lazy query for the current accumulated amount on a specific bucket.
        /// Used by BuildingCollectButton when it becomes visible (hover or
        /// flash) to avoid emitting OnCoinStateChanged on every tick.
        /// </summary>
        public float GetCurrentAccumulated(string buildingId)
        {
            if (string.IsNullOrEmpty(buildingId)) return 0f;
            if (!_accumulators.TryGetValue(buildingId, out var acc)) return 0f;
            return acc.DailyPayout;
        }

        // ═══════════════════════════════════════════════════════════════
        // EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════════

        private void HandleSaveStateLoaded(GamePlayerStateDTO dto) => Hydrate(dto);

        private void HandleGameStart()
        {
            _warnedIds.Clear();
            EnsureBucket(RestaurantBuildingId);
        }

        /// <summary>
        /// IBankruptcyResettable. Soft reset: drop all pending income buckets
        /// (lots are also being released back to "for sale"). Re-seed the
        /// starter restaurant bucket so income flow resumes immediately.
        /// </summary>
        public void OnBankruptcyReset()
        {
            _accumulators.Clear();
            _warnedIds.Clear();
            _tickKeysScratch.Clear();
            _totalDailyDirty = true;
            _lastTotalDaily = -1f;
            EnsureBucket(RestaurantBuildingId);
        }

        private void HandleTick(int tickNumber)
        {
            int ticksPerDay = Clock != null ? Clock.TicksPerDay : 0;
            if (ticksPerDay <= 0) return;

            // Snapshot keys into the pre-allocated scratch list so handlers
            // mutating _accumulators mid-tick cannot invalidate the iterator.
            _tickKeysScratch.Clear();
            foreach (var k in _accumulators.Keys) _tickKeysScratch.Add(k);

            for (int i = 0; i < _tickKeysScratch.Count; i++)
            {
                AccumulateTick(_tickKeysScratch[i], ticksPerDay);
            }
        }

        private void AccumulateTick(string buildingId, int ticksPerDay)
        {
            if (!_accumulators.TryGetValue(buildingId, out var acc)) return;
            if (acc.IsReady) return; // OwnershipLost path is awaiting collection

            if (acc.RateDirty)
            {
                float dayRate = ComputeDayRate(buildingId);
                acc.CachedPerTickRate = ticksPerDay > 0 ? dayRate / ticksPerDay : 0f;
                acc.RateDirty = false;
            }

            acc.DailyPayout += acc.CachedPerTickRate;
            // Intentionally no WriteAndRaise here: the per-tick label update is
            // dropped to avoid TMP rebuild churn; BuildingCollectButton lazily
            // queries GetCurrentAccumulated when it becomes visible.
        }

        /// <summary>
        /// MUST reuse the pre-allocated _tickKeysScratch list; do not allocate
        /// a new List per day-end (the system runs in WebGL with mobile-class
        /// GC budgets).
        /// </summary>
        private void HandleDayEnd(int day)
        {
            _tickKeysScratch.Clear();
            foreach (var k in _accumulators.Keys) _tickKeysScratch.Add(k);

            for (int i = 0; i < _tickKeysScratch.Count; i++)
            {
                var buildingId = _tickKeysScratch[i];
                if (!_accumulators.TryGetValue(buildingId, out var acc)) continue;
                if (acc.DailyPayout <= 0f) continue;

                acc.IsReady = true;
                WriteAndRaise(buildingId, acc);
                GameEvents.RaiseIncomeCollectRequested(buildingId, CollectReason.DayEnd);
            }

            // Tier or level changes during the day could shift tomorrow's rate.
            _totalDailyDirty = true;
        }

        private void HandleLotOwnershipChanged(string lotId, Owner previousOwner, Owner newOwner)
        {
            bool isStarterLot = LotRegistry != null && lotId == LotRegistry.PlayerStarterLotId;

            if (newOwner == Owner.Player)
            {
                EnsureBucket(lotId);
                if (isStarterLot)
                {
                    // Starter folds restaurant base; drop the standalone bucket.
                    RemoveBucket(RestaurantBuildingId);
                }
                MarkAllRatesDirty();
                return;
            }

            if (previousOwner == Owner.Player)
            {
                PrepareLostLotCollect(lotId);
                GameEvents.RaiseIncomeCollectRequested(lotId, CollectReason.OwnershipLost);
                RemoveBucket(lotId);

                if (isStarterLot)
                {
                    EnsureBucket(RestaurantBuildingId);
                }
                MarkAllRatesDirty();
            }
        }

        private void HandleLotTierChanged(string lotId, int newTier)
        {
            MarkAllRatesDirty();
        }

        private void HandleRestaurantUpgraded(int level)
        {
            MarkAllRatesDirty();
        }

        private void HandleQuery(string buildingId)
        {
            if (string.IsNullOrEmpty(buildingId)) return;
            if (!_accumulators.TryGetValue(buildingId, out var acc)) return;
            WriteAndRaise(buildingId, acc);
        }

        // ═══════════════════════════════════════════════════════════════
        // TOTAL DAILY INCOME (HUD)
        // ═══════════════════════════════════════════════════════════════

        private void LateUpdate()
        {
            if (!_totalDailyDirty) return;
            _totalDailyDirty = false;

            float total = 0f;
            foreach (var kvp in _accumulators)
            {
                total += ComputeDayRate(kvp.Key);
            }

            int rounded = Mathf.FloorToInt(total);
            int lastRounded = Mathf.FloorToInt(_lastTotalDaily);
            if (rounded == lastRounded && _lastTotalDaily >= 0f) return;

            _lastTotalDaily = total;
            GameEvents.RaiseTotalDailyIncomeChanged(total);
        }

        private void MarkAllRatesDirty()
        {
            // Re-emit per-bucket so BuildingCollectButton's cached daily rate
            // (used for the hover and post-flash labels) stays current after
            // tier upgrades / restaurant level-ups. Use the scratch list so
            // a subscriber that mutates _accumulators can't invalidate the
            // iterator.
            _tickKeysScratch.Clear();
            foreach (var k in _accumulators.Keys) _tickKeysScratch.Add(k);
            for (int i = 0; i < _tickKeysScratch.Count; i++)
            {
                if (!_accumulators.TryGetValue(_tickKeysScratch[i], out var acc)) continue;
                acc.RateDirty = true;
                WriteAndRaise(_tickKeysScratch[i], acc);
            }
            _totalDailyDirty = true;
        }

        // ═══════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════

        // OnCoinStateChanged carries the per-day RATE (full daily rate), not
        // the running accumulated balance. The button uses it for the hover
        // and post-flash label "+$X/day". The running accumulated balance is
        // queried lazily via GetCurrentAccumulated when the button needs it.
        private void WriteAndRaise(string buildingId, IncomeAccumulator acc)
        {
            int ticksPerDay = Clock != null ? Clock.TicksPerDay : 0;
            float progress01 = ticksPerDay > 0 ? (float)acc.TicksRemaining / ticksPerDay : 0f;
            float dailyRate = ComputeDayRate(buildingId);
            GameEvents.RaiseCoinStateChanged(buildingId, dailyRate, progress01, acc.IsReady);
        }

        private bool ShouldHaveBucket(string buildingId)
        {
            if (buildingId == RestaurantBuildingId)
            {
                if (LotRegistry == null) return true;
                var starter = LotRegistry.PlayerStarterLotId;
                if (string.IsNullOrEmpty(starter)) return true;
                return LotRegistry.GetOwner(starter) != Owner.Player;
            }
            return LotRegistry != null && LotRegistry.GetOwner(buildingId) == Owner.Player;
        }

        private float ComputeDayRate(string buildingId)
        {
            int ticksPerDay = Clock != null ? Clock.TicksPerDay : 0;
            if (ticksPerDay <= 0) return 0f;

            if (buildingId == RestaurantBuildingId)
            {
                if (LotRegistry != null)
                {
                    var starter = LotRegistry.PlayerStarterLotId;
                    if (!string.IsNullOrEmpty(starter) && LotRegistry.GetOwner(starter) == Owner.Player) return 0f;
                }
                return _restaurantSystem != null ? _restaurantSystem.IncomePerTick * ticksPerDay : 0f;
            }

            if (LotRegistry == null) return 0f;
            if (!LotRegistry.LotExists(buildingId)) return 0f;

            int tier = LotRegistry.GetTier(buildingId);
            float perTick = LotRegistry.GetIncomeAtTier(buildingId, tier);

            // Starter lot folds the restaurant's level-scaled base income so
            // both streams pay out from one coin.
            if (_restaurantSystem != null && buildingId == LotRegistry.PlayerStarterLotId)
            {
                perTick += _restaurantSystem.IncomePerTick;
            }
            return perTick * ticksPerDay;
        }

        private void WarnOnce(string buildingId, string opName)
        {
            if (_warnedIds.Contains(buildingId)) return;
            _warnedIds.Add(buildingId);
            Debug.LogWarning($"[DailyIncomeAccumulator] Unknown buildingId '{buildingId}' in {opName}");
        }
    }
}
