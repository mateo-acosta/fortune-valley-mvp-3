using UnityEngine;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;

namespace FortuneValley.City
{
    /// <summary>
    /// Generates vertical glow quads around a lot's perimeter that fade upward.
    /// The glow color indicates ownership: White = vacant, Green = player, Red = rival.
    /// Pulse animation runs entirely in the shader (zero CPU cost).
    /// </summary>
    public class LotEdgeGlow : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        [Header("Glow Dimensions")]
        [Tooltip("Height of the glow wall in world units")]
        [SerializeField] private float _glowHeight = 0.7f;

        [Tooltip("Small outward offset so glow doesn't z-fight with the lot mesh")]
        [SerializeField] private float _edgeOffset = 0.02f;

        [Tooltip("Start glow from mesh bottom instead of top (use for tall buildings)")]
        [SerializeField] private bool _glowFromBase = false;

        [Header("Pulse")]
        [Tooltip("Speed of the pulse animation")]
        [SerializeField] private float _pulseSpeed = 1.5f;

        [Header("Initial State")]
        [Tooltip("Owner to apply on Awake. Lots leave this as None (LotVisual sets color in Start).")]
        [SerializeField] private Owner _initialOwner = Owner.None;

        [Header("Ownership Colors")]
        [SerializeField] private Color _vacantColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private Color _playerColor = new Color(0.1f, 0.9f, 0.2f, 0.5f);
        [SerializeField] private Color _rivalColor = new Color(0.9f, 0.2f, 0.2f, 0.5f);

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private Material _glowMaterial;
        private MeshRenderer _glowRenderer;
        private Mesh _glowMesh;

        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        private static readonly int PulseSpeedId = Shader.PropertyToID("_PulseSpeed");

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Awake()
        {
            CreateGlowChild();
        }

        private void OnDestroy()
        {
            if (_glowMaterial != null)
            {
                Destroy(_glowMaterial);
            }
            if (_glowMesh != null)
            {
                Destroy(_glowMesh);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Update the glow color to reflect the current lot owner.
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

            // Set directly on material instance (SRP Batcher ignores MaterialPropertyBlock)
            _glowMaterial.SetColor(GlowColorId, color);
            _glowMaterial.SetFloat(PulseSpeedId, _pulseSpeed);
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE
        // ═══════════════════════════════════════════════════════════════

        private void CreateGlowChild()
        {
            // Find the mesh on this GameObject to get lot dimensions
            MeshFilter parentFilter = GetComponent<MeshFilter>();
            if (parentFilter == null || parentFilter.sharedMesh == null)
            {
                UnityEngine.Debug.LogWarning($"[LotEdgeGlow] No MeshFilter on {gameObject.name}, glow disabled.");
                return;
            }

            Shader glowShader = Shader.Find("FortuneValley/LotEdgeGlow");
            if (glowShader == null)
            {
                UnityEngine.Debug.LogWarning("[LotEdgeGlow] Shader 'FortuneValley/LotEdgeGlow' not found.");
                return;
            }

            // Create material instance
            _glowMaterial = new Material(glowShader);
            _glowMaterial.name = "LotEdgeGlow_Runtime";

            // Build procedural glow mesh from lot bounds
            Bounds bounds = parentFilter.sharedMesh.bounds;
            _glowMesh = BuildPerimeterMesh(bounds);

            // Build child GameObject
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

            // Initialize to configured owner color (defaults to None/vacant)
            SetOwnershipColor(_initialOwner);
        }

        /// <summary>
        /// Build 4 vertical quads around the lot perimeter.
        /// Bottom edge sits at the lot's top surface, top edge extends upward by _glowHeight.
        /// UV.y goes 0 (bottom) to 1 (top) to drive the shader's vertical fade.
        /// </summary>
        private Mesh BuildPerimeterMesh(Bounds bounds)
        {
            // Account for parent lossy scale so glow dimensions are in world units
            Vector3 scale = transform.lossyScale;
            float safeScaleX = Mathf.Max(Mathf.Abs(scale.x), 0.001f);
            float safeScaleY = Mathf.Max(Mathf.Abs(scale.y), 0.001f);
            float safeScaleZ = Mathf.Max(Mathf.Abs(scale.z), 0.001f);

            float heightLocal = _glowHeight / safeScaleY;
            float offsetX = _edgeOffset / safeScaleX;
            float offsetZ = _edgeOffset / safeScaleZ;

            float xMin = bounds.min.x - offsetX;
            float xMax = bounds.max.x + offsetX;
            float zMin = bounds.min.z - offsetZ;
            float zMax = bounds.max.z + offsetZ;
            float yBottom = _glowFromBase ? bounds.min.y : bounds.max.y;
            float yTop = yBottom + heightLocal;

            // 4 quads × 4 verts = 16 vertices, 4 quads × 6 indices = 24
            Vector3[] verts = new Vector3[16];
            Vector2[] uvs = new Vector2[16];
            int[] tris = new int[24];

            // +X side (facing outward)
            verts[0] = new Vector3(xMax, yBottom, zMin);
            verts[1] = new Vector3(xMax, yTop, zMin);
            verts[2] = new Vector3(xMax, yTop, zMax);
            verts[3] = new Vector3(xMax, yBottom, zMax);

            // -X side
            verts[4] = new Vector3(xMin, yBottom, zMax);
            verts[5] = new Vector3(xMin, yTop, zMax);
            verts[6] = new Vector3(xMin, yTop, zMin);
            verts[7] = new Vector3(xMin, yBottom, zMin);

            // +Z side
            verts[8] = new Vector3(xMax, yBottom, zMax);
            verts[9] = new Vector3(xMax, yTop, zMax);
            verts[10] = new Vector3(xMin, yTop, zMax);
            verts[11] = new Vector3(xMin, yBottom, zMax);

            // -Z side
            verts[12] = new Vector3(xMin, yBottom, zMin);
            verts[13] = new Vector3(xMin, yTop, zMin);
            verts[14] = new Vector3(xMax, yTop, zMin);
            verts[15] = new Vector3(xMax, yBottom, zMin);

            // UVs: y=0 at bottom, y=1 at top (drives shader fade)
            for (int quad = 0; quad < 4; quad++)
            {
                int b = quad * 4;
                uvs[b + 0] = new Vector2(0f, 0f); // bottom-left
                uvs[b + 1] = new Vector2(0f, 1f); // top-left
                uvs[b + 2] = new Vector2(1f, 1f); // top-right
                uvs[b + 3] = new Vector2(1f, 0f); // bottom-right
            }

            // Triangles (two per quad, winding for outward-facing normals)
            for (int quad = 0; quad < 4; quad++)
            {
                int b = quad * 4;
                int t = quad * 6;
                tris[t + 0] = b + 0;
                tris[t + 1] = b + 1;
                tris[t + 2] = b + 2;
                tris[t + 3] = b + 0;
                tris[t + 4] = b + 2;
                tris[t + 5] = b + 3;
            }

            Mesh mesh = new Mesh();
            mesh.name = "LotEdgeGlow_Perimeter";
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();

            return mesh;
        }
    }
}
