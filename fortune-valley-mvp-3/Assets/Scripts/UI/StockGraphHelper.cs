using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;
using FortuneValley.UI.Components;

namespace FortuneValley.UI
{
    /// <summary>
    /// Shared utility for creating and refreshing stock price graphs.
    /// Used by both InvestingTradeSubPanel and PortfolioDetailView
    /// to avoid duplicating graph setup and refresh logic.
    /// </summary>
    public static class StockGraphHelper
    {
        /// <summary>
        /// Ensure a LineGraphGraphic exists on the placeholder.
        /// Creates one if missing, configures layout to fill the placeholder.
        /// Returns the graph component (cached by caller).
        /// </summary>
        public static LineGraphGraphic EnsureGraphCreated(
            Image placeholder, TMP_FontAsset labelFont)
        {
            if (placeholder == null) return null;

            // Clear placeholder background so graph renders cleanly
            placeholder.color = Color.clear;

            var go = new GameObject("Graph", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(placeholder.transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(
                LineGraphGraphic.YLabelWidth - 8f,
                LineGraphGraphic.XLabelHeight + 2f);
            rt.offsetMax = Vector2.zero;

            var graph = go.AddComponent<LineGraphGraphic>();
            if (labelFont != null)
                graph.SetLabelFont(labelFont);

            return graph;
        }

        /// <summary>
        /// Refresh the graph with price history data for the given window size.
        /// Handles null checks and data retrieval from the history store.
        /// </summary>
        /// <param name="graph">The LineGraphGraphic to update</param>
        /// <param name="store">Price history data source</param>
        /// <param name="def">Which investment to show</param>
        /// <param name="windowSize">Number of days to display (e.g., 7, 30, 60, 200)</param>
        /// <param name="currentDayTick">Current game day for x-axis labeling</param>
        /// <param name="dataBuffer">Reusable list to avoid per-call allocations (caller owns)</param>
        public static void RefreshGraph(
            LineGraphGraphic graph,
            StockPriceHistoryStore store,
            InvestmentDefinition def,
            int windowSize,
            int currentDayTick,
            List<float> dataBuffer)
        {
            if (graph == null || store == null || def == null) return;

            var window = store.GetWindow(def, windowSize);

            dataBuffer.Clear();
            for (int i = 0; i < window.Count; i++)
                dataBuffer.Add(window[i]);

            int startDay = currentDayTick - (dataBuffer.Count - 1);
            graph.SetData(dataBuffer, startDay);
        }
    }
}
