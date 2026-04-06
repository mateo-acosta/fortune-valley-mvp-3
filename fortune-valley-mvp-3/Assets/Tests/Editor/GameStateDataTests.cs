using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Tests for GameStateData serialization and default values.
    /// Guards against JsonUtility silent-drop bugs.
    /// </summary>
    [TestFixture]
    public class GameStateDataTests
    {
        [Test]
        public void SerializeAndDeserialize_AllFieldsSurvive()
        {
            var original = new GameStateData
            {
                Balance = 1234.56f,
                RestaurantLevel = 3,
                RivalBalance = 789.01f,
                CurrentTick = 42,
                CurrentDay = 4,
                LastSaveTimestamp = 1712345678L
            };
            original.OwnedLotIds.Add("lot_0");
            original.OwnedLotIds.Add("lot_2");
            original.RivalOwnedLotIds.Add("lot_1");

            string json = JsonUtility.ToJson(original);
            var restored = new GameStateData();
            JsonUtility.FromJsonOverwrite(json, restored);

            Assert.AreEqual(original.Balance, restored.Balance, 0.01f);
            Assert.AreEqual(original.RestaurantLevel, restored.RestaurantLevel);
            Assert.AreEqual(original.RivalBalance, restored.RivalBalance, 0.01f);
            Assert.AreEqual(original.CurrentTick, restored.CurrentTick);
            Assert.AreEqual(original.CurrentDay, restored.CurrentDay);
            Assert.AreEqual(original.LastSaveTimestamp, restored.LastSaveTimestamp);
            Assert.AreEqual(2, restored.OwnedLotIds.Count);
            Assert.AreEqual("lot_0", restored.OwnedLotIds[0]);
            Assert.AreEqual("lot_2", restored.OwnedLotIds[1]);
            Assert.AreEqual(1, restored.RivalOwnedLotIds.Count);
            Assert.AreEqual("lot_1", restored.RivalOwnedLotIds[0]);
        }

        [Test]
        public void DefaultValues_ListsAreNotNull()
        {
            var data = new GameStateData();

            Assert.IsNotNull(data.OwnedLotIds, "OwnedLotIds should be initialized to empty list, not null");
            Assert.IsNotNull(data.RivalOwnedLotIds, "RivalOwnedLotIds should be initialized to empty list, not null");
            Assert.AreEqual(0, data.OwnedLotIds.Count);
            Assert.AreEqual(0, data.RivalOwnedLotIds.Count);
        }
    }
}
