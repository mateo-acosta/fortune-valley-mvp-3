using System;

namespace FortuneValley.Core
{
    /// <summary>
    /// Typed pub/sub bus for game events. Each event is a plain DTO class or struct.
    /// Production binding lives at <see cref="GameEventBus"/>; tests inject a fake for
    /// fast EditMode coverage without touching static state.
    /// </summary>
    public interface IGameEventBus
    {
        void Subscribe<TEvent>(Action<TEvent> handler);
        void Unsubscribe<TEvent>(Action<TEvent> handler);
        void Raise<TEvent>(TEvent payload);
    }
}
