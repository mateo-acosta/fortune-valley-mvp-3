using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Managers.Tutorial;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class TutorialSequenceMachineTests
    {
        private IntroScriptSO _script;
        private TutorialStepSO _s0;
        private TutorialStepSO _s1;
        private TutorialStepSO _s2;

        [SetUp]
        public void SetUp()
        {
            _s0 = ScriptableObject.CreateInstance<TutorialStepSO>();
            _s1 = ScriptableObject.CreateInstance<TutorialStepSO>();
            _s2 = ScriptableObject.CreateInstance<TutorialStepSO>();
            _script = ScriptableObject.CreateInstance<IntroScriptSO>();

            var stepsField = typeof(IntroScriptSO).GetField("_steps",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            stepsField.SetValue(_script, new[] { _s0, _s1, _s2 });
        }

        [TearDown]
        public void TearDown()
        {
            if (_s0 != null) Object.DestroyImmediate(_s0);
            if (_s1 != null) Object.DestroyImmediate(_s1);
            if (_s2 != null) Object.DestroyImmediate(_s2);
            if (_script != null) Object.DestroyImmediate(_script);
        }

        [Test]
        public void NewMachine_IsNotStartedOrComplete()
        {
            var m = new TutorialSequenceMachine(_script);
            Assert.IsFalse(m.IsStarted);
            Assert.IsFalse(m.IsComplete);
            Assert.AreEqual(-1, m.CurrentIndex);
            Assert.IsNull(m.CurrentStep);
            Assert.AreEqual(3, m.StepCount);
        }

        [Test]
        public void Start_PlacesAtFirstStep()
        {
            var m = new TutorialSequenceMachine(_script);
            m.Start();
            Assert.IsTrue(m.IsStarted);
            Assert.AreEqual(0, m.CurrentIndex);
            Assert.AreSame(_s0, m.CurrentStep);
            Assert.IsFalse(m.IsComplete);
        }

        [Test]
        public void Advance_MovesForward()
        {
            var m = new TutorialSequenceMachine(_script);
            m.Start();
            m.Advance();
            Assert.AreSame(_s1, m.CurrentStep);
            m.Advance();
            Assert.AreSame(_s2, m.CurrentStep);
        }

        [Test]
        public void Advance_PastLastStep_CompletesAndClampsAtStepCount()
        {
            var m = new TutorialSequenceMachine(_script);
            m.Start();
            m.Advance(); m.Advance(); m.Advance();
            Assert.IsTrue(m.IsComplete);
            Assert.AreEqual(3, m.CurrentIndex);
            Assert.IsNull(m.CurrentStep);

            // Further Advance calls should not inflate the index.
            for (int i = 0; i < 5; i++) m.Advance();
            Assert.AreEqual(3, m.CurrentIndex);
            Assert.IsTrue(m.IsComplete);
        }

        [Test]
        public void Advance_WithoutStart_IsNoOp()
        {
            var m = new TutorialSequenceMachine(_script);
            m.Advance();
            Assert.IsFalse(m.IsStarted);
            Assert.AreEqual(-1, m.CurrentIndex);
        }

        [Test]
        public void JumpToEnd_CompletesImmediately()
        {
            var m = new TutorialSequenceMachine(_script);
            m.Start();
            m.JumpToEnd();
            Assert.IsTrue(m.IsComplete);
            Assert.AreEqual(3, m.CurrentIndex);
            Assert.IsNull(m.CurrentStep);
        }

        [Test]
        public void Reset_ReturnsToPreStart()
        {
            var m = new TutorialSequenceMachine(_script);
            m.Start(); m.Advance();
            m.Reset();
            Assert.IsFalse(m.IsStarted);
            Assert.AreEqual(-1, m.CurrentIndex);
            Assert.IsFalse(m.IsComplete);
        }

        [Test]
        public void NullScript_IsSafe()
        {
            var m = new TutorialSequenceMachine(null);
            Assert.AreEqual(0, m.StepCount);
            Assert.IsFalse(m.IsComplete);
            Assert.IsNull(m.CurrentStep);
            Assert.DoesNotThrow(() => m.Start());
            Assert.DoesNotThrow(() => m.Advance());
            Assert.DoesNotThrow(() => m.JumpToEnd());
        }

        [Test]
        public void EmptyScript_IsCompleteAfterStart()
        {
            var empty = ScriptableObject.CreateInstance<IntroScriptSO>();
            try
            {
                var m = new TutorialSequenceMachine(empty);
                m.Start();
                Assert.IsTrue(m.IsComplete, "Starting an empty script should be immediately complete");
            }
            finally
            {
                Object.DestroyImmediate(empty);
            }
        }

        [Test]
        public void Start_AfterCompletion_ResumesFromZero()
        {
            var m = new TutorialSequenceMachine(_script);
            m.Start(); m.Advance(); m.Advance(); m.Advance();
            Assert.IsTrue(m.IsComplete);

            m.Start();
            Assert.AreEqual(0, m.CurrentIndex);
            Assert.IsFalse(m.IsComplete);
            Assert.AreSame(_s0, m.CurrentStep);
        }
    }
}
