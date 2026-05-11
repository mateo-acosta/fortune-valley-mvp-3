using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using FortuneValley.City;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class NeighborBuildingPickerTests
    {
        private NeighborBuildingCatalogSO _catalog;
        private Vector3[] _forwards;

        [SetUp]
        public void SetUp()
        {
            // 3 buildings per bucket - enough that a shuffle has freedom but small
            // enough that variety assertions are meaningful.
            var small = new[]
            {
                CityTestHelpers.MakeBuilding("S1", NeighborBuildingSize.Small),
                CityTestHelpers.MakeBuilding("S2", NeighborBuildingSize.Small),
                CityTestHelpers.MakeBuilding("S3", NeighborBuildingSize.Small),
            };
            var medium = new[]
            {
                CityTestHelpers.MakeBuilding("M1", NeighborBuildingSize.Medium),
                CityTestHelpers.MakeBuilding("M2", NeighborBuildingSize.Medium),
                CityTestHelpers.MakeBuilding("M3", NeighborBuildingSize.Medium),
            };
            var large = new[]
            {
                CityTestHelpers.MakeBuilding("L1", NeighborBuildingSize.Large),
                CityTestHelpers.MakeBuilding("L2", NeighborBuildingSize.Large),
                CityTestHelpers.MakeBuilding("L3", NeighborBuildingSize.Large),
            };
            _catalog = CityTestHelpers.MakeCatalog(small, medium, large);
            _forwards = new[] { Vector3.forward, Vector3.forward, Vector3.forward };
        }

        [Test]
        public void Pick_SameSeed_ProducesIdenticalBuildingsAndRotations()
        {
            var a = NeighborBuildingPicker.Pick(_catalog, 12345, _forwards);
            var b = NeighborBuildingPicker.Pick(_catalog, 12345, _forwards);

            for (int i = 0; i < NeighborBuildingPicker.SlotCount; i++)
            {
                Assert.AreSame(a.Buildings[i], b.Buildings[i], $"Buildings[{i}] differs across same-seed runs");
                Assert.AreEqual(a.Rotations[i].eulerAngles, b.Rotations[i].eulerAngles, $"Rotations[{i}] differs across same-seed runs");
            }
        }

        [Test]
        public void Pick_HundredSeeds_ProducesAtLeastFiftyUniqueTrios()
        {
            var trios = new HashSet<string>();
            for (int seed = 0; seed < 100; seed++)
            {
                var r = NeighborBuildingPicker.Pick(_catalog, seed, _forwards);
                string key = $"{r.Buildings[0].name}|{r.Buildings[1].name}|{r.Buildings[2].name}";
                trios.Add(key);
            }
            Assert.GreaterOrEqual(trios.Count, 50,
                $"Picker produced only {trios.Count} unique trios across 100 seeds; expected at least 50.");
        }

        [Test]
        public void Pick_PopulatedCatalog_ReturnsExactlyOneOfEachSize()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var r = NeighborBuildingPicker.Pick(_catalog, seed, _forwards);

                int small = 0, medium = 0, large = 0;
                for (int i = 0; i < NeighborBuildingPicker.SlotCount; i++)
                {
                    Assert.NotNull(r.Buildings[i], $"Building at slot {i} is null for seed {seed}");
                    if (r.Buildings[i].Size == NeighborBuildingSize.Small) small++;
                    if (r.Buildings[i].Size == NeighborBuildingSize.Medium) medium++;
                    if (r.Buildings[i].Size == NeighborBuildingSize.Large) large++;
                }
                Assert.AreEqual(1, small, $"Seed {seed}: expected 1 Small, got {small}");
                Assert.AreEqual(1, medium, $"Seed {seed}: expected 1 Medium, got {medium}");
                Assert.AreEqual(1, large, $"Seed {seed}: expected 1 Large, got {large}");
            }
        }

        [Test]
        public void Pick_EmptySmallBucket_PreservesOtherSlots()
        {
            var medium = new[] { CityTestHelpers.MakeBuilding("M1", NeighborBuildingSize.Medium) };
            var large = new[] { CityTestHelpers.MakeBuilding("L1", NeighborBuildingSize.Large) };
            var partialCatalog = CityTestHelpers.MakeCatalog(new NeighborBuildingSO[0], medium, large);

            var r = NeighborBuildingPicker.Pick(partialCatalog, 7, _forwards);

            int nullCount = 0;
            int nonNullCount = 0;
            for (int i = 0; i < NeighborBuildingPicker.SlotCount; i++)
            {
                if (r.Buildings[i] == null) nullCount++;
                else nonNullCount++;
            }
            Assert.AreEqual(1, nullCount, "Expected exactly one null slot when Small bucket is empty");
            Assert.AreEqual(2, nonNullCount, "Expected two non-null slots (Medium + Large)");
        }

        [Test]
        public void Pick_AllEmptyCatalog_ReturnsAllNullsWithoutThrowing()
        {
            var emptyCatalog = CityTestHelpers.MakeCatalog(
                new NeighborBuildingSO[0], new NeighborBuildingSO[0], new NeighborBuildingSO[0]);

            NeighborBuildingPickResult r = default;
            Assert.DoesNotThrow(() => r = NeighborBuildingPicker.Pick(emptyCatalog, 42, _forwards));

            for (int i = 0; i < NeighborBuildingPicker.SlotCount; i++)
            {
                Assert.IsNull(r.Buildings[i], $"Building at slot {i} should be null for empty catalog");
            }
        }

        [Test]
        public void Pick_RotationsLieInPreferredForwardConePlusMinusNinety()
        {
            // Preferred forward = +Z gives a base yaw of 0. Allowed final yaws are -90, 0, +90.
            for (int seed = 0; seed < 30; seed++)
            {
                var r = NeighborBuildingPicker.Pick(_catalog, seed, _forwards);
                for (int i = 0; i < NeighborBuildingPicker.SlotCount; i++)
                {
                    float yaw = NormalizeYaw(r.Rotations[i].eulerAngles.y);
                    Assert.IsTrue(
                        Mathf.Approximately(yaw, -90f) || Mathf.Approximately(yaw, 0f) || Mathf.Approximately(yaw, 90f),
                        $"Seed {seed} slot {i}: yaw {yaw} not in {{-90, 0, 90}}");
                }
            }
        }

        [Test]
        public void Pick_DifferentPreferredForward_ShiftsRotationCone()
        {
            // Preferred forward = +X gives base yaw of 90. Allowed yaws shift to {0, 90, 180}.
            var rightForwards = new[] { Vector3.right, Vector3.right, Vector3.right };
            var r = NeighborBuildingPicker.Pick(_catalog, 0, rightForwards);

            for (int i = 0; i < NeighborBuildingPicker.SlotCount; i++)
            {
                float yaw = NormalizeYaw(r.Rotations[i].eulerAngles.y);
                Assert.IsTrue(
                    Mathf.Approximately(yaw, 0f) || Mathf.Approximately(yaw, 90f) || Mathf.Approximately(yaw, 180f),
                    $"Slot {i}: yaw {yaw} not in {{0, 90, 180}} for +X preferred forward");
            }
        }

        // Convert [0, 360) yaw into [-180, 180] for easier equality checks.
        private static float NormalizeYaw(float yaw)
        {
            float n = yaw % 360f;
            if (n > 180f) n -= 360f;
            if (n <= -180f) n += 360f;
            return n;
        }
    }
}
