using System;
using UnityEngine;
using FortuneValley.Domain.Notifications;

namespace FortuneValley.UI.Notifications
{
    /// <summary>
    /// Single source of truth for severity styling. Per-tip overrides are limited
    /// to the icon (an optional sprite on the tip SO); color and display duration
    /// always come from this palette so all banners of the same severity feel
    /// consistent in classroom play.
    /// </summary>
    [CreateAssetMenu(fileName = "BannerSeverityPalette", menuName = "FortuneValley/UI/Banner Severity Palette")]
    public class BannerSeverityPalette : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public GuidanceSeverity severity;
            public Color color;
            public Sprite defaultIcon;
            public float durationSeconds;
        }

        [SerializeField] private Entry[] _entries;

        public bool TryGet(GuidanceSeverity severity, out Entry entry)
        {
            if (_entries != null)
            {
                for (int i = 0; i < _entries.Length; i++)
                {
                    if (_entries[i].severity == severity)
                    {
                        entry = _entries[i];
                        return true;
                    }
                }
            }
            entry = default;
            return false;
        }

        public Entry Get(GuidanceSeverity severity)
        {
            if (TryGet(severity, out var entry)) return entry;
            throw new InvalidOperationException(
                $"BannerSeverityPalette has no entry for severity '{severity}'. " +
                "Add the missing severity in the Inspector.");
        }
    }
}
