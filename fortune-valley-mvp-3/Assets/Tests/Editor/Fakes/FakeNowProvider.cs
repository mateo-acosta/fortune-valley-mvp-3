using System;
using FortuneValley.Core;

namespace FortuneValley.Tests.Fakes
{
    /// <summary>
    /// Virtual clock for cooldown and debounce tests. Time only advances when
    /// <see cref="Advance"/> is called, so test outcomes are independent of
    /// wall-clock timing.
    /// </summary>
    public class FakeNowProvider : INowProvider
    {
        public DateTime UtcNow { get; private set; }

        public FakeNowProvider() : this(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)) { }

        public FakeNowProvider(DateTime startUtc)
        {
            UtcNow = startUtc;
        }

        public void Advance(TimeSpan delta) => UtcNow = UtcNow.Add(delta);
        public void AdvanceSeconds(double seconds) => Advance(TimeSpan.FromSeconds(seconds));
    }
}
