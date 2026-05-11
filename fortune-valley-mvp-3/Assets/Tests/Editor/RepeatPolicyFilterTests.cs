using System;
using NUnit.Framework;
using FortuneValley.Domain.Notifications;
using FortuneValley.Managers.Notifications;
using FortuneValley.Tests.Fakes;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class RepeatPolicyFilterTests
    {
        private FakeNowProvider _clock;
        private InMemoryKeyValueStore _store;
        private PlayerPrefsDebouncedFlusher _prefs;
        private RepeatPolicyFilter _filter;

        [SetUp]
        public void SetUp()
        {
            _clock = new FakeNowProvider();
            _store = new InMemoryKeyValueStore();
            _prefs = new PlayerPrefsDebouncedFlusher(_store, _clock, TimeSpan.FromSeconds(5));
            _filter = new RepeatPolicyFilter(_clock, _prefs);
        }

        // ===============================================================
        // EveryTime
        // ===============================================================

        [Test]
        public void EveryTime_AlwaysFires()
        {
            for (int i = 0; i < 100; i++)
            {
                Assert.IsTrue(_filter.ShouldFire("tip-1", RepeatPolicy.EveryTime, 0));
                _filter.MarkFired("tip-1", RepeatPolicy.EveryTime);
            }
        }

        // ===============================================================
        // OncePerSession
        // ===============================================================

        [Test]
        public void OncePerSession_FiresOnceThenSuppresses()
        {
            Assert.IsTrue(_filter.ShouldFire("tip-1", RepeatPolicy.OncePerSession, 0));
            _filter.MarkFired("tip-1", RepeatPolicy.OncePerSession);
            Assert.IsFalse(_filter.ShouldFire("tip-1", RepeatPolicy.OncePerSession, 0));
        }

        [Test]
        public void OncePerSession_DistinctTipIds_DoNotInterfere()
        {
            _filter.MarkFired("tip-A", RepeatPolicy.OncePerSession);
            Assert.IsFalse(_filter.ShouldFire("tip-A", RepeatPolicy.OncePerSession, 0));
            Assert.IsTrue(_filter.ShouldFire("tip-B", RepeatPolicy.OncePerSession, 0));
        }

        [Test]
        public void OncePerSession_ClearSession_AllowsRefire()
        {
            _filter.MarkFired("tip-1", RepeatPolicy.OncePerSession);
            _filter.ClearSession();
            Assert.IsTrue(_filter.ShouldFire("tip-1", RepeatPolicy.OncePerSession, 0));
        }

        // ===============================================================
        // OncePerPlayer
        // ===============================================================

        [Test]
        public void OncePerPlayer_FiresOnceThenSuppressesPersistently()
        {
            Assert.IsTrue(_filter.ShouldFire("tip-1", RepeatPolicy.OncePerPlayer, 0));
            _filter.MarkFired("tip-1", RepeatPolicy.OncePerPlayer);
            Assert.IsFalse(_filter.ShouldFire("tip-1", RepeatPolicy.OncePerPlayer, 0));

            // ClearSession should NOT affect OncePerPlayer.
            _filter.ClearSession();
            Assert.IsFalse(_filter.ShouldFire("tip-1", RepeatPolicy.OncePerPlayer, 0));
        }

        [Test]
        public void OncePerPlayer_PersistedKeyIsCorrect()
        {
            _filter.MarkFired("tip-XYZ", RepeatPolicy.OncePerPlayer);
            Assert.AreEqual(1, _store.GetInt("FV_GuidanceTipFired_tip-XYZ", 0));
        }

        [Test]
        public void OncePerPlayer_SurvivesAcrossNewFilterInstance_WhenStoreShared()
        {
            _filter.MarkFired("tip-1", RepeatPolicy.OncePerPlayer);
            _prefs.ForceFlush();

            // Simulate fresh session: new filter, new flusher, but the same KV store.
            var newPrefs = new PlayerPrefsDebouncedFlusher(_store, _clock, TimeSpan.FromSeconds(5));
            var newFilter = new RepeatPolicyFilter(_clock, newPrefs);
            Assert.IsFalse(newFilter.ShouldFire("tip-1", RepeatPolicy.OncePerPlayer, 0));
        }

        // ===============================================================
        // OncePerCooldown
        // ===============================================================

        [Test]
        public void OncePerCooldown_FirstCall_AlwaysAllowed()
        {
            Assert.IsTrue(_filter.ShouldFire("tip-1", RepeatPolicy.OncePerCooldown, 60));
        }

        [Test]
        public void OncePerCooldown_BeforeWindow_Suppressed()
        {
            _filter.MarkFired("tip-1", RepeatPolicy.OncePerCooldown);
            _clock.AdvanceSeconds(30);
            Assert.IsFalse(_filter.ShouldFire("tip-1", RepeatPolicy.OncePerCooldown, 60));
        }

        [Test]
        public void OncePerCooldown_AfterWindow_Allowed()
        {
            _filter.MarkFired("tip-1", RepeatPolicy.OncePerCooldown);
            _clock.AdvanceSeconds(61);
            Assert.IsTrue(_filter.ShouldFire("tip-1", RepeatPolicy.OncePerCooldown, 60));
        }

        [Test]
        public void OncePerCooldown_AtExactWindow_Allowed()
        {
            _filter.MarkFired("tip-1", RepeatPolicy.OncePerCooldown);
            _clock.AdvanceSeconds(60);
            Assert.IsTrue(_filter.ShouldFire("tip-1", RepeatPolicy.OncePerCooldown, 60),
                "At exactly cooldown window the tip should be allowed (>= cooldown).");
        }

        [Test]
        public void OncePerCooldown_DistinctTipIds_DoNotInterfere()
        {
            _filter.MarkFired("tip-A", RepeatPolicy.OncePerCooldown);
            Assert.IsTrue(_filter.ShouldFire("tip-B", RepeatPolicy.OncePerCooldown, 60));
        }

        // ===============================================================
        // Cross-policy isolation
        // ===============================================================

        [Test]
        public void DifferentPoliciesOnSameTipId_AreEvaluatedIndependently()
        {
            // Scenario: a tip used to be OncePerSession, now reauthored as OncePerPlayer.
            // Marking under one policy should not auto-suppress the other.
            _filter.MarkFired("tip-1", RepeatPolicy.OncePerSession);

            Assert.IsFalse(_filter.ShouldFire("tip-1", RepeatPolicy.OncePerSession, 0));
            Assert.IsTrue(_filter.ShouldFire("tip-1", RepeatPolicy.OncePerPlayer, 0));
            Assert.IsTrue(_filter.ShouldFire("tip-1", RepeatPolicy.OncePerCooldown, 60));
            Assert.IsTrue(_filter.ShouldFire("tip-1", RepeatPolicy.EveryTime, 0));
        }
    }
}
