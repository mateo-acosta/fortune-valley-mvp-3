using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Reference-counting semantics of TimeManager.AcquirePause / ReleasePause.
    /// Tick emission is an Update-driven concern; these tests cover only the
    /// lock state machine, not the actual tick-suppression path (that is
    /// verified in PlayMode integration once the tutorial controller lands).
    /// </summary>
    [TestFixture]
    public class TimeManagerPauseLockTests
    {
        private GameObject _go;
        private TimeManager _tm;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TimeManager");
            _tm = _go.AddComponent<TimeManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void NewlyConstructed_LockCountIsZero_AndNotLocked()
        {
            Assert.AreEqual(0, _tm.PauseLockCount);
            Assert.IsFalse(_tm.IsPauseLocked);
        }

        [Test]
        public void AcquirePause_SetsLocked()
        {
            _tm.AcquirePause();
            Assert.AreEqual(1, _tm.PauseLockCount);
            Assert.IsTrue(_tm.IsPauseLocked);
        }

        [Test]
        public void AcquireTwice_RequiresTwoReleases()
        {
            _tm.AcquirePause();
            _tm.AcquirePause();
            Assert.AreEqual(2, _tm.PauseLockCount);

            _tm.ReleasePause();
            Assert.IsTrue(_tm.IsPauseLocked, "Still locked after one of two releases");

            _tm.ReleasePause();
            Assert.IsFalse(_tm.IsPauseLocked);
            Assert.AreEqual(0, _tm.PauseLockCount);
        }

        [Test]
        public void ReleaseWithoutAcquire_IsClampedAtZero()
        {
            _tm.ReleasePause();
            Assert.AreEqual(0, _tm.PauseLockCount,
                "Unmatched Release must not drive the counter negative");
            Assert.IsFalse(_tm.IsPauseLocked);

            _tm.AcquirePause();
            Assert.AreEqual(1, _tm.PauseLockCount,
                "Stray Release should not poison a subsequent Acquire");
            Assert.IsTrue(_tm.IsPauseLocked);
        }

        [Test]
        public void ManyReleases_NeverDriveCounterNegative()
        {
            _tm.AcquirePause();
            for (int i = 0; i < 10; i++) _tm.ReleasePause();
            Assert.AreEqual(0, _tm.PauseLockCount);
            Assert.IsFalse(_tm.IsPauseLocked);
        }

        [Test]
        public void PauseLock_IsIndependentFromSpeedPause()
        {
            // Player hits pause (speed-based). Tutorial-style pause lock also active.
            _tm.SetSpeedIndex(0);
            _tm.AcquirePause();

            Assert.IsTrue(_tm.IsPaused, "speed=0 → IsPaused");
            Assert.IsTrue(_tm.IsPauseLocked, "acquire active → IsPauseLocked");

            _tm.ReleasePause();
            Assert.IsTrue(_tm.IsPaused, "speed-based pause unaffected by ReleasePause");
            Assert.IsFalse(_tm.IsPauseLocked);
        }

        [Test]
        public void NestedAcquireRelease_DoesNotLeakThroughMultipleCycles()
        {
            for (int cycle = 0; cycle < 5; cycle++)
            {
                _tm.AcquirePause();
                Assert.IsTrue(_tm.IsPauseLocked);
                _tm.ReleasePause();
                Assert.IsFalse(_tm.IsPauseLocked);
            }
            Assert.AreEqual(0, _tm.PauseLockCount);
        }
    }
}
