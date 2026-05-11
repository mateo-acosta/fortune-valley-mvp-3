using System;

namespace FortuneValley.Domain.Entities.WebPanels
{
    /// <summary>
    /// Wire payload from Unity to the HTML QuestionMaster panel iframe.
    /// JsonUtility-serialized, so public fields are intentional.
    /// One DTO instance is reused per push; the bridge overwrites fields
    /// in place to avoid per-tick allocation.
    ///
    /// Phase discriminates which subset of fields is meaningful:
    ///   "asking"     - currentQuestion fields valid; reveal fields are placeholder.
    ///   "revealing"  - currentQuestion + reveal fields valid.
    ///   "idle"       - session not active; only streak + nextReward meaningful.
    /// </summary>
    [Serializable]
    public class QuestionMasterPanelDTO
    {
        public string phase;

        public int streak;
        public float timeLimitSeconds;
        public float timeRemainingSeconds;
        public int nextReward;

        public string questionId;
        public string category;
        public string prompt;
        public string[] choices;

        public int correctIndex;
        public int chosenIndex;
        public bool wasCorrect;
        public int rewardEarned;
        public string explanation;
        public bool wasTimeout;
    }
}
