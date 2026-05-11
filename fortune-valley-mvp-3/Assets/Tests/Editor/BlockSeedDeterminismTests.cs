using NUnit.Framework;
using UnityEngine;
using FortuneValley.City;
using FortuneValley.Core;
using FortuneValley.Core.Hashing;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Tests
{
    /// <summary>
    /// End-to-end determinism for the editor seed pipeline (LotId -> hash -> picker).
    /// Pure data flow - no GameObject instantiation. Catches any drift in the picker,
    /// hash, or call ordering that would re-randomize previously-seeded blocks.
    /// </summary>
    [TestFixture]
    public class BlockSeedDeterminismTests
    {
        private NeighborBuildingCatalogSO _catalog;
        private Vector3[] _forwards;

        [SetUp]
        public void SetUp()
        {
            var small = new[]
            {
                CityTestHelpers.MakeBuilding("S1", NeighborBuildingSize.Small),
                CityTestHelpers.MakeBuilding("S2", NeighborBuildingSize.Small),
            };
            var medium = new[]
            {
                CityTestHelpers.MakeBuilding("M1", NeighborBuildingSize.Medium),
                CityTestHelpers.MakeBuilding("M2", NeighborBuildingSize.Medium),
            };
            var large = new[]
            {
                CityTestHelpers.MakeBuilding("L1", NeighborBuildingSize.Large),
                CityTestHelpers.MakeBuilding("L2", NeighborBuildingSize.Large),
            };
            _catalog = CityTestHelpers.MakeCatalog(small, medium, large);
            _forwards = new[] { Vector3.forward, Vector3.forward, Vector3.forward };
        }

        [Test]
        public void Pipeline_AllNineteenLotIds_ProduceIdenticalResultsAcrossRuns()
        {
            for (int blockNumber = 1; blockNumber <= 19; blockNumber++)
            {
                string lotId = $"Lot_Block{blockNumber:D2}";
                int seed = DeterministicHash.FromString(lotId);

                var first = NeighborBuildingPicker.Pick(_catalog, seed, _forwards);
                var second = NeighborBuildingPicker.Pick(_catalog, seed, _forwards);

                for (int i = 0; i < NeighborBuildingPicker.SlotCount; i++)
                {
                    Assert.AreSame(
                        first.Buildings[i],
                        second.Buildings[i],
                        $"{lotId} slot {i} building drifted between runs");
                    Assert.AreEqual(
                        first.Rotations[i].eulerAngles,
                        second.Rotations[i].eulerAngles,
                        $"{lotId} slot {i} rotation drifted between runs");
                }
            }
        }
    }
}
