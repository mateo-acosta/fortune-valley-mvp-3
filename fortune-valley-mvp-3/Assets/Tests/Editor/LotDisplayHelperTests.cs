using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.UI;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class LotDisplayHelperTests
    {
        private List<CityLotDefinition> _lots;

        [SetUp]
        public void SetUp()
        {
            _lots = new List<CityLotDefinition>();
            _lots.Add(CreateLot("lot_01", "Downtown Corner"));
            _lots.Add(CreateLot("lot_02", "Park Avenue"));
            _lots.Add(CreateLot("lot_03", "Harbor District"));
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _lots.Count; i++)
                Object.DestroyImmediate(_lots[i]);
        }

        [Test]
        public void FoundById_ReturnsDisplayName()
        {
            string result = LotDisplayHelper.GetDisplayName(_lots, "lot_02");
            Assert.AreEqual("Park Avenue", result);
        }

        [Test]
        public void NotFound_ReturnsRawLotId()
        {
            string result = LotDisplayHelper.GetDisplayName(_lots, "lot_99");
            Assert.AreEqual("lot_99", result);
        }

        [Test]
        public void NullList_ReturnsRawLotId()
        {
            string result = LotDisplayHelper.GetDisplayName(null, "lot_01");
            Assert.AreEqual("lot_01", result);
        }

        [Test]
        public void NullLotId_ReturnsEmpty()
        {
            string result = LotDisplayHelper.GetDisplayName(_lots, null);
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void EmptyLotId_ReturnsEmpty()
        {
            string result = LotDisplayHelper.GetDisplayName(_lots, "");
            Assert.AreEqual(string.Empty, result);
        }

        private static CityLotDefinition CreateLot(string lotId, string displayName)
        {
            var lot = ScriptableObject.CreateInstance<CityLotDefinition>();
            lot.name = displayName;

            var so = new UnityEditor.SerializedObject(lot);
            so.FindProperty("_lotId").stringValue = lotId;
            so.FindProperty("_displayName").stringValue = displayName;
            so.ApplyModifiedPropertiesWithoutUndo();

            return lot;
        }
    }
}
