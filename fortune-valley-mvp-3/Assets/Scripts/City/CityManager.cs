using System.Collections.Generic;
using UnityEngine;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Interfaces;

namespace FortuneValley.Core
{
    /// <summary>
    /// Manages city lots and tracks ownership.
    /// Determines win/lose conditions.
    ///
    /// LEARNING DESIGN: Lots are the goal that makes financial decisions matter.
    /// Owning lots provides visual progress and income bonuses.
    /// The race to own lots creates urgency for financial optimization.
    /// </summary>
    public class CityManager : MonoBehaviour, ICityService
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        [Header("City Lots")]
        [Tooltip("All lots in the city (5 for POC)")]
        [SerializeField] private List<CityLotDefinition> _allLots;

        [Header("Dependencies")]
        [SerializeField] private CurrencyManager _currencyManager;

        [Header("Debug")]
        [SerializeField] private bool _logPurchases = false;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private Dictionary<string, Owner> _lotOwnership = new Dictionary<string, Owner>();
        private Dictionary<string, int> _purchaseTick = new Dictionary<string, int>();

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// All lot definitions.
        /// </summary>
        public IReadOnlyList<CityLotDefinition> AllLots => _allLots;

        /// <summary>
        /// Total number of lots in the city.
        /// </summary>
        public int TotalLots => _allLots.Count;

        /// <summary>
        /// Number of lots owned by player.
        /// </summary>
        public int PlayerLotCount => CountLotsOwnedBy(Owner.Player);

        /// <summary>
        /// Number of lots owned by rival.
        /// </summary>
        public int RivalLotCount => CountLotsOwnedBy(Owner.Rival);

        /// <summary>
        /// Number of lots not yet owned.
        /// </summary>
        public int AvailableLotCount => TotalLots - PlayerLotCount - RivalLotCount;

        /// <summary>
        /// Total income bonus from player-owned lots per tick.
        /// </summary>
        public float PlayerLotIncomeBonus
        {
            get
            {
                float total = 0f;
                foreach (var lot in _allLots)
                {
                    if (GetOwner(lot.LotId) == Owner.Player)
                    {
                        total += lot.IncomeBonus;
                    }
                }
                return total;
            }
        }

        /// <summary>
        /// Total income bonus from rival-owned lots per tick.
        /// </summary>
        public float RivalLotIncomeBonus
        {
            get
            {
                float total = 0f;
                foreach (var lot in _allLots)
                {
                    if (GetOwner(lot.LotId) == Owner.Rival)
                    {
                        total += lot.IncomeBonus;
                    }
                }
                return total;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void OnEnable()
        {
            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnTick += HandleTick;
            GameEvents.OnPurchaseLotRequested += HandlePurchaseLotRequested;
            GameEvents.OnLoanOriginated += HandleLoanOriginated;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnTick -= HandleTick;
            GameEvents.OnPurchaseLotRequested -= HandlePurchaseLotRequested;
            GameEvents.OnLoanOriginated -= HandleLoanOriginated;
        }

        /// <summary>
        /// Intent event handler: UI requested a cash lot purchase.
        /// </summary>
        private void HandlePurchaseLotRequested(string lotId, int tick)
        {
            TryPurchaseLot(lotId, tick);
        }

        /// <summary>
        /// Loan originated: LoanSystem processed the loan, now assign lot to player.
        /// Down payment already deducted by LoanSystem.
        /// </summary>
        private void HandleLoanOriginated(ActiveLoan loan)
        {
            if (loan == null) return;

            var lot = GetLot(loan.LotId);
            if (lot == null) return;

            // Verify lot is still available (rival could have bought it)
            if (GetOwner(loan.LotId) != Owner.None) return;

            // Assign lot to player (no currency deduction -- loan covers it)
            SetOwner(loan.LotId, Owner.Player, loan.StartDay);
        }

        private void HandleGameStart()
        {
            ResetOwnership();
            // Notify UI components of lot count so they can initialize without querying CityManager directly
            GameEvents.RaiseCityInitialized(_allLots.Count);
        }

        private void HandleTick(int tickNumber)
        {
            // Lot income is now handled by RestaurantSystem to unify floating text
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Get the owner of a lot.
        /// </summary>
        public Owner GetOwner(string lotId)
        {
            return _lotOwnership.TryGetValue(lotId, out Owner owner) ? owner : Owner.None;
        }

        /// <summary>
        /// Get a lot definition by ID.
        /// </summary>
        public CityLotDefinition GetLot(string lotId)
        {
            return _allLots.Find(lot => lot.LotId == lotId);
        }

        /// <summary>
        /// Get all lots available for purchase.
        /// </summary>
        public List<CityLotDefinition> GetAvailableLots()
        {
            var available = new List<CityLotDefinition>();
            foreach (var lot in _allLots)
            {
                if (GetOwner(lot.LotId) == Owner.None)
                {
                    available.Add(lot);
                }
            }
            return available;
        }

        /// <summary>
        /// Try to purchase a lot for the player.
        /// </summary>
        /// <returns>True if purchase succeeded</returns>
        public bool TryPurchaseLot(string lotId, int currentTick)
        {
            var lot = GetLot(lotId);
            if (lot == null)
            {
                Debug.LogWarning($"[CityManager] Lot {lotId} not found");
                return false;
            }

            // Check ownership
            if (GetOwner(lotId) != Owner.None)
            {
                Debug.Log($"[CityManager] Lot {lotId} already owned");
                return false;
            }

            // Try to spend
            if (!_currencyManager.TrySpendChecking(lot.BaseCost, $"Purchase lot: {lot.DisplayName}"))
            {
                Debug.Log($"[CityManager] Cannot afford lot {lotId}. Cost: ${lot.BaseCost:F0}");
                return false;
            }

            // Purchase successful
            SetOwner(lotId, Owner.Player, currentTick);
            return true;
        }

        /// <summary>
        /// Purchase a lot for the rival (no currency check - rival has own economy).
        /// </summary>
        /// <returns>True if purchase succeeded</returns>
        public bool RivalPurchaseLot(string lotId, int currentTick)
        {
            var lot = GetLot(lotId);
            if (lot == null || GetOwner(lotId) != Owner.None)
            {
                return false;
            }

            SetOwner(lotId, Owner.Rival, currentTick);
            return true;
        }

        /// <summary>
        /// Check if the game has ended.
        /// </summary>
        /// <returns>Winner if game ended, or null</returns>
        public Owner? CheckWinCondition()
        {
            // Win: Player owns all lots
            if (PlayerLotCount == TotalLots)
            {
                return Owner.Player;
            }

            // Lose: Rival owns all lots
            if (RivalLotCount == TotalLots)
            {
                return Owner.Rival;
            }

            // Alternative lose: No lots left and rival has more
            if (AvailableLotCount == 0 && RivalLotCount > PlayerLotCount)
            {
                return Owner.Rival;
            }

            // Alternative win: No lots left and player has more
            if (AvailableLotCount == 0 && PlayerLotCount > RivalLotCount)
            {
                return Owner.Player;
            }

            // Game continues
            return null;
        }

        /// <summary>
        /// Get the tick (day) a lot was purchased, or -1 if not purchased.
        /// Used by LearningReflectionBuilder for end-game insights.
        /// </summary>
        public int GetPurchaseTick(string lotId)
        {
            return _purchaseTick.TryGetValue(lotId, out int tick) ? tick : -1;
        }

        /// <summary>
        /// Get game progress (0 to 1) based on lots owned.
        /// </summary>
        public float GetGameProgress()
        {
            return (float)(PlayerLotCount + RivalLotCount) / TotalLots;
        }

        /// <summary>
        /// Get summary for UI.
        /// </summary>
        public string GetCitySummary()
        {
            return $"City Status:\n" +
                   $"• Your lots: {PlayerLotCount}\n" +
                   $"• Rival's lots: {RivalLotCount}\n" +
                   $"• Available: {AvailableLotCount}\n" +
                   $"• Your lot income: ${PlayerLotIncomeBonus:F0}/day";
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════

        private void ResetOwnership()
        {
            _lotOwnership.Clear();
            _purchaseTick.Clear();

            foreach (var lot in _allLots)
            {
                _lotOwnership[lot.LotId] = Owner.None;
            }
        }

        private void SetOwner(string lotId, Owner owner, int tick)
        {
            _lotOwnership[lotId] = owner;
            _purchaseTick[lotId] = tick;

            var lot = GetLot(lotId);

            if (_logPurchases)
            {
                Debug.Log($"[CityManager] {owner} purchased {lot.DisplayName} for ${lot.BaseCost:F0}");
            }

            GameEvents.RaiseLotPurchased(lotId, owner);

            // Check for game end
            var winner = CheckWinCondition();
            if (winner.HasValue)
            {
                GameEvents.RaiseGameEnd(winner.Value);
            }
        }

        private int CountLotsOwnedBy(Owner owner)
        {
            int count = 0;
            foreach (var kvp in _lotOwnership)
            {
                if (kvp.Value == owner)
                    count++;
            }
            return count;
        }
    }
}
