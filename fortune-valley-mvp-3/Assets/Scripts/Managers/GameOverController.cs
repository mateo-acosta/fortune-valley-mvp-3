using UnityEngine;
using UnityEngine.SceneManagement;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Managers
{
    /// <summary>
    /// Hard-pauses the game when game-over fires, and restarts the active
    /// scene when the player clicks Play Again on the GameEndPanel.
    ///
    /// Auto-spawns once at startup (no scene wiring needed) and persists
    /// across scene reloads via DontDestroyOnLoad. Listens to two events:
    ///   - OnGameEndWithSummary: Time.timeScale = 0 so splines, ticks,
    ///     animators, particles, and any scaled-time work freeze. UI input
    ///     and IMGUI continue to function because they do not depend on
    ///     timeScale, so the player can still click Play Again / Main Menu.
    ///   - OnRestartRequested: restore Time.timeScale, reload the active
    ///     scene. Cleans up GameEvents subscriptions before reload (the
    ///     project's standard scene-isolation rule).
    ///   - OnReturnToTitleRequested: same as restart for now (Homebase has
    ///     no separate title scene to load to). Future: route to a title
    ///     scene by name.
    /// </summary>
    public class GameOverController : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoSpawn()
        {
            var go = new GameObject("[GameOverController]");
            go.AddComponent<GameOverController>();
            DontDestroyOnLoad(go);
        }

        private void OnEnable()
        {
            GameEvents.OnGameEndWithSummary += HandleGameEnd;
            GameEvents.OnRestartRequested += HandleRestart;
            GameEvents.OnReturnToTitleRequested += HandleRestart;
        }

        private void OnDisable()
        {
            GameEvents.OnGameEndWithSummary -= HandleGameEnd;
            GameEvents.OnRestartRequested -= HandleRestart;
            GameEvents.OnReturnToTitleRequested -= HandleRestart;
        }

        private void HandleGameEnd(bool isPlayerWin, GameSummary summary)
        {
            Time.timeScale = 0f;
        }

        private void HandleRestart()
        {
            Time.timeScale = 1f;
            // Per the project's scene-isolation rule, wipe all event
            // subscriptions before unloading so the next scene's components
            // start with a clean bus. ClearAllSubscriptions also drops our
            // own handlers (we are DontDestroyOnLoad and survive the reload),
            // so re-register ours immediately to keep listening for the next
            // game-over / restart cycle.
            GameEvents.ClearAllSubscriptions();
            ResubscribeSelf();
            var active = SceneManager.GetActiveScene();
            SceneManager.LoadScene(active.buildIndex);
        }

        private void ResubscribeSelf()
        {
            GameEvents.OnGameEndWithSummary += HandleGameEnd;
            GameEvents.OnRestartRequested += HandleRestart;
            GameEvents.OnReturnToTitleRequested += HandleRestart;
        }
    }
}
