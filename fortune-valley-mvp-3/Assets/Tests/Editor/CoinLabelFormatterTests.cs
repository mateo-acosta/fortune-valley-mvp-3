using NUnit.Framework;
using FortuneValley.UI.World;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class CoinLabelFormatterTests
    {
        [Test]
        public void FormatRate_ZeroAmount_RendersZero()
        {
            string result = CoinLabelFormatter.FormatRate(0f, "+${0:N0}/day");
            Assert.AreEqual("+$0/day", result);
        }

        [Test]
        public void FormatRate_SmallAmount_FloorsToInt()
        {
            string result = CoinLabelFormatter.FormatRate(1.7f, "+${0:N0}/day");
            Assert.AreEqual("+$1/day", result);
        }

        [Test]
        public void FormatRate_LargeAmount_AppliesThousandSeparator()
        {
            // Use a value that survives float roundtripping: 8 significant
            // digits is the safe limit for IEEE 754 single-precision.
            string result = CoinLabelFormatter.FormatRate(12_345_678f, "+${0:N0}/day");
            Assert.AreEqual("+$12,345,678/day", result);
        }

        [Test]
        public void FormatDeposit_LargeAmount_OmitsPerDaySuffixWhenFormatDoesNot()
        {
            string result = CoinLabelFormatter.FormatDeposit(2500f, "+${0:N0}");
            Assert.AreEqual("+$2,500", result);
        }

        [Test]
        public void FormatDeposit_FractionalAmount_FloorsToInt()
        {
            string result = CoinLabelFormatter.FormatDeposit(99.95f, "+${0:N0}");
            Assert.AreEqual("+$99", result);
        }

        [Test]
        public void FormatRate_FormatWithoutPlaceholder_ReturnsLiteral()
        {
            // string.Format with a format that has no {0} should return the
            // literal unchanged. Defensive: callers may misconfigure the
            // SerializeField in the prefab.
            string result = CoinLabelFormatter.FormatRate(50f, "literal");
            Assert.AreEqual("literal", result);
        }
    }
}
