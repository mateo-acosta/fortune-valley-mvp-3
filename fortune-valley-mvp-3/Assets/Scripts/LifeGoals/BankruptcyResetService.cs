using System;
using System.Collections.Generic;
using FortuneValley.Domain.Interfaces;

namespace FortuneValley.Core
{
    /// <summary>
    /// Orchestrates a soft bankruptcy reset.
    ///
    /// Subscribes to GameEvents.OnBankruptcyTriggered. On fire:
    ///  1. Iterates the registered IBankruptcyResettable list (each system
    ///     resets its own state per the locked plan).
    ///  2. Invokes the lot-batch-reset hook (CityManager.BatchResetPlayerLots
    ///     wired in Step 14) so non-starter lots return to "for sale" and
    ///     the starter lot is forced to T1, all in a single batched event.
    ///  3. Sets bankruptcy_flag = true (persists for the rest of the life).
    ///  4. Fires OnSoftBankruptcyReset for the popup, autosave, and HUD.
    ///
    /// The bankruptcy_flag is also hydratable from a saved DTO so a player
    /// who reloads after a previous bankruptcy keeps the flag set.
    /// </summary>
    public class BankruptcyResetService : IDisposable
    {
        private readonly List<IBankruptcyResettable> _resettables = new List<IBankruptcyResettable>();
        private Action _batchLotResetAction;
        private bool _bankruptcyFlag;
        private bool _disposed;

        public BankruptcyResetService()
        {
            GameEvents.OnBankruptcyTriggered += HandleBankruptcyTriggered;
        }

        public bool BankruptcyFlag => _bankruptcyFlag;
        public IReadOnlyList<IBankruptcyResettable> RegisteredResettables => _resettables;

        public void Register(IBankruptcyResettable resettable)
        {
            if (resettable == null) return;
            if (_resettables.Contains(resettable)) return;
            _resettables.Add(resettable);
        }

        public void Unregister(IBankruptcyResettable resettable)
        {
            if (resettable == null) return;
            _resettables.Remove(resettable);
        }

        /// <summary>
        /// Wire CityManager's batched lot-reset hook. Optional -- when null,
        /// only the IBankruptcyResettable systems are reset.
        /// </summary>
        public void SetBatchLotResetAction(Action action)
        {
            _batchLotResetAction = action;
        }

        /// <summary>
        /// Restore bankruptcy_flag value from a saved DTO. Called by
        /// GameManager after loading state from the server.
        /// </summary>
        public void HydrateFlag(bool flag)
        {
            _bankruptcyFlag = flag;
        }

        /// <summary>
        /// Reset the flag back to false. Used on a brand-new game start
        /// (NOT during a soft reset, which preserves the flag for the life).
        /// </summary>
        public void ResetForNewGame()
        {
            _bankruptcyFlag = false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            GameEvents.OnBankruptcyTriggered -= HandleBankruptcyTriggered;
            _disposed = true;
        }

        private void HandleBankruptcyTriggered()
        {
            for (int i = 0; i < _resettables.Count; i++)
            {
                _resettables[i].OnBankruptcyReset();
            }

            _batchLotResetAction?.Invoke();

            _bankruptcyFlag = true;

            GameEvents.RaiseSoftBankruptcyReset();
        }
    }
}
