using UnityEngine;
using System.Collections.Generic;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Core
{
    /// <summary>
    /// Runtime debug buttons to force-trigger game end events during Play Mode.
    /// Attach to a UI GameObject with two buttons wired to ForceWin() and ForceLose().
    /// Self-destructs in release builds.
    /// </summary>
    public class DebugGameEndButton : MonoBehaviour
    {
        /// <summary>
        /// Returns true if this GameObject has children that are NOT debug buttons
        /// (meaning it's a container like BottomBar and should not be destroyed).
        /// </summary>
        public bool ShouldPreserveGameObject()
        {
            foreach (Transform child in transform)
            {
                if (child.GetComponent<DebugGameEndButton>() == null)
                    return true;
            }
            return false;
        }

        private void Awake()
        {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
            if (ShouldPreserveGameObject())
            {
                // Container (e.g. BottomBar): destroy debug-only children, keep non-debug children
                foreach (Transform child in transform)
                {
                    if (child.GetComponent<DebugGameEndButton>() != null)
                        Destroy(child.gameObject);
                }
                Destroy(this);
            }
            else
            {
                // Leaf debug button (e.g. WinButton): destroy entire GameObject
                Destroy(gameObject);
            }
            return;
#endif
        }

        /// <summary>
        /// Wire this to a "DBG: Win" button's OnClick event.
        /// Only fires OnGameEndWithSummary (skips OnGameEnd to avoid double-event race).
        /// </summary>
        public void ForceWin()
        {
            UnityEngine.Debug.Log("[DebugGameEnd] ForceWin() called");

            var summary = new GameSummary
            {
                DaysPlayed = 45,
                PlayerLots = 3,
                RivalLots = 2,
                TotalLots = 5,
                FinalNetWorth = 12500f,
                TotalInvestmentGains = 3200f,
                TotalRestaurantIncome = 8400f,
                TotalSpentOnLots = 4500f,
                InvestmentCount = 4,
                PeakPortfolioValue = 5600f,
                Headline = "Smart Investor!",
                InvestmentInsight = "Your investments earned compound interest, " +
                    "growing your money by $3,200 while you focused on the restaurant.",
                OpportunityCostInsight = "By investing early instead of buying lots immediately, " +
                    "you had more money when it mattered most.",
                WhatIfMessage = "What if you had invested even earlier? " +
                    "Compound interest rewards patience — every extra day counts!",
                KeyDecisions = new List<string>
                {
                    "Day 5: Invested $1,000 in Tech Fund — grew 40% by endgame",
                    "Day 12: Bought Bond at 5% APY — steady, safe income",
                    "Day 20: Sold stocks at peak to buy Riverside lot"
                }
            };

            GameEvents.RaiseGameEndWithSummary(true, summary);
            UnityEngine.Debug.Log("[DebugGameEnd] Forced WIN with sample summary.");
        }

        /// <summary>
        /// Wire this to a "DBG: Lose" button's OnClick event.
        /// Only fires OnGameEndWithSummary (skips OnGameEnd to avoid double-event race).
        /// </summary>
        public void ForceLose()
        {
            UnityEngine.Debug.Log("[DebugGameEnd] ForceLose() called");

            var summary = new GameSummary
            {
                DaysPlayed = 60,
                PlayerLots = 1,
                RivalLots = 4,
                TotalLots = 5,
                FinalNetWorth = 2100f,
                TotalInvestmentGains = -400f,
                TotalRestaurantIncome = 4200f,
                TotalSpentOnLots = 1500f,
                InvestmentCount = 1,
                PeakPortfolioValue = 800f,
                Headline = "The Rival Got Ahead",
                InvestmentInsight = "You invested late and sold at a loss. " +
                    "Investing earlier would have given compound interest more time to work.",
                OpportunityCostInsight = "Spending all your money on lots left nothing to invest. " +
                    "The rival's investments gave them more buying power over time.",
                WhatIfMessage = "What if you had saved 30% of your income for investments? " +
                    "Even small amounts grow significantly with compound interest.",
                KeyDecisions = new List<string>
                {
                    "Day 3: Spent all savings on Downtown lot — no money left to invest",
                    "Day 25: Bought risky stock that dropped 30%",
                    "Day 40: Rival bought 3 lots in a row while you were short on cash"
                }
            };

            GameEvents.RaiseGameEndWithSummary(false, summary);
            UnityEngine.Debug.Log("[DebugGameEnd] Forced LOSE with sample summary.");
        }
    }
}
