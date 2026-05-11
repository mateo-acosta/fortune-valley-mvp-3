using System.Collections.Generic;
using FortuneValley.Domain.Notifications;

namespace FortuneValley.Managers.Notifications
{
    /// <summary>
    /// Bounded FIFO queue of banner requests with severity-aware eviction.
    /// When at capacity, an incoming request whose severity is strictly greater
    /// than the lowest currently queued severity evicts the oldest banner of that
    /// lowest severity. An incoming request whose severity is less than or equal
    /// to the queue's lowest severity is dropped.
    /// </summary>
    public class BannerQueue
    {
        public const int DefaultCapacity = 8;

        private readonly List<GuidanceBannerRequest> _items;
        private readonly int _capacity;

        public BannerQueue() : this(DefaultCapacity) { }

        public BannerQueue(int capacity)
        {
            _capacity = capacity;
            _items = new List<GuidanceBannerRequest>(capacity);
        }

        public int Count => _items.Count;
        public int Capacity => _capacity;
        public bool IsFull => _items.Count >= _capacity;
        public bool IsEmpty => _items.Count == 0;

        /// <summary>
        /// Attempts to enqueue. Returns true if accepted (with optional eviction)
        /// and false if dropped because the queue is full of equal-or-higher-severity items.
        /// </summary>
        public bool TryEnqueue(GuidanceBannerRequest request)
        {
            if (!IsFull)
            {
                _items.Add(request);
                return true;
            }

            int evictionIndex = FindLowestSeverityOldestIndex();
            if (request.Severity > _items[evictionIndex].Severity)
            {
                _items.RemoveAt(evictionIndex);
                _items.Add(request);
                return true;
            }

            return false;
        }

        public bool TryDequeue(out GuidanceBannerRequest request)
        {
            if (IsEmpty)
            {
                request = default;
                return false;
            }

            request = _items[0];
            _items.RemoveAt(0);
            return true;
        }

        public bool TryPeek(out GuidanceBannerRequest request)
        {
            if (IsEmpty)
            {
                request = default;
                return false;
            }

            request = _items[0];
            return true;
        }

        public void Clear() => _items.Clear();

        /// <summary>
        /// Read-only snapshot for assertions and inspection. Order is FIFO (index 0
        /// is the oldest).
        /// </summary>
        public IReadOnlyList<GuidanceBannerRequest> Snapshot() => _items;

        private int FindLowestSeverityOldestIndex()
        {
            int candidateIndex = 0;
            GuidanceSeverity candidateSeverity = _items[0].Severity;
            for (int i = 1; i < _items.Count; i++)
            {
                if (_items[i].Severity < candidateSeverity)
                {
                    candidateIndex = i;
                    candidateSeverity = _items[i].Severity;
                }
            }
            return candidateIndex;
        }
    }
}
