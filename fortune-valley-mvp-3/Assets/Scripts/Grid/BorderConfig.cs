using UnityEngine;

namespace FortuneValley.Grid
{
    /// <summary>
    /// Configuration for the city border appearance.
    /// Controls which prefabs to use, randomization, and density.
    /// </summary>
    [CreateAssetMenu(fileName = "BorderConfig", menuName = "Fortune Valley/Grid/Border Config")]
    public class BorderConfig : ScriptableObject
    {
        // ═══════════════════════════════════════════════════════════════
        // BORDER PREFABS
        // ═══════════════════════════════════════════════════════════════

        [Header("Border Prefabs")]
        [Tooltip("Prefabs to randomly place on border tiles (rocks, trees, boulders)")]
        [SerializeField] private GameObject[] _borderPrefabs;

        [Tooltip("Weight for each prefab (higher = more likely to be selected)")]
        [SerializeField] private float[] _prefabWeights;

        // ═══════════════════════════════════════════════════════════════
        // BORDER DIMENSIONS
        // ═══════════════════════════════════════════════════════════════

        [Header("Border Dimensions")]
        [Tooltip("How many tiles thick the border should be")]
        [Range(1, 5)]
        [SerializeField] private int _borderThickness = 2;

        [Tooltip("Density of objects per border tile (1.0 = one per tile)")]
        [Range(0.5f, 3f)]
        [SerializeField] private float _density = 1.0f;

        // ═══════════════════════════════════════════════════════════════
        // RANDOMIZATION
        // ═══════════════════════════════════════════════════════════════

        [Header("Randomization")]
        [Tooltip("Random rotation on Y axis (0 = fixed, 360 = full rotation)")]
        [Range(0f, 360f)]
        [SerializeField] private float _rotationVariance = 360f;

        [Tooltip("Random scale multiplier range")]
        [SerializeField] private Vector2 _scaleRange = new Vector2(0.8f, 1.2f);

        [Tooltip("Random position jitter within tile (for natural look)")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _positionJitter = 0.1f;

        [Tooltip("Y offset from ground")]
        [SerializeField] private float _heightOffset = 0f;

        // ═══════════════════════════════════════════════════════════════
        // CAMERA BOUNDS
        // ═══════════════════════════════════════════════════════════════

        [Header("Camera Bounds")]
        [Tooltip("Extra padding for camera bounds beyond the border")]
        [SerializeField] private float _cameraPadding = 2f;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public GameObject[] BorderPrefabs => _borderPrefabs;
        public int BorderThickness => _borderThickness;
        public float Density => _density;
        public float RotationVariance => _rotationVariance;
        public Vector2 ScaleRange => _scaleRange;
        public float PositionJitter => _positionJitter;
        public float HeightOffset => _heightOffset;
        public float CameraPadding => _cameraPadding;

        /// <summary>
        /// Get a random prefab based on weights.
        /// </summary>
        public GameObject GetRandomPrefab()
        {
            if (_borderPrefabs == null || _borderPrefabs.Length == 0)
            {
                return null;
            }

            // If no weights or mismatched count, use uniform distribution
            if (_prefabWeights == null || _prefabWeights.Length != _borderPrefabs.Length)
            {
                return _borderPrefabs[Random.Range(0, _borderPrefabs.Length)];
            }

            // Weighted random selection
            float totalWeight = 0f;
            foreach (float w in _prefabWeights)
            {
                totalWeight += w;
            }

            float random = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            for (int i = 0; i < _borderPrefabs.Length; i++)
            {
                cumulative += _prefabWeights[i];
                if (random <= cumulative)
                {
                    return _borderPrefabs[i];
                }
            }

            return _borderPrefabs[_borderPrefabs.Length - 1];
        }

        /// <summary>
        /// Get a random rotation for a border object.
        /// </summary>
        public Quaternion GetRandomRotation()
        {
            float yRotation = Random.Range(0f, _rotationVariance);
            return Quaternion.Euler(0f, yRotation, 0f);
        }

        /// <summary>
        /// Get a random scale for a border object.
        /// </summary>
        public float GetRandomScale()
        {
            return Random.Range(_scaleRange.x, _scaleRange.y);
        }

        /// <summary>
        /// Get a random position offset for natural look.
        /// </summary>
        public Vector3 GetRandomJitter()
        {
            return new Vector3(
                Random.Range(-_positionJitter, _positionJitter),
                0f,
                Random.Range(-_positionJitter, _positionJitter)
            );
        }

        private void OnValidate()
        {
            // Ensure scale range is valid
            if (_scaleRange.x > _scaleRange.y)
            {
                _scaleRange = new Vector2(_scaleRange.y, _scaleRange.x);
            }

            // Ensure weights array matches prefabs
            if (_borderPrefabs != null && _prefabWeights != null &&
                _prefabWeights.Length != _borderPrefabs.Length)
            {
                System.Array.Resize(ref _prefabWeights, _borderPrefabs.Length);
                for (int i = 0; i < _prefabWeights.Length; i++)
                {
                    if (_prefabWeights[i] <= 0f)
                    {
                        _prefabWeights[i] = 1f;
                    }
                }
            }
        }
    }
}
