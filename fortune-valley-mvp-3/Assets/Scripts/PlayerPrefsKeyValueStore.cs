using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Production <see cref="IKeyValueStore"/> backed by UnityEngine.PlayerPrefs.
    /// Save() performs a synchronous IndexedDB write on WebGL; callers should
    /// debounce via <see cref="Managers.Notifications.PlayerPrefsDebouncedFlusher"/>
    /// when writing many keys in succession.
    /// </summary>
    public class PlayerPrefsKeyValueStore : IKeyValueStore
    {
        public int GetInt(string key, int defaultValue) => PlayerPrefs.GetInt(key, defaultValue);
        public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
        public bool HasKey(string key) => PlayerPrefs.HasKey(key);
        public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
        public void Save() => PlayerPrefs.Save();
    }
}
