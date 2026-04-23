using System;
using System.Collections.Generic;
using FortuneValley.Core;

namespace FortuneValley.Tests.Fakes
{
    /// <summary>
    /// Test double for <see cref="IGameEventBus"/>. Records every Raise call so
    /// assertions can verify both the type and the payload that was published,
    /// while still notifying real subscribers.
    /// </summary>
    public class FakeGameEventBus : IGameEventBus
    {
        private readonly Dictionary<Type, Delegate> _handlers = new Dictionary<Type, Delegate>();
        public List<object> RaisedEvents { get; } = new List<object>();

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
            RaisedEvents.Add(payload);
            if (!_handlers.TryGetValue(typeof(TEvent), out var existing)) return;
            ((Action<TEvent>)existing).Invoke(payload);
        }

        public int CountOf<TEvent>()
        {
            var count = 0;
            foreach (var e in RaisedEvents)
            {
                if (e is TEvent) count++;
            }
            return count;
        }
    }
}
