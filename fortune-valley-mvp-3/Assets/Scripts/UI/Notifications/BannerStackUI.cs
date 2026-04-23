using System.Collections.Generic;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Notifications;

namespace FortuneValley.UI.Notifications
{
    /// <summary>
    /// Visualizes the live banner stack with manual slot positioning (no
    /// VerticalLayoutGroup, which would thrash under DOTween animations on
    /// WebGL). Slot 0 is the topmost; surviving banners slide up when an
    /// earlier slot dismisses.
    ///
    /// This component is a pure visualizer. Cooldown, suppression, and queue
    /// eviction live in <see cref="Managers.Notifications.GuidanceController"/>;
    /// banners arrive here only when the controller has decided they should display.
    /// </summary>
    public class BannerStackUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameEventBusBehaviour _busBehaviour;
        [SerializeField] private BannerSeverityPalette _palette;
        [SerializeField] private BannerView _bannerPrefab;
        [SerializeField] private RectTransform _bannerParent;

        [Header("Slots")]
        [Tooltip("Pre-positioned anchored positions for visible banners (top to bottom). " +
                 "Length determines how many banners can be visible at once.")]
        [SerializeField] private Vector2[] _slotPositions = new Vector2[3];

        private readonly List<BannerView> _pool = new List<BannerView>();
        private readonly BannerView[] _slotOccupants = new BannerView[3];

        private void Awake()
        {
            // Defensive: keep _slotOccupants sized to slot count if designer changed it.
            if (_slotOccupants.Length != _slotPositions.Length)
            {
                Debug.LogWarning($"{nameof(BannerStackUI)}: slot occupant array length ({_slotOccupants.Length}) " +
                                 $"differs from slot positions length ({_slotPositions.Length}). Using positions length.");
            }
        }

        private void OnEnable()
        {
            if (_busBehaviour == null) return;
            _busBehaviour.Bus.Subscribe<GuidanceBannerRequest>(HandleRequest);
        }

        private void OnDisable()
        {
            if (_busBehaviour == null) return;
            _busBehaviour.Bus.Unsubscribe<GuidanceBannerRequest>(HandleRequest);
        }

        public int VisibleCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < SlotCount; i++)
                {
                    if (_slotOccupants[i] != null) count++;
                }
                return count;
            }
        }

        public int SlotCount => Mathf.Min(_slotOccupants.Length, _slotPositions.Length);

        private void HandleRequest(GuidanceBannerRequest request)
        {
            int slot = FirstFreeSlot();
            if (slot < 0)
            {
                // Step 5 keeps this simple: when all visible slots are full, the
                // request is dropped here. Step 7 (GuidanceController) takes over
                // queuing so this branch becomes unreachable in production.
                return;
            }

            var view = TakeFromPool();
            _slotOccupants[slot] = view;
            view.SetSlotPosition(_slotPositions[slot]);

            if (!_palette.TryGet(request.Severity, out var entry))
            {
                Debug.LogError($"{nameof(BannerStackUI)}: missing severity entry for {request.Severity}");
                return;
            }
            view.Show(request, entry, iconOverride: null);
        }

        private void HandleViewDismissed(BannerView view, GuidanceBannerRequest _)
        {
            int slot = SlotOf(view);
            if (slot >= 0) _slotOccupants[slot] = null;
            ReturnToPool(view);
            CompactSlots();
        }

        private void CompactSlots()
        {
            // Slide later occupants forward so visible ordering stays top-aligned.
            int writeIndex = 0;
            for (int readIndex = 0; readIndex < SlotCount; readIndex++)
            {
                var occupant = _slotOccupants[readIndex];
                if (occupant == null) continue;
                if (readIndex != writeIndex)
                {
                    _slotOccupants[writeIndex] = occupant;
                    _slotOccupants[readIndex] = null;
                    occupant.SetSlotPosition(_slotPositions[writeIndex]);
                }
                writeIndex++;
            }
        }

        private int FirstFreeSlot()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slotOccupants[i] == null) return i;
            }
            return -1;
        }

        private int SlotOf(BannerView view)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slotOccupants[i] == view) return i;
            }
            return -1;
        }

        private BannerView TakeFromPool()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (!_pool[i].gameObject.activeSelf)
                {
                    return _pool[i];
                }
            }
            var view = Instantiate(_bannerPrefab, _bannerParent);
            view.OnDismissed += HandleViewDismissed;
            _pool.Add(view);
            return view;
        }

        private void ReturnToPool(BannerView view)
        {
            // Pool entries stay in _pool; deactivation is handled by BannerView itself.
        }
    }
}
