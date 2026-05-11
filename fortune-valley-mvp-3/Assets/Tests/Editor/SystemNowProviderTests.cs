using System;
using System.Threading;
using NUnit.Framework;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class SystemNowProviderTests
    {
        [Test]
        public void UtcNow_IsKindUtc()
        {
            var provider = new SystemNowProvider();
            Assert.AreEqual(DateTimeKind.Utc, provider.UtcNow.Kind);
        }

        [Test]
        public void UtcNow_AdvancesBetweenCalls()
        {
            var provider = new SystemNowProvider();
            var first = provider.UtcNow;
            Thread.Sleep(15);
            var second = provider.UtcNow;
            Assert.GreaterOrEqual(second, first);
        }

        [Test]
        public void UtcNow_IsCloseToDateTimeUtcNow()
        {
            var provider = new SystemNowProvider();
            var diff = (DateTime.UtcNow - provider.UtcNow).Duration();
            Assert.Less(diff, TimeSpan.FromSeconds(1));
        }
    }
}
