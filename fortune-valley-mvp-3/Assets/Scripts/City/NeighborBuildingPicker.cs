using System;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;

namespace FortuneValley.City
{
    /// <summary>
    /// Pure-C# deterministic picker. Given a catalog and a seed, produces a stratified trio
    /// (one Small, one Medium, one Large) shuffled into slot order, plus a per-slot rotation
    /// constrained to the slot's preferred-forward cone (+/- one quantized 90-degree step).
    ///
    /// Stability invariant: the order of rng calls below is part of the determinism contract.
    /// Reordering them changes every block's appearance. If the algorithm needs to change,
    /// expect to re-seed every block.
    /// </summary>
    public static class NeighborBuildingPicker
    {
        public const int SlotCount = 3;

        // Three quantized cone steps: -90, 0, +90 degrees off the slot's preferred forward.
        private const int RotationConeStepCount = 3;
        private const float RotationStepDegrees = 90f;

        public static NeighborBuildingPickResult Pick(
            NeighborBuildingCatalogSO catalog,
            int seed,
            Vector3[] preferredForwardsLocal)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (preferredForwardsLocal == null || preferredForwardsLocal.Length != SlotCount)
            {
                throw new ArgumentException(
                    $"preferredForwardsLocal must have length {SlotCount}",
                    nameof(preferredForwardsLocal));
            }

            var rng = new System.Random(seed);

            // Pull one of each size in fixed Small/Medium/Large order. This keeps the rng
            // sequence stable so future shuffle/rotation calls produce identical bytes.
            var picks = new NeighborBuildingSO[SlotCount];
            picks[0] = catalog.GetRandomBySize(NeighborBuildingSize.Small, rng);
            picks[1] = catalog.GetRandomBySize(NeighborBuildingSize.Medium, rng);
            picks[2] = catalog.GetRandomBySize(NeighborBuildingSize.Large, rng);

            // Fisher-Yates shuffle into slot positions so slot 0 isn't always Small.
            var shuffled = new NeighborBuildingSO[SlotCount];
            for (int i = 0; i < SlotCount; i++) shuffled[i] = picks[i];
            for (int i = SlotCount - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                var tmp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = tmp;
            }

            // Per-slot rotation, constrained to the preferred-forward cone.
            var rotations = new Quaternion[SlotCount];
            for (int i = 0; i < SlotCount; i++)
            {
                int step = rng.Next(0, RotationConeStepCount) - 1;
                float deviationDegrees = step * RotationStepDegrees;
                float baseYawDegrees = ComputeYawDegrees(preferredForwardsLocal[i]);
                float finalYawDegrees = baseYawDegrees + deviationDegrees;
                rotations[i] = Quaternion.Euler(0f, finalYawDegrees, 0f);
            }

            return new NeighborBuildingPickResult(shuffled, rotations);
        }

        private static float ComputeYawDegrees(Vector3 forwardLocal)
        {
            // Project to XZ plane (Y ignored for ground-aligned buildings) and convert to yaw.
            return Mathf.Atan2(forwardLocal.x, forwardLocal.z) * Mathf.Rad2Deg;
        }
    }
}
