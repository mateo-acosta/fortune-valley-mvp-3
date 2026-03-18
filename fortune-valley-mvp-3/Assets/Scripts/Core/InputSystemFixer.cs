using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace FortuneValley.Core
{
    /// <summary>
    /// Automatically fixes EventSystem at runtime to use New Input System.
    /// Replaces StandaloneInputModule with InputSystemUIInputModule.
    /// </summary>
    public static class InputSystemFixer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void FixInputSystem()
        {
            // Subscribe to scene loaded to fix EventSystem in each scene
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            FixEventSystem();
        }

        private static void FixEventSystem()
        {
            // Find EventSystem and replace StandaloneInputModule with InputSystemUIInputModule
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                var standalone = eventSystem.GetComponent<StandaloneInputModule>();
                if (standalone != null)
                {
                    standalone.enabled = false; // Disable immediately (Destroy is deferred)
                    Object.Destroy(standalone);

                    // Add InputSystemUIInputModule if not already present
                    if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                    {
                        eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                    }

                    Debug.Log("[InputSystemFixer] Replaced StandaloneInputModule with InputSystemUIInputModule");
                }
            }
        }
    }
}
