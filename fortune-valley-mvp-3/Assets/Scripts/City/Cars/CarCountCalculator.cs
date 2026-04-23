using System.Collections.Generic;

namespace FortuneValley.City.Cars
{
    /// <summary>
    /// Pure-C# calculator for ambient car count based on city tier progress.
    /// Kept out of CarSpawner so the MonoBehaviour stays free of arithmetic
    /// and collection state (per architecture guidelines).
    /// </summary>
    public class CarCountCalculator
    {
        private readonly Dictionary<string, int> _lotTiers = new Dictionary<string, int>();

        public void SetLotTier(string lotId, int tier)
        {
            if (string.IsNullOrEmpty(lotId)) return;
            _lotTiers[lotId] = tier;
        }

        public int ComputeTarget(int baseCount, float perTierMultiplier)
        {
            int sum = 0;
            foreach (var kv in _lotTiers) sum += kv.Value;
            int extra = UnityEngine.Mathf.FloorToInt(sum * perTierMultiplier);
            return baseCount + extra;
        }
    }
}
