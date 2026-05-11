using UnityEditor;
using UnityEngine;

public static class LogoImportFix
{
    const string LogoPath = "Assets/Art/GUI/fortune-valley-logo.png";

    [MenuItem("Tools/Fortune Valley/Fix Logo Texture Import")]
    static void FixLogoImport()
    {
        var importer = AssetImporter.GetAtPath(LogoPath) as TextureImporter;
        if (importer == null) { Debug.LogError($"[LogoImportFix] No TextureImporter at {LogoPath}"); return; }

        importer.textureType = TextureImporterType.Sprite;
        importer.mipmapEnabled = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.anisoLevel = 4;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        importer.SaveAndReimport();
        Debug.Log($"[LogoImportFix] Reimported {LogoPath} with mipmaps, aniso=4, uncompressed.");
    }
}
