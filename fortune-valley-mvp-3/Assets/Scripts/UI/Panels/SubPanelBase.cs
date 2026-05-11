using UnityEngine;

namespace FortuneValley.UI.Panels
{
    /// <summary>
    /// Minimal base class for all Homebase sub-panel controllers.
    /// SidebarController toggles sub-panel GameObjects via SetActive(),
    /// which triggers OnEnable/OnDisable automatically.
    ///
    /// Subclasses: subscribe to GameEvents in OnEnable (before base call),
    /// unsubscribe in OnDisable (after base call), implement Refresh().
    /// </summary>
    public abstract class SubPanelBase : MonoBehaviour
    {
        protected virtual void OnEnable()
        {
            Refresh();
        }

        protected virtual void OnDisable() { }

        /// <summary>
        /// Populate all UI elements from live data.
        /// Called automatically on enable and by event handlers.
        /// </summary>
        protected abstract void Refresh();
    }
}
