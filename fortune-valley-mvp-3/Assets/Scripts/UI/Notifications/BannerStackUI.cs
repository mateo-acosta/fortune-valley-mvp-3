using System.Collections.Generic;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Notifications;

namespace FortuneValley.UI.Notifications
{
    /// <summary>
    /// Visualizes the live banner stack. Two layout modes:
    ///
    /// 1. Manual slot mode: <c>_slotPositions</c> is non-empty. Each entry is a
    ///    pre-positioned anchored position; banners slide between slots and
    ///    surviving occupants compact forward when an earlier slot dismisses.
    ///    Use this when there is no LayoutGroup on the parent.
    ///
    /// 2. LayoutGroup mode: <c>_slotPositions</c> is empty. The parent
    ///    Transform owns positioning (typically a VerticalLayoutGroup);
    ///    banners are spawned without slot assignment up to
    ///    <c>_layoutGroupMaxConcurrent</c> visible at a time. <see cref="BannerView"/>
    ///    detects the parent LayoutGroup in Awake and runs fade-only animation.
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

        [Header("Manual slot mode")]
        [Tooltip("Pre-positioned anchored positions for visible banners (top to bottom). " +
                 "Leave empty to use LayoutGroup mode (parent owns positioning).")]
        [SerializeField] private Vector2[] _slotPositions = new Vector2[0];

        [Header("LayoutGroup mode")]
        [Tooltip("Max concurrent banners when the parent has a LayoutGroup. " +
                 "Ignored when _slotPositions is non-empty.")]
        [SerializeField] private int _layoutGroupMaxConcurrent = 3;

        [Header("Display")]
        [Tooltip("Seconds each banner stays visible before auto-dismissing. " +
                 "Applies to every severity. Set to 0 to use the per-severity " +
                 "durationSeconds authored on BannerSeverityPalette instead.")]
        [SerializeField, Min(0f)] private float _displayDurationOverride = 4f;

        private readonly List<BannerView> _pool = new List<BannerView>();
        private BannerView[] _slotOccupants;
        private int _layoutGroupVisibleCount;

        private bool UsesManualSlots => _slotPositions != null && _slotPositions.Length > 0;

        private void Awake()
        {
            _slotOccupants = UsesManualSlots ? new BannerView[_slotPositions.Length] : null;
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
                if (!UsesManualSlots) return _layoutGroupVisibleCount;
                int count = 0;
                for (int i = 0; i < _slotOccupants.Length; i++)
                {
                    if (_slotOccupants[i] != null) count++;
                }
                return count;
            }
        }

        public int Capacity => UsesManualSlots ? _slotPositions.Length : _layoutGroupMaxConcurrent;

        private void HandleRequest(GuidanceBannerRequest request)
        {
            if (!_palette.TryGet(request.Severity, out var entry))
            {
                Debug.LogError($"{nameof(BannerStackUI)}: missing severity entry for {request.Severity}");
                return;
            }

            if (_displayDurationOverride > 0f) entry.durationSeconds = _displayDurationOverride;

            BannerView view;
            if (UsesManualSlots)
            {
                int slot = FirstFreeSlot();
                if (slot < 0) return;
                view = TakeFromPool();
                _slotOccupants[slot] = view;
                view.SetSlotPosition(_slotPositions[slot]);
            }
            else
            {
                if (_layoutGroupVisibleCount >= _layoutGroupMaxConcurrent) return;
                view = TakeFromPool();
                _layoutGroupVisibleCount++;
            }

            view.Show(request, entry, iconOverride: null);
        }

        private void HandleViewDismissed(BannerView view, GuidanceBannerRequest _)
        {
            if (UsesManualSlots)
            {
                int slot = SlotOf(view);
                if (slot >= 0) _slotOccupants[slot] = null;
                CompactSlots();
            }
            else
            {
                _layoutGroupVisibleCount = Mathf.Max(0, _layoutGroupVisibleCount - 1);
            }
        }

        private void CompactSlots()
        {
            // Slide later occupants forward so visible ordering stays top-aligned.
            int writeIndex = 0;
            for (int readIndex = 0; readIndex < _slotOccupants.Length; readIndex++)
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
            for (int i = 0; i < _slotOccupants.Length; i++)
            {
                if (_slotOccupants[i] == null) return i;
            }
            return -1;
        }

        private int SlotOf(BannerView view)
        {
            for (int i = 0; i < _slotOccupants.Length; i++)
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
    }
}
