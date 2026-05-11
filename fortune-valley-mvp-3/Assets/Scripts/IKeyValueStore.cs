namespace FortuneValley.Core
{
    /// <summary>
    /// Persistent string-keyed integer store. Production binding wraps
    /// UnityEngine.PlayerPrefs; tests inject an in-memory fake to avoid
    /// touching the player's real prefs.
    /// </summary>
    public interface IKeyValueStore
    {
        int GetInt(string key, int defaultValue);
        void SetInt(string key, int value);
        bool HasKey(string key);
        void DeleteKey(string key);
        void Save();
    }
}
