using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// The set of preset Life Goals the player can choose from during the
    /// intro tutorial. Expected: exactly 9 entries (3 Starter, 3 Mid, 3 Ambitious).
    /// Authoring lives in the Inspector; runtime systems read it via the
    /// GoalsById lookup or the AllGoals enumeration.
    /// </summary>
    [CreateAssetMenu(fileName = "LifeGoalCatalog", menuName = "Fortune Valley/Life Goal Catalog")]
    public class LifeGoalCatalog : ScriptableObject
    {
        [Header("Catalog")]
        [Tooltip("All preset goals. Author exactly 9 entries (3 per tier).")]
        [SerializeField] private LifeGoalSO[] _goals;

        [Header("Notes")]
        [Tooltip("Author note: within-tier thresholds are intentionally identical so " +
                 "the within-tier choice is narrative identity, not mechanical optimization.")]
        [TextArea(2, 4)]
        [SerializeField] private string _designNote =
            "Within-tier thresholds are intentionally identical so the within-tier " +
            "choice is narrative identity, not mechanical optimization.";

        public LifeGoalSO[] AllGoals => _goals;
        public int Count => _goals != null ? _goals.Length : 0;
        public string DesignNote => _designNote;

        public LifeGoalSO FindById(string goalId)
        {
            if (string.IsNullOrEmpty(goalId) || _goals == null) return null;

            for (int i = 0; i < _goals.Length; i++)
            {
                if (_goals[i] != null && _goals[i].GoalId == goalId)
                {
                    return _goals[i];
                }
            }
            return null;
        }
    }
}
