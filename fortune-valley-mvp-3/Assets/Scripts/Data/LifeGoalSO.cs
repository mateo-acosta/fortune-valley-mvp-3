using UnityEngine;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Core
{
    /// <summary>
    /// One preset life goal in the catalog. Players pick exactly three of
    /// these (one per tier) during the intro tutorial. Goal realizes
    /// automatically when the player's Total Net Worth reaches the threshold.
    ///
    /// LEARNING DESIGN: Within-tier thresholds are intentionally identical so
    /// the player's choice is about *what* they personally value, not which
    /// optimization is cheapest.
    /// </summary>
    [CreateAssetMenu(fileName = "NewLifeGoal", menuName = "Fortune Valley/Life Goal")]
    public class LifeGoalSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable id used in DTOs and lookups. Snake_case, never localized.")]
        [SerializeField] private string _goalId;

        [Tooltip("Display name shown in the goal-selection panel and status badges.")]
        [SerializeField] private string _displayName;

        [Tooltip("One-line description shown on the goal-selection card.")]
        [TextArea(2, 3)]
        [SerializeField] private string _description;

        [Header("Tier")]
        [Tooltip("Starter / Mid / Ambitious. Player picks exactly one per tier.")]
        [SerializeField] private LifeGoalTier _tier = LifeGoalTier.Starter;

        [Header("Threshold")]
        [Tooltip("Total Net Worth required to realize this goal (USD).")]
        [SerializeField] private float _netWorthThreshold = 100000f;

        [Header("Visuals")]
        [Tooltip("Icon used in the goal-selection card.")]
        [SerializeField] private Sprite _icon;

        [Tooltip("Badge art shown in the player status panel when this goal is realized.")]
        [SerializeField] private Sprite _badgeArtRealized;

        [Tooltip("Badge art shown in the player status panel when this goal is locked / unrealized.")]
        [SerializeField] private Sprite _badgeArtLocked;

        public string GoalId => _goalId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public LifeGoalTier Tier => _tier;
        public float NetWorthThreshold => _netWorthThreshold;
        public Sprite Icon => _icon;
        public Sprite BadgeArtRealized => _badgeArtRealized;
        public Sprite BadgeArtLocked => _badgeArtLocked;
    }
}
