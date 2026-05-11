using System.Collections.Generic;
using FortuneValley.Core;
using FortuneValley.Domain.Notifications;

namespace FortuneValley.Managers.Notifications
{
    /// <summary>
    /// Decides whether a tip should fire under its declared <see cref="RepeatPolicy"/>.
    /// Stateless across types but stateful per tipId; one filter instance is shared
    /// by the entire <see cref="GuidanceController"/> and persists for the life of
    /// the game session.
    /// </summary>
    public class RepeatPolicyFilter
    {
        public const string PlayerPrefsKeyPrefix = "FV_GuidanceTipFired_";

        private readonly INowProvider _now;
        private readonly PlayerPrefsDebouncedFlusher _prefs;
        private readonly HashSet<string> _firedThisSession = new HashSet<string>();
        private readonly Dictionary<string, double> _lastFireSecondsSinceEpoch = new Dictionary<string, double>();

        public RepeatPolicyFilter(INowProvider now, PlayerPrefsDebouncedFlusher prefs)
        {
            _now = now;
            _prefs = prefs;
        }

        /// <summary>
        /// True if firing this tip is allowed under its policy right now.
        /// Caller must invoke <see cref="MarkFired"/> after actually emitting
        /// the banner so subsequent calls observe the firing.
        /// </summary>
        public bool ShouldFire(string tipId, RepeatPolicy policy, double cooldownSeconds)
        {
            switch (policy)
            {
                case RepeatPolicy.EveryTime:
                    return true;
                case RepeatPolicy.OncePerSession:
                    return !_firedThisSession.Contains(tipId);
                case RepeatPolicy.OncePerPlayer:
                    return !_prefs.GetFlag(KeyFor(tipId));
                case RepeatPolicy.OncePerCooldown:
                    return SecondsSinceLastFire(tipId) >= cooldownSeconds;
                default:
                    return true;
            }
        }

        public void MarkFired(string tipId, RepeatPolicy policy)
        {
            switch (policy)
            {
                case RepeatPolicy.EveryTime:
                    return;
                case RepeatPolicy.OncePerSession:
                    _firedThisSession.Add(tipId);
                    return;
                case RepeatPolicy.OncePerPlayer:
                    _prefs.SetFlag(KeyFor(tipId), true);
                    return;
                case RepeatPolicy.OncePerCooldown:
                    _lastFireSecondsSinceEpoch[tipId] = ToEpochSeconds(_now.UtcNow);
                    return;
            }
        }

        /// <summary>
        /// Resets per-session state. Production callers invoke on a fresh game start.
        /// Per-player and cooldown state is unaffected.
        /// </summary>
        public void ClearSession() => _firedThisSession.Clear();

        private double SecondsSinceLastFire(string tipId)
        {
            if (!_lastFireSecondsSinceEpoch.TryGetValue(tipId, out var last)) return double.MaxValue;
            return ToEpochSeconds(_now.UtcNow) - last;
        }

        private static double ToEpochSeconds(System.DateTime utc) =>
            (utc - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds;

        private static string KeyFor(string tipId) => PlayerPrefsKeyPrefix + tipId;
    }
}
