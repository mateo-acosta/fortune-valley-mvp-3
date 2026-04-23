using UnityEngine;
using FortuneValley.Domain.Enums;

namespace FortuneValley.City
{
    /// <summary>
    /// Block-scale version of LotEdgeGlow. Rebuilds a 4-edge perimeter mesh from
    /// 4 corner anchor Transforms authored under the Block parent, instead of
    /// from a MeshFilter's bounds. Wraps the entire 2x2 block footprint with one
    /// glow regardless of individual lot positions.
    /// Reuses the existing FortuneValley/LotEdgeGlow shader unchanged.
    /// </summary>
    public class BlockEdgeGlow : MonoBehaviour
    {
        [Header("Corner Anchors (NW, NE, SE, SW order)")]
        [SerializeField] private Transform _cornerNW;
        [SerializeField] private Transform _cornerNE;
        [SerializeField] private Transform _cornerSE;
        [SerializeField] private Transform _cornerSW;

        [Header("Glow Dimensions")]
        [Tooltip("Height of the glow wall in world units")]
        [SerializeField] private float _glowHeight = 0.7f;

        [Header("Pulse")]
        [Tooltip("Speed of the pulse animation")]
        [SerializeField] private float _pulseSpeed = 1.5f;

        [Header("Initial State")]
        [Tooltip("Owner color applied on Awake. Blocks start vacant (None=white).")]
        [SerializeField] private Owner _initialOwner = Owner.None;

        [Header("Ownership Colors")]
        [SerializeField] private Color _vacantColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private Color _playerColor = new Color(0.1f, 0.9f, 0.2f, 0.5f);
        [SerializeField] private Color _rivalColor = new Color(0.9f, 0.2f, 0.2f, 0.5f);

        private Material _glowMaterial;
        private MeshRenderer _glowRenderer;
        private Mesh _glowMesh;

        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        private static readonly int PulseSpeedId = Shader.PropertyToID("_PulseSpeed");

        private void Awake()
        {
            CreateGlowChild();
        }

        private void OnDestroy()
        {
            if (_glowMaterial != null) Destroy(_glowMaterial);
            if (_glowMesh != null) Destroy(_glowMesh);
        }

        /// <summary>
        /// Update the glow color to reflect the current block owner.
        /// Called by BlockController on OnLotPurchased.
        /// </summary>
        public void SetOwnershipColor(Owner owner)
        {
            if (_glowMaterial == null) return;

            Color color;
            switch (owner)
            {
                case Owner.Player:
                    color = _playerColor;
                    break;
                case Owner.Rival:
                    color = _rivalColor;
                    break;
                default:
                    color = _vacantColor;
                    break;
            }

            _glowMaterial.SetColor(GlowColorId, color);
            _glowMaterial.SetFloat(PulseSpeedId, _pulseSpeed);
        }

        private void CreateGlowChild()
        {
            if (_cornerNW == null || _cornerNE == null || _cornerSE == null || _cornerSW == null)
            {
                UnityEngine.Debug.LogWarning($"[BlockEdgeGlow] {gameObject.name} missing corner anchors; glow disabled.");
                return;
            }

            Shader glowShader = Shader.Find("FortuneValley/LotEdgeGlow");
            if (glowShader == null)
            {
                UnityEngine.Debug.LogWarning("[BlockEdgeGlow] Shader 'FortuneValley/LotEdgeGlow' not found.");
                return;
            }

            _glowMaterial = new Material(glowShader);
            _glowMaterial.name = "BlockEdgeGlow_Runtime";

            _glowMesh = BuildPerimeterMesh();

            GameObject glowObj = new GameObject("EdgeGlow");
            glowObj.transform.SetParent(transform, false);
            glowObj.transform.localPosition = Vector3.zero;
            glowObj.transform.localRotation = Quaternion.identity;
            glowObj.transform.localScale = Vector3.one;

            MeshFilter glowFilter = glowObj.AddComponent<MeshFilter>();
            glowFilter.sharedMesh = _glowMesh;

            _glowRenderer = glowObj.AddComponent<MeshRenderer>();
            _glowRenderer.sharedMaterial = _glowMaterial;
            _glowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _glowRenderer.receiveShadows = false;

            SetOwnershipColor(_initialOwner);
        }

        /// <summary>
        /// Builds 4 vertical quads for edges NW->NE, NE->SE, SE->SW, SW->NW.
        /// Bottom of each quad sits at the average y of its two anchors; top extends up by _glowHeight.
        /// Mesh vertices are in this component's local space.
        /// </summary>
        private Mesh BuildPerimeterMesh()
        {
            Vector3 localNW = transform.InverseTransformPoint(_cornerNW.position);
            Vector3 localNE = transform.InverseTransformPoint(_cornerNE.position);
            Vector3 localSE = transform.InverseTransformPoint(_cornerSE.position);
            Vector3 localSW = transform.InverseTransformPoint(_cornerSW.position);

            Vector3[] edgeStarts = new Vector3[] { localNW, localNE, localSE, localSW };
            Vector3[] edgeEnds = new Vector3[] { localNE, localSE, localSW, localNW };

            Vector3[] verts = new Vector3[16];
            Vector2[] uvs = new Vector2[16];
            int[] tris = new int[24];

            for (int i = 0; i < 4; i++)
            {
                Vector3 a = edgeStarts[i];
                Vector3 b = edgeEnds[i];
                float yBottom = (a.y + b.y) * 0.5f;
                float yTop = yBottom + _glowHeight;

                int baseIdx = i * 4;
                verts[baseIdx + 0] = new Vector3(a.x, yBottom, a.z);
                verts[baseIdx + 1] = new Vector3(a.x, yTop, a.z);
                verts[baseIdx + 2] = new Vector3(b.x, yTop, b.z);
                verts[baseIdx + 3] = new Vector3(b.x, yBottom, b.z);

                uvs[baseIdx + 0] = new Vector2(0f, 0f);
                uvs[baseIdx + 1] = new Vector2(0f, 1f);
                uvs[baseIdx + 2] = new Vector2(1f, 1f);
                uvs[baseIdx + 3] = new Vector2(1f, 0f);

                int t = i * 6;
                tris[t + 0] = baseIdx + 0;
                tris[t + 1] = baseIdx + 1;
                tris[t + 2] = baseIdx + 2;
                tris[t + 3] = baseIdx + 0;
                tris[t + 4] = baseIdx + 2;
                tris[t + 5] = baseIdx + 3;
            }

            Mesh mesh = new Mesh();
            mesh.name = "BlockEdgeGlow_Perimeter";
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();

            return mesh;
        }
    }
}
