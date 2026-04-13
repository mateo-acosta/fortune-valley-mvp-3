using System;
using System.Collections.Generic;

namespace FortuneValley.Domain.Entities
{
    /// <summary>
    /// JSON DTO root for the QuestionMaster question bank.
    /// </summary>
    [Serializable]
    public class QuestionBank
    {
        public List<QuestionData> questions;
    }
}
