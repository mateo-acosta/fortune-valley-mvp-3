using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class NeighborBuildingCatalogValidationTests
    {
        // OnValidate is private, so we invoke it via reflection to simulate the editor calling it
        // when the asset is edited.
        private static void InvokeOnValidate(NeighborBuildingCatalogSO catalog)
        {
            var method = typeof(NeighborBuildingCatalogSO)
                .GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(catalog, null);
        }

        [Test]
        public void OnValidate_EmptySmallBucket_LogsError()
        {
            var medium = new[] { CityTestHelpers.MakeBuilding("M1", NeighborBuildingSize.Medium) };
            var large = new[] { CityTestHelpers.MakeBuilding("L1", NeighborBuildingSize.Large) };
            var catalog = CityTestHelpers.MakeCatalog(new NeighborBuildingSO[0], medium, large);

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Small bucket is empty"));
            InvokeOnValidate(catalog);
        }

        [Test]
        public void OnValidate_NullEntryInBucket_LogsError()
        {
            var small = new[]
            {
                CityTestHelpers.MakeBuilding("S1", NeighborBuildingSize.Small),
                null,
            };
            var medium = new[] { CityTestHelpers.MakeBuilding("M1", NeighborBuildingSize.Medium) };
            var large = new[] { CityTestHelpers.MakeBuilding("L1", NeighborBuildingSize.Large) };
            var catalog = CityTestHelpers.MakeCatalog(small, medium, large);

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Small bucket has a null entry at index 1"));
            InvokeOnValidate(catalog);
        }

        [Test]
        public void OnValidate_FullyPopulatedCatalog_LogsNothing()
        {
            var small = new[] { CityTestHelpers.MakeBuilding("S1", NeighborBuildingSize.Small) };
            var medium = new[] { CityTestHelpers.MakeBuilding("M1", NeighborBuildingSize.Medium) };
            var large = new[] { CityTestHelpers.MakeBuilding("L1", NeighborBuildingSize.Large) };
            var catalog = CityTestHelpers.MakeCatalog(small, medium, large);

            // No LogAssert.Expect calls; if OnValidate logs anything, the test framework
            // surfaces it as an unexpected log and the test fails.
            InvokeOnValidate(catalog);
        }
    }
}
