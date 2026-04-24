using FortuneValley.Domain.Interfaces;

namespace FortuneValley.Tests
{
    internal sealed class FakeTickClock : ITickClock
    {
        public int TicksPerDay { get; set; }
    }
}
