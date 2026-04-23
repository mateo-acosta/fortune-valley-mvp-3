using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Tutorial;
using FortuneValley.Managers;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Verifies that GameFlowController routes each BootFlow value to the
    /// right downstream event. Uses the real static GameEvents bus with
    /// SetUp/TearDown isolation so tests don't leak subscribers across runs.
    /// </summary>
    [TestFixture]
    public class GameFlowControllerRoutingTests
    {
        private GameObject _go;
        private GameFlowController _controller;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();
            _go = new GameObject("GameFlowController");
            _controller = _go.AddComponent<GameFlowController>();

            // EditMode tests do not auto-run Unity lifecycle methods, so
            // invoke OnEnable by reflection to establish the subscriptions.
            var onEnable = typeof(GameFlowController).GetMethod("OnEnable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onEnable.Invoke(_controller, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            GameEvents.ClearAllSubscriptions();
        }

        [Test]
        public void FirstTimeTutorialFlow_RaisesHideTitleAndTutorialStart()
        {
            bool hidTitle = false;
            bool requestedTutorial = false;
            bool showedCarousel = false;
            GameEvents.OnHideTitleScreen += () => hidTitle = true;
            GameEvents.OnTutorialStartRequested += () => requestedTutorial = true;
            GameEvents.OnShowRulesCarousel += () => showedCarousel = true;

            GameEvents.RaiseBootFlowDecided(BootFlow.FirstTimeTutorial);

            Assert.IsTrue(hidTitle, "FirstTimeTutorial must hide the title");
            Assert.IsTrue(requestedTutorial, "FirstTimeTutorial must request the tutorial");
            Assert.IsFalse(showedCarousel, "FirstTimeTutorial must NOT show the rules carousel");
        }

        [Test]
        public void NormalCarouselFlow_HidesTitleAndShowsCarousel()
        {
            bool hidTitle = false;
            bool showedCarousel = false;
            bool startedCountdown = false;
            bool requestedTutorial = false;
            GameEvents.OnHideTitleScreen += () => hidTitle = true;
            GameEvents.OnShowRulesCarousel += () => showedCarousel = true;
            GameEvents.OnStartCountdown += () => startedCountdown = true;
            GameEvents.OnTutorialStartRequested += () => requestedTutorial = true;

            GameEvents.RaiseBootFlowDecided(BootFlow.NormalCarousel);

            Assert.IsTrue(hidTitle);
            Assert.IsTrue(showedCarousel);
            Assert.IsFalse(startedCountdown, "Countdown should wait for OnCarouselComplete");
            Assert.IsFalse(requestedTutorial);
        }

        [Test]
        public void SkipTutorialFlow_SkipsCarouselAndStartsCountdown()
        {
            bool hidTitle = false;
            bool showedCarousel = false;
            bool startedCountdown = false;
            bool requestedTutorial = false;
            GameEvents.OnHideTitleScreen += () => hidTitle = true;
            GameEvents.OnShowRulesCarousel += () => showedCarousel = true;
            GameEvents.OnStartCountdown += () => startedCountdown = true;
            GameEvents.OnTutorialStartRequested += () => requestedTutorial = true;

            GameEvents.RaiseBootFlowDecided(BootFlow.SkipTutorial);

            Assert.IsTrue(hidTitle);
            Assert.IsFalse(showedCarousel, "SkipTutorial must bypass the carousel");
            Assert.IsTrue(startedCountdown);
            Assert.IsFalse(requestedTutorial);
        }

        [Test]
        public void TutorialComplete_StartsCountdown()
        {
            bool startedCountdown = false;
            GameEvents.OnStartCountdown += () => startedCountdown = true;

            GameEvents.RaiseTutorialComplete();

            Assert.IsTrue(startedCountdown,
                "OnTutorialComplete should route into the normal countdown path");
        }
    }
}
