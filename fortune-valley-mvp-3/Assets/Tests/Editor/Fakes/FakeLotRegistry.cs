using System.Collections.Generic;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Interfaces;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Hand-rolled ILotRegistry stub for editor tests. Owns a minimal lot
    /// table indexed by id; callers register lots and mutate ownership/tier
    /// through explicit helpers.
    /// </summary>
    internal sealed class FakeLotRegistry : ILotRegistry
    {
        private readonly Dictionary<string, LotRecord> _lots = new Dictionary<string, LotRecord>();
        private string _starterLotId;

        public string PlayerStarterLotId => _starterLotId;

        public void SetStarterLotId(string id) => _starterLotId = id;

        public void RegisterLot(string id, Owner owner, int tier, float perTickAtTier1)
        {
            _lots[id] = new LotRecord
            {
                Owner = owner,
                Tier = tier,
                RatePerTier = new Dictionary<int, float> { { tier, perTickAtTier1 } }
            };
        }

        public void SetOwner(string id, Owner owner)
        {
            if (_lots.TryGetValue(id, out var r)) { r.Owner = owner; _lots[id] = r; }
        }

        public void UpgradeLotTier(string id, int newTier, float perTickAtNewTier)
        {
            if (!_lots.TryGetValue(id, out var r)) return;
            r.Tier = newTier;
            r.RatePerTier[newTier] = perTickAtNewTier;
            _lots[id] = r;
        }

        public Owner GetOwner(string lotId)
            => _lots.TryGetValue(lotId, out var r) ? r.Owner : Owner.None;

        public int GetTier(string lotId)
            => _lots.TryGetValue(lotId, out var r) ? r.Tier : 0;

        public bool LotExists(string lotId) => _lots.ContainsKey(lotId);

        public float GetIncomeAtTier(string lotId, int tier)
        {
            if (!_lots.TryGetValue(lotId, out var r)) return 0f;
            return r.RatePerTier.TryGetValue(tier, out var rate) ? rate : 0f;
        }

        public IEnumerable<(string lotId, float income)> EnumeratePlayerLotIncomes()
        {
            foreach (var kv in _lots)
            {
                if (kv.Value.Owner == Owner.Player)
                {
                    yield return (kv.Key, GetIncomeAtTier(kv.Key, kv.Value.Tier));
                }
            }
        }

        private struct LotRecord
        {
            public Owner Owner;
            public int Tier;
            public Dictionary<int, float> RatePerTier;
        }
    }
}
