using UnityEngine;
using FortuneValley.Domain.Notifications;

namespace FortuneValley.Core
{
    /// <summary>
    /// Authored content for a single guidance banner. Designers edit these
    /// assets (not code) to tune copy, severity, repeat behavior, and
    /// click-through targets. Step 7 (GuidanceTipRegistry) aggregates a
    /// library of these into a lookup by trigger kind.
    ///
    /// Severity drives color, icon, and display duration via the
    /// BannerSeverityPalette; per-tip icon overrides are optional.
    /// </summary>
    [CreateAssetMenu(fileName = "GuidanceTip", menuName = "FortuneValley/Notifications/Guidance Tip")]
    public class GuidanceTipSO : ScriptableObject
    {
        [Header("Routing")]
        [SerializeField] private GuidanceTriggerKind _triggerKind;
        [SerializeField] private GuidanceSeverity _severity = GuidanceSeverity.Info;
        [SerializeField] private GuidanceTargetIntent _targetIntent = GuidanceTargetIntent.None;

        [Header("Copy")]
        [Tooltip("Template string consumed by the matching IBannerMessageBuilder. " +
                 "Argument order is defined by the builder implementation.")]
        [SerializeField] private string _titleTemplate;
        [Tooltip("Template string consumed by the matching IBannerMessageBuilder.")]
        [TextArea(2, 4)]
        [SerializeField] private string _messageTemplate;

        [Header("Repeat")]
        [SerializeField] private RepeatPolicy _repeatPolicy = RepeatPolicy.EveryTime;
        [Tooltip("Seconds of cooldown between firings. Only used when repeatPolicy is OncePerCooldown.")]
        [SerializeField] private double _cooldownSeconds = 0;

        [Header("Style Override (optional)")]
        [Tooltip("Leave null to use the default icon from BannerSeverityPalette.")]
        [SerializeField] private Sprite _iconOverride;

        public GuidanceTriggerKind TriggerKind => _triggerKind;
        public GuidanceSeverity Severity => _severity;
        public GuidanceTargetIntent TargetIntent => _targetIntent;
        public string TitleTemplate => _titleTemplate;
        public string MessageTemplate => _messageTemplate;
        public RepeatPolicy RepeatPolicy => _repeatPolicy;
        public double CooldownSeconds => _cooldownSeconds;
        public Sprite IconOverride => _iconOverride;
    }
}
