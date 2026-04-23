using DG.Tweening;
using UnityEngine;
using FortuneValley.Core;

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

        private void Awake()
        {
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            if (_arrowRect != null) _arrowRect.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.OnTutorialHighlightTarget += HandleHighlightTarget;
        }

        private void OnDisable()
        {
            GameEvents.OnTutorialHighlightTarget -= HandleHighlightTarget;
        }

        private void OnDestroy() => _fadeTween?.Kill();

        private void HandleHighlightTarget(Transform target)
        {
            if (target == null) Hide();
            else Show(target);
        }

        private void LateUpdate()
        {
            if (!_isShowing || _target == null || _trackingCamera == null || _arrowRect == null) return;

            Vector3 screen = _trackingCamera.WorldToScreenPoint(_target.position);
            Vector2 baseScreenPos = (Vector2)screen + _screenOffset;

            float phase = _bouncePeriod > 0f ? (Time.unscaledTime / _bouncePeriod) : 0f;
            float bounceY = Mathf.Sin(phase * Mathf.PI * 2f) * _bounceDistance;
            _arrowRect.position = new Vector3(baseScreenPos.x, baseScreenPos.y + bounceY, 0f);
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
