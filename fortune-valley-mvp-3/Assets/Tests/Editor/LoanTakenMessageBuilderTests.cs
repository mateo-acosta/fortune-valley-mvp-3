using NUnit.Framework;
using FortuneValley.Domain.Notifications.Contexts;
using FortuneValley.Managers.Notifications.Builders;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class LoanTakenMessageBuilderTests
    {
        private LoanTakenMessageBuilder _builder;

        [SetUp]
        public void SetUp() => _builder = new LoanTakenMessageBuilder();

        [Test]
        public void Build_FormatsPrincipalWithDollarAndThousandsSeparator()
        {
            var ctx = new LoanTakenContext(principal: 5000f, lotId: "lot_a", termYears: 24, monthlyPayment: 250f);
            var (title, message) = _builder.Build("Took a {0} loan", "Amount {0}", ctx);
            Assert.AreEqual("Took a $5,000 loan", title);
            Assert.AreEqual("Amount $5,000", message);
        }

        [Test]
        public void Build_PassesLotIdAsArg1()
        {
            var ctx = new LoanTakenContext(0, "lot_block02", 0, 0);
            var (_, message) = _builder.Build("", "on {1}", ctx);
            Assert.AreEqual("on lot_block02", message);
        }

        [Test]
        public void Build_PassesTermYearsAsArg2()
        {
            var ctx = new LoanTakenContext(0, "", 36, 0);
            var (_, message) = _builder.Build("", "over {2} months", ctx);
            Assert.AreEqual("over 36 months", message);
        }

        [Test]
        public void Build_FormatsMonthlyPaymentAsArg3()
        {
            var ctx = new LoanTakenContext(12000, "", 48, 1250.75f);
            var (_, message) = _builder.Build("", "payment: {3}", ctx);
            Assert.AreEqual("payment: $1,251", message);
        }

        [Test]
        public void Build_AllPlaceholders_Simultaneously()
        {
            var ctx = new LoanTakenContext(10000, "Lot_X", 24, 500);
            var (title, message) = _builder.Build(
                "New Loan: {0}",
                "{0} for {1} over {2}mo at {3}/mo",
                ctx);
            Assert.AreEqual("New Loan: $10,000", title);
            Assert.AreEqual("$10,000 for Lot_X over 24mo at $500/mo", message);
        }

        [Test]
        public void Build_NullLotId_RendersEmptyString()
        {
            var ctx = new LoanTakenContext(0, null, 0, 0);
            var (_, message) = _builder.Build("", "[{1}]", ctx);
            Assert.AreEqual("[]", message);
        }

        [Test]
        public void Build_NullTemplates_ReturnEmptyStrings()
        {
            var ctx = new LoanTakenContext(100, "x", 1, 50);
            var (title, message) = _builder.Build(null, null, ctx);
            Assert.AreEqual(string.Empty, title);
            Assert.AreEqual(string.Empty, message);
        }

        [Test]
        public void Build_UsesInvariantCulture_DecimalsAlwaysDot()
        {
            // Even on a comma-decimal locale, output uses period as thousands separator.
            var originalCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture =
                new System.Globalization.CultureInfo("de-DE");
            try
            {
                var ctx = new LoanTakenContext(12345f, "lot", 24, 500f);
                var (_, message) = _builder.Build("", "{0}", ctx);
                Assert.AreEqual("$12,345", message);
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = originalCulture;
            }
        }
    }
}
