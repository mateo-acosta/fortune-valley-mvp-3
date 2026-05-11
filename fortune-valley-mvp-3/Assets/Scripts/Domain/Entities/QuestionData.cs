using System;

namespace FortuneValley.Domain.Entities
{
    /// <summary>
    /// JSON DTO for a single QuestionMaster question. Public fields intentional:
    /// matches existing Serializable DTO pattern and JsonUtility requirements.
    /// </summary>
    [Serializable]
    public class QuestionData
    {
        public string id;
        public string category;
        public string prompt;
        public string[] choices;
        public int correctIndex;
        public string explanation;

        public bool TryGetCategory(out QuestionCategory value)
        {
            return Enum.TryParse(category, ignoreCase: true, out value);
        }
    }
}
