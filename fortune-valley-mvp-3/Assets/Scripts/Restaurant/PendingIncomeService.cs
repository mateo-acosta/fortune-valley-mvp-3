using System;
using System.Collections.Generic;
using UnityEngine;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Interfaces;

namespace FortuneValley.Core
{
    /// <summary>
    /// Per-building coin state owner for the tap-to-collect loop.
    ///
    /// Daily-locked model: each bucket's DailyPayout is snapshotted at
    /// day-start from ComputeDayRate. Ticks drain TicksRemaining; at zero
    /// the bucket flips ready. Collecting deposits DailyPayout and
    /// immediately restarts the countdown with a freshly recomputed rate.
    /// Production stops while ready (Clash-of-Clans cap).
    ///
    /// Per-bucket countdowns drift from TimeManager.CurrentDay on purpose.
    /// Collecting restarts the local countdown, so each building follows
    /// its own day cycle and this service does not listen to OnDayEnd.
    /// </summary>
    public class PendingIncomeService : MonoBehaviour
    {
        public const string RestaurantBuildingId = "restaurant";

        [Header("Dependencies")]
        [SerializeField] private CityManager _cityManager;
        [SerializeField] private RestaurantSystem _restaurantSystem;
        [SerializeField] private TimeManager _timeManager;

        private ILotRegistry _lotRegistryOverride;
        private ITickClock _tickClockOverride;

        private readonly Dictionary<string, PendingBucket> _buckets = new Dictionary<string, PendingBucket>();
        private readonly HashSet<string> _warnedIds = new HashSet<string>();
        private readonly List<string> _tickKeysScratch = new List<string>();

        public IReadOnlyDictionary<string, PendingBucket> Buckets => _buckets;

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
            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnLotOwnershipChanged += HandleLotOwnershipChanged;
            GameEvents.OnSaveStateLoaded += HandleSaveStateLoaded;
            GameEvents.OnIncomePendingQuery += HandleQuery;
            GameEvents.OnIncomeCollected += HandleIncomeCollected;
        }

        private void OnDisable()
        {
            GameEvents.OnTick -= HandleTick;
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnLotOwnershipChanged -= HandleLotOwnershipChanged;
            GameEvents.OnSaveStateLoaded -= HandleSaveStateLoaded;
            GameEvents.OnIncomePendingQuery -= HandleQuery;
            GameEvents.OnIncomeCollected -= HandleIncomeCollected;
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC API
        // ═══════════════════════════════════════════════════════════════

        public void EnsureBucket(string buildingId)
        {
            if (string.IsNullOrEmpty(buildingId)) return;
            if (_buckets.ContainsKey(buildingId)) return;
            if (!ShouldHaveBucket(buildingId)) return;

            _buckets[buildingId] = new PendingBucket();
            StartNewDay(buildingId);
        }

        public void StartNewDay(string buildingId)
        {
            if (!_buckets.TryGetValue(buildingId, out var bucket))
            {
                WarnOnce(buildingId, nameof(StartNewDay));
                return;
            }
            if (bucket.IsReady) return; // production cap

            int ticksPerDay = Clock != null ? Clock.TicksPerDay : 0;
            if (ticksPerDay <= 0)
            {
                Debug.LogWarning($"[PendingIncomeService] ticksPerDay <= 0; cannot start day for '{buildingId}'");
                return;
            }

            bucket.DailyPayout = ComputeDayRate(buildingId);
            bucket.TicksRemaining = ticksPerDay;
            bucket.IsReady = false;
            WriteAndRaise(buildingId, bucket);
        }

        public bool TryCollect(string buildingId, out float amount)
        {
            amount = 0f;
            if (!_buckets.TryGetValue(buildingId, out var bucket))
            {
                WarnOnce(buildingId, nameof(TryCollect));
                return false;
            }
            if (!bucket.IsReady || bucket.DailyPayout <= 0f) return false;

            amount = bucket.DailyPayout;
            bucket.IsReady = false;
            bucket.DailyPayout = 0f;
            bucket.TicksRemaining = 0;
            WriteAndRaise(buildingId, bucket);

            // Immediately lock tomorrow's payout so the overlay refills
            // without the player having to wait for an external trigger.
            StartNewDay(buildingId);
            return true;
        }

        /// <summary>
        /// Ownership-loss path. If the bucket is already ready, leave it so
        /// IncomeCollectionController's TryCollect pays the final coin.
        /// Otherwise zero out (mid-day progress forfeits).
        /// </summary>
        public void PrepareLostLotCollect(string buildingId)
        {
            if (!_buckets.TryGetValue(buildingId, out var bucket))
            {
                WarnOnce(buildingId, nameof(PrepareLostLotCollect));
                return;
            }
            if (bucket.IsReady) return;

            bucket.DailyPayout = 0f;
            bucket.TicksRemaining = 0;
            bucket.IsReady = false;
            WriteAndRaise(buildingId, bucket);
        }

        public void RemoveBucket(string buildingId)
        {
            if (_buckets.Remove(buildingId))
            {
                GameEvents.RaiseCoinStateChanged(buildingId, 0f, 0f, false);
            }
        }

        public void Snapshot(GamePlayerStateDTO dto)
        {
            if (dto == null) return;
            var entries = new PendingIncomeEntryDTO[_buckets.Count];
            int i = 0;
            foreach (var kvp in _buckets)
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

        public void Hydrate(GamePlayerStateDTO dto)
        {
            _buckets.Clear();
            _warnedIds.Clear();

            if (dto == null) return;

            bool isLegacy = dto.schema_version < 1;

            if (dto.pending_incomes == null) return;

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
                    _buckets[entry.building_id] = new PendingBucket();
                }
                else
                {
                    _buckets[entry.building_id] = new PendingBucket
                    {
                        DailyPayout = entry.daily_payout,
                        TicksRemaining = Math.Max(0, entry.ticks_remaining),
                        IsReady = entry.is_ready,
                    };
                }
            }

            if (dropped.Count > 0)
            {
                Debug.LogWarning($"[PendingIncomeService] Dropped {dropped.Count} orphan bucket(s) during hydrate: {string.Join(", ", dropped)}");
            }

            if (isLegacy)
            {
                var ids = new List<string>(_buckets.Keys);
                foreach (var id in ids) StartNewDay(id);
            }
            else
            {
                var ids = new List<string>(_buckets.Keys);
                foreach (var id in ids)
                {
                    var b = _buckets[id];
                    int ticksPerDay = Clock != null ? Clock.TicksPerDay : 0;
                    float progress01 = ticksPerDay > 0 ? (float)b.TicksRemaining / ticksPerDay : 0f;
                    GameEvents.RaiseCoinStateChanged(id, b.DailyPayout, progress01, b.IsReady);
                }
            }
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

        private void HandleTick(int tickNumber)
        {
            // Snapshot keys into a pre-allocated scratch list so handlers
            // mutating _buckets mid-tick cannot invalidate the iterator.
            _tickKeysScratch.Clear();
            foreach (var k in _buckets.Keys) _tickKeysScratch.Add(k);

            for (int i = 0; i < _tickKeysScratch.Count; i++)
            {
                TickDrain(_tickKeysScratch[i]);
            }
        }

        private void TickDrain(string buildingId)
        {
            if (!_buckets.TryGetValue(buildingId, out var bucket)) return;
            if (bucket.IsReady) return;

            if (bucket.TicksRemaining <= 0)
            {
                bucket.TicksRemaining = 0;
                bucket.IsReady = true;
                WriteAndRaise(buildingId, bucket);
                return;
            }

            bucket.TicksRemaining--;
            if (bucket.TicksRemaining <= 0)
            {
                bucket.TicksRemaining = 0;
                bucket.IsReady = true;
            }
            WriteAndRaise(buildingId, bucket);
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
            }
        }

        private void HandleQuery(string buildingId)
        {
            if (string.IsNullOrEmpty(buildingId)) return;
            if (!_buckets.TryGetValue(buildingId, out var bucket)) return;

            int ticksPerDay = Clock != null ? Clock.TicksPerDay : 0;
            float progress01 = ticksPerDay > 0 ? (float)bucket.TicksRemaining / ticksPerDay : 0f;
            GameEvents.RaiseCoinStateChanged(buildingId, bucket.DailyPayout, progress01, bucket.IsReady);
        }

        private void HandleIncomeCollected(string buildingId, float amount)
        {
            // Belt-and-suspenders: TryCollect already restarts the countdown.
            // If some other path raises OnIncomeCollected, ensure restart.
            if (!_buckets.TryGetValue(buildingId, out var bucket)) return;
            if (bucket.IsReady) return;
            if (bucket.DailyPayout > 0f || bucket.TicksRemaining > 0) return;
            StartNewDay(buildingId);
        }

        // ═══════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════

        private void WriteAndRaise(string buildingId, PendingBucket bucket)
        {
            _buckets[buildingId] = bucket;
            int ticksPerDay = Clock != null ? Clock.TicksPerDay : 0;
            float progress01 = ticksPerDay > 0 ? (float)bucket.TicksRemaining / ticksPerDay : 0f;
            GameEvents.RaiseCoinStateChanged(buildingId, bucket.DailyPayout, progress01, bucket.IsReady);
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
            Debug.LogWarning($"[PendingIncomeService] Unknown buildingId '{buildingId}' in {opName}");
        }
    }
}
