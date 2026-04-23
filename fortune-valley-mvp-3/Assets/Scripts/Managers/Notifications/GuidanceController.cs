using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Notifications;

namespace FortuneValley.Managers.Notifications
{
    /// <summary>
    /// Coordinator for the guidance banner pipeline. Dispatchers do not emit
    /// banner requests to the event bus directly; they call
    /// <see cref="Submit"/> here, and this controller decides whether the
    /// request is displayed, deferred while a modal popup is open, or
    /// dropped because the tutorial is suppressing events or the tip's
    /// <see cref="RepeatPolicy"/> is not satisfied.
    ///
    /// Invariants:
    /// - While suppressed (tutorial active), Submit drops the request and
    ///   does NOT mark the tip fired. Events fired during the tutorial are
    ///   gone forever (by explicit design).
    /// - While at least one modal popup is open, Submit enqueues into a
    ///   bounded BannerQueue (cap 8, severity-based eviction). On modal
    ///   close, the queue drains in FIFO order onto the bus.
    /// - RepeatPolicy filtering is applied at Submit time, and MarkFired
    ///   happens immediately on accept. This is the optimistic path:
    ///   OncePerPlayer / OncePerCooldown are consumed even if the player
    ///   closes the tab before the modal drain displays them. Acceptable
    ///   trade-off; the alternative (filter at drain time) mis-counts
    ///   when multiple same-id tips arrive inside one modal window.
    /// </summary>
    public class GuidanceController : MonoBehaviour
    {
        [SerializeField] private GameEventBusBehaviour _busBehaviour;

        private IGameEventBus _bus;
        private RepeatPolicyFilter _filter;
        private BannerQueue _modalDeferredQueue;
        private int _modalOpenCount;
        private bool _tutorialSuppressed;

        private void Awake()
        {
            if (_busBehaviour != null) _bus = _busBehaviour.Bus;
            if (_filter == null)
            {
                var now = new SystemNowProvider();
                var store = new PlayerPrefsKeyValueStore();
                var prefs = new PlayerPrefsDebouncedFlusher(store, now);
                _filter = new RepeatPolicyFilter(now, prefs);
            }
            _modalDeferredQueue ??= new BannerQueue();
        }

        private void OnEnable()
        {
            GameEvents.OnBlockingPanelOpenChanged += HandleBlockingPanelOpenChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnBlockingPanelOpenChanged -= HandleBlockingPanelOpenChanged;
        }

        /// <summary>
        /// Test hook. Replaces the runtime bus, filter, and queue so EditMode
        /// tests don't need a scene-wired GameEventBusBehaviour.
        /// </summary>
        public void Initialize(IGameEventBus bus, RepeatPolicyFilter filter, BannerQueue modalDeferredQueue = null)
        {
            _bus = bus;
            _filter = filter;
            _modalDeferredQueue = modalDeferredQueue ?? new BannerQueue();
        }

        public bool IsSuppressed => _tutorialSuppressed;
        public int ModalOpenCount => _modalOpenCount;
        public int ModalDeferredCount => _modalDeferredQueue?.Count ?? 0;

        /// <summary>
        /// Tutorial entry / exit. While true, Submit drops every request.
        /// </summary>
        public void SetSuppressed(bool value) => _tutorialSuppressed = value;

        /// <summary>
        /// Entry point for dispatchers. The tip carries the RepeatPolicy
        /// and tipId (tip.name) used for filter bookkeeping; the request
        /// carries the already-built banner content.
        /// </summary>
        public void Submit(GuidanceTipSO tip, GuidanceBannerRequest request)
        {
            if (tip == null || _bus == null || _filter == null) return;

            if (_tutorialSuppressed) return;
            if (!_filter.ShouldFire(tip.name, tip.RepeatPolicy, tip.CooldownSeconds)) return;

            _filter.MarkFired(tip.name, tip.RepeatPolicy);

            if (_modalOpenCount > 0)
            {
                _modalDeferredQueue.TryEnqueue(request);
                return;
            }

            _bus.Raise(request);
        }

        /// <summary>
        /// Test hook for modal-open state. Production callers go through
        /// GameEvents.OnBlockingPanelOpenChanged.
        /// </summary>
        public void HandleBlockingPanelOpenChanged(bool open)
        {
            if (open)
            {
                _modalOpenCount++;
                return;
            }

            _modalOpenCount = Mathf.Max(0, _modalOpenCount - 1);
            if (_modalOpenCount == 0) DrainModalDeferred();
        }

        private void DrainModalDeferred()
        {
            while (_modalDeferredQueue.TryDequeue(out var request))
            {
                _bus.Raise(request);
            }
        }
    }
}
