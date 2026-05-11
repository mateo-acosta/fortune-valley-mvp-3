using System.Collections.Generic;
using UnityEngine;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Core.Questions
{
    /// <summary>
    /// Loads the QuestionMaster bank from Resources and validates each entry.
    /// Invalid entries are skipped with a warning; missing/malformed file logs an error and returns empty.
    /// </summary>
    public static class QuestionBankLoader
    {
        private const string DefaultResourcePath = "Questions/question_bank";
        private const int RequiredChoiceCount = 4;

        /// <summary>
        /// Load and validate the question bank. Returns an empty list on any unrecoverable error.
        /// </summary>
        public static List<QuestionData> Load(string resourcePath = DefaultResourcePath)
        {
            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                Debug.LogError($"[QuestionBankLoader] Missing question bank at Resources/{resourcePath}.json");
                return new List<QuestionData>();
            }

            QuestionBank bank = null;
            try
            {
                bank = JsonUtility.FromJson<QuestionBank>(asset.text);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[QuestionBankLoader] Failed to parse question bank JSON: {ex.Message}");
                return new List<QuestionData>();
            }

            if (bank == null || bank.questions == null)
            {
                Debug.LogError("[QuestionBankLoader] Question bank JSON has no 'questions' array.");
                return new List<QuestionData>();
            }

            var valid = new List<QuestionData>(bank.questions.Count);
            foreach (var q in bank.questions)
            {
                if (!IsValid(q, out string reason))
                {
                    Debug.LogWarning($"[QuestionBankLoader] Skipping invalid question '{q?.id ?? "<null>"}': {reason}");
                    continue;
                }
                valid.Add(q);
            }

            return valid;
        }

        /// <summary>
        /// Validate a single question. Public so tests and editor tools can reuse the rules.
        /// </summary>
        public static bool IsValid(QuestionData q, out string reason)
        {
            if (q == null) { reason = "null entry"; return false; }
            if (string.IsNullOrWhiteSpace(q.prompt)) { reason = "empty prompt"; return false; }
            if (q.choices == null || q.choices.Length != RequiredChoiceCount)
            {
                reason = $"expected {RequiredChoiceCount} choices, got {(q.choices == null ? 0 : q.choices.Length)}";
                return false;
            }
            for (int i = 0; i < q.choices.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(q.choices[i])) { reason = $"empty choice at index {i}"; return false; }
            }
            if (q.correctIndex < 0 || q.correctIndex >= q.choices.Length)
            {
                reason = $"correctIndex {q.correctIndex} out of range";
                return false;
            }
            if (!q.TryGetCategory(out _))
            {
                reason = $"unknown category '{q.category}'";
                return false;
            }
            reason = null;
            return true;
        }
    }
}
