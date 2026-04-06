namespace FortuneValley.Domain.Entities
{
    /// <summary>
    /// Lightweight info about an accident definition for rolling.
    /// Copies primitive values from AccidentDefinition ScriptableObject
    /// so Domain layer can use it without importing Core.
    /// </summary>
    public struct AccidentInfo
    {
        private string _accidentId;
        private string _displayName;
        private float _baseDamageCost;
        private int _windowIntervalDays;
        private float _rollProbability;

        public AccidentInfo(string accidentId, string displayName, float baseDamageCost,
                           int windowIntervalDays, float rollProbability)
        {
            _accidentId = accidentId;
            _displayName = displayName;
            _baseDamageCost = baseDamageCost;
            _windowIntervalDays = windowIntervalDays;
            _rollProbability = rollProbability;
        }

        public string AccidentId => _accidentId;
        public string DisplayName => _displayName;
        public float BaseDamageCost => _baseDamageCost;
        public int WindowIntervalDays => _windowIntervalDays;
        public float RollProbability => _rollProbability;
    }
}
