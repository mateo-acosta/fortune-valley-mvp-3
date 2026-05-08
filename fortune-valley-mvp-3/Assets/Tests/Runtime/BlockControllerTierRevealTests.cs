using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using FortuneValley.City;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Regression test for BlockController's tier-based neighbor reveal. The seeder change
    /// keeps the runtime path identical (still toggles _neighborBuildings and _neighborVacantMeshes
    /// on OnLotTierChanged), but this test locks that contract so future refactors that touch
    /// either Awake-time wiring or the event handler can't silently break it.
    /// </summary>
    [TestFixture]
    public class BlockControllerTierRevealTests
    {
        private const string TestLotId = "Lot_Test01";

        private GameObject _blockGO;
        private GameObject[] _neighbors;
        private GameObject[] _dirtMeshes;
        private CityLotDefinition _lot;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();

            // Build a disabled hierarchy so we can wire serialized fields BEFORE OnEnable runs.
            _blockGO = new GameObject("TestBlock");
            _blockGO.SetActive(false);

            _neighbors = new GameObject[3];
            _dirtMeshes = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                _neighbors[i] = new GameObject($"Neighbor_{i}");
                _neighbors[i].transform.SetParent(_blockGO.transform);
                _dirtMeshes[i] = new GameObject($"Dirt_{i}");
                _dirtMeshes[i].transform.SetParent(_blockGO.transform);
            }

            _lot = ScriptableObject.CreateInstance<CityLotDefinition>();
            SetPrivate(_lot, "_lotId", TestLotId);
            SetPrivate(_lot, "_displayName", "Test Lot");

            var block = _blockGO.AddComponent<BlockController>();
            SetPrivate(block, "_ownedLot", _lot);
            SetPrivate(block, "_neighborBuildings", _neighbors);
            SetPrivate(block, "_neighborVacantMeshes", _dirtMeshes);

            // Activate - this fires OnEnable, which subscribes to events and calls
            // ResetNeighborVisibility (all neighbors inactive, all dirt meshes active).
            _blockGO.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.ClearAllSubscriptions();
            if (_blockGO != null) Object.DestroyImmediate(_blockGO);
            for (int i = 0; i < _neighbors.Length; i++)
            {
                if (_neighbors[i] != null) Object.DestroyImmediate(_neighbors[i]);
                if (_dirtMeshes[i] != null) Object.DestroyImmediate(_dirtMeshes[i]);
            }
            if (_lot != null) Object.DestroyImmediate(_lot);
        }

        [Test]
        public void OnEnable_StartsWithAllNeighborsHiddenAndAllDirtVisible()
        {
            for (int i = 0; i < 3; i++)
            {
                Assert.IsFalse(_neighbors[i].activeSelf, $"Neighbor {i} should be hidden initially");
                Assert.IsTrue(_dirtMeshes[i].activeSelf, $"Dirt {i} should be visible initially");
            }
        }

        [Test]
        public void TierOne_RevealsSlotZeroOnly()
        {
            GameEvents.RaiseLotTierChanged(TestLotId, 1);

            Assert.IsTrue(_neighbors[0].activeSelf, "Neighbor 0 should be revealed at tier 1");
            Assert.IsFalse(_neighbors[1].activeSelf, "Neighbor 1 should still be hidden at tier 1");
            Assert.IsFalse(_neighbors[2].activeSelf, "Neighbor 2 should still be hidden at tier 1");

            Assert.IsFalse(_dirtMeshes[0].activeSelf, "Dirt 0 should be hidden at tier 1");
            Assert.IsTrue(_dirtMeshes[1].activeSelf, "Dirt 1 should still be visible at tier 1");
            Assert.IsTrue(_dirtMeshes[2].activeSelf, "Dirt 2 should still be visible at tier 1");
        }

        [Test]
        public void TierTwo_RevealsSlotsZeroAndOne()
        {
            GameEvents.RaiseLotTierChanged(TestLotId, 2);

            Assert.IsTrue(_neighbors[0].activeSelf);
            Assert.IsTrue(_neighbors[1].activeSelf);
            Assert.IsFalse(_neighbors[2].activeSelf);

            Assert.IsFalse(_dirtMeshes[0].activeSelf);
            Assert.IsFalse(_dirtMeshes[1].activeSelf);
            Assert.IsTrue(_dirtMeshes[2].activeSelf);
        }

        [Test]
        public void TierThree_RevealsAllSlots()
        {
            GameEvents.RaiseLotTierChanged(TestLotId, 3);

            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(_neighbors[i].activeSelf, $"Neighbor {i} should be revealed at tier 3");
                Assert.IsFalse(_dirtMeshes[i].activeSelf, $"Dirt {i} should be hidden at tier 3");
            }
        }

        [Test]
        public void TierEvent_ForDifferentLotId_DoesNotChangeState()
        {
            GameEvents.RaiseLotTierChanged("SomeOtherLot", 3);

            for (int i = 0; i < 3; i++)
            {
                Assert.IsFalse(_neighbors[i].activeSelf, $"Neighbor {i} should remain hidden");
                Assert.IsTrue(_dirtMeshes[i].activeSelf, $"Dirt {i} should remain visible");
            }
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            var f = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (f == null) throw new System.InvalidOperationException(
                $"Field '{fieldName}' not found on {target.GetType().Name}");
            f.SetValue(target, value);
        }
    }
}
