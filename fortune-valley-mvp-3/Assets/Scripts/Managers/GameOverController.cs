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
            // Full fresh restart. Ordering is load-bearing:
            //
            // 1. Request the server-side player-state wipe FIRST, while the
            //    current scene's ReplayTutorialService is still alive to
            //    handle the event (ClearAllSubscriptions below nulls the
            //    event; the scene reload destroys that listener). This also
            //    clears the local tutorial-completed flags so the reloaded
            //    scene re-runs the intro tutorial and goal selection.
            GameEvents.RaisePlayerStateWipeRequested();

            // 2. Clear the persistence statics that intentionally survive
            //    scene reloads, so the reloaded scene cold-boots as a genuine
            //    new game: catch-up handlers see no cached DTO (fresh-default
            //    seeding runs), and TimeManager.HandleGameStart runs
            //    ResetTime() (clock/age back to day 0 / age 25) instead of
            //    early-returning on the stale end-of-game state.
            //
            //    HasServerConfirmedFreshUser is set true because the wipe
            //    just created a deterministic fresh-default server row, so
            //    SaveRoundTripResolved stays true and the new game starts
            //    immediately (no ~20s start-barrier timeout) while still NOT
            //    reusing stale state. The first autosave then harmlessly
            //    rewrites the already-wiped row, and the finished game's
            //    full history is preserved server-side in the append-only
            //    game_state_snapshots table.
            GameEvents.LastLoadedSaveDto = null;
            GameEvents.HasSaveBeenRestored = false;
            GameEvents.SaveStateRestoredFromServer = false;
            GameEvents.StartBarrierReleased = false;
            GameEvents.HasServerConfirmedFreshUser = true;

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
