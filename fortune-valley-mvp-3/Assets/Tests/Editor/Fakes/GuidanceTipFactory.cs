using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Notifications;

namespace FortuneValley.Tests.Fakes
{
    /// <summary>
    /// Builds a runtime <see cref="GuidanceTipSO"/> for tests without
    /// going through the Unity ScriptableObject asset pipeline. Sets
    /// every authored field via reflection so tests stay independent
    /// of Inspector-only serialization.
    /// </summary>
    public static class GuidanceTipFactory
    {
        public static GuidanceTipSO Make(
            GuidanceTriggerKind triggerKind = GuidanceTriggerKind.LoanTaken,
            GuidanceSeverity severity = GuidanceSeverity.Info,
            GuidanceTargetIntent targetIntent = GuidanceTargetIntent.None,
            string title = "",
            string message = "",
            RepeatPolicy repeatPolicy = RepeatPolicy.EveryTime,
            double cooldownSeconds = 0,
            string name = "test-tip")
        {
            var tip = ScriptableObject.CreateInstance<GuidanceTipSO>();
            tip.name = name;
            SetPrivateField(tip, "_triggerKind", triggerKind);
            SetPrivateField(tip, "_severity", severity);
            SetPrivateField(tip, "_targetIntent", targetIntent);
            SetPrivateField(tip, "_titleTemplate", title);
            SetPrivateField(tip, "_messageTemplate", message);
            SetPrivateField(tip, "_repeatPolicy", repeatPolicy);
            SetPrivateField(tip, "_cooldownSeconds", cooldownSeconds);
            return tip;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(target, value);
        }
    }
}
