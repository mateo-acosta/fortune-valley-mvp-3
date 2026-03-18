using UnityEditor;
using UnityEngine;

namespace FortuneValley.Editor
{
    /// <summary>
    /// Creates a RenderTexture asset for the character display in the rules carousel.
    /// </summary>
    public static class RenderTextureSetup
    {
        [MenuItem("Fortune Valley/Create Driver RenderTexture")]
        public static void CreateDriverRenderTexture()
        {
            // Ensure folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Art/RenderTextures"))
            {
                AssetDatabase.CreateFolder("Assets/Art", "RenderTextures");
            }

            string path = "Assets/Art/RenderTextures/DriverCharacterRT.renderTexture";

            // Check if it already exists
            var existing = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
            if (existing != null)
            {
                Debug.Log($"RenderTexture already exists at {path}");
                Selection.activeObject = existing;
                return;
            }

            // Create 512x768 ARGB32 with alpha support
            var rt = new RenderTexture(512, 768, 24, RenderTextureFormat.ARGB32);
            rt.name = "DriverCharacterRT";
            rt.antiAliasing = 4;
            rt.filterMode = FilterMode.Bilinear;
            rt.wrapMode = TextureWrapMode.Clamp;
            rt.Create();

            AssetDatabase.CreateAsset(rt, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Created RenderTexture at {path}");
            Selection.activeObject = rt;
        }
    }
}
