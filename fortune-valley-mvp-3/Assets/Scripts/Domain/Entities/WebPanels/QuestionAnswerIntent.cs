using System;

namespace FortuneValley.Domain.Entities.WebPanels
{
    /// <summary>
    /// JS -> Unity intent payload for QuestionMaster answer submission.
    /// idx == -1 represents a client-side timeout signal.
    /// </summary>
    [Serializable]
    public class QuestionAnswerIntent
    {
        public int idx;
    }
}
