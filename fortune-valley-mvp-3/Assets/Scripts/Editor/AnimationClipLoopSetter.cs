using UnityEditor;
using UnityEngine;

namespace FortuneValley.Editor
{
    /// <summary>
    /// Sets loop flags on imported animation clips via the FBX importer settings.
    /// </summary>
    public static class AnimationClipLoopSetter
    {
        [MenuItem("Fortune Valley/Set Idle_A Loop Flag")]
        public static void SetIdleALoop()
        {
            string fbxPath = "Assets/Art/Models/Characters/Animations/Rig_Medium_General.fbx";
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"Could not find ModelImporter at {fbxPath}");
                return;
            }

            // Get existing clip animations - if empty, we need to build from defaultClipAnimations
            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                // Use the auto-detected clips as a base
                clips = importer.defaultClipAnimations;
            }

            if (clips == null || clips.Length == 0)
            {
                Debug.LogError("No clips found in the FBX");
                return;
            }

            bool modified = false;
            foreach (var clip in clips)
            {
                Debug.Log($"Clip: {clip.name}, frames {clip.firstFrame}-{clip.lastFrame}, loop={clip.loopTime}");
                
                // Set loop on idle clips
                if (clip.name.StartsWith("Idle"))
                {
                    clip.loopTime = true;
                    clip.loopPose = true;
                    modified = true;
                    Debug.Log($"  -> Set loopTime=true on {clip.name}");
                }
            }

            if (modified)
            {
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
                Debug.Log("Reimported General FBX with loop flags set");
            }

            // Lock down Simulation FBX clip definitions so fileIDs stay stable across reimports.
            // Without explicit clipAnimations, Unity auto-generates fileIDs that can change,
            // breaking any Animator Controller references to clips in this FBX.
            string simPath = "Assets/Art/Models/Characters/Animations/Rig_Medium_Simulation.fbx";
            var simImporter = AssetImporter.GetAtPath(simPath) as ModelImporter;
            if (simImporter != null)
            {
                var simClips = simImporter.clipAnimations;
                bool needsWrite = false;

                // If clipAnimations is empty, we must write defaultClipAnimations to lock fileIDs
                if (simClips == null || simClips.Length == 0)
                {
                    simClips = simImporter.defaultClipAnimations;
                    needsWrite = true;
                    Debug.Log("Simulation FBX: writing defaultClipAnimations to lock stable fileIDs");
                }

                if (simClips != null && simClips.Length > 0)
                {
                    foreach (var clip in simClips)
                    {
                        Debug.Log($"Sim clip: {clip.name}, frames {clip.firstFrame}-{clip.lastFrame}, loop={clip.loopTime}");
                        // Waving should NOT loop (plays once then transitions to idle)
                        if (clip.name == "Waving" && clip.loopTime)
                        {
                            clip.loopTime = false;
                            needsWrite = true;
                            Debug.Log($"  -> Set loopTime=false on {clip.name}");
                        }
                    }
                    if (needsWrite)
                    {
                        simImporter.clipAnimations = simClips;
                        simImporter.SaveAndReimport();
                        Debug.Log("Reimported Simulation FBX with stable clip definitions");
                    }
                    else
                    {
                        Debug.Log("Simulation FBX clipAnimations already written — no changes needed");
                    }
                }
            }
        }
    }
}
