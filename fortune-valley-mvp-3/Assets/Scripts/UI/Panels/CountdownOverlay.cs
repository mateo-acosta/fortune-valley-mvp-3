using UnityEngine;
using TMPro;
using FortuneValley.Core;

namespace FortuneValley.UI.Panels
{
    /// <summary>
    /// Full-screen 3-2-1-GO! overlay that plays before the game starts.
    /// Blocks all input via CanvasGroup.blocksRaycasts during countdown.
    /// Uses Update-loop timing (no coroutines) for consistency with other overlays.
    /// </summary>
    public class CountdownOverlay : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════════════════════════

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _numberText;   // "3","2","1" — MuseoModerno Red
        [SerializeField] private TextMeshProUGUI _goText;       // "GO!" — Quicksand Gold
        [SerializeField] private CanvasGroup _canvasGroup;      // Blocks raycasts during countdown

        [Header("Animation Timing")]
        [Min(0.01f)] [SerializeField] private float _stepDuration    = 1.0f;  // Seconds per number
        [Min(0.01f)] [SerializeField] private float _goDuration      = 0.8f;  // Seconds GO! is visible
        [Min(0.01f)] [SerializeField] private float _popDuration     = 0.35f; // Length of scale-pop phase
        [Min(0.01f)] [SerializeField] private float _popPeakFraction = 0.60f; // Fraction into pop when peak is reached

        [Header("Animation Scale")]
        [SerializeField] private float _scaleStart     = 0.40f;
        [SerializeField] private float _scaleOvershoot = 1.15f;
        [SerializeField] private float _scaleFull      = 1.00f;

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE STATE
        // ═══════════════════════════════════════════════════════════════

        private enum CountdownState { Idle, Number3, Number2, Number1, Go, Done }

        private CountdownState _state     = CountdownState.Idle;
        private float          _stepTimer = 0f;
        private System.Action  _onComplete;

        private void OnEnable()
        {
            GameEvents.OnStartCountdown += HandleStartCountdown;
        }

        private void OnDisable()
        {
            GameEvents.OnStartCountdown -= HandleStartCountdown;
        }

        /// <summary>
        /// Handles the GameEvents.OnStartCountdown event.
        /// Starts the countdown and fires OnCountdownComplete when done.
        /// </summary>
        private void HandleStartCountdown()
        {
            StartCountdown(() => GameEvents.RaiseCountdownComplete());
        }

        private void Update()
        {
            if (_state == CountdownState.Idle || _state == CountdownState.Done) return;

            _stepTimer += Time.deltaTime;

            switch (_state)
            {
                case CountdownState.Number3:
                case CountdownState.Number2:
                case CountdownState.Number1:
                    UpdateNumber();
                    break;
                case CountdownState.Go:
                    UpdateGo();
                    break;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC API
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Start the 3-2-1-GO! sequence, then invoke onComplete.
        /// If a countdown is already running, it is cancelled (old callback is NOT invoked).
        /// </summary>
        public void StartCountdown(System.Action onComplete)
        {
            // Cancel in-flight countdown without invoking the old callback
            if (_state != CountdownState.Idle && _state != CountdownState.Done)
            {
                _onComplete = null;   // Discard old callback before FinishCountdown clears it
                FinishCountdown();
            }

            _onComplete = onComplete;
            _state      = CountdownState.Number3;
            _stepTimer  = 0f;

            // Reset number text to "3"
            if (_numberText != null)
            {
                _numberText.text = "3";
                SetAlpha(_numberText, 1f);
                _numberText.rectTransform.localScale = Vector3.one * _scaleStart;
                _numberText.gameObject.SetActive(true);
            }

            // Hide GO! text until needed
            if (_goText != null)
                _goText.gameObject.SetActive(false);

            // Block all input while counting down
            if (_canvasGroup != null)
                _canvasGroup.blocksRaycasts = true;

            gameObject.SetActive(true);
        }

        // ═══════════════════════════════════════════════════════════════
        // ANIMATION — NUMBER PHASE
        // ═══════════════════════════════════════════════════════════════

        private void UpdateNumber()
        {
            // Guard against bad Inspector values causing division by zero / NaN
            float safePop  = Mathf.Max(0.01f, _popDuration);
            float safeFade = Mathf.Max(0.01f, _stepDuration - safePop);

            if (_stepTimer < safePop)
            {
                // Pop phase: scale punches from scaleStart → scaleOvershoot → scaleFull
                float t = _stepTimer / safePop;
                float peakFrac = Mathf.Clamp(_popPeakFraction, 0.01f, 0.99f);
                float scale;

                if (t < peakFrac)
                    scale = Mathf.Lerp(_scaleStart, _scaleOvershoot, EaseOut(t / peakFrac));
                else
                    scale = Mathf.Lerp(_scaleOvershoot, _scaleFull, (t - peakFrac) / Mathf.Max(0.01f, 1f - peakFrac));

                if (_numberText != null)
                {
                    _numberText.rectTransform.localScale = Vector3.one * scale;
                    SetAlpha(_numberText, 1f); // Fully opaque during pop
                }
            }
            else
            {
                // Fade phase: scale holds at full; text fades out
                float t = (_stepTimer - safePop) / safeFade;

                if (_numberText != null)
                {
                    _numberText.rectTransform.localScale = Vector3.one * _scaleFull;
                    SetAlpha(_numberText, Mathf.Lerp(1f, 0f, t));
                }
            }

            if (_stepTimer >= _stepDuration)
                AdvanceToNextState();
        }

        // ═══════════════════════════════════════════════════════════════
        // ANIMATION — GO! PHASE
        // ═══════════════════════════════════════════════════════════════

        private void UpdateGo()
        {
            float safeDuration = Mathf.Max(0.01f, _goDuration);
            float progress     = _stepTimer / safeDuration; // 0 → 1 over goDuration

            if (_goText != null)
            {
                if (progress < 0.2f)
                {
                    // 0–20%: scale in from scaleStart to scaleFull
                    float t = progress / 0.2f;
                    _goText.rectTransform.localScale = Vector3.one * Mathf.Lerp(_scaleStart, _scaleFull, EaseOut(t));
                    SetAlpha(_goText, 1f);
                }
                else if (progress < 0.7f)
                {
                    // 20–70%: hold at full scale and full alpha
                    _goText.rectTransform.localScale = Vector3.one * _scaleFull;
                    SetAlpha(_goText, 1f);
                }
                else
                {
                    // 70–100%: fade out
                    float t = (progress - 0.7f) / 0.3f;
                    _goText.rectTransform.localScale = Vector3.one * _scaleFull;
                    SetAlpha(_goText, Mathf.Lerp(1f, 0f, t));
                }
            }

            if (_stepTimer >= _goDuration)
                FinishCountdown();
        }

        // ═══════════════════════════════════════════════════════════════
        // STATE MACHINE
        // ═══════════════════════════════════════════════════════════════

        private void AdvanceToNextState()
        {
            _stepTimer = 0f;

            switch (_state)
            {
                case CountdownState.Number3:
                    _state = CountdownState.Number2;
                    PrepareNumberText("2");
                    break;

                case CountdownState.Number2:
                    _state = CountdownState.Number1;
                    PrepareNumberText("1");
                    break;

                case CountdownState.Number1:
                    _state = CountdownState.Go;
                    if (_numberText != null) _numberText.gameObject.SetActive(false);
                    if (_goText != null)
                    {
                        _goText.gameObject.SetActive(true);
                        _goText.rectTransform.localScale = Vector3.one * _scaleStart;
                        SetAlpha(_goText, 1f);
                    }
                    break;
            }
        }

        private void PrepareNumberText(string text)
        {
            if (_numberText == null) return;
            _numberText.text = text;
            SetAlpha(_numberText, 1f);
            _numberText.rectTransform.localScale = Vector3.one * _scaleStart;
        }

        private void FinishCountdown()
        {
            _state = CountdownState.Done;

            if (_canvasGroup != null)
                _canvasGroup.blocksRaycasts = false;

            gameObject.SetActive(false);

            // Invoke and immediately null the callback to prevent double-invocation
            var cb  = _onComplete;
            _onComplete = null;
            cb?.Invoke();
        }

        // ═══════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Quadratic ease-out: fast start, slow end.</summary>
        private float EaseOut(float t) => 1f - (1f - t) * (1f - t);

        /// <summary>Sets the alpha channel on a TMP text's color directly (not CanvasGroup),
        /// keeping the CanvasGroup alpha independent so it can control raycast blocking separately.</summary>
        private static void SetAlpha(TextMeshProUGUI text, float alpha)
        {
            Color c = text.color;
            c.a = alpha;
            text.color = c;
        }
    }
}
