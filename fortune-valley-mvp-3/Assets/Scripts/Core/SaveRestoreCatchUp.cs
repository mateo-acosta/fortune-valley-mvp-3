using System;
using System.Collections.Generic;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Core
{
    /// <summary>
    /// Single source of truth for "subscribe to save-restore events and catch up if I'm late."
    ///
    /// Background: GameSaveBootstrapper fires OnSaveStateLoaded (Phase 1) then OnSaveRestored
    /// (Phase 2) one frame later. Systems that exist before Phase 1 fires get live delivery.
    /// Systems that spawn after either phase need a synthetic replay from the cached DTO
    /// (GameEvents.LastLoadedSaveDto + HasSaveBeenRestored). This helper handles all three
    /// timing buckets in one Subscribe call.
    ///
    /// Contract: handlers MUST be idempotent. They may be invoked once via the live event
    /// and once via synthetic replay. The same-DTO guard skips synthetic replay when the
    /// caller already saw that DTO, but live events still pass through.
    /// </summary>
    public static class SaveRestoreCatchUp
    {
        private static readonly Dictionary<Delegate, GamePlayerStateDTO> _lastReplayedDto
            = new Dictionary<Delegate, GamePlayerStateDTO>();

        /// <summary>
        /// Subscribe to both Phase 1 and Phase 2 of save restore. Either handler may be null.
        ///
        /// Timing buckets:
        ///   - Subscribed BEFORE Phase 1 fires: both handlers called live; no replay.
        ///   - Subscribed BETWEEN Phase 1 and Phase 2: phase1 invoked synthetically with
        ///     LastLoadedSaveDto; phase2 invoked live.
        ///   - Subscribed AFTER Phase 2 (HasSaveBeenRestored == true): both invoked
        ///     synthetically (phase1 first, then phase2).
        ///
        /// Same-DTO guard: if the same phase1 delegate already saw the cached DTO during
        /// a prior Subscribe call, the synthetic replay for that delegate is skipped. This
        /// matters for components that disable + re-enable across scene reloads with the
        /// same DTO still in cache.
        ///
        /// Callers MUST call Unsubscribe in OnDisable / Dispose with the same delegates.
        /// </summary>
        public static void Subscribe(Action<GamePlayerStateDTO> phase1, Action phase2)
        {
            if (phase1 != null)
            {
                GameEvents.OnSaveStateLoaded += phase1;
            }
            if (phase2 != null)
            {
                GameEvents.OnSaveRestored += phase2;
            }

            var cachedDto = GameEvents.LastLoadedSaveDto;
            bool phase2Already = GameEvents.HasSaveBeenRestored;

            if (cachedDto != null && phase1 != null)
            {
                if (!_lastReplayedDto.TryGetValue(phase1, out var prior) || !ReferenceEquals(prior, cachedDto))
                {
                    _lastReplayedDto[phase1] = cachedDto;
                    phase1.Invoke(cachedDto);
                }
            }

            if (phase2Already && phase2 != null)
            {
                phase2.Invoke();
            }
        }

        /// <summary>
        /// Unsubscribe both handlers. Pass the exact same delegate instances handed to Subscribe.
        ///
        /// Same-DTO tracking is intentionally retained: if the caller re-subscribes the same
        /// delegate while the same DTO is still cached, the synthetic replay is skipped (the
        /// caller already processed that DTO). For destructive lifecycle resets where the
        /// caller wants a fresh replay on next subscribe, call ClearCache.
        /// </summary>
        public static void Unsubscribe(Action<GamePlayerStateDTO> phase1, Action phase2)
        {
            if (phase1 != null)
            {
                GameEvents.OnSaveStateLoaded -= phase1;
            }
            if (phase2 != null)
            {
                GameEvents.OnSaveRestored -= phase2;
            }
        }

        /// <summary>
        /// Wipe the same-DTO replay tracking. Wired to run alongside
        /// GameEvents.ClearAllSubscriptions so scene-restart paths get a clean
        /// replay slate on the next Subscribe.
        /// </summary>
        public static void ClearCache()
        {
            _lastReplayedDto.Clear();
        }
    }
}
