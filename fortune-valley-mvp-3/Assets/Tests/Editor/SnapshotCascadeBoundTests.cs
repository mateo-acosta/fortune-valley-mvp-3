using NUnit.Framework;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Issue 14A: bound the cascade of OnNetWorthChanged emissions during a
    /// Phase 2 catch-up so the WebGL bridge push doesn't get hammered. The
    /// plan accepts up to ~3 emissions per Phase 2 (LifeGoalsHud snapshot +
    /// ProfileWebBridge snapshot + any future subscriber). This test pins
    /// that ceiling so a regression that adds a 5th caller is caught early.
    /// </summary>
    [TestFixture]
    public class SnapshotCascadeBoundTests
    {
        private int _netWorthChangedCount;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();
            _netWorthChangedCount = 0;
            GameEvents.OnNetWorthChanged += (_, __) => _netWorthChangedCount++;
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.ClearAllSubscriptions();
        }

        [Test]
        public void TwoSnapshotRequests_ProduceAtMostThreeNetWorthChangedEmissions()
        {
            // Two snapshot requests is the realistic Phase 2 scenario: LifeGoalsHud
            // and (in future code) any other HUD that needs a post-restore value.
            // Each call to RaiseRequestNetWorthSnapshot maps to one OnNetWorthChanged
            // emission, so two calls = two emissions. The headroom (3) accommodates
            // one additional internal Pump triggered by hydration events.
            float liquid = 1000f;
            float business = 5000f;
            using (new NetWorthService(() => liquid, () => business))
            {
                GameEvents.RaiseRequestNetWorthSnapshot();
                GameEvents.RaiseRequestNetWorthSnapshot();

                Assert.LessOrEqual(_netWorthChangedCount, 3,
                    "Cascade must stay bounded; if this fires, a new caller "
                    + "joined the chain and budget should be revisited.");
                Assert.GreaterOrEqual(_netWorthChangedCount, 2,
                    "Two snapshot requests must produce at least two emissions");
            }
        }
    }
}
