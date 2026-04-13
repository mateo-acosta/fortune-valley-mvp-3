using System;
using System.Collections.Generic;
using NUnit.Framework;
using FortuneValley.Core.Questions;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class QuestionSessionTests
    {
        private static List<QuestionData> MakeBank(int n)
        {
            var list = new List<QuestionData>(n);
            for (int i = 0; i < n; i++)
            {
                list.Add(new QuestionData
                {
                    id = $"q_{i}",
                    category = "Investing",
                    prompt = $"Q{i}?",
                    choices = new[] { "A", "B", "C", "D" },
                    correctIndex = i % 4,
                    explanation = ""
                });
            }
            return list;
        }

        [Test]
        public void Start_ResetsStreakAndLoadsDeck()
        {
            var s = new QuestionSession(new Random(0));
            s.Start(MakeBank(4));
            Assert.AreEqual(0, s.Streak);
            Assert.AreEqual(4, s.DeckCount);
            Assert.AreEqual(4, s.RemainingInDeck);
        }

        [Test]
        public void Next_ReturnsAllBeforeRepeating()
        {
            var s = new QuestionSession(new Random(42));
            var bank = MakeBank(5);
            s.Start(bank);

            var seen = new HashSet<string>();
            for (int i = 0; i < 5; i++)
            {
                var q = s.Next();
                Assert.IsNotNull(q);
                Assert.IsTrue(seen.Add(q.id), $"Question {q.id} appeared twice before deck exhausted");
            }
            Assert.AreEqual(5, seen.Count);
        }

        [Test]
        public void Next_ReshufflesAfterExhaust()
        {
            var s = new QuestionSession(new Random(1));
            s.Start(MakeBank(3));
            for (int i = 0; i < 3; i++) s.Next();
            var after = s.Next(); // triggers reshuffle
            Assert.IsNotNull(after);
        }

        [Test]
        public void Submit_CorrectAnswer_IncrementsStreak()
        {
            var s = new QuestionSession(new Random(0));
            s.Start(MakeBank(3));
            var q = s.Next();
            Assert.IsTrue(s.Submit(q.correctIndex));
            Assert.AreEqual(1, s.Streak);
        }

        [Test]
        public void Submit_WrongAnswer_ResetsStreak()
        {
            var s = new QuestionSession(new Random(0));
            s.Start(MakeBank(3));
            var q = s.Next();
            s.Submit(q.correctIndex); // streak 1
            var q2 = s.Next();
            int wrong = (q2.correctIndex + 1) % 4;
            Assert.IsFalse(s.Submit(wrong));
            Assert.AreEqual(0, s.Streak);
        }

        [Test]
        public void Submit_TimeoutSentinelMinusOne_TreatedAsWrong()
        {
            var s = new QuestionSession(new Random(0));
            s.Start(MakeBank(3));
            s.Next();
            s.Submit(s.Current.correctIndex); // streak 1
            s.Next();
            Assert.IsFalse(s.Submit(-1));
            Assert.AreEqual(0, s.Streak);
        }

        [Test]
        public void Submit_WithNoCurrent_ReturnsFalse()
        {
            var s = new QuestionSession(new Random(0));
            s.Start(MakeBank(3));
            Assert.IsFalse(s.Submit(0)); // no Next() called yet
        }

        [Test]
        public void ResetStreakOnly_PreservesDeckCursor()
        {
            var s = new QuestionSession(new Random(0));
            s.Start(MakeBank(3));
            var first = s.Next();
            s.Submit(first.correctIndex);
            Assert.AreEqual(1, s.Streak);

            s.ResetStreakOnly();
            Assert.AreEqual(0, s.Streak);

            // Next() should advance cursor, not reset it.
            var second = s.Next();
            Assert.AreNotEqual(first.id, second.id);
        }

        [Test]
        public void EmptyBank_NextReturnsNull()
        {
            var s = new QuestionSession(new Random(0));
            s.Start(new List<QuestionData>());
            Assert.IsNull(s.Next());
        }
    }
}
