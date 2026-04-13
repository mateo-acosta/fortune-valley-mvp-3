using UnityEngine;

namespace FortuneValley.UI.Effects
{
    /// <summary>
    /// Thin wrapper around a ParticleSystem for one-shot celebration bursts.
    /// No game logic -- just Play() on trigger.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class ConfettiBurst : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _particles;

        private void Awake()
        {
            if (_particles == null) _particles = GetComponent<ParticleSystem>();
        }

        public void Play()
        {
            if (_particles == null) return;
            _particles.Stop();
            _particles.Play();
        }
    }
}
