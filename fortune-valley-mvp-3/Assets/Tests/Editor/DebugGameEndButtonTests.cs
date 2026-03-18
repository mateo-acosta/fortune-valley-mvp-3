using NUnit.Framework;
using FortuneValley.Domain.Entities;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Tests for DebugGameEndButton to verify it fires the correct events
    /// without triggering the problematic OnGameEnd double-event chain.
    /// </summary>
    [TestFixture]
    public class DebugGameEndButtonTests
    {
        private DebugGameEndButton _button;
        private UnityEngine.GameObject _go;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();
            _go = new UnityEngine.GameObject("TestDebugButton");
            _button = _go.AddComponent<DebugGameEndButton>();
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.ClearAllSubscriptions();
            UnityEngine.Object.DestroyImmediate(_go);
        }

        [Test]
        public void ForceWin_RaisesGameEndWithSummary_PlayerWins()
        {
            bool receivedIsPlayerWin = false;
            GameSummary receivedSummary = null;
            GameEvents.OnGameEndWithSummary += (isWin, summary) =>
            {
                receivedIsPlayerWin = isWin;
                receivedSummary = summary;
            };

            _button.ForceWin();

            Assert.IsTrue(receivedIsPlayerWin);
            Assert.IsNotNull(receivedSummary);
            Assert.AreEqual(45, receivedSummary.DaysPlayed);
            Assert.AreEqual(3, receivedSummary.PlayerLots);
            Assert.AreEqual("Smart Investor!", receivedSummary.Headline);
        }

        [Test]
        public void ForceLose_RaisesGameEndWithSummary_PlayerLoses()
        {
            bool receivedIsPlayerWin = true;
            GameSummary receivedSummary = null;
            GameEvents.OnGameEndWithSummary += (isWin, summary) =>
            {
                receivedIsPlayerWin = isWin;
                receivedSummary = summary;
            };

            _button.ForceLose();

            Assert.IsFalse(receivedIsPlayerWin);
            Assert.IsNotNull(receivedSummary);
            Assert.AreEqual(60, receivedSummary.DaysPlayed);
            Assert.AreEqual(4, receivedSummary.RivalLots);
            Assert.AreEqual("The Rival Got Ahead", receivedSummary.Headline);
        }

        [Test]
        public void ForceWin_DoesNotRaiseOnGameEnd()
        {
            bool gameEndFired = false;
            GameEvents.OnGameEnd += (winner) => gameEndFired = true;

            _button.ForceWin();

            Assert.IsFalse(gameEndFired, "OnGameEnd should NOT fire — only OnGameEndWithSummary");
        }

        // --- ShouldPreserveGameObject tests ---

        [Test]
        public void ShouldPreserveGameObject_NoChildren_ReturnsFalse()
        {
            // Leaf debug button with no children → safe to destroy entirely
            Assert.IsFalse(_button.ShouldPreserveGameObject());
        }

        [Test]
        public void ShouldPreserveGameObject_OnlyDebugChildren_ReturnsFalse()
        {
            // All children are debug buttons → no non-debug children to preserve
            var child1 = new UnityEngine.GameObject("DebugChild1");
            child1.AddComponent<DebugGameEndButton>();
            child1.transform.SetParent(_go.transform);

            var child2 = new UnityEngine.GameObject("DebugChild2");
            child2.AddComponent<DebugGameEndButton>();
            child2.transform.SetParent(_go.transform);

            Assert.IsFalse(_button.ShouldPreserveGameObject());
        }

        [Test]
        public void ShouldPreserveGameObject_MixedChildren_ReturnsTrue()
        {
            // Container with both debug and non-debug children (e.g. BottomBar)
            var debugChild = new UnityEngine.GameObject("WinButton");
            debugChild.AddComponent<DebugGameEndButton>();
            debugChild.transform.SetParent(_go.transform);

            var normalChild = new UnityEngine.GameObject("PortfolioButton");
            normalChild.transform.SetParent(_go.transform);

            Assert.IsTrue(_button.ShouldPreserveGameObject());
        }

        [Test]
        public void ShouldPreserveGameObject_OnlyNonDebugChildren_ReturnsTrue()
        {
            // Container with only non-debug children → must preserve
            var child = new UnityEngine.GameObject("PortfolioButton");
            child.transform.SetParent(_go.transform);

            Assert.IsTrue(_button.ShouldPreserveGameObject());
        }
    }
}
