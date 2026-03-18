using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using FortuneValley.UI.Panels;

namespace FortuneValley.Tests
{
    /// <summary>
    /// EditMode tests for CountdownOverlay.
    /// Uses reflection to access private state and methods without needing
    /// a full UI hierarchy — null-guarded UI refs in CountdownOverlay
    /// allow the core logic to be exercised in isolation.
    /// </summary>
    [TestFixture]
    public class CountdownOverlayTests
    {
        // Created GameObjects tracked for cleanup
        private System.Collections.Generic.List<GameObject> _created =
            new System.Collections.Generic.List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created)
                if (go != null) Object.DestroyImmediate(go);
            _created.Clear();
        }

        // ─── helpers ─────────────────────────────────────────────────

        /// <summary>Creates a CountdownOverlay with a CanvasGroup wired up (minimum setup).</summary>
        private CountdownOverlay CreateOverlay()
        {
            var go = new GameObject("CountdownOverlay");
            _created.Add(go);

            // Add CanvasGroup first so Awake can reference it via SetField
            var cg = go.AddComponent<CanvasGroup>();

            // Awake() is invoked here by the Unity test runner
            var overlay = go.AddComponent<CountdownOverlay>();

            // Wire the CanvasGroup so blocksRaycasts tests work
            SetField(overlay, "_canvasGroup", cg);

            return overlay;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            typeof(CountdownOverlay)
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(target, value);
        }

        private static object GetField(object target, string fieldName)
        {
            return typeof(CountdownOverlay)
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(target);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            typeof(CountdownOverlay)
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(target, null);
        }

        // ─── tests ───────────────────────────────────────────────────

        /// <summary>
        /// A fresh CountdownOverlay instance must start in Idle state so StartCountdown()
        /// does not incorrectly attempt to cancel a phantom in-progress countdown.
        /// </summary>
        [Test]
        public void DefaultState_IsIdle()
        {
            var go = new GameObject("CountdownOverlay");
            _created.Add(go);
            var overlay = go.AddComponent<CountdownOverlay>();

            // A fresh instance must start in Idle state so StartCountdown()
            // does not incorrectly attempt to cancel a phantom in-progress countdown.
            int stateVal = (int)GetField(overlay, "_state");
            Assert.AreEqual(0, stateVal, "CountdownState should be Idle (0) on fresh instantiation.");
        }

        /// <summary>
        /// The onComplete callback should fire exactly once when FinishCountdown is called,
        /// and not fire again on a second FinishCountdown call.
        /// </summary>
        [Test]
        public void Callback_FiresExactlyOnce_OnFinish()
        {
            var overlay = CreateOverlay();

            int callCount = 0;
            overlay.StartCountdown(() => callCount++);

            // Simulate the natural end of the countdown
            InvokePrivate(overlay, "FinishCountdown");
            Assert.AreEqual(1, callCount, "Callback should fire once on finish.");

            // A second FinishCountdown (e.g. stale call) should be a no-op
            InvokePrivate(overlay, "FinishCountdown");
            Assert.AreEqual(1, callCount, "Callback should not fire again after state is Done.");
        }

        /// <summary>
        /// The CanvasGroup should block raycasts while the countdown is running,
        /// and stop blocking as soon as FinishCountdown is called.
        /// </summary>
        [Test]
        public void BlocksRaycasts_TrueDuringCountdown_FalseAfter()
        {
            var overlay = CreateOverlay();
            var cg = overlay.GetComponent<CanvasGroup>();

            overlay.StartCountdown(null);
            Assert.IsTrue(cg.blocksRaycasts,
                "Input should be blocked while the countdown is active.");

            InvokePrivate(overlay, "FinishCountdown");
            Assert.IsFalse(cg.blocksRaycasts,
                "Input should be unblocked after the countdown finishes.");
        }

        /// <summary>
        /// If StartCountdown is called while a countdown is already in progress,
        /// the OLD callback must NOT be invoked and the NEW callback must fire
        /// when the (new) countdown finishes.
        /// </summary>
        [Test]
        public void CancelRestart_OldCallbackNotInvoked_NewCallbackIs()
        {
            var overlay = CreateOverlay();

            int oldCount = 0;
            int newCount = 0;

            // Start the first countdown
            overlay.StartCountdown(() => oldCount++);

            // Interrupt with a second countdown before the first finishes
            overlay.StartCountdown(() => newCount++);

            // Complete the (second) countdown
            InvokePrivate(overlay, "FinishCountdown");

            Assert.AreEqual(0, oldCount,
                "The cancelled callback should never be invoked.");
            Assert.AreEqual(1, newCount,
                "The new callback should fire exactly once.");
        }
    }
}
