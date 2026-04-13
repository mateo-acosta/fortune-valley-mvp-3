using NUnit.Framework;
using FortuneValley.Core.Questions;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Validation tests for QuestionBankLoader.IsValid.
    /// The Load() path (Resources + JsonUtility) is exercised in play; here we test the invariants.
    /// </summary>
    [TestFixture]
    public class QuestionBankLoaderTests
    {
        private static QuestionData Valid()
        {
            return new QuestionData
            {
                id = "q_001",
                category = "Investing",
                prompt = "What is compound interest?",
                choices = new[] { "A", "B", "C", "D" },
                correctIndex = 2,
                explanation = "Interest on interest."
            };
        }

        [Test]
        public void HappyPath_ValidQuestion_Passes()
        {
            Assert.IsTrue(QuestionBankLoader.IsValid(Valid(), out _));
        }

        [Test]
        public void Null_Fails()
        {
            Assert.IsFalse(QuestionBankLoader.IsValid(null, out string reason));
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void EmptyPrompt_Fails()
        {
            var q = Valid(); q.prompt = "";
            Assert.IsFalse(QuestionBankLoader.IsValid(q, out string reason));
            StringAssert.Contains("prompt", reason.ToLower());
        }

        [Test]
        public void WhitespacePrompt_Fails()
        {
            var q = Valid(); q.prompt = "   ";
            Assert.IsFalse(QuestionBankLoader.IsValid(q, out _));
        }

        [Test]
        public void ThreeChoices_Fails()
        {
            var q = Valid(); q.choices = new[] { "A", "B", "C" };
            Assert.IsFalse(QuestionBankLoader.IsValid(q, out string reason));
            StringAssert.Contains("4", reason);
        }

        [Test]
        public void NullChoices_Fails()
        {
            var q = Valid(); q.choices = null;
            Assert.IsFalse(QuestionBankLoader.IsValid(q, out _));
        }

        [Test]
        public void EmptyChoiceEntry_Fails()
        {
            var q = Valid(); q.choices[1] = "";
            Assert.IsFalse(QuestionBankLoader.IsValid(q, out string reason));
            StringAssert.Contains("1", reason);
        }

        [TestCase(-1)]
        [TestCase(4)]
        [TestCase(99)]
        public void CorrectIndexOutOfRange_Fails(int idx)
        {
            var q = Valid(); q.correctIndex = idx;
            Assert.IsFalse(QuestionBankLoader.IsValid(q, out string reason));
            StringAssert.Contains("correctIndex", reason);
        }

        [Test]
        public void UnknownCategory_Fails()
        {
            var q = Valid(); q.category = "Gambling";
            Assert.IsFalse(QuestionBankLoader.IsValid(q, out string reason));
            StringAssert.Contains("category", reason.ToLower());
        }

        [TestCase("investing")]
        [TestCase("INSURANCE")]
        [TestCase("Credit")]
        public void CategoryCasing_Permissive(string category)
        {
            var q = Valid(); q.category = category;
            Assert.IsTrue(QuestionBankLoader.IsValid(q, out _));
        }
    }
}
