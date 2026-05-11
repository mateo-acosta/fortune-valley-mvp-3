using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Configuration for credit score calculation.
    ///
    /// LEARNING DESIGN: Students see their credit score change based on
    /// real-world factors: payment history, utilization, and debt-to-income.
    /// This teaches responsible credit use.
    /// </summary>
    [CreateAssetMenu(fileName = "CreditScoringConfig", menuName = "Fortune Valley/Credit Scoring Config")]
    public class CreditScoringConfig : ScriptableObject
    {
        [Header("Score Range")]
        [Tooltip("Starting credit score for new players")]
        [SerializeField] private int _startingScore = 650;

        [Tooltip("Minimum possible credit score")]
        [SerializeField] private int _minScore = 300;

        [Tooltip("Maximum possible credit score")]
        [SerializeField] private int _maxScore = 850;

        [Header("Payment History")]
        [Tooltip("Points gained per on-time payment")]
        [SerializeField] private int _onTimePaymentBonus = 15;

        [Tooltip("Points lost per missed payment")]
        [SerializeField] private int _missedPaymentPenalty = 50;

        [Header("Utilization Thresholds")]
        [Tooltip("Utilization below this is 'good' (bonus points)")]
        [SerializeField] private float _lowUtilizationThreshold = 0.30f;

        [Tooltip("Points bonus for low utilization")]
        [SerializeField] private int _lowUtilizationBonus = 10;

        [Tooltip("Utilization above this is 'high' (penalty points)")]
        [SerializeField] private float _highUtilizationThreshold = 0.70f;

        [Tooltip("Points penalty for high utilization")]
        [SerializeField] private int _highUtilizationPenalty = 20;

        [Header("Debt-to-Income")]
        [Tooltip("DTI above this ratio is penalized")]
        [SerializeField] private float _highDtiThreshold = 0.40f;

        [Tooltip("Points penalty for high DTI")]
        [SerializeField] private int _highDtiPenalty = 15;

        // Read-only accessors
        public int StartingScore => _startingScore;
        public int MinScore => _minScore;
        public int MaxScore => _maxScore;
        public int OnTimePaymentBonus => _onTimePaymentBonus;
        public int MissedPaymentPenalty => _missedPaymentPenalty;
        public float LowUtilizationThreshold => _lowUtilizationThreshold;
        public int LowUtilizationBonus => _lowUtilizationBonus;
        public float HighUtilizationThreshold => _highUtilizationThreshold;
        public int HighUtilizationPenalty => _highUtilizationPenalty;
        public float HighDtiThreshold => _highDtiThreshold;
        public int HighDtiPenalty => _highDtiPenalty;
    }
}
