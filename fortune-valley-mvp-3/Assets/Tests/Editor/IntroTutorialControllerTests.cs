using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Tutorial;
using FortuneValley.Managers.Notifications;
using FortuneValley.Managers.Tutorial;
using FortuneValley.Tests.Fakes;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Orchestration tests for IntroTutorialController. Covers the full
    /// state machine: start flips pause + suppression + overlay/input events,
    /// step kinds subscribe to the right game events, advances work, skip
    /// only after step 0, end clears every side effect.
    /// </summary>
    [TestFixture]
    public class IntroTutorialControllerTests
    {
        private GameObject _go;
        private GameObject _timeGo;
        private GameObject _guidanceGo;
        private IntroTutorialController _controller;
        private TimeManager _time;
        private GuidanceController _guidance;
        private TutorialTargetRegistry _registry;
        private IntroScriptSO _script;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            if (_timeGo != null) Object.DestroyImmediate(_timeGo);
            if (_guidanceGo != null) Object.DestroyImmediate(_guidanceGo);
            if (_script != null) Object.DestroyImmediate(_script);
            GameEvents.ClearAllSubscriptions();
        }

        private void Build(TutorialStepSO[] steps)
        {
            _timeGo = new GameObject("TimeManager");
            _time = _timeGo.AddComponent<TimeManager>();

            _guidanceGo = new GameObject("GuidanceController");
            _guidance = _guidanceGo.AddComponent<GuidanceController>();
            var bus = new FakeGameEventBus();
            var now = new FakeNowProvider();
            var prefs = new PlayerPrefsDebouncedFlusher(new InMemoryKeyValueStore(), now);
            _guidance.Initialize(bus, new RepeatPolicyFilter(now, prefs));

            _go = new GameObject("IntroTutorialController");
            _controller = _go.AddComponent<IntroTutorialController>();

            _registry = _go.AddComponent<TutorialTargetRegistry>();
            _registry.Initialize(staticEntries: null, resolvers: null);

            _script = ScriptableObject.CreateInstance<IntroScriptSO>();
            var stepsField = typeof(IntroScriptSO).GetField("_steps",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            stepsField.SetValue(_script, steps);

            _controller.Initialize(_time, _guidance, _registry, _script);

            // EditMode tests do not auto-run Unity lifecycle methods; invoke
            // OnEnable by reflection to establish the GameEvents subscriptions
            // (OnTutorialAdvanceRequested / OnTutorialSkipRequested in particular).
            var onEnable = typeof(IntroTutorialController).GetMethod("OnEnable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onEnable.Invoke(_controller, null);
        }

        private static TutorialStepSO MakeStep(TutorialStepKind kind, string dialog = "", TutorialTargetKind target = TutorialTargetKind.None)
        {
            var s = ScriptableObject.CreateInstance<TutorialStepSO>();
            SetField(s, "_kind", kind);
            SetField(s, "_dialogText", dialog);
            SetField(s, "_targetKind", target);
            return s;
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(target, value);
        }

        // ===============================================================
        // START
        // ===============================================================

        [Test]
        public void Start_AcquiresPauseAndSuppressesGuidanceAndShowsOverlay()
        {
            Build(new[] { MakeStep(TutorialStepKind.Dialog, "hi") });

            bool overlayShown = false;
            bool inputBlocked = false;
            GameEvents.OnTutorialOverlayVisibilityChanged += v => overlayShown = v;
            GameEvents.OnTutorialInputBlockChanged += b => inputBlocked = b;

            _controller.HandleTutorialStartRequested();

            Assert.IsTrue(_controller.IsActive);
            Assert.AreEqual(1, _time.PauseLockCount);
            Assert.IsTrue(_guidance.IsSuppressed);
            Assert.IsTrue(overlayShown);
            Assert.IsTrue(inputBlocked);
            Assert.AreEqual(0, _controller.CurrentStepIndex);
        }

        [Test]
        public void Start_WhenAlreadyActive_IsNoOp()
        {
            Build(new[] { MakeStep(TutorialStepKind.Dialog) });
            _controller.HandleTutorialStartRequested();
            _controller.HandleTutorialStartRequested();
            Assert.AreEqual(1, _time.PauseLockCount, "Second Start must not double-acquire");
        }

        [Test]
        public void Start_EmptyScript_EndsImmediately()
        {
            Build(new TutorialStepSO[0]);
            bool complete = false;
            GameEvents.OnTutorialComplete += () => complete = true;

            _controller.HandleTutorialStartRequested();

            Assert.IsFalse(_controller.IsActive);
            Assert.IsTrue(complete);
            Assert.AreEqual(0, _time.PauseLockCount);
        }

        // ===============================================================
        // DIALOG ADVANCE
        // ===============================================================

        [Test]
        public void DialogStep_BroadcastsTextAndPose()
        {
            var step = MakeStep(TutorialStepKind.Dialog, "Welcome!");
            SetField(step, "_pose", CharacterPose.Happy);
            Build(new[] { step });

            string sawText = null;
            CharacterPose sawPose = CharacterPose.Neutral;
            GameEvents.OnTutorialDialogChanged += (t, p) => { sawText = t; sawPose = p; };

            _controller.HandleTutorialStartRequested();

            Assert.AreEqual("Welcome!", sawText);
            Assert.AreEqual(CharacterPose.Happy, sawPose);
        }

        [Test]
        public void AdvanceTap_OnDialogStep_MovesToNextStep()
        {
            Build(new[]
            {
                MakeStep(TutorialStepKind.Dialog, "one"),
                MakeStep(TutorialStepKind.Dialog, "two")
            });

            _controller.HandleTutorialStartRequested();
            Assert.AreEqual(0, _controller.CurrentStepIndex);

            _controller.HandleAdvanceRequested();
            Assert.AreEqual(1, _controller.CurrentStepIndex);
        }

        [Test]
        public void AdvanceTap_WhenNotActive_IsNoOp()
        {
            Build(new[] { MakeStep(TutorialStepKind.Dialog) });
            Assert.DoesNotThrow(() => _controller.HandleAdvanceRequested());
            Assert.IsFalse(_controller.IsActive);
        }

        [Test]
        public void AdvanceTap_OnWaitForStep_IsIgnored()
        {
            Build(new[]
            {
                MakeStep(TutorialStepKind.WaitForLifeGoalsSelected),
                MakeStep(TutorialStepKind.Dialog, "done")
            });

            _controller.HandleTutorialStartRequested();

            _controller.HandleAdvanceRequested();
            Assert.AreEqual(0, _controller.CurrentStepIndex,
                "Dialog-tap must not advance a WaitForX step");
        }

        // ===============================================================
        // WAIT-FOR EVENT ADVANCE
        // ===============================================================

        [Test]
        public void WaitForLifeGoalsSelected_AdvancesOnOnLifeGoalsSelected()
        {
            Build(new[]
            {
                MakeStep(TutorialStepKind.WaitForLifeGoalsSelected),
                MakeStep(TutorialStepKind.Dialog, "done")
            });

            _controller.HandleTutorialStartRequested();
            GameEvents.RaiseLifeGoalsSelected(MakeSelection());
            Assert.AreEqual(1, _controller.CurrentStepIndex);
        }

        [Test]
        public void StepSwitch_UnsubscribesPreviousWaitEvent()
        {
            Build(new[]
            {
                MakeStep(TutorialStepKind.WaitForLifeGoalsSelected),
                MakeStep(TutorialStepKind.Dialog, "done")
            });

            _controller.HandleTutorialStartRequested();
            GameEvents.RaiseLifeGoalsSelected(MakeSelection());   // advances to step 1 (Dialog)

            // Firing another OnLifeGoalsSelected must NOT re-advance.
            GameEvents.RaiseLifeGoalsSelected(MakeSelection());
            Assert.AreEqual(1, _controller.CurrentStepIndex,
                "After moving off a WaitFor step the subscription must be torn down");
        }

        private static LifeGoalSelection MakeSelection()
        {
            var entries = new[]
            {
                new LifeGoalEntry("starter", LifeGoalTier.Starter, 100_000f),
                new LifeGoalEntry("mid", LifeGoalTier.Mid, 500_000f),
                new LifeGoalEntry("ambitious", LifeGoalTier.Ambitious, 2_000_000f),
            };
            return new LifeGoalSelection(entries);
        }

        // ===============================================================
        // SKIP
        // ===============================================================

        [Test]
        public void Skip_BeforeFirstAdvance_IsIgnored()
        {
            Build(new[]
            {
                MakeStep(TutorialStepKind.Dialog, "one"),
                MakeStep(TutorialStepKind.Dialog, "two")
            });

            _controller.HandleTutorialStartRequested();
            _controller.HandleSkipRequested();

            Assert.IsTrue(_controller.IsActive, "Skip must not fire before the first step advances");
            Assert.AreEqual(0, _controller.CurrentStepIndex);
        }

        [Test]
        public void Skip_AfterFirstAdvance_EndsTutorialImmediately()
        {
            Build(new[]
            {
                MakeStep(TutorialStepKind.Dialog, "one"),
                MakeStep(TutorialStepKind.Dialog, "two"),
                MakeStep(TutorialStepKind.Dialog, "three")
            });

            // Skip-reveal is replay-only; this test exercises that behavior so
            // start the tutorial as a replay run.
            _controller.HandleTutorialStartRequested(isReplay: true);
            _controller.HandleAdvanceRequested();   // moves to step 1; skip revealed
            bool complete = false;
            GameEvents.OnTutorialComplete += () => complete = true;

            _controller.HandleSkipRequested();

            Assert.IsFalse(_controller.IsActive);
            Assert.IsTrue(complete);
            Assert.AreEqual(0, _time.PauseLockCount);
            Assert.IsFalse(_guidance.IsSuppressed);
        }

        [Test]
        public void FirstAdvance_RaisesSkipRevealed()
        {
            Build(new[]
            {
                MakeStep(TutorialStepKind.Dialog, "one"),
                MakeStep(TutorialStepKind.Dialog, "two")
            });
            bool revealed = false;
            GameEvents.OnTutorialSkipRevealed += () => revealed = true;

            // Skip-reveal only fires on replay runs, not first-pass tutorials.
            _controller.HandleTutorialStartRequested(isReplay: true);
            Assert.IsFalse(revealed, "Skip must not be revealed on start");

            _controller.HandleAdvanceRequested();
            Assert.IsTrue(revealed);
        }

        [Test]
        public void FurtherAdvances_DoNotDouble_RevealSkip()
        {
            Build(new[]
            {
                MakeStep(TutorialStepKind.Dialog, "one"),
                MakeStep(TutorialStepKind.Dialog, "two"),
                MakeStep(TutorialStepKind.Dialog, "three")
            });
            int revealCount = 0;
            GameEvents.OnTutorialSkipRevealed += () => revealCount++;

            // Skip-reveal only fires on replay runs, not first-pass tutorials.
            _controller.HandleTutorialStartRequested(isReplay: true);
            _controller.HandleAdvanceRequested();   // reveal
            _controller.HandleAdvanceRequested();
            _controller.HandleAdvanceRequested();

            Assert.AreEqual(1, revealCount);
        }

        // ===============================================================
        // COMPLETION SIDE EFFECTS
        // ===============================================================

        [Test]
        public void Complete_ReleasesPauseAndUnsuppressesAndHidesOverlay()
        {
            Build(new[] { MakeStep(TutorialStepKind.Dialog, "only") });

            bool overlayShown = true;
            bool inputBlocked = true;
            GameEvents.OnTutorialOverlayVisibilityChanged += v => overlayShown = v;
            GameEvents.OnTutorialInputBlockChanged += b => inputBlocked = b;
            bool complete = false;
            GameEvents.OnTutorialComplete += () => complete = true;

            _controller.HandleTutorialStartRequested();
            _controller.HandleAdvanceRequested();

            Assert.IsFalse(_controller.IsActive);
            Assert.AreEqual(0, _time.PauseLockCount);
            Assert.IsFalse(_guidance.IsSuppressed);
            Assert.IsFalse(overlayShown);
            Assert.IsFalse(inputBlocked);
            Assert.IsTrue(complete);
        }

        [Test]
        public void ExitStep_ClearsHighlight()
        {
            Build(new[]
            {
                MakeStep(TutorialStepKind.WaitForLifeGoalsSelected),
                MakeStep(TutorialStepKind.Dialog, "done")
            });

            Transform lastHighlight = null;
            int highlightCalls = 0;
            GameEvents.OnTutorialHighlightTarget += t => { lastHighlight = t; highlightCalls++; };

            _controller.HandleTutorialStartRequested();
            GameEvents.RaiseLifeGoalsSelected(MakeSelection());   // advances to Dialog step

            Assert.IsNull(lastHighlight,
                "Moving off a WaitFor step should clear the highlight by broadcasting null");
        }
    }
}
