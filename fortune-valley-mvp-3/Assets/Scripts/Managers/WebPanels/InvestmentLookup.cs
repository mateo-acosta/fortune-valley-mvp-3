using System.Collections.Generic;
using FortuneValley.Core;

namespace FortuneValley.Managers.WebPanels
{
    /// <summary>
    /// Pure-C# lookup helpers used by web bridge intent handlers to map
    /// the iframe's string symbol (ScriptableObject asset name) onto the
    /// runtime InvestmentDefinition or ActiveInvestment instances.
    /// Lifted out of InvestingWebBridge so the MonoBehaviour stays free
    /// of loops per CLAUDE.md MonoBehaviour method scope rule.
    /// </summary>
    public static class InvestmentLookup
    {
        public static InvestmentDefinition FindDefinitionByName(
            IReadOnlyList<InvestmentDefinition> available,
            string symbol)
        {
            if (available == null || string.IsNullOrEmpty(symbol)) return null;
            for (int i = 0; i < available.Count; i++)
            {
                var def = available[i];
                if (def != null && def.name == symbol) return def;
            }
            return null;
        }

        public static ActiveInvestment FindHoldingByName(
            IReadOnlyList<ActiveInvestment> holdings,
            string symbol)
        {
            if (holdings == null || string.IsNullOrEmpty(symbol)) return null;
            for (int i = 0; i < holdings.Count; i++)
            {
                var inv = holdings[i];
                if (inv != null && inv.Definition != null && inv.Definition.name == symbol) return inv;
            }
            return null;
        }
    }
}
