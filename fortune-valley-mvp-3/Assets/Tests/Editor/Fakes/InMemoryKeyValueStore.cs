using System.Collections.Generic;
using FortuneValley.Core;

namespace FortuneValley.Tests.Fakes
{
    /// <summary>
    /// Test double for <see cref="IKeyValueStore"/>. Records Save() calls so
    /// debounce/flush tests can assert how many times persistence actually
    /// hit the underlying store.
    /// </summary>
    public class InMemoryKeyValueStore : IKeyValueStore
    {
        private readonly Dictionary<string, int> _store = new Dictionary<string, int>();
        public int SaveCallCount { get; private set; }

        public int GetInt(string key, int defaultValue) =>
            _store.TryGetValue(key, out var v) ? v : defaultValue;

        public void SetInt(string key, int value) => _store[key] = value;
        public bool HasKey(string key) => _store.ContainsKey(key);
        public void DeleteKey(string key) => _store.Remove(key);
        public void Save() => SaveCallCount++;

        public IReadOnlyDictionary<string, int> Snapshot() => _store;
    }
}
