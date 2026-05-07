using FortuneValley.Domain.Interfaces;

namespace FortuneValley.Tests
{
    internal sealed class FakeTickClock : ITickClock
    {
        public int TicksPerDay { get; set; }

        // Stage 0a alias: same value, new name. Explicit property (not default
        // interface impl) so callers using the concrete FakeTickClock type can
        // resolve the member without an interface cast.
        public int EnginePulsesPerTick => TicksPerDay;
    }
}
