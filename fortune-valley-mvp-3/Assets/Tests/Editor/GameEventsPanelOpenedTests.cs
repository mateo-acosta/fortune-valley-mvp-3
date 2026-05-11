using NUnit.Framework;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class GameEventsPanelOpenedTests
    {
        [SetUp]
        public void SetUp() => GameEvents.ClearAllSubscriptions();

        [TearDown]
        public void TearDown() => GameEvents.ClearAllSubscriptions();

        [Test]
        public void Raise_InvokesSubscriberWithCorrectPanelType()
        {
            PanelType? captured = null;
            GameEvents.OnPanelOpened += p => captured = p;

            GameEvents.RaisePanelOpened(PanelType.Loan);

            Assert.AreEqual(PanelType.Loan, captured);
        }

        [Test]
        public void ClearAllSubscriptions_RemovesListener()
        {
            int count = 0;
            GameEvents.OnPanelOpened += _ => count++;

            GameEvents.ClearAllSubscriptions();
            GameEvents.RaisePanelOpened(PanelType.Portfolio);

            Assert.AreEqual(0, count);
        }
    }
}
