using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Tunables for the QuestionMaster panel.
    /// </summary>
    [CreateAssetMenu(fileName = "QuestionConfig", menuName = "Fortune Valley/Question Config")]
    public class QuestionConfig : ScriptableObject
    {
        [Header("Reward")]
        [SerializeField] private float _baseReward = 25f;
        [SerializeField] private float _streakMultiplier = 1.15f;
        [SerializeField] private int _rewardRoundingStep = 5;

        [Header("Timing")]
        [SerializeField] private float _questionTimeSeconds = 15f;
        [SerializeField] private float _overlayDurationSeconds = 2f;

        public float BaseReward => _baseReward;
        public float StreakMultiplier => _streakMultiplier;
        public int RewardRoundingStep => _rewardRoundingStep;
        public float QuestionTimeSeconds => _questionTimeSeconds;
        public float OverlayDurationSeconds => _overlayDurationSeconds;
    }
}
