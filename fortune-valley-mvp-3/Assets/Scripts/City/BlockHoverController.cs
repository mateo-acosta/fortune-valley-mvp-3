using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;

namespace FortuneValley.City
{
    /// <summary>
    /// Shows and hides a world-space canvas on the block when the mouse is over
    /// the block's footprint. The footprint is derived from 4 corner anchors in
    /// world space, so it matches where the block visually sits regardless of
    /// the block GameObject's own transform position.
    ///
    /// Also handles left-click while hovered: if the block is player-owned
    /// (queried via the inspector-wired CityManager + BlockController), raises
    /// GameEvents.OnRestaurantSelected so existing listeners
    /// (RestaurantUpgradePanel, the onboarding tutorial's WaitForRestaurantTap
    /// step) can react. No Unity EventSystem / IPointerClickHandler is used
    /// because blocks have no Collider; clicks come from Mouse.current polling
    /// in the same Update loop that drives hover.
    /// </summary>
    public class BlockHoverController : MonoBehaviour
    {
        [Header("Canvas Refs")]
        [SerializeField] private GameObject _canvasRoot;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Footprint Anchors (4 corners in world space)")]
        [SerializeField] private Transform _cornerNW;
        [SerializeField] private Transform _cornerNE;
        [SerializeField] private Transform _cornerSE;
        [SerializeField] private Transform _cornerSW;

        [Header("Camera Ref")]
        [SerializeField] private Camera _camera;

        [Header("Click-to-select (optional, wire for player-owned lots)")]
        [Tooltip("BlockController on this same block. Used to read the owned lot id.")]
        [SerializeField] private BlockController _block;
        [Tooltip("CityManager reference. Used to look up the current owner of this block's lot.")]
        [SerializeField] private CityManager _cityManager;
        [Tooltip("If true, left-clicking while hovered raises OnRestaurantSelected when the block is owned by the player.")]
        [SerializeField] private bool _raiseRestaurantSelectedOnClick = true;

        [Header("Footprint Height")]
        [Tooltip("Height of the hover box above the corner anchors, in world units.")]
        [SerializeField] private float _footprintHeight = 5f;

        [Header("Fade Timing")]
        [SerializeField] private float _fadeInDuration = 0.15f;
        [SerializeField] private float _fadeOutDuration = 0.2f;
        [SerializeField] private float _exitGrace = 0.1f;

        private Bounds _bounds;
        private bool _boundsReady;
        private bool _isHovered;
        private Tween _currentTween;
        private Sequence _exitSequence;
        private static int s_openPanelCount;

        private void Awake()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }
            if (_canvasRoot != null) _canvasRoot.SetActive(false);

            BuildFootprintBounds();
        }

        private void OnEnable()
        {
            GameEvents.OnBlockingPanelOpenChanged += HandlePanelStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnBlockingPanelOpenChanged -= HandlePanelStateChanged;
            if (_currentTween != null) _currentTween.Kill();
            if (_exitSequence != null) _exitSequence.Kill();
        }

        private void HandlePanelStateChanged(bool open)
        {
            if (open) s_openPanelCount++;
            else if (s_openPanelCount > 0) s_openPanelCount--;

            if (s_openPanelCount > 0 && _isHovered)
            {
                _isHovered = false;
                Hide();
            }
        }

        private void BuildFootprintBounds()
        {
            if (_cornerNW == null || _cornerNE == null || _cornerSE == null || _cornerSW == null)
            {
                return;
            }

            Vector3 a = _cornerNW.position;
            Vector3 b = _cornerNE.position;
            Vector3 c = _cornerSE.position;
            Vector3 d = _cornerSW.position;

            Vector3 min = Vector3.Min(Vector3.Min(a, b), Vector3.Min(c, d));
            Vector3 max = Vector3.Max(Vector3.Max(a, b), Vector3.Max(c, d));

            Vector3 center = (min + max) * 0.5f;
            center.y = min.y + _footprintHeight * 0.5f;

            Vector3 size = max - min;
            size.y = _footprintHeight;

            _bounds = new Bounds(center, size);
            _boundsReady = true;
        }

        private void Update()
        {
            if (!_boundsReady) return;
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 mousePos = mouse.position.ReadValue();
            Ray ray = _camera.ScreenPointToRay(mousePos);
            bool hitThis = _bounds.IntersectRay(ray);

            // Click detection runs even when a modal panel is open so the
            // onboarding tutorial's WaitForRestaurantTap step can advance
            // while the tutorial overlay's OnBlockingPanelOpenChanged broadcast
            // has incremented s_openPanelCount.
            if (_raiseRestaurantSelectedOnClick && hitThis
                && mouse.leftButton.wasPressedThisFrame
                && IsPlayerOwned())
            {
                Debug.Log($"[BlockHover] Click on {name} (player-owned) -> RaiseRestaurantSelected");
                GameEvents.RaiseRestaurantSelected();
            }

            // Hover canvas is suppressed while any modal panel is open.
            if (s_openPanelCount > 0)
            {
                if (_isHovered) { _isHovered = false; Hide(); }
                return;
            }

            if (hitThis && !_isHovered)
            {
                _isHovered = true;
                Debug.Log($"[BlockHover] Enter on {name}");
                Show();
            }
            else if (!hitThis && _isHovered)
            {
                _isHovered = false;
                Debug.Log($"[BlockHover] Exit on {name}");
                Hide();
            }
        }

        private bool IsPlayerOwned()
        {
            if (_block == null || _cityManager == null) return false;
            var lot = _block.OwnedLot;
            if (lot == null) return false;
            return _cityManager.GetOwner(lot.LotId) == Owner.Player;
        }

        private void Show()
        {
            if (_canvasRoot == null || _canvasGroup == null) return;

            if (_exitSequence != null) _exitSequence.Kill();
            if (_currentTween != null) _currentTween.Kill();

            _canvasRoot.SetActive(true);
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
            _currentTween = FadeCanvasGroup(1f, _fadeInDuration);
        }

        private void Hide()
        {
            if (_canvasRoot == null || _canvasGroup == null) return;

            if (_currentTween != null) _currentTween.Kill();
            if (_exitSequence != null) _exitSequence.Kill();

            _exitSequence = DOTween.Sequence();
            _exitSequence.AppendInterval(_exitGrace);
            _exitSequence.Append(FadeCanvasGroup(0f, _fadeOutDuration));
            _exitSequence.OnComplete(FinalizeHide);
        }

        private Tween FadeCanvasGroup(float targetAlpha, float duration)
        {
            CanvasGroup cg = _canvasGroup;
            return DOTween.To(() => cg.alpha, x => cg.alpha = x, targetAlpha, duration);
        }

        private void FinalizeHide()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }
            if (_canvasRoot != null) _canvasRoot.SetActive(false);
        }
    }
}
