using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FortuneValley.Domain.Notifications;

namespace FortuneValley.UI.Notifications
{
    /// <summary>
    /// One pooled banner instance. Show/hide animations are cached DOTween
    /// Sequences built once in Awake and replayed per show via Restart(),
    /// so banner display is allocation-free under high-throughput notification load.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class BannerView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform _root;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Button _clickArea;

        [Header("Animation")]
        [Tooltip("Local-space offset where the banner starts off-screen for the slide-in.")]
        [SerializeField] private Vector2 _offscreenOffset = new Vector2(600f, 0f);
        [SerializeField] private float _showDuration = 0.35f;
        [SerializeField] private float _hideDuration = 0.25f;

        private Sequence _showSeq;
        private Sequence _hideSeq;
        private Vector2 _onscreenAnchoredPosition;
        private Vector2 _offscreenAnchoredPosition;
        private GuidanceBannerRequest _currentRequest;
        private float _autoDismissAt;
        private bool _isShowing;

        public event Action<BannerView, GuidanceBannerRequest> OnClicked;
        public event Action<BannerView, GuidanceBannerRequest> OnDismissed;

        private void Awake()
        {
            // Capture the inspector-set anchored position as the on-screen target;
            // off-screen is computed once relative to it.
            _onscreenAnchoredPosition = _root.anchoredPosition;
            _offscreenAnchoredPosition = _onscreenAnchoredPosition + _offscreenOffset;

            BuildSequences();

            if (_clickArea != null) _clickArea.onClick.AddListener(HandleClick);
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _showSeq?.Kill();
            _hideSeq?.Kill();
            if (_clickArea != null) _clickArea.onClick.RemoveListener(HandleClick);
        }

        private void Update()
        {
            if (!_isShowing) return;
            if (Time.unscaledTime >= _autoDismissAt) Hide();
        }

        public void Show(
            GuidanceBannerRequest request,
            BannerSeverityPalette.Entry styleEntry,
            Sprite iconOverride)
        {
            _currentRequest = request;
            _titleText.text = request.Title;
            _messageText.text = request.Message;
            if (_backgroundImage != null) _backgroundImage.color = styleEntry.color;
            if (_iconImage != null) _iconImage.sprite = iconOverride != null ? iconOverride : styleEntry.defaultIcon;

            _autoDismissAt = Time.unscaledTime + Mathf.Max(0.5f, styleEntry.durationSeconds);
            _isShowing = true;

            gameObject.SetActive(true);
            _root.anchoredPosition = _offscreenAnchoredPosition;
            _canvasGroup.alpha = 0f;
            _hideSeq.Pause();
            _showSeq.Restart();
        }

        public void Hide()
        {
            if (!_isShowing) return;
            _isShowing = false;
            _showSeq.Pause();
            _hideSeq.Restart();
        }

        public void SetSlotPosition(Vector2 anchoredPosition)
        {
            _onscreenAnchoredPosition = anchoredPosition;
            _offscreenAnchoredPosition = _onscreenAnchoredPosition + _offscreenOffset;
            BuildSequences();
            if (_isShowing) _root.anchoredPosition = _onscreenAnchoredPosition;
        }

        private void BuildSequences()
        {
            _showSeq?.Kill();
            _hideSeq?.Kill();

            // Use explicit DOTween.To() lambdas instead of DOAnchorPos / DOFade
            // shortcut extensions, since the FortuneValley.UI assembly only
            // references DOTween.dll (not the loose DOTweenModuleUI.cs source).
            _showSeq = DOTween.Sequence().SetAutoKill(false).Pause();
            _showSeq.Append(AnchoredPositionTween(_onscreenAnchoredPosition, _showDuration).SetEase(Ease.OutBack));
            _showSeq.Join(AlphaTween(1f, _showDuration * 0.7f));

            _hideSeq = DOTween.Sequence().SetAutoKill(false).Pause();
            _hideSeq.Append(AnchoredPositionTween(_offscreenAnchoredPosition, _hideDuration).SetEase(Ease.InCubic));
            _hideSeq.Join(AlphaTween(0f, _hideDuration));
            _hideSeq.OnComplete(HandleHideComplete);
        }

        private Tween AnchoredPositionTween(Vector2 target, float duration) =>
            DOTween.To(() => _root.anchoredPosition, p => _root.anchoredPosition = p, target, duration);

        private Tween AlphaTween(float target, float duration) =>
            DOTween.To(() => _canvasGroup.alpha, a => _canvasGroup.alpha = a, target, duration);

        private void HandleHideComplete()
        {
            gameObject.SetActive(false);
            OnDismissed?.Invoke(this, _currentRequest);
        }

        private void HandleClick()
        {
            OnClicked?.Invoke(this, _currentRequest);
            Hide();
        }
    }
}
