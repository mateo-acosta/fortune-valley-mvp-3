using System;
using System.Collections.Generic;
using FortuneValley.Core;

namespace FortuneValley.Managers.Notifications
{
    /// <summary>
    /// Buffers PlayerPrefs writes in memory and flushes on a debounce interval
    /// (default 5s) plus on application pause/quit. WebGL persists PlayerPrefs
    /// to IndexedDB synchronously on Save(); coalescing many bursty writes into
    /// one flush avoids visible main-thread stutter when several "first-time"
    /// tips fire in close succession.
    /// </summary>
    public class PlayerPrefsDebouncedFlusher
    {
        private readonly IKeyValueStore _store;
        private readonly INowProvider _now;
        private readonly TimeSpan _flushInterval;
        private readonly HashSet<string> _dirty = new HashSet<string>();
        private DateTime _lastFlushUtc;

        public PlayerPrefsDebouncedFlusher(IKeyValueStore store, INowProvider now)
            : this(store, now, TimeSpan.FromSeconds(5)) { }

        public PlayerPrefsDebouncedFlusher(IKeyValueStore store, INowProvider now, TimeSpan flushInterval)
        {
            _store = store;
            _now = now;
            _flushInterval = flushInterval;
            _lastFlushUtc = now.UtcNow;
        }

        public int DirtyCount => _dirty.Count;

        public bool GetFlag(string key) => _store.GetInt(key, 0) == 1;

        public void SetFlag(string key, bool value)
        {
            int target = value ? 1 : 0;
            if (_store.GetInt(key, 0) == target) return;
            _store.SetInt(key, target);
            _dirty.Add(key);
            MaybeFlush();
        }

        /// <summary>
        /// Forces a flush regardless of debounce timer. Call on
        /// OnApplicationPause / OnApplicationQuit so a tab close does not
        /// drop pending writes.
        /// </summary>
        public void ForceFlush()
        {
            if (_dirty.Count == 0) return;
            _store.Save();
            _dirty.Clear();
            _lastFlushUtc = _now.UtcNow;
        }

        public void MaybeFlush()
        {
            if (_dirty.Count == 0) return;
            if ((_now.UtcNow - _lastFlushUtc) < _flushInterval) return;
            ForceFlush();
        }
    }
}
