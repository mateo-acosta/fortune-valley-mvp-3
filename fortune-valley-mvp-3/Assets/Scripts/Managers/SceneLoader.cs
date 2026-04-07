using UnityEngine.SceneManagement;
using FortuneValley.Core;

namespace FortuneValley.Managers
{
    /// <summary>
    /// Static utility for scene transitions.
    /// Owns the cleanup sequence that prevents cross-scene event leaks.
    ///
    /// All scene transitions MUST go through this class. Never call
    /// SceneManager.LoadScene directly -- that skips event cleanup
    /// and causes stale subscriptions from the previous scene.
    /// </summary>
    public static class SceneLoader
    {
        /// <summary>
        /// Transition to a new scene. Cleans up all event subscriptions
        /// from the current scene before loading the target.
        /// </summary>
        /// <param name="sceneName">Target scene name (use SceneNames constants)</param>
        public static void LoadScene(string sceneName)
        {
            // Wipe all static event subscriptions from the departing scene.
            // Systems in the new scene re-subscribe in their OnEnable.
            GameEvents.ClearAllSubscriptions();

            SceneManager.LoadScene(sceneName);
        }
    }
}
