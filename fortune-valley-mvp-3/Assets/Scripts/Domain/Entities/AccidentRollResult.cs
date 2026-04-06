namespace FortuneValley.Domain.Entities
{
    /// <summary>
    /// Result of a single accident roll: which lot was hit and the damage.
    /// </summary>
    public struct AccidentRollResult
    {
        private string _lotId;
        private string _accidentId;
        private string _accidentName;
        private float _damageCost;

        public AccidentRollResult(string lotId, string accidentId, string accidentName, float damageCost)
        {
            _lotId = lotId;
            _accidentId = accidentId;
            _accidentName = accidentName;
            _damageCost = damageCost;
        }

        public string LotId => _lotId;
        public string AccidentId => _accidentId;
        public string AccidentName => _accidentName;
        public float DamageCost => _damageCost;
    }
}
