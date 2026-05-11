using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class TitleCanvasFrameFix
{
    const string FramePath = "Assets/Art/GUI/Layer Lab/GUI Pro-SimpleCasual/ResourcesData/Sprites/Components/Frame/Frame_Custom/PictureFrame00_White4.png";

    [MenuItem("Tools/Fortune Valley/Fix TitleCanvas Frame")]
    static void FixTitleCanvasFrame()
    {
        var importer = AssetImporter.GetAtPath(FramePath) as TextureImporter;
        if (importer == null) { Debug.LogError($"[FrameFix] No TextureImporter at {FramePath}"); return; }

        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 100;
        importer.spriteBorder = new Vector4(20, 20, 20, 20);
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = true;
        importer.anisoLevel = 4;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        Debug.Log($"[FrameFix] Reimported frame with border (20,20,20,20) and uncompressed mipmaps.");

        var canvas = GameObject.Find("TitleCanvas");
        if (canvas == null) { Debug.LogError("[FrameFix] TitleCanvas not in active scene"); return; }

        var img = canvas.GetComponent<Image>();
        if (img == null) { Debug.LogError("[FrameFix] No Image component on TitleCanvas"); return; }

        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1f;
        EditorUtility.SetDirty(img);
        Debug.Log($"[FrameFix] Set TitleCanvas Image to Sliced. Corner pixels now stay native.");
    }
}
