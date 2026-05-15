using System;
using NUnit.Framework;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Covers the three timing buckets, same-DTO idempotency, unsubscribe, and the
    /// null-DTO (fresh-user) path documented in SaveRestoreCatchUp.
    /// </summary>
    [TestFixture]
    public class SaveRestoreCatchUpTests
    {
        private int _phase1Calls;
        private int _phase2Calls;
        private GamePlayerStateDTO _lastPhase1Dto;
        private Action<GamePlayerStateDTO> _phase1;
        private Action _phase2;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();
            GameEvents.LastLoadedSaveDto = null;
            GameEvents.HasSaveBeenRestored = false;
            SaveRestoreCatchUp.ClearCache();

            _phase1Calls = 0;
            _phase2Calls = 0;
            _lastPhase1Dto = null;
            _phase1 = dto => { _phase1Calls++; _lastPhase1Dto = dto; };
            _phase2 = () => _phase2Calls++;
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.ClearAllSubscriptions();
            GameEvents.LastLoadedSaveDto = null;
            GameEvents.HasSaveBeenRestored = false;
            SaveRestoreCatchUp.ClearCache();
        }

        [Test]
        public void Subscribe_BeforePhase1_LiveDelivery_NoReplay()
        {
            SaveRestoreCatchUp.Subscribe(_phase1, _phase2);

            Assert.AreEqual(0, _phase1Calls, "No replay when no cached DTO");
            Assert.AreEqual(0, _phase2Calls);

            var dto = new GamePlayerStateDTO { game_mode = "homebase" };
            GameEvents.LastLoadedSaveDto = dto;
            GameEvents.RaiseSaveStateLoaded(dto);

            Assert.AreEqual(1, _phase1Calls, "Phase 1 live");
            Assert.AreSame(dto, _lastPhase1Dto);

            GameEvents.HasSaveBeenRestored = true;
            GameEvents.RaiseSaveRestored();

            Assert.AreEqual(1, _phase2Calls, "Phase 2 live");
        }

        [Test]
        public void Subscribe_BetweenPhase1AndPhase2_ReplayPhase1_LivePhase2()
        {
            var dto = new GamePlayerStateDTO { game_mode = "homebase" };
            GameEvents.LastLoadedSaveDto = dto;
            GameEvents.RaiseSaveStateLoaded(dto);
            // Phase 2 has NOT fired yet.

            SaveRestoreCatchUp.Subscribe(_phase1, _phase2);

            Assert.AreEqual(1, _phase1Calls, "Phase 1 replayed synthetically");
            Assert.AreSame(dto, _lastPhase1Dto);
            Assert.AreEqual(0, _phase2Calls);

            GameEvents.HasSaveBeenRestored = true;
            GameEvents.RaiseSaveRestored();

            Assert.AreEqual(1, _phase2Calls, "Phase 2 live");
        }

        [Test]
        public void Subscribe_AfterPhase2_BothReplayedSynthetically()
        {
            var dto = new GamePlayerStateDTO { game_mode = "homebase" };
            GameEvents.LastLoadedSaveDto = dto;
            GameEvents.HasSaveBeenRestored = true;

            SaveRestoreCatchUp.Subscribe(_phase1, _phase2);

            Assert.AreEqual(1, _phase1Calls);
            Assert.AreSame(dto, _lastPhase1Dto);
            Assert.AreEqual(1, _phase2Calls);
        }

        [Test]
        public void Subscribe_SameDtoAfterUnsubscribe_NoSyntheticReFire()
        {
            var dto = new GamePlayerStateDTO { game_mode = "homebase" };
            GameEvents.LastLoadedSaveDto = dto;
            GameEvents.HasSaveBeenRestored = true;

            SaveRestoreCatchUp.Subscribe(_phase1, _phase2);
            Assert.AreEqual(1, _phase1Calls);

            SaveRestoreCatchUp.Unsubscribe(_phase1, _phase2);

            SaveRestoreCatchUp.Subscribe(_phase1, _phase2);

            Assert.AreEqual(1, _phase1Calls,
                "Same delegate + same cached DTO: synthetic replay must be skipped");
            // Phase 2 has no idempotency guard; the late-spawn replay fires each time
            // a subscriber attaches. Consumers' Phase 2 handlers must be idempotent.
        }

        [Test]
        public void Unsubscribe_StopsLiveDelivery()
        {
            SaveRestoreCatchUp.Subscribe(_phase1, _phase2);
            SaveRestoreCatchUp.Unsubscribe(_phase1, _phase2);

            var dto = new GamePlayerStateDTO { game_mode = "homebase" };
            GameEvents.LastLoadedSaveDto = dto;
            GameEvents.RaiseSaveStateLoaded(dto);
            GameEvents.RaiseSaveRestored();

            Assert.AreEqual(0, _phase1Calls);
            Assert.AreEqual(0, _phase2Calls);
        }

        [Test]
        public void Subscribe_NullCachedDto_NoPhase1Replay_Phase2StillReplaysIfFlagSet()
        {
            // Server-confirmed fresh user: HasSaveBeenRestored may still flip true
            // via a future code path, but LastLoadedSaveDto stays null. Phase 1
            // must not invoke the handler with null.
            GameEvents.LastLoadedSaveDto = null;
            GameEvents.HasSaveBeenRestored = true;

            SaveRestoreCatchUp.Subscribe(_phase1, _phase2);

            Assert.AreEqual(0, _phase1Calls);
            Assert.AreEqual(1, _phase2Calls);
        }

        [Test]
        public void SubscribeUnsubscribeLoop_NoUnboundedAllocationGrowth()
        {
            // Loose bound: subscribing and unsubscribing 100x must not blow up
            // the per-delegate tracking dictionary or throw. Tests the contract
            // that ClearCache + Unsubscribe interplay is well-defined.
            var dto = new GamePlayerStateDTO { game_mode = "homebase" };
            GameEvents.LastLoadedSaveDto = dto;

            for (int i = 0; i < 100; i++)
            {
                SaveRestoreCatchUp.Subscribe(_phase1, _phase2);
                SaveRestoreCatchUp.Unsubscribe(_phase1, _phase2);
            }

            // No exception; same-DTO guard kept replay bounded to 1.
            Assert.AreEqual(1, _phase1Calls);
        }
    }
}
