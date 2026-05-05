using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace FortuneValley.UI.World
{
    /// <summary>
    /// Pure-C# orchestrator for the coin button's day-end flash sequence:
    /// fade-in -> punch-scale + color tint -> hold -> conditional fade-out.
    /// The host MonoBehaviour passes its tween targets in the constructor and
    /// calls Play with a "stayVisibleAfter" predicate so the sequencer respects
    /// whether the player is still hovering when the flash ends (hover keeps
    /// the coin visible; otherwise it fades back to alpha 0).
    /// </summary>
    public class CoinFlashSequencer
    {
        private readonly Transform _transform;
        private readonly Vector3 _baseScale;
        private readonly CanvasGroup _visibilityGroup;
        private readonly Image _tintImage;
        private readonly Color _baseColor;
        private readonly Color _flashColor;
        private readonly float _flashScale;
        private readonly float _flashDuration;
        private readonly float _fadeInDuration;
        private readonly float _holdDuration;
        private readonly float _fadeOutDuration;

        private Sequence _current;

        public bool IsPlaying => _current != null && _current.IsActive() && !_current.IsComplete();

        public CoinFlashSequencer(
            Transform transform,
            Vector3 baseScale,
            CanvasGroup visibilityGroup,
            Image tintImage,
            Color flashColor,
            float flashScale,
            float flashDuration,
            float fadeInDuration,
            float holdDuration,
            float fadeOutDuration)
        {
            _transform = transform;
            _baseScale = baseScale;
            _visibilityGroup = visibilityGroup;
            _tintImage = tintImage;
            _baseColor = tintImage != null ? tintImage.color : Color.white;
            _flashColor = flashColor;
            _flashScale = flashScale;
            _flashDuration = flashDuration;
            _fadeInDuration = fadeInDuration;
            _holdDuration = holdDuration;
            _fadeOutDuration = fadeOutDuration;
        }

        public void Play(Func<bool> stayVisibleAfter, Action onComplete = null)
        {
            Kill();

            _transform.localScale = _baseScale;
            if (_tintImage != null) _tintImage.color = _baseColor;

            var seq = DOTween.Sequence();

            if (_visibilityGroup != null && _fadeInDuration > 0f)
            {
                var group = _visibilityGroup;
                seq.Append(DOTween.To(() => group.alpha, a => group.alpha = a, 1f, _fadeInDuration));
            }
            else if (_visibilityGroup != null)
            {
                _visibilityGroup.alpha = 1f;
            }

            float punchMagnitude = Mathf.Max(0f, _flashScale - 1f);
            seq.Append(_transform.DOPunchScale(_baseScale * punchMagnitude, _flashDuration, 1, 0.4f));

            if (_tintImage != null)
            {
                var img = _tintImage;
                seq.Join(DOTween.To(() => img.color, c => img.color = c, _flashColor, _flashDuration * 0.4f)
                    .SetLoops(2, LoopType.Yoyo));
            }

            if (_holdDuration > 0f)
            {
                seq.AppendInterval(_holdDuration);
            }

            seq.AppendCallback(() =>
            {
                if (_tintImage != null) _tintImage.color = _baseColor;
            });

            if (_visibilityGroup != null && _fadeOutDuration > 0f)
            {
                var group = _visibilityGroup;
                float fadeOut = _fadeOutDuration;
                seq.AppendCallback(() =>
                {
                    if (stayVisibleAfter != null && stayVisibleAfter())
                    {
                        return;
                    }
                    DOTween.To(() => group.alpha, a => group.alpha = a, 0f, fadeOut);
                });
            }

            if (onComplete != null)
            {
                seq.OnComplete(() => onComplete());
            }

            _current = seq;
        }

        public void Kill()
        {
            if (_current != null && _current.IsActive())
            {
                _current.Kill();
            }
            _current = null;
        }
    }
}
