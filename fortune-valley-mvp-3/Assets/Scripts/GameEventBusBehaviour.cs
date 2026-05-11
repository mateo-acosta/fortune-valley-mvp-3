using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Scene-bound MonoBehaviour that owns a single <see cref="GameEventBus"/>
    /// instance and exposes it via <see cref="Bus"/>. Other MonoBehaviours
    /// reference this via [SerializeField] for inspector-wired dependency
    /// injection (no FindFirstObjectByType, no static singletons).
    /// </summary>
    public class GameEventBusBehaviour : MonoBehaviour
    {
        private GameEventBus _bus;

        public IGameEventBus Bus => _bus ??= new GameEventBus();
    }
}
