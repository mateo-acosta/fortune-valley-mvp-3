using System.Collections.Generic;
using UnityEngine;

namespace FortuneValley.Grid
{
    /// <summary>
    /// Generates and manages the city border decorations.
    /// Places rocks, trees, and other objects around the city perimeter.
    /// Also provides camera bounds information.
    /// </summary>
    public class CityBorder : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        [Header("Configuration")]
        [Tooltip("Border configuration asset")]
        [SerializeField] private BorderConfig _borderConfig;

        [Tooltip("Grid configuration asset")]
        [SerializeField] private IsometricGridConfig _gridConfig;

        [Header("Generation Settings")]
        [Tooltip("Random seed for reproducible generation (0 = random)")]
        [SerializeField] private int _seed = 0;

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool _showBoundsGizmo = true;
#endif

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private List<GameObject> _spawnedObjects = new List<GameObject>();
        private Transform _borderContainer;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Get the camera bounds for the playable area.
        /// </summary>
        public Bounds CameraBounds
        {
            get
            {
                if (_gridConfig == null) return new Bounds(Vector3.zero, Vector3.one * 10f);

                // Calculate world bounds of the playable grid
                Vector3 min = IsometricUtils.GridToWorld(0, 0, _gridConfig);
                Vector3 max = IsometricUtils.GridToWorld(_gridConfig.GridWidth - 1, _gridConfig.GridHeight - 1, _gridConfig);

                // Include all corners for proper isometric bounds
                Vector3 corner1 = IsometricUtils.GridToWorld(_gridConfig.GridWidth - 1, 0, _gridConfig);
                Vector3 corner2 = IsometricUtils.GridToWorld(0, _gridConfig.GridHeight - 1, _gridConfig);

                float minX = Mathf.Min(min.x, max.x, corner1.x, corner2.x);
                float maxX = Mathf.Max(min.x, max.x, corner1.x, corner2.x);
                float minZ = Mathf.Min(min.z, max.z, corner1.z, corner2.z);
                float maxZ = Mathf.Max(min.z, max.z, corner1.z, corner2.z);

                float padding = _borderConfig != null ? _borderConfig.CameraPadding : 2f;
                Vector3 center = new Vector3((minX + maxX) / 2f, 0f, (minZ + maxZ) / 2f);
                Vector3 size = new Vector3(maxX - minX + padding * 2f, 50f, maxZ - minZ + padding * 2f);

                return new Bounds(center, size);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Awake()
        {
            // Find or create container for border objects
            _borderContainer = transform.Find("BorderObjects");
            if (_borderContainer == null)
            {
                var containerGO = new GameObject("BorderObjects");
                containerGO.transform.SetParent(transform);
                _borderContainer = containerGO.transform;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Generate the border decorations.
        /// </summary>
        public void GenerateBorder()
        {
            if (_borderConfig == null || _gridConfig == null)
            {
                Debug.LogWarning("[CityBorder] Missing BorderConfig or GridConfig!");
                return;
            }

            if (_borderConfig.BorderPrefabs == null || _borderConfig.BorderPrefabs.Length == 0)
            {
                Debug.LogWarning("[CityBorder] No border prefabs configured!");
                return;
            }

            // Clear existing border objects
            ClearBorder();

            // Initialize random seed
            if (_seed != 0)
            {
                Random.InitState(_seed);
            }

            // Get grid dimensions
            int width = _gridConfig.GridWidth;
            int height = _gridConfig.GridHeight;
            int thickness = _borderConfig.BorderThickness;

            // Generate border on all four edges
            // Top edge (y = -thickness to -1)
            for (int y = -thickness; y < 0; y++)
            {
                for (int x = -thickness; x < width + thickness; x++)
                {
                    SpawnBorderObject(x, y);
                }
            }

            // Bottom edge (y = height to height + thickness - 1)
            for (int y = height; y < height + thickness; y++)
            {
                for (int x = -thickness; x < width + thickness; x++)
                {
                    SpawnBorderObject(x, y);
                }
            }

            // Left edge (x = -thickness to -1, excluding corners already done)
            for (int x = -thickness; x < 0; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    SpawnBorderObject(x, y);
                }
            }

            // Right edge (x = width to width + thickness - 1, excluding corners already done)
            for (int x = width; x < width + thickness; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    SpawnBorderObject(x, y);
                }
            }

            Debug.Log($"[CityBorder] Generated {_spawnedObjects.Count} border objects");
        }

        /// <summary>
        /// Clear all border decorations.
        /// </summary>
        public void ClearBorder()
        {
            foreach (var obj in _spawnedObjects)
            {
                if (obj != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(obj);
                    }
                    else
                    {
                        DestroyImmediate(obj);
                    }
                }
            }

            _spawnedObjects.Clear();

            // Also clear any orphaned children
            if (_borderContainer != null)
            {
                while (_borderContainer.childCount > 0)
                {
                    var child = _borderContainer.GetChild(0);
                    if (Application.isPlaying)
                    {
                        Destroy(child.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
            }
        }

        /// <summary>
        /// Check if a world position is within the playable area.
        /// </summary>
        public bool IsWithinBounds(Vector3 worldPosition)
        {
            return CameraBounds.Contains(worldPosition);
        }

        /// <summary>
        /// Clamp a position to stay within camera bounds.
        /// </summary>
        public Vector3 ClampToBounds(Vector3 position)
        {
            Bounds bounds = CameraBounds;
            return new Vector3(
                Mathf.Clamp(position.x, bounds.min.x, bounds.max.x),
                position.y,
                Mathf.Clamp(position.z, bounds.min.z, bounds.max.z)
            );
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════

        private void SpawnBorderObject(int gridX, int gridY)
        {
            // Check density - may skip some tiles
            if (Random.value > _borderConfig.Density)
            {
                return;
            }

            GameObject prefab = _borderConfig.GetRandomPrefab();
            if (prefab == null) return;

            // Calculate world position
            Vector3 worldPos = IsometricUtils.GridToWorld(gridX, gridY, _gridConfig);
            worldPos += _borderConfig.GetRandomJitter();
            worldPos.y = _borderConfig.HeightOffset;

            // Instantiate
            GameObject obj = Instantiate(prefab, worldPos, _borderConfig.GetRandomRotation(), _borderContainer);
            obj.name = $"Border_{gridX}_{gridY}";

            // Apply random scale
            float scale = _borderConfig.GetRandomScale();
            obj.transform.localScale = Vector3.one * scale;

            _spawnedObjects.Add(obj);
        }

        // ═══════════════════════════════════════════════════════════════
        // EDITOR GIZMOS
        // ═══════════════════════════════════════════════════════════════

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!_showBoundsGizmo || _gridConfig == null) return;

            // Draw playable area bounds
            Bounds bounds = CameraBounds;
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            // Draw border area
            if (_borderConfig != null)
            {
                int thickness = _borderConfig.BorderThickness;
                int width = _gridConfig.GridWidth;
                int height = _gridConfig.GridHeight;

                Gizmos.color = new Color(0.6f, 0.4f, 0.2f, 0.2f);

                // Draw border region corners
                Vector3 outerMin = IsometricUtils.GridToWorld(-thickness, -thickness, _gridConfig);
                Vector3 outerMax = IsometricUtils.GridToWorld(width + thickness - 1, height + thickness - 1, _gridConfig);
                Vector3 innerMin = IsometricUtils.GridToWorld(0, 0, _gridConfig);
                Vector3 innerMax = IsometricUtils.GridToWorld(width - 1, height - 1, _gridConfig);

                // Just draw a line around the outer boundary
                Gizmos.color = new Color(0.8f, 0.4f, 0.1f, 0.8f);
                DrawIsometricRect(-thickness, -thickness, width + thickness * 2, height + thickness * 2);

                // And the inner boundary
                Gizmos.color = new Color(0f, 0.8f, 0f, 0.8f);
                DrawIsometricRect(0, 0, width, height);
            }
        }

        private void DrawIsometricRect(int startX, int startY, int width, int height)
        {
            Vector3 topLeft = IsometricUtils.GridToWorld(startX, startY, _gridConfig);
            Vector3 topRight = IsometricUtils.GridToWorld(startX + width - 1, startY, _gridConfig);
            Vector3 bottomLeft = IsometricUtils.GridToWorld(startX, startY + height - 1, _gridConfig);
            Vector3 bottomRight = IsometricUtils.GridToWorld(startX + width - 1, startY + height - 1, _gridConfig);

            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);
        }
#endif
    }
}
