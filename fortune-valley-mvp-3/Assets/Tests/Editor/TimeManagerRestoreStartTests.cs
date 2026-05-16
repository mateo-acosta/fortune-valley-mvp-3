using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Regression: a returning player's clock must start on game start even
    /// though TimeManager.HandleGameStart early-returns out of the fresh
    /// Reset+Start path when SaveStateRestoredFromServer is true. Nothing else
    /// on the save-restore path calls StartTime(), so if HandleGameStart does
    /// not start the clock here the simulation is frozen forever (no ticks,
    /// age stuck, no income, can't invest) whenever GameSaveBootstrapper.Apply
    /// runs before the OnGameStart dispatch.
    /// </summary>
    [TestFixture]
    public class TimeManagerRestoreStartTests
    {
        private GameObject _go;
        private TimeManager _tm;
        private bool _priorRestoredFlag;

        [SetUp]
        public void SetUp()
        {
            _priorRestoredFlag = GameEvents.SaveStateRestoredFromServer;
            GameEvents.ClearAllSubscriptions();
            GameEvents.SaveStateRestoredFromServer = false;
            _go = new GameObject("TimeManager");
            _tm = _go.AddComponent<TimeManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            GameEvents.ClearAllSubscriptions();
            GameEvents.SaveStateRestoredFromServer = _priorRestoredFlag;
        }

        [Test]
        public void ReturningPlayer_GameStart_StartsClock_WithoutResettingCounters()
        {
            _tm.Hydrate(new GamePlayerStateDTO { current_day = 34, current_tick = 341 });
            GameEvents.SaveStateRestoredFromServer = true;

            Invoke(_tm, "HandleGameStart");

            Assert.IsTrue(_tm.IsRunning,
                "Returning-player clock must run; the restore path never calls StartTime() elsewhere");
            Assert.AreEqual(34, _tm.CurrentDay, "restore must NOT ResetTime the hydrated day");
            Assert.AreEqual(341, _tm.CurrentTick, "restore must NOT ResetTime the hydrated tick");
        }

        [Test]
        public void FreshPlayer_GameStart_ResetsCountersAndStarts()
        {
            _tm.Hydrate(new GamePlayerStateDTO { current_day = 34, current_tick = 341 });
            GameEvents.SaveStateRestoredFromServer = false;

            Invoke(_tm, "HandleGameStart");

            Assert.IsTrue(_tm.IsRunning, "fresh game clock runs");
            Assert.AreEqual(0, _tm.CurrentDay, "fresh game ResetTime zeroes the day");
            Assert.AreEqual(0, _tm.CurrentTick, "fresh game ResetTime zeroes the tick");
        }

        private static void Invoke(object obj, string method)
        {
            var m = obj.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            m?.Invoke(obj, null);
        }
    }
}
