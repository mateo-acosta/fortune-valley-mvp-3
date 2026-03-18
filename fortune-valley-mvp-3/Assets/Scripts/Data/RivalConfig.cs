using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Configuration for the rival AI that competes to buy city lots.
    ///
    /// LEARNING DESIGN: The rival creates time pressure that forces
    /// meaningful trade-offs. Without a rival, there's no urgency to
    /// make financial decisions - students could just wait forever.
    /// </summary>
    [CreateAssetMenu(fileName = "RivalConfig", menuName = "Fortune Valley/Rival Config")]
    public class RivalConfig : ScriptableObject
    {
        // ═══════════════════════════════════════════════════════════════
        // RIVAL ECONOMY
        // ═══════════════════════════════════════════════════════════════

        [Header("Rival Economy")]
        [Tooltip("How much money the rival starts with")]
        [SerializeField] private float _startingMoney = 500f;

        [Tooltip("Income the rival earns per tick")]
        [SerializeField] private float _incomePerTick = 8f;

        // ═══════════════════════════════════════════════════════════════
        // PURCHASE BEHAVIOR
        // ═══════════════════════════════════════════════════════════════

        [Header("Purchase Behavior")]
        [Tooltip("Ticks between purchase attempts")]
        [SerializeField] private int _purchaseInterval = 60; // Every 60 days

        [Tooltip("Ticks before a purchase where player gets warned")]
        [SerializeField] private int _warningTicks = 30;

        [Tooltip("Rival only buys if they have this much buffer above lot cost")]
        [SerializeField] private float _purchaseBuffer = 100f;

        // ═══════════════════════════════════════════════════════════════
        // DIFFICULTY SCALING
        // ═══════════════════════════════════════════════════════════════

        [Header("Difficulty Scaling")]
        [Tooltip("Curve that scales aggression over game progress (0-1). Higher = buys faster.")]
        [SerializeField] private AnimationCurve _aggressionCurve = AnimationCurve.Linear(0f, 1f, 1f, 1.5f);

        [Tooltip("How game progress is measured: lots owned by rival / total lots")]
        [SerializeField] private bool _scaleByProgress = true;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public float StartingMoney => _startingMoney;
        public float IncomePerTick => _incomePerTick;
        public int PurchaseInterval => _purchaseInterval;
        public int WarningTicks => _warningTicks;
        public float PurchaseBuffer => _purchaseBuffer;

        /// <summary>
        /// Get the effective purchase interval based on game progress.
        /// As rival gets more aggressive, interval decreases.
        /// </summary>
        /// <param name="progress">Game progress from 0 (start) to 1 (near end)</param>
        public int GetEffectivePurchaseInterval(float progress)
        {
            if (!_scaleByProgress)
                return _purchaseInterval;

            float aggressionMultiplier = _aggressionCurve.Evaluate(progress);
            // Higher aggression = lower interval (buys faster)
            return Mathf.Max(10, Mathf.RoundToInt(_purchaseInterval / aggressionMultiplier));
        }

        /// <summary>
        /// Get explanation of what the rival is doing (for UI/learning).
        /// </summary>
        public string GetRivalExplanation()
        {
            return $"Your rival earns ${_incomePerTick:F0} per day.\n" +
                   $"They attempt to buy a lot every ~{_purchaseInterval} days.\n" +
                   $"As they get stronger, they'll buy faster!\n" +
                   $"You'll get a {_warningTicks}-day warning before they purchase.";
        }
    }
}
