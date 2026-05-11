using System;
using NUnit.Framework;
using FortuneValley.Tests.Fakes;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class FakeNowProviderTests
    {
        [Test]
        public void DefaultStart_IsDeterministic()
        {
            var clock = new FakeNowProvider();
            Assert.AreEqual(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), clock.UtcNow);
        }

        [Test]
        public void Advance_MovesTimeForward()
        {
            var clock = new FakeNowProvider();
            var start = clock.UtcNow;

            clock.Advance(TimeSpan.FromMinutes(5));

            Assert.AreEqual(start.AddMinutes(5), clock.UtcNow);
        }

        [Test]
        public void AdvanceSeconds_MovesTimeForward()
        {
            var clock = new FakeNowProvider();
            var start = clock.UtcNow;

            clock.AdvanceSeconds(7.5);

            Assert.AreEqual(start.AddSeconds(7.5), clock.UtcNow);
        }

        [Test]
        public void CustomStart_IsHonored()
        {
            var custom = new DateTime(2030, 6, 15, 12, 0, 0, DateTimeKind.Utc);
            var clock = new FakeNowProvider(custom);
            Assert.AreEqual(custom, clock.UtcNow);
        }
    }
}
