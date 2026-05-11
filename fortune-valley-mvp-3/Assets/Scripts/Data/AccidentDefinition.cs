using UnityEngine;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Core
{
    /// <summary>
    /// Defines an accident type that can affect buildings.
    /// Each accident has its own frequency window and probability.
    ///
    /// LEARNING DESIGN: Different accident types with different frequencies
    /// teach students that risk varies by category, making insurance
    /// choices meaningful.
    /// </summary>
    [CreateAssetMenu(fileName = "AccidentDefinition", menuName = "Fortune Valley/Accident Definition")]
    public class AccidentDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for this accident type")]
        [SerializeField] private string _accidentId;

        [Tooltip("Display name shown to the player")]
        [SerializeField] private string _displayName;

        [Tooltip("Description of what happened")]
        [SerializeField] private string _description;

        [Header("Damage")]
        [Tooltip("Base cost to repair this type of damage")]
        [SerializeField] private float _baseDamageCost;

        [Tooltip("Which insurance category covers this accident")]
        [SerializeField] private AccidentCategory _category;

        [Header("Frequency")]
        [Tooltip("Accident window opens every N in-game days")]
        [SerializeField] private int _windowIntervalDays;

        [Tooltip("Probability of accident when window is open (0 to 1)")]
        [SerializeField] private float _rollProbability;

        // Read-only accessors
        public string AccidentId => _accidentId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public float BaseDamageCost => _baseDamageCost;
        public AccidentCategory Category => _category;
        public int WindowIntervalDays => _windowIntervalDays;
        public float RollProbability => _rollProbability;
    }
}
