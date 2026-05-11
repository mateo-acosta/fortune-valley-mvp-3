using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using FortuneValley.Core;
using FortuneValley.Core.Hashing;
using FortuneValley.City;
using Debug = UnityEngine.Debug;

namespace FortuneValley.Editor
{
    /// <summary>
    /// Editor utility that walks every BlockController in the open scene and seeds its
    /// CosmeticSlot_* anchors with a deterministic, size-stratified neighbor trio plus
    /// per-slot rotations. Run after first wiring the scene or after editing the catalog.
    ///
    /// Output is committed to the scene file (children become prefab instances under the
    /// slots, and _neighborBuildings[] is populated via SerializedObject so changes save).
    /// </summary>
    public static class BlockSceneSeeder
    {
        private const string MenuPath = "Fortune Valley/Seed Cosmetic Neighbors";
        private const string NeighborBuildingsFieldName = "_neighborBuildings";

        [MenuItem(MenuPath)]
        public static void SeedOpenScene()
        {
            var catalog = ResolveCatalog();
            if (catalog == null) return;

            var stopwatch = Stopwatch.StartNew();
            var blocks = Object.FindObjectsByType<BlockController>(FindObjectsSortMode.None);

            int seededCount = 0;
            int skippedCount = 0;
            var dirtyScenes = new HashSet<UnityEngine.SceneManagement.Scene>();

            foreach (var block in blocks)
            {
                bool seeded = SeedBlock(block, catalog);
                if (seeded)
                {
                    seededCount++;
                    dirtyScenes.Add(block.gameObject.scene);
                }
                else
                {
                    skippedCount++;
                }
            }

            foreach (var scene in dirtyScenes)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }

            stopwatch.Stop();
            Debug.Log($"[BlockSceneSeeder] Seeded {seededCount} blocks, skipped {skippedCount}, " +
                      $"{stopwatch.ElapsedMilliseconds}ms. Save the scene to commit.");
        }

        private static NeighborBuildingCatalogSO ResolveCatalog()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(NeighborBuildingCatalogSO)}");
            if (guids.Length == 0)
            {
                Debug.LogError("[BlockSceneSeeder] No NeighborBuildingCatalogSO found in project. " +
                               "Create one at Assets/Data/Buildings/NeighborBuildingCatalog.asset and populate it.");
                return null;
            }
            if (guids.Length > 1)
            {
                Debug.LogError($"[BlockSceneSeeder] Found {guids.Length} NeighborBuildingCatalogSO assets. " +
                               "Expected exactly one. Delete duplicates or remove the seeder's auto-resolve.");
                return null;
            }
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<NeighborBuildingCatalogSO>(path);
        }

        private static bool SeedBlock(BlockController block, NeighborBuildingCatalogSO catalog)
        {
            string seedSource = block.GetSeedSource();
            if (string.IsNullOrEmpty(seedSource))
            {
                Debug.LogWarning($"[BlockSceneSeeder] Skipping '{block.name}': no _ownedLot and no _seedOverride. " +
                                 "Set one or the other before seeding.", block);
                return false;
            }

            var slots = block.CosmeticSlots;
            if (slots == null || slots.Length != NeighborBuildingPicker.SlotCount)
            {
                Debug.LogWarning($"[BlockSceneSeeder] Skipping '{block.name}': _cosmeticSlots length is " +
                                 $"{(slots == null ? 0 : slots.Length)}, expected {NeighborBuildingPicker.SlotCount}.", block);
                return false;
            }

            var preferredForwards = new Vector3[NeighborBuildingPicker.SlotCount];
            for (int i = 0; i < NeighborBuildingPicker.SlotCount; i++)
            {
                if (slots[i] == null)
                {
                    Debug.LogWarning($"[BlockSceneSeeder] Skipping '{block.name}': _cosmeticSlots[{i}] is null.", block);
                    return false;
                }
                var anchor = slots[i].GetComponent<CosmeticSlotAnchor>();
                preferredForwards[i] = anchor != null ? anchor.PreferredForwardLocal : Vector3.forward;
                if (anchor == null)
                {
                    Debug.LogWarning($"[BlockSceneSeeder] '{block.name}' slot {i} ('{slots[i].name}') has no CosmeticSlotAnchor. " +
                                     "Using Vector3.forward as fallback. Add the component for road-facing rotations.", block);
                }
            }

            int seed = DeterministicHash.FromString(seedSource);
            var pickResult = NeighborBuildingPicker.Pick(catalog, seed, preferredForwards);

            // Destroy any existing children under each slot before instantiating the picks.
            for (int i = 0; i < NeighborBuildingPicker.SlotCount; i++)
            {
                var slot = slots[i];
                for (int c = slot.childCount - 1; c >= 0; c--)
                {
                    Object.DestroyImmediate(slot.GetChild(c).gameObject);
                }
            }

            // Instantiate the picks as prefab instances under each slot, then write the
            // GameObject refs into _neighborBuildings via SerializedObject so the scene saves.
            var so = new SerializedObject(block);
            var neighborsProp = so.FindProperty(NeighborBuildingsFieldName);
            if (neighborsProp == null || !neighborsProp.isArray)
            {
                Debug.LogError($"[BlockSceneSeeder] Could not find serialized array '{NeighborBuildingsFieldName}' on BlockController. Aborting block.", block);
                return false;
            }
            neighborsProp.arraySize = NeighborBuildingPicker.SlotCount;

            for (int i = 0; i < NeighborBuildingPicker.SlotCount; i++)
            {
                var pick = pickResult.Buildings[i];
                if (pick == null || pick.Prefab == null)
                {
                    Debug.LogWarning($"[BlockSceneSeeder] '{block.name}' slot {i}: catalog returned null. " +
                                     "Slot left empty - dirt mesh will show pre-tier.", block);
                    neighborsProp.GetArrayElementAtIndex(i).objectReferenceValue = null;
                    continue;
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(pick.Prefab, slots[i]);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = pickResult.Rotations[i];
                instance.transform.localScale = Vector3.one;
                Undo.RegisterCreatedObjectUndo(instance, "Seed Neighbor Building");

                neighborsProp.GetArrayElementAtIndex(i).objectReferenceValue = instance;
            }

            so.ApplyModifiedProperties();
            return true;
        }
    }
}
