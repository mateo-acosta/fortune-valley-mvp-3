using NUnit.Framework;
using FortuneValley.UI.HUD;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class CurrencyFormatterTests
    {
        [Test]
        public void Zero_RendersF2()
        {
            Assert.AreEqual("$0.00", CurrencyFormatter.FormatCurrency(0f));
        }

        [Test]
        public void SubDollar_RendersF2()
        {
            Assert.AreEqual("$0.50", CurrencyFormatter.FormatCurrency(0.5f));
        }

        [Test]
        public void JustBelowThousand_RendersF2()
        {
            Assert.AreEqual("$999.99", CurrencyFormatter.FormatCurrency(999.99f));
        }

        [Test]
        public void AtThousand_RendersN0()
        {
            Assert.AreEqual("$1,000", CurrencyFormatter.FormatCurrency(1000f));
        }

        [Test]
        public void Millions_RendersN0WithSeparators()
        {
            Assert.AreEqual("$1,234,567", CurrencyFormatter.FormatCurrency(1234567f));
        }

        [Test]
        public void NegativeSmall_SignBeforeDollar()
        {
            Assert.AreEqual("-$50.00", CurrencyFormatter.FormatCurrency(-50f));
        }

        [Test]
        public void NegativeAtThousand_SignBeforeDollar()
        {
            Assert.AreEqual("-$5,000", CurrencyFormatter.FormatCurrency(-5000f));
        }

        [Test]
        public void NegativeMillions_SignBeforeDollar()
        {
            Assert.AreEqual("-$1,234,567", CurrencyFormatter.FormatCurrency(-1234567f));
        }
    }
}
