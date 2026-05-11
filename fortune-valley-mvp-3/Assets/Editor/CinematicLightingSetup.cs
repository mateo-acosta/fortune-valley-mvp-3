using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class CinematicLightingSetup
{
    const string ProfilePath = "Assets/Settings/DefaultVolumeProfile.asset";

    [MenuItem("Tools/Fortune Valley/Apply Cinematic Lighting")]
    static void Apply()
    {
        TweakDirectionalLight();
        TweakVolumeProfile();

        var stage = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(stage);
        Debug.Log("[Cinematic] Done. Save the scene to persist directional light changes.");
    }

    static void TweakDirectionalLight()
    {
        Light sun = null;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { sun = l; break; }

        if (sun == null) { Debug.LogWarning("[Cinematic] No directional light in scene"); return; }

        sun.useColorTemperature = true;
        sun.colorTemperature = 5800f;
        sun.color = new Color(1f, 0.97f, 0.92f, 1f);
        sun.intensity = 1.25f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.75f;

        var addData = sun.GetUniversalAdditionalLightData();
        if (addData != null) addData.softShadowQuality = SoftShadowQuality.High;

        EditorUtility.SetDirty(sun);
        Debug.Log($"[Cinematic] Directional light tuned: 5800K, intensity 1.25, soft shadows.");
    }

    static void TweakVolumeProfile()
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null) { Debug.LogError($"[Cinematic] No VolumeProfile at {ProfilePath}"); return; }

        if (profile.TryGet<Bloom>(out var bloom))
        {
            bloom.active = true;
            bloom.threshold.overrideState = true; bloom.threshold.value = 0.95f;
            bloom.intensity.overrideState = true; bloom.intensity.value = 0.45f;
            bloom.scatter.overrideState = true;   bloom.scatter.value = 0.7f;
            bloom.highQualityFiltering.overrideState = true; bloom.highQualityFiltering.value = true;
        }

        if (profile.TryGet<Tonemapping>(out var tone))
        {
            tone.active = true;
            tone.mode.overrideState = true;
            tone.mode.value = TonemappingMode.ACES;
        }

        if (profile.TryGet<ColorAdjustments>(out var color))
        {
            color.active = true;
            color.postExposure.overrideState = true;  color.postExposure.value = 0.1f;
            color.contrast.overrideState = true;       color.contrast.value = 8f;
            color.saturation.overrideState = true;     color.saturation.value = 6f;
        }

        if (profile.TryGet<WhiteBalance>(out var wb))
        {
            wb.active = true;
            wb.temperature.overrideState = true; wb.temperature.value = 5f;
            wb.tint.overrideState = true;        wb.tint.value = 0f;
        }

        if (profile.TryGet<Vignette>(out var vig))
        {
            vig.active = true;
            vig.intensity.overrideState = true;  vig.intensity.value = 0.12f;
            vig.smoothness.overrideState = true; vig.smoothness.value = 0.4f;
            vig.rounded.overrideState = true;    vig.rounded.value = false;
        }

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssetIfDirty(profile);
        Debug.Log("[Cinematic] Volume profile tuned: Bloom, ACES, color adjust, white balance, light vignette.");
    }
}
