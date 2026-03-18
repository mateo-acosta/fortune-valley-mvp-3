using UnityEngine;
using UnityEngine.EventSystems;

namespace FortuneValley.Core
{
    /// <summary>
    /// Attach this to the restaurant building GameObject in the scene.
    /// When the player clicks the building, it fires OnRestaurantSelected
    /// so the UI can open the upgrade panel without a direct reference.
    ///
    /// REQUIRES: A PhysicsRaycaster on the Main Camera so world-space
    /// clicks reach the EventSystem.
    /// </summary>
    public class RestaurantClickHandler : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            GameEvents.RaiseRestaurantSelected();
        }
    }
}
