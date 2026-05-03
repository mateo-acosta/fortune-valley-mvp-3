using UnityEngine;
using UnityEditor;
using Unity.Cinemachine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

public static class DollySetupTool
{
    [MenuItem("Tools/Fortune Valley/Setup Dolly Keyframes")]
    static void SetupDollyKeyframes()
    {
        // Load the animation clip
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/VideoAnimations/DollyMove.anim");
        if (clip == null) { Debug.LogError("[DollySetup] DollyMove.anim not found at Assets/VideoAnimations/DollyMove.anim"); return; }

        // Find the dolly component in the scene
        var dolly = Object.FindFirstObjectByType<CinemachineSplineDolly>();
        if (dolly == null) { Debug.LogError("[DollySetup] No CinemachineSplineDolly found in the scene"); return; }

        // Force Position Units to Normalized (1) so 0->1 sweeps the entire spline.
        // Done via SerializedObject so the change persists even though m_SplineSettings is a struct.
        var so = new SerializedObject(dolly);
        var unitsProp = so.FindProperty("m_SplineSettings.Units");
        if (unitsProp != null)
        {
            unitsProp.intValue = 1;
            so.ApplyModifiedProperties();
            Debug.Log("[DollySetup] Set CinemachineSplineDolly Position Units to Normalized (1)");
        }
        else
        {
            Debug.LogWarning("[DollySetup] Could not find m_SplineSettings.Units serialized property");
        }

        // Log all animatable bindings so we can see the exact property name
        var allBindings = AnimationUtility.GetAnimatableBindings(dolly.gameObject, dolly.gameObject);
        string foundProperty = null;
        Debug.Log("[DollySetup] Animatable properties on CinemachineSplineDolly:");
        foreach (var b in allBindings)
        {
            if (b.type == typeof(CinemachineSplineDolly))
            {
                Debug.Log($"  -> {b.propertyName}");
                if (b.propertyName == "m_SplineSettings.Position")
                    foundProperty = b.propertyName;
            }
        }

        if (foundProperty == null)
        {
            Debug.LogError("[DollySetup] Could not find a position property on CinemachineSplineDolly. Check the log above for all available properties.");
            return;
        }

        // Build a 6-second linear curve from 0 (start of spline) to 1 (end of spline)
        var curve = AnimationCurve.EaseInOut(0f, 0f, 6f, 1f);
        clip.SetCurve("", typeof(CinemachineSplineDolly), foundProperty, curve);
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        Debug.Log($"[DollySetup] Keyframes written. Property: {foundProperty}");

        // Fix the CinemachineShot binding -- find the Timeline Director and bind the virtual camera
        var director = Object.FindFirstObjectByType<PlayableDirector>();
        if (director == null) { Debug.LogWarning("[DollySetup] No PlayableDirector found -- skipping shot binding"); return; }

        var vcam = dolly.GetComponent<CinemachineCamera>();
        if (vcam == null) { Debug.LogWarning("[DollySetup] No CinemachineCamera on same GameObject as dolly"); return; }

        var timeline = director.playableAsset as TimelineAsset;
        if (timeline == null) { Debug.LogWarning("[DollySetup] PlayableDirector has no TimelineAsset"); return; }

        // Find the CinemachineCamera's Animator for rebinding the Animation Track
        var vcamAnimator = dolly.GetComponent<Animator>();

        foreach (var track in timeline.GetOutputTracks())
        {
            if (track is CinemachineTrack)
            {
                foreach (var clip2 in track.GetClips())
                {
                    if (clip2.asset is CinemachineShot shot)
                    {
                        director.SetReferenceValue(shot.VirtualCamera.exposedName, vcam);
                        Debug.Log($"[DollySetup] Bound CinemachineCamera to Cinemachine shot: {clip2.displayName}");
                    }
                }
            }

            // Rebind the Animation Track to CinemachineCamera so the dolly curve drives the right object
            if (track is AnimationTrack && vcamAnimator != null)
            {
                director.SetGenericBinding(track, vcamAnimator);
                Debug.Log("[DollySetup] Rebound Animation Track to CinemachineCamera Animator");
            }
        }

        EditorUtility.SetDirty(director);
        Debug.Log("[DollySetup] Done. Press Play in the Timeline to preview the dolly move.");
    }
}
