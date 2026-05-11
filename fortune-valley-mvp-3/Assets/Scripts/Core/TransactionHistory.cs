using System.Collections.Generic;

namespace FortuneValley.Core
{
    /// <summary>
    /// Fixed-capacity circular buffer of financial transaction records.
    /// Pure C# with zero Unity dependencies for easy unit testing.
    ///
    /// LEARNING DESIGN: Transaction history lets students review
    /// past financial decisions to understand cause and effect.
    /// </summary>
    public class TransactionHistory
    {
        private readonly TransactionRecord[] _buffer;
        private int _head;
        private int _count;

        public TransactionHistory(int capacity)
        {
            _buffer = new TransactionRecord[capacity];
            _head = 0;
            _count = 0;
        }

        public int Count => _count;
        public int Capacity => _buffer.Length;

        /// <summary>
        /// Record a new transaction. Evicts the oldest entry when at capacity.
        /// </summary>
        public void Record(TransactionType type, string description, float amount, int tick)
        {
            _buffer[_head] = new TransactionRecord(type, description, amount, tick);
            _head = (_head + 1) % _buffer.Length;

            if (_count < _buffer.Length)
                _count++;
        }

        /// <summary>
        /// Fills <paramref name="dest"/> with all records newest-first. Reuses
        /// the supplied list (clears and refills); zero per-call allocation
        /// when the list capacity is sufficient. Hot-path-safe.
        /// No-op if dest is null.
        /// </summary>
        public void CopyAllInto(List<TransactionRecord> dest)
        {
            if (dest == null) return;
            dest.Clear();
            if (dest.Capacity < _count) dest.Capacity = _count;
            for (int i = 0; i < _count; i++)
            {
                // Walk backwards from (_head - 1) wrapping around
                int index = ((_head - 1 - i) % _buffer.Length + _buffer.Length) % _buffer.Length;
                dest.Add(_buffer[index]);
            }
        }

        /// <summary>
        /// Returns all records newest-first.
        /// Allocates a new list per call; intended for UI refresh, not per-frame use.
        /// Re-implemented in terms of CopyAllInto so the ring-walk math has a
        /// single source of truth.
        /// </summary>
        public List<TransactionRecord> GetAll()
        {
            var result = new List<TransactionRecord>(_count);
            CopyAllInto(result);
            return result;
        }

        /// <summary>
        /// Returns records of a specific type, newest-first.
        /// </summary>
        public List<TransactionRecord> GetByType(TransactionType type)
        {
            var result = new List<TransactionRecord>();
            for (int i = 0; i < _count; i++)
            {
                int index = ((_head - 1 - i) % _buffer.Length + _buffer.Length) % _buffer.Length;
                if (_buffer[index].Type == type)
                    result.Add(_buffer[index]);
            }
            return result;
        }

        /// <summary>
        /// Returns records matching any of the given types, newest-first.
        /// Used by History sub-panels that display multiple related transaction types.
        /// </summary>
        public List<TransactionRecord> GetByTypes(params TransactionType[] types)
        {
            var result = new List<TransactionRecord>();
            for (int i = 0; i < _count; i++)
            {
                int index = ((_head - 1 - i) % _buffer.Length + _buffer.Length) % _buffer.Length;
                var record = _buffer[index];
                for (int t = 0; t < types.Length; t++)
                {
                    if (record.Type == types[t])
                    {
                        result.Add(record);
                        break;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Record a new transaction with an entity identifier for filtering.
        /// Evicts the oldest entry when at capacity.
        /// </summary>
        public void Record(TransactionType type, string description, float amount, int tick, string entityId)
        {
            _buffer[_head] = new TransactionRecord(type, description, amount, tick, entityId);
            _head = (_head + 1) % _buffer.Length;

            if (_count < _buffer.Length)
                _count++;
        }

        /// <summary>
        /// Removes all recorded transactions.
        /// </summary>
        public void Clear()
        {
            _head = 0;
            _count = 0;
        }
    }
}
