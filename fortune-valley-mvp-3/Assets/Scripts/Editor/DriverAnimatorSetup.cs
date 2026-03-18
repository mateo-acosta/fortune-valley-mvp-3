using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace FortuneValley.Editor
{
    /// <summary>
    /// Creates the Animator Controller for the Driver character in the rules carousel.
    /// Wave plays once, then transitions to Idle loop.
    /// </summary>
    public static class DriverAnimatorSetup
    {
        [MenuItem("Fortune Valley/Create Driver Animator Controller")]
        public static void CreateController()
        {
            string controllerPath = "Assets/Art/Models/Characters/Animations/DriverCarousel.controller";

            // Find the animation clips from the imported FBXes
            // Waving is in Rig_Medium_Simulation.fbx
            AnimationClip wavingClip = FindClipInFBX(
                "Assets/Art/Models/Characters/Animations/Rig_Medium_Simulation.fbx", "Waving");
            // Idle_A is in Rig_Medium_General.fbx
            AnimationClip idleClip = FindClipInFBX(
                "Assets/Art/Models/Characters/Animations/Rig_Medium_General.fbx", "Idle_A");

            if (wavingClip == null)
            {
                Debug.LogWarning("Could not find Waving clip in Rig_Medium_Simulation.fbx. " +
                    "Will create controller with placeholder states.");
            }
            if (idleClip == null)
            {
                Debug.LogWarning("Could not find Idle_A clip in Rig_Medium_General.fbx. " +
                    "Will create controller with placeholder states.");
            }

            // Create the Animator Controller
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var rootStateMachine = controller.layers[0].stateMachine;

            // Add Wave state (default/entry)
            var waveState = rootStateMachine.AddState("Wave", new Vector3(300, 0, 0));
            if (wavingClip != null)
            {
                waveState.motion = wavingClip;
            }
            waveState.speed = 1f;
            // Don't loop wave - it plays once

            // Add Idle state
            var idleState = rootStateMachine.AddState("Idle", new Vector3(600, 0, 0));
            if (idleClip != null)
            {
                idleState.motion = idleClip;
            }
            idleState.speed = 1f;

            // Set wave as default state
            rootStateMachine.defaultState = waveState;

            // Transition from Wave → Idle when Wave finishes
            var waveToIdle = waveState.AddTransition(idleState);
            waveToIdle.hasExitTime = true;
            waveToIdle.exitTime = 1f; // After animation completes
            waveToIdle.duration = 0.25f; // Smooth blend
            waveToIdle.hasFixedDuration = true;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Created DriverCarousel controller at {controllerPath}");
            if (wavingClip != null) Debug.Log($"  Wave clip: {wavingClip.name} ({wavingClip.length}s)");
            if (idleClip != null) Debug.Log($"  Idle clip: {idleClip.name} ({idleClip.length}s)");

            Selection.activeObject = controller;
        }

        private static AnimationClip FindClipInFBX(string fbxPath, string clipName)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    Debug.Log($"  Found clip in {fbxPath}: {clip.name} ({clip.length}s, wrapMode={clip.wrapMode})");
                    if (clip.name == clipName)
                        return clip;
                }
            }
            return null;
        }
    }
}
