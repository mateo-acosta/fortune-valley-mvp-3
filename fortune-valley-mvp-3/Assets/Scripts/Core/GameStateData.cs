using System;
using System.Collections.Generic;
using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Single source of truth for all serializable game state.
    /// All systems read from and write to this object.
    /// Serializable via JsonUtility for save/load to PlayerPrefs.
    ///
    /// Extend with loans, insurance, and credit score fields
    /// as those systems are implemented.
    /// </summary>
    [Serializable]
    public class GameStateData
    {
        // ═══════════════════════════════════════════════════════════════
        // PLAYER ECONOMY
        // ═══════════════════════════════════════════════════════════════

        [SerializeField] private float _balance;
        [SerializeField] private int _restaurantLevel;

        // ═══════════════════════════════════════════════════════════════
        // CITY OWNERSHIP
        // ═══════════════════════════════════════════════════════════════

        [SerializeField] private List<string> _ownedLotIds = new List<string>();
        [SerializeField] private List<string> _rivalOwnedLotIds = new List<string>();

        // ═══════════════════════════════════════════════════════════════
        // RIVAL STATE
        // ═══════════════════════════════════════════════════════════════

        [SerializeField] private float _rivalBalance;

        // ═══════════════════════════════════════════════════════════════
        // TIME
        // ═══════════════════════════════════════════════════════════════

        [SerializeField] private int _currentTick;
        [SerializeField] private int _currentDay;
        [SerializeField] private long _lastSaveTimestamp;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public float Balance { get => _balance; set => _balance = value; }
        public int RestaurantLevel { get => _restaurantLevel; set => _restaurantLevel = value; }
        public List<string> OwnedLotIds { get => _ownedLotIds; set => _ownedLotIds = value; }
        public List<string> RivalOwnedLotIds { get => _rivalOwnedLotIds; set => _rivalOwnedLotIds = value; }
        public float RivalBalance { get => _rivalBalance; set => _rivalBalance = value; }
        public int CurrentTick { get => _currentTick; set => _currentTick = value; }
        public int CurrentDay { get => _currentDay; set => _currentDay = value; }
        public long LastSaveTimestamp { get => _lastSaveTimestamp; set => _lastSaveTimestamp = value; }
    }
}
