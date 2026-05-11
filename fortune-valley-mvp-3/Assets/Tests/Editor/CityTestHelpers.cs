using System.Reflection;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Shared builders for City-related tests. Uses reflection to populate private serialized
    /// fields on ScriptableObjects so tests don't depend on UnityEditor APIs.
    /// </summary>
    internal static class CityTestHelpers
    {
        public static NeighborBuildingSO MakeBuilding(string displayName, NeighborBuildingSize size)
        {
            var b = ScriptableObject.CreateInstance<NeighborBuildingSO>();
            b.name = displayName;
            SetPrivate(b, "_displayName", displayName);
            SetPrivate(b, "_size", size);
            return b;
        }

        public static NeighborBuildingCatalogSO MakeCatalog(
            NeighborBuildingSO[] small,
            NeighborBuildingSO[] medium,
            NeighborBuildingSO[] large)
        {
            var c = ScriptableObject.CreateInstance<NeighborBuildingCatalogSO>();
            SetPrivate(c, "_smallBuildings", small);
            SetPrivate(c, "_mediumBuildings", medium);
            SetPrivate(c, "_largeBuildings", large);
            return c;
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
