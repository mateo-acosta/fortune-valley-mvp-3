using NUnit.Framework;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Tutorial;
using FortuneValley.Managers.Tutorial;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class BootFlowRouterTests
    {
        [Test]
        public void NullState_Student_ReturnsFirstTimeTutorial()
        {
            Assert.AreEqual(BootFlow.FirstTimeTutorial,
                BootFlowRouter.Decide(state: null, role: "student"));
        }

        [Test]
        public void IncompleteState_Student_ReturnsFirstTimeTutorial()
        {
            var state = new GamePlayerStateDTO { tutorial_completed = false };
            Assert.AreEqual(BootFlow.FirstTimeTutorial,
                BootFlowRouter.Decide(state, "student"));
        }

        [Test]
        public void CompleteState_Student_ReturnsNormalCarousel()
        {
            var state = new GamePlayerStateDTO { tutorial_completed = true };
            Assert.AreEqual(BootFlow.NormalCarousel,
                BootFlowRouter.Decide(state, "student"));
        }

        [Test]
        public void NullState_TeacherPreview_ReturnsSkipTutorial()
        {
            Assert.AreEqual(BootFlow.SkipTutorial,
                BootFlowRouter.Decide(state: null, role: IntroGate.TeacherPreviewRole));
        }

        [Test]
        public void IncompleteState_TeacherPreview_ReturnsSkipTutorial()
        {
            // Preview bypass is absolute: even an uncompleted tutorial does
            // not re-route teacher preview back through the tutorial.
            var state = new GamePlayerStateDTO { tutorial_completed = false };
            Assert.AreEqual(BootFlow.SkipTutorial,
                BootFlowRouter.Decide(state, IntroGate.TeacherPreviewRole));
        }

        [Test]
        public void CompleteState_TeacherPreview_ReturnsSkipTutorial()
        {
            var state = new GamePlayerStateDTO { tutorial_completed = true };
            Assert.AreEqual(BootFlow.SkipTutorial,
                BootFlowRouter.Decide(state, IntroGate.TeacherPreviewRole));
        }

        [Test]
        public void NullRole_WithCompleteState_ReturnsNormalCarousel()
        {
            var state = new GamePlayerStateDTO { tutorial_completed = true };
            Assert.AreEqual(BootFlow.NormalCarousel,
                BootFlowRouter.Decide(state, null));
        }

        [Test]
        public void UnknownNonPreviewRole_TreatedAsStudent()
        {
            var state = new GamePlayerStateDTO { tutorial_completed = false };
            Assert.AreEqual(BootFlow.FirstTimeTutorial,
                BootFlowRouter.Decide(state, "teacher"));
        }
    }
}
