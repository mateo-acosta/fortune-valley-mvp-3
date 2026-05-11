namespace FortuneValley.Domain
{
    /// <summary>
    /// Constants for the player's in-game life span. Domain-layer so
    /// builders, services, and renderers all read from one source of truth.
    ///
    /// Tuning target: full life ~80-100 minutes real time. With
    /// DaysPerYear=30 and TimeManager day length 4-5 sec, the 40-year life
    /// fits in 80-100 min.
    /// </summary>
    public static class LifespanConstants
    {
        public const int StartingAge = 25;
        public const int RetirementAge = 65;

        // Legacy "day" naming (Stage 0a alias chain). New code should use
        // TicksPerYear / TotalLifeTicks / AgeFromTick / HasReachedRetirementTick.
        // These will be removed in Stage 0c.
        public const int DaysPerYear = 30;
        public const int TotalLifeYears = RetirementAge - StartingAge;
        public const int TotalLifeDays = TotalLifeYears * DaysPerYear;

        // New canonical naming. A "tick" is the gameplay heartbeat (1 tick per
        // 10 atomic engine pulses, 30 ticks per year). Mateo's decision: the
        // engine internals use this language now; the panel UI never exposes
        // the unit "tick" since all displays are yearly.
        public const int TicksPerYear = DaysPerYear;
        public const int TotalLifeTicks = TotalLifeDays;

        /// <summary>Convert an in-game day to the player's age in years.</summary>
        public static int AgeFromDay(int currentDay)
        {
            if (currentDay < 0) return StartingAge;
            return StartingAge + (currentDay / DaysPerYear);
        }

        /// <summary>Convert an in-game tick count to the player's age in years.</summary>
        public static int AgeFromTick(int currentTick) => AgeFromDay(currentTick);

        public static bool HasReachedRetirement(int currentDay)
        {
            return AgeFromDay(currentDay) >= RetirementAge;
        }

        public static bool HasReachedRetirementTick(int currentTick) => HasReachedRetirement(currentTick);
    }
}
