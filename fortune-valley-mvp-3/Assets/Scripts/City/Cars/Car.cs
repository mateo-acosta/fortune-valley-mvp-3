using UnityEngine;
using UnityEngine.Splines;

namespace FortuneValley.City.Cars
{
    /// <summary>
    /// Lightweight wrapper around SplineAnimate. The spawner assigns a
    /// SplineContainer and starts playback. Animation, rotation, and looping
    /// are handled by Unity's built-in SplineAnimate component.
    /// </summary>
    [RequireComponent(typeof(SplineAnimate))]
    public class Car : MonoBehaviour
    {
        [SerializeField] private SplineAnimate _animate;

        private void Reset()
        {
            _animate = GetComponent<SplineAnimate>();
        }

        public void AssignRoute(SplineContainer container, float duration, float startT)
        {
            if (_animate == null) _animate = GetComponent<SplineAnimate>();
            if (_animate == null) return;

            _animate.Container = container;
            _animate.Duration = duration;
            _animate.Loop = SplineAnimate.LoopMode.Loop;
            _animate.StartOffset = startT;
            _animate.Restart(true);
            _animate.Play();
        }

        public void StopAndHide()
        {
            if (_animate != null) _animate.Pause();
            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }
    }
}
