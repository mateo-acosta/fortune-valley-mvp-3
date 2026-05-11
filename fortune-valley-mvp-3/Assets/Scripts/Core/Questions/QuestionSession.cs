using System;
using System.Collections.Generic;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Core.Questions
{
    /// <summary>
    /// Pure streak + deck state for the QuestionMaster panel. Wrapped by QuestionManager.
    /// Deck reshuffles when exhausted so the player sees every question once before any repeat.
    /// </summary>
    public class QuestionSession
    {
        private readonly List<QuestionData> _deck = new List<QuestionData>();
        private readonly Random _rng;
        private int _cursor;
        private int _streak;
        private QuestionData _current;

        public int Streak => _streak;
        public QuestionData Current => _current;
        public int DeckCount => _deck.Count;
        public int RemainingInDeck => Math.Max(0, _deck.Count - _cursor);

        public QuestionSession(Random rng = null)
        {
            _rng = rng ?? new Random();
        }

        /// <summary>
        /// Load the deck and reset streak + cursor. Shuffles once.
        /// </summary>
        public void Start(IList<QuestionData> source)
        {
            _deck.Clear();
            if (source != null) _deck.AddRange(source);
            _cursor = 0;
            _streak = 0;
            _current = null;
            Shuffle();
        }

        /// <summary>
        /// Reset streak only (deck + cursor preserved so exhausted pool doesn't restart on every open).
        /// </summary>
        public void ResetStreakOnly()
        {
            _streak = 0;
        }

        /// <summary>
        /// Advance to the next question. Reshuffles on exhaust.
        /// Returns null only when the deck is empty.
        /// </summary>
        public QuestionData Next()
        {
            if (_deck.Count == 0)
            {
                _current = null;
                return null;
            }
            if (_cursor >= _deck.Count)
            {
                _cursor = 0;
                Shuffle();
            }
            _current = _deck[_cursor++];
            return _current;
        }

        /// <summary>
        /// Submit an answer for the current question. Returns true if correct.
        /// Timeout callers pass chosenIndex = -1 which is always wrong.
        /// </summary>
        public bool Submit(int chosenIndex)
        {
            if (_current == null) return false;
            bool correct = chosenIndex == _current.correctIndex;
            if (correct)
            {
                _streak++;
            }
            else
            {
                _streak = 0;
            }
            return correct;
        }

        private void Shuffle()
        {
            // Fisher-Yates in-place. Reusable deck buffer -- no per-call allocation.
            for (int i = _deck.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(0, i + 1);
                (_deck[i], _deck[j]) = (_deck[j], _deck[i]);
            }
        }
    }
}
