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
        public const int DaysPerYear = 30;
        public const int TotalLifeYears = RetirementAge - StartingAge;
        public const int TotalLifeDays = TotalLifeYears * DaysPerYear;

        /// <summary>Convert an in-game day to the player's age in years.</summary>
        public static int AgeFromDay(int currentDay)
        {
            if (currentDay < 0) return StartingAge;
            return StartingAge + (currentDay / DaysPerYear);
        }

        public static bool HasReachedRetirement(int currentDay)
        {
            return AgeFromDay(currentDay) >= RetirementAge;
        }
    }
}
