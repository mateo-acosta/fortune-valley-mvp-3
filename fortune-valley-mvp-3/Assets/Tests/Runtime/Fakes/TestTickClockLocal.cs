using FortuneValley.Domain.Interfaces;

namespace FortuneValley.Tests
{
    internal sealed class TestTickClockLocal : ITickClock
    {
        public int TicksPerDay { get; set; }
    }
}
