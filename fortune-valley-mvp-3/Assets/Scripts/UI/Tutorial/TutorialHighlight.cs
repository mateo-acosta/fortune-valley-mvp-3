using DG.Tweening;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Managers.Tutorial;

namespace FortuneValley.UI.Tutorial
{
    /// <summary>
    /// Pulsing arrow + ring that points at a tutorial target Transform.
    /// Subscribes to <c>GameEvents.OnTutorialHighlightTarget</c>; null
    /// clears the highlight. Positions its RectTransform from the target's
    /// world position via the wired tracking Camera each LateUpdate while
    /// active, so the highlight stays aligned even as the camera or the
    /// target moves.
    /// </summary>
    public class TutorialHighlight : MonoBehaviour
    {
        [SerializeField] private RectTransform _arrowRect;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Camera _trackingCamera;
        [SerializeField] private Vector2 _screenOffset = new Vector2(0f, 80f);
        [SerializeField] private float _bounceDistance = 12f;
        [SerializeField] private float _bouncePeriod = 0.8f;
        [SerializeField] private float _fadeInSeconds = 0.2f;
        [SerializeField] private float _fadeOutSeconds = 0.15f;

        private Transform _target;
        private Tween _fadeTween;
        private bool _isShowing;
        private Vector2 _stepOffset;

        private void Awake()
        {
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            if (_arrowRect != null) _arrowRect.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.OnTutorialHighlightTarget += HandleHighlightTarget;
            GameEvents.OnTutorialArrowOffsetChanged += HandleArrowOffsetChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnTutorialHighlightTarget -= HandleHighlightTarget;
            GameEvents.OnTutorialArrowOffsetChanged -= HandleArrowOffsetChanged;
        }

        private void HandleArrowOffsetChanged(Vector2 offset) => _stepOffset = offset;

        private void OnDestroy() => _fadeTween?.Kill();

        private void HandleHighlightTarget(Transform target)
        {
            if (target == null) Hide();
            else Show(target);
        }

        private void LateUpdate()
        {
            if (!_isShowing || _target == null || _arrowRect == null) return;

            Vector2 baseScreenPos = ResolveScreenPos(_target) + _screenOffset + _stepOffset;

            float phase = _bouncePeriod > 0f ? (Time.unscaledTime / _bouncePeriod) : 0f;
            float bounceY = Mathf.Sin(phase * Mathf.PI * 2f) * _bounceDistance;
            _arrowRect.position = new Vector3(baseScreenPos.x, baseScreenPos.y + bounceY, 0f);
        }

        /// <summary>
        /// Returns the screen-space position of the target. Handles both
        /// 3D world transforms (convert via the tracking Camera) and UI
        /// RectTransforms (already in screen pixels for a Screen Space
        /// Overlay canvas; converted via the camera for other canvas modes).
        /// </summary>
        private Vector2 ResolveScreenPos(Transform target)
        {
            var rt = target as RectTransform;
            if (rt != null)
            {
                // UI element: its world position IS already screen-space on
                // a Screen Space Overlay canvas. On Screen Space Camera /
                // World Space canvases, rt.position is still the correct
                // world position to convert.
                var canvas = rt.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    return rt.position;
                }
                if (_trackingCamera == null) return rt.position;
                return _trackingCamera.WorldToScreenPoint(rt.position);
            }

            // World-space targets: prefer authored corner anchors so the arrow
            // lands on the actual footprint center, not the parent transform
            // (e.g. block GOs sit at world origin while their lot lives
            // somewhere else entirely).
            var anchors = target.GetComponent<TutorialWorldBoundsAnchors>();
            if (anchors != null && anchors.TryGetBounds(out var bounds))
            {
                Camera cam = _trackingCamera != null ? _trackingCamera : Camera.main;
                if (cam != null) return cam.WorldToScreenPoint(bounds.center);
            }

            if (_trackingCamera == null) return Vector2.zero;
            return _trackingCamera.WorldToScreenPoint(target.position);
        }

        public void Show(Transform target)
        {
            _target = target;
            _isShowing = true;
            if (_arrowRect != null) _arrowRect.gameObject.SetActive(true);
            AnimateAlpha(1f, _fadeInSeconds);
        }

        public void Hide()
        {
            _isShowing = false;
            AnimateAlpha(0f, _fadeOutSeconds, onDone: () =>
            {
                if (_arrowRect != null) _arrowRect.gameObject.SetActive(false);
            });
        }

        private void AnimateAlpha(float target, float duration, System.Action onDone = null)
        {
            _fadeTween?.Kill();
            if (_canvasGroup == null)
            {
                onDone?.Invoke();
                return;
            }
            _fadeTween = DOTween
                .To(() => _canvasGroup.alpha, a => _canvasGroup.alpha = a, target, duration)
                .OnComplete(() => onDone?.Invoke());
        }
    }
}
