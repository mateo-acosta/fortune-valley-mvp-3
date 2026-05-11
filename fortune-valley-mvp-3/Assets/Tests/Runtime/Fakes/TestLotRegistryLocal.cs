using System.Collections.Generic;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Interfaces;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Minimal ILotRegistry stub for runtime tests. Always returns None/0 so
    /// Hydrate's ShouldHaveBucket path drops non-restaurant ids unless the
    /// test explicitly seeds the buckets dictionary via reflection.
    /// </summary>
    internal sealed class TestLotRegistryLocal : ILotRegistry
    {
        public string PlayerStarterLotId => null;
        public Owner GetOwner(string lotId) => Owner.None;
        public int GetTier(string lotId) => 0;
        public bool LotExists(string lotId) => false;
        public float GetIncomeAtTier(string lotId, int tier) => 0f;
        public IEnumerable<(string lotId, float income)> EnumeratePlayerLotIncomes()
            => System.Array.Empty<(string, float)>();
    }
}
