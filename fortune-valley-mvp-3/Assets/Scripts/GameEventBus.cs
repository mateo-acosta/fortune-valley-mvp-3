using System;
using System.Collections.Generic;

namespace FortuneValley.Core
{
    /// <summary>
    /// Default <see cref="IGameEventBus"/> implementation backed by a per-type
    /// delegate dictionary. Thread-affinity: Unity main thread only.
    /// </summary>
    public class GameEventBus : IGameEventBus
    {
        private readonly Dictionary<Type, Delegate> _handlers = new Dictionary<Type, Delegate>();

        public void Subscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null) return;
            var type = typeof(TEvent);
            _handlers.TryGetValue(type, out var existing);
            _handlers[type] = Delegate.Combine(existing, handler);
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null) return;
            var type = typeof(TEvent);
            if (!_handlers.TryGetValue(type, out var existing)) return;
            var remaining = Delegate.Remove(existing, handler);
            if (remaining == null) _handlers.Remove(type);
            else _handlers[type] = remaining;
        }

        public void Raise<TEvent>(TEvent payload)
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out var existing)) return;
            ((Action<TEvent>)existing).Invoke(payload);
        }
    }
}
