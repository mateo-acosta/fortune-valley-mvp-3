using NUnit.Framework;
using FortuneValley.Domain.Entities;
using FortuneValley.Managers.Tutorial;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class IntroGateTests
    {
        [Test]
        public void NullState_Student_ShouldRunIntro()
        {
            Assert.IsTrue(IntroGate.ShouldRunIntro(state: null, role: "student"));
        }

        [Test]
        public void NullState_TeacherPreview_DoesNotRun()
        {
            Assert.IsFalse(IntroGate.ShouldRunIntro(state: null, role: IntroGate.TeacherPreviewRole));
        }

        [Test]
        public void IncompleteState_Student_ShouldRunIntro()
        {
            var state = new GamePlayerStateDTO { tutorial_completed = false };
            Assert.IsTrue(IntroGate.ShouldRunIntro(state, "student"));
        }

        [Test]
        public void CompleteState_Student_DoesNotRun()
        {
            var state = new GamePlayerStateDTO { tutorial_completed = true };
            Assert.IsFalse(IntroGate.ShouldRunIntro(state, "student"));
        }

        [Test]
        public void CompleteState_TeacherPreview_DoesNotRun()
        {
            var state = new GamePlayerStateDTO { tutorial_completed = true };
            Assert.IsFalse(IntroGate.ShouldRunIntro(state, IntroGate.TeacherPreviewRole));
        }

        [Test]
        public void IncompleteState_TeacherPreview_DoesNotRun()
        {
            // Preview role trumps tutorial_completed either way.
            var state = new GamePlayerStateDTO { tutorial_completed = false };
            Assert.IsFalse(IntroGate.ShouldRunIntro(state, IntroGate.TeacherPreviewRole));
        }

        [Test]
        public void NullRole_WithIncompleteState_RunsIntro()
        {
            var state = new GamePlayerStateDTO { tutorial_completed = false };
            Assert.IsTrue(IntroGate.ShouldRunIntro(state, null));
        }

        [Test]
        public void UnknownRole_TreatedAsStudent()
        {
            var state = new GamePlayerStateDTO { tutorial_completed = false };
            Assert.IsTrue(IntroGate.ShouldRunIntro(state, "teacher"));
        }
    }
}
