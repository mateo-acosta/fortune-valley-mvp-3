using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Managers.Tutorial
{
    /// <summary>
    /// Caches the most recent <see cref="GamePlayerStateDTO"/> delivered
    /// via <see cref="GameEvents.OnSaveStateLoaded"/> and exposes it as a
    /// plain property. BootFlowRouter reads <see cref="Current"/> at the
    /// moment the player clicks Start; if the server has not yet returned
    /// a state (first-ever login, slow network), Current is null which the
    /// router treats as "run the tutorial".
    /// </summary>
    public class PlayerStateAccessor : MonoBehaviour
    {
        public GamePlayerStateDTO Current { get; private set; }

        private void OnEnable()
        {
            GameEvents.OnSaveStateLoaded += HandleSaveStateLoaded;
        }

        private void OnDisable()
        {
            GameEvents.OnSaveStateLoaded -= HandleSaveStateLoaded;
        }

        public void SetCurrent(GamePlayerStateDTO state) => Current = state;

        private void HandleSaveStateLoaded(GamePlayerStateDTO state) => Current = state;
    }
}
