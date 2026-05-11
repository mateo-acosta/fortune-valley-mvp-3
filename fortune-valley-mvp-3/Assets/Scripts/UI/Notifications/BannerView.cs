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
    ///
    /// Auto-detects whether the parent Transform has a LayoutGroup
    /// (Vertical/Horizontal/Grid). When present, the slide animation is
    /// skipped because the layout group would overwrite anchoredPosition
    /// every frame; only the alpha fade runs. When absent, the full slide +
    /// fade runs (driven by the slot position set via <see cref="SetSlotPosition"/>).
    ///
    /// TextMeshProUGUI refs for title and message are both optional: banner
    /// designs with a single text field only wire MessageText and the
    /// component writes the request's message there, leaving title off.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class BannerView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform _root;
        [SerializeField] private CanvasGroup _canvasGroup;
        [Tooltip("Optional. When null, title is not rendered.")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [Tooltip("Optional. When null, message is not rendered. If only one text field exists on the design, wire it here.")]
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Button _clickArea;

        [Header("Animation")]
        [Tooltip("Local-space offset where the banner starts off-screen for the slide-in. " +
                 "Ignored when the parent has a LayoutGroup component; fade-only runs instead.")]
        [SerializeField] private Vector2 _offscreenOffset = new Vector2(-600f, 0f);
        [SerializeField] private float _showDuration = 0.35f;
        [SerializeField] private float _hideDuration = 0.25f;

        private Sequence _showSeq;
        private Sequence _hideSeq;
        private Vector2 _onscreenAnchoredPosition;
        private Vector2 _offscreenAnchoredPosition;
        private GuidanceBannerRequest _currentRequest;
        private float _autoDismissAt;
        private bool _isShowing;
        private bool _parentControlsLayout;

        public event Action<BannerView, GuidanceBannerRequest> OnClicked;
        public event Action<BannerView, GuidanceBannerRequest> OnDismissed;

        private void Awake()
        {
            _parentControlsLayout = DetectParentLayoutGroup();

            // Capture the inspector-set anchored position as the on-screen target;
            // off-screen is computed once relative to it (only used when the parent
            // does not control layout).
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
            if (_titleText != null) _titleText.text = request.Title;
            if (_messageText != null) _messageText.text = request.Message;
            if (_backgroundImage != null) _backgroundImage.color = styleEntry.color;
            if (_iconImage != null) _iconImage.sprite = iconOverride != null ? iconOverride : styleEntry.defaultIcon;

            _autoDismissAt = Time.unscaledTime + Mathf.Max(0.5f, styleEntry.durationSeconds);
            _isShowing = true;

            gameObject.SetActive(true);
            if (!_parentControlsLayout) _root.anchoredPosition = _offscreenAnchoredPosition;
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
            if (_parentControlsLayout) return; // parent LayoutGroup owns positioning
            _onscreenAnchoredPosition = anchoredPosition;
            _offscreenAnchoredPosition = _onscreenAnchoredPosition + _offscreenOffset;
            BuildSequences();
            if (_isShowing) _root.anchoredPosition = _onscreenAnchoredPosition;
        }

        private void BuildSequences()
        {
            _showSeq?.Kill();
            _hideSeq?.Kill();

            // Explicit DOTween.To() lambdas (FortuneValley.UI references only
            // DOTween.dll, not the loose DOTweenModuleUI.cs shortcut source).
            _showSeq = DOTween.Sequence().SetAutoKill(false).Pause();
            if (!_parentControlsLayout)
            {
                _showSeq.Append(AnchoredPositionTween(_onscreenAnchoredPosition, _showDuration).SetEase(Ease.OutBack));
                _showSeq.Join(AlphaTween(1f, _showDuration * 0.7f));
            }
            else
            {
                _showSeq.Append(AlphaTween(1f, _showDuration).SetEase(Ease.OutCubic));
            }

            _hideSeq = DOTween.Sequence().SetAutoKill(false).Pause();
            if (!_parentControlsLayout)
            {
                _hideSeq.Append(AnchoredPositionTween(_offscreenAnchoredPosition, _hideDuration).SetEase(Ease.InCubic));
                _hideSeq.Join(AlphaTween(0f, _hideDuration));
            }
            else
            {
                _hideSeq.Append(AlphaTween(0f, _hideDuration).SetEase(Ease.InCubic));
            }
            _hideSeq.OnComplete(HandleHideComplete);
        }

        private bool DetectParentLayoutGroup()
        {
            var parent = _root != null ? _root.parent : transform.parent;
            return parent != null && parent.GetComponent<HorizontalOrVerticalLayoutGroup>() != null;
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
