using UnityEngine;
using FortuneValley.Core;

namespace FortuneValley.UI.Tutorial
{
    /// <summary>
    /// Full-screen CanvasGroup that swallows raycasts while the tutorial
    /// needs to gate input. Subscribes to
    /// <c>GameEvents.OnTutorialInputBlockChanged</c> for activation.
    ///
    /// Selective passthrough (one specific target is clickable, everything
    /// else is blocked) is achieved at scene design time by placing the
    /// target on a Canvas with a higher sort order than the blocker, or by
    /// disabling world-space input via LotSelector.SetEnabled and
    /// re-enabling it for the highlighted lot only.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class InputBlocker : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private GameObject _root;

        private void Awake()
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_root == null) _root = gameObject;
            Deactivate();
        }

        private void OnEnable()
        {
            GameEvents.OnTutorialInputBlockChanged += HandleBlockChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnTutorialInputBlockChanged -= HandleBlockChanged;
        }

        private void HandleBlockChanged(bool blocked)
        {
            if (blocked) Activate();
            else Deactivate();
        }

        public bool IsActive { get; private set; }

        public void Activate()
        {
            IsActive = true;
            if (_root != null) _root.SetActive(true);
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = false;
            }
        }

        public void Deactivate()
        {
            IsActive = false;
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }
            if (_root != null) _root.SetActive(false);
        }
    }
}
