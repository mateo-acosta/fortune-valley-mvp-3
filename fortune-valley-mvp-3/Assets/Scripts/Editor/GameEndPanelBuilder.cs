using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using FortuneValley.UI;
using FortuneValley.UI.Panels;
using FortuneValley.UI.HUD;

/// <summary>
/// Editor scripts that build UI hierarchies under UI_Canvas.
/// Run via: Fortune Valley > Setup > Build Game End Panel
///          Fortune Valley > Setup > Build Speed Controls
/// </summary>
public static class GameEndPanelBuilder
{
    // ═══════════════════════════════════════════════════════════════
    // FIX 1: GameEndPanel — overlay lives on PanelRoot, not root
    // ═══════════════════════════════════════════════════════════════

    [MenuItem("Fortune Valley/Setup/Build Game End Panel")]
    public static void BuildGameEndPanel()
    {
        var canvas = GameObject.Find("UI_Canvas");
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "UI_Canvas not found in scene.", "OK");
            return;
        }

        // Delete existing if present
        var existing = canvas.transform.Find("GameEndPanel");
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("GameEndPanel Exists",
                "A GameEndPanel already exists under UI_Canvas. Delete it and rebuild?",
                "Rebuild", "Cancel"))
            {
                return;
            }
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        // ── Root: bare container with script only (NO Image/overlay) ──
        var panel = UIBuilderUtils.CreateUIElement("GameEndPanel", canvas.transform);
        UIBuilderUtils.StretchToFill(panel);

        var panelScript = panel.AddComponent<GameEndPanel>();

        // ── PanelRoot: full-screen dark overlay (this gets toggled by UIPanel.Hide) ──
        var panelRoot = UIBuilderUtils.CreateUIElement("PanelRoot", panel.transform);
        UIBuilderUtils.StretchToFill(panelRoot);
        var overlayImage = panelRoot.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.85f);
        overlayImage.raycastTarget = true;

        // ── ContentPanel: centered content area — now HORIZONTAL for two-column layout ──
        var contentPanel = UIBuilderUtils.CreateUIElement("ContentPanel", panelRoot.transform);
        var contentRT = contentPanel.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0.05f, 0.05f);
        contentRT.anchorMax = new Vector2(0.95f, 0.95f);
        contentRT.offsetMin = Vector2.zero;
        contentRT.offsetMax = Vector2.zero;
        var contentImage = contentPanel.AddComponent<Image>();
        contentImage.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);
        var contentLayout = contentPanel.AddComponent<HorizontalLayoutGroup>();
        contentLayout.padding = new RectOffset(10, 10, 10, 10);
        contentLayout.spacing = 10;
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = false;
        contentLayout.childForceExpandHeight = true;

        // ── StatsColumn: all existing stat content (left side) ──
        var statsColumn = UIBuilderUtils.CreateUIElement("StatsColumn", contentPanel.transform);
        var statsColumnLE = statsColumn.AddComponent<LayoutElement>();
        statsColumnLE.flexibleWidth = 3;
        var statsColumnLayout = statsColumn.AddComponent<VerticalLayoutGroup>();
        statsColumnLayout.padding = new RectOffset(20, 20, 10, 10);
        statsColumnLayout.spacing = 12;
        statsColumnLayout.childAlignment = TextAnchor.UpperCenter;
        statsColumnLayout.childControlWidth = true;
        statsColumnLayout.childControlHeight = false;
        statsColumnLayout.childForceExpandWidth = true;
        statsColumnLayout.childForceExpandHeight = false;

        // ── Outcome Section ──
        var outcomeSection = UIBuilderUtils.CreateUIElement("OutcomeSection", statsColumn.transform);
        var outcomeLayout = outcomeSection.AddComponent<HorizontalLayoutGroup>();
        outcomeLayout.spacing = 15;
        outcomeLayout.childAlignment = TextAnchor.MiddleCenter;
        outcomeLayout.childControlWidth = false;
        outcomeLayout.childControlHeight = false;
        outcomeLayout.childForceExpandWidth = false;
        UIBuilderUtils.SetPreferredHeight(outcomeSection, 60);

        var outcomeIcon = UIBuilderUtils.CreateUIElement("OutcomeIcon", outcomeSection.transform);
        var outcomeIconImage = outcomeIcon.AddComponent<Image>();
        outcomeIconImage.color = new Color(0.9f, 0.8f, 0.2f);
        var outcomeIconLE = outcomeIcon.AddComponent<LayoutElement>();
        outcomeIconLE.preferredWidth = 50;
        outcomeIconLE.preferredHeight = 50;

        var outcomeText = UIBuilderUtils.CreateTMPText("OutcomeText", outcomeSection.transform,
            "VICTORY!", 36, FontStyles.Bold, new Color(0.9f, 0.8f, 0.2f));
        var outcomeTextLE = outcomeText.gameObject.AddComponent<LayoutElement>();
        outcomeTextLE.preferredHeight = 50;
        outcomeTextLE.flexibleWidth = 1;

        // ── Outcome Background (colored bar) ──
        var outcomeBg = UIBuilderUtils.CreateUIElement("OutcomeBackground", statsColumn.transform);
        var outcomeBgImage = outcomeBg.AddComponent<Image>();
        outcomeBgImage.color = new Color(0.2f, 0.6f, 0.9f, 0.3f);
        UIBuilderUtils.SetPreferredHeight(outcomeBg, 4);

        // ── Stats Section ──
        var statsSection = UIBuilderUtils.CreateUIElement("StatsSection", statsColumn.transform);
        var statsLayout = statsSection.AddComponent<VerticalLayoutGroup>();
        statsLayout.spacing = 6;
        statsLayout.childAlignment = TextAnchor.UpperLeft;
        statsLayout.childControlWidth = true;
        statsLayout.childControlHeight = false;
        statsLayout.childForceExpandWidth = true;
        statsLayout.padding = new RectOffset(10, 10, 5, 5);

        var daysPlayed = UIBuilderUtils.CreateTMPText("DaysPlayedText", statsSection.transform,
            "Game Duration: 45 days", 18, FontStyles.Normal, Color.white);
        UIBuilderUtils.SetPreferredHeight(daysPlayed.gameObject, 28);

        var lotsOwned = UIBuilderUtils.CreateTMPText("LotsOwnedText", statsSection.transform,
            "Your Lots: 3/5", 18, FontStyles.Normal, Color.white);
        UIBuilderUtils.SetPreferredHeight(lotsOwned.gameObject, 28);

        var rivalLots = UIBuilderUtils.CreateTMPText("RivalLotsText", statsSection.transform,
            "Rival Lots: 2/5", 18, FontStyles.Normal, Color.white);
        UIBuilderUtils.SetPreferredHeight(rivalLots.gameObject, 28);

        var netWorth = UIBuilderUtils.CreateTMPText("NetWorthText", statsSection.transform,
            "Final Net Worth: $12,500", 18, FontStyles.Normal, Color.white);
        UIBuilderUtils.SetPreferredHeight(netWorth.gameObject, 28);

        var investGains = UIBuilderUtils.CreateTMPText("InvestmentGainsText", statsSection.transform,
            "Investment Gains: +$3,200", 18, FontStyles.Normal, new Color(0.2f, 0.8f, 0.2f));
        UIBuilderUtils.SetPreferredHeight(investGains.gameObject, 28);

        var restaurantIncome = UIBuilderUtils.CreateTMPText("RestaurantIncomeText", statsSection.transform,
            "Restaurant Income: $8,400", 18, FontStyles.Normal, Color.white);
        UIBuilderUtils.SetPreferredHeight(restaurantIncome.gameObject, 28);

        // ── Divider ──
        var divider = UIBuilderUtils.CreateUIElement("Divider", statsColumn.transform);
        var dividerImage = divider.AddComponent<Image>();
        dividerImage.color = new Color(1f, 1f, 1f, 0.2f);
        UIBuilderUtils.SetPreferredHeight(divider, 2);

        // ── Reflections Section ──
        var reflections = UIBuilderUtils.CreateUIElement("ReflectionsSection", statsColumn.transform);
        var reflLayout = reflections.AddComponent<VerticalLayoutGroup>();
        reflLayout.spacing = 8;
        reflLayout.childAlignment = TextAnchor.UpperLeft;
        reflLayout.childControlWidth = true;
        reflLayout.childControlHeight = false;
        reflLayout.childForceExpandWidth = true;
        reflLayout.padding = new RectOffset(10, 10, 5, 5);

        var headline = UIBuilderUtils.CreateTMPText("HeadlineText", reflections.transform,
            "Smart Investor!", 22, FontStyles.Bold, new Color(0.9f, 0.8f, 0.2f));
        UIBuilderUtils.SetPreferredHeight(headline.gameObject, 32);

        var investInsight = UIBuilderUtils.CreateTMPText("InvestmentInsightText", reflections.transform,
            "Your investments earned compound interest, growing your money while you waited.",
            16, FontStyles.Normal, new Color(0.8f, 0.8f, 0.9f));
        UIBuilderUtils.SetPreferredHeight(investInsight.gameObject, 45);

        var oppCost = UIBuilderUtils.CreateTMPText("OpportunityCostText", reflections.transform,
            "By choosing to invest instead of buying lots immediately, you had more money later.",
            16, FontStyles.Normal, new Color(0.8f, 0.8f, 0.9f));
        UIBuilderUtils.SetPreferredHeight(oppCost.gameObject, 45);

        var whatIf = UIBuilderUtils.CreateTMPText("WhatIfText", reflections.transform,
            "What if you had invested even earlier? Compound interest rewards patience!",
            16, FontStyles.Italic, new Color(0.7f, 0.7f, 0.8f));
        UIBuilderUtils.SetPreferredHeight(whatIf.gameObject, 45);

        // ── Decisions Section ──
        var decisionsSection = UIBuilderUtils.CreateUIElement("DecisionsSection", statsColumn.transform);
        var decLayout = decisionsSection.AddComponent<VerticalLayoutGroup>();
        decLayout.spacing = 4;
        decLayout.childAlignment = TextAnchor.UpperLeft;
        decLayout.childControlWidth = true;
        decLayout.childControlHeight = false;
        decLayout.childForceExpandWidth = true;
        decLayout.padding = new RectOffset(10, 10, 5, 5);

        var decisionsContainer = UIBuilderUtils.CreateUIElement("DecisionsContainer", decisionsSection.transform);
        var decContainerLayout = decisionsContainer.AddComponent<VerticalLayoutGroup>();
        decContainerLayout.spacing = 4;
        decContainerLayout.childAlignment = TextAnchor.UpperLeft;
        decContainerLayout.childControlWidth = true;
        decContainerLayout.childControlHeight = false;
        decContainerLayout.childForceExpandWidth = true;

        var decisionItemPrefab = UIBuilderUtils.CreateTMPText("DecisionItemPrefab", decisionsContainer.transform,
            "\u2022 Invested in Tech Fund on Day 5 \u2014 grew 40% by end game",
            15, FontStyles.Normal, new Color(0.7f, 0.8f, 0.7f));
        UIBuilderUtils.SetPreferredHeight(decisionItemPrefab.gameObject, 25);

        // ── Buttons Section ──
        var buttonsSection = UIBuilderUtils.CreateUIElement("ButtonsSection", statsColumn.transform);
        var btnLayout = buttonsSection.AddComponent<HorizontalLayoutGroup>();
        btnLayout.spacing = 20;
        btnLayout.childAlignment = TextAnchor.MiddleCenter;
        btnLayout.childControlWidth = false;
        btnLayout.childControlHeight = false;
        btnLayout.childForceExpandWidth = false;
        UIBuilderUtils.SetPreferredHeight(buttonsSection, 55);

        var playAgainBtn = UIBuilderUtils.CreateButton("PlayAgainButton", buttonsSection.transform,
            "Play Again", new Color(0.2f, 0.6f, 0.3f), 160, 45);

        var mainMenuBtn = UIBuilderUtils.CreateButton("MainMenuButton", buttonsSection.transform,
            "Main Menu", new Color(0.4f, 0.4f, 0.5f), 160, 45);

        // ── ChatColumn: empty container for Coach Val (right side) ──
        var chatColumn = UIBuilderUtils.CreateUIElement("ChatColumn", contentPanel.transform);
        var chatColumnLE = chatColumn.AddComponent<LayoutElement>();
        chatColumnLE.flexibleWidth = 2;
        var chatColumnImage = chatColumn.AddComponent<Image>();
        chatColumnImage.color = new Color(0.08f, 0.08f, 0.12f, 0.9f);

        // ── Add GameEndChatIntegration component ──
        var chatIntegration = panel.AddComponent<GameEndChatIntegration>();

        // ── Wire serialized references ──
        Undo.RegisterCreatedObjectUndo(panel, "Build Game End Panel");

        var so = new SerializedObject(panelScript);

        so.FindProperty("_panelRoot").objectReferenceValue = panelRoot;

        so.FindProperty("_outcomeText").objectReferenceValue = outcomeText;
        so.FindProperty("_outcomeBackground").objectReferenceValue = outcomeBgImage;
        so.FindProperty("_outcomeIcon").objectReferenceValue = outcomeIconImage;

        so.FindProperty("_daysPlayedText").objectReferenceValue = daysPlayed;
        so.FindProperty("_lotsOwnedText").objectReferenceValue = lotsOwned;
        so.FindProperty("_rivalLotsText").objectReferenceValue = rivalLots;
        so.FindProperty("_netWorthText").objectReferenceValue = netWorth;
        so.FindProperty("_investmentGainsText").objectReferenceValue = investGains;
        so.FindProperty("_restaurantIncomeText").objectReferenceValue = restaurantIncome;

        so.FindProperty("_decisionsContainer").objectReferenceValue = decisionsContainer.transform;
        so.FindProperty("_decisionItemPrefab").objectReferenceValue = decisionItemPrefab;

        so.FindProperty("_headlineText").objectReferenceValue = headline;
        so.FindProperty("_investmentInsightText").objectReferenceValue = investInsight;
        so.FindProperty("_opportunityCostText").objectReferenceValue = oppCost;
        so.FindProperty("_whatIfText").objectReferenceValue = whatIf;

        so.FindProperty("_playAgainButton").objectReferenceValue = playAgainBtn;
        so.FindProperty("_mainMenuButton").objectReferenceValue = mainMenuBtn;

        so.ApplyModifiedProperties();

        // ── Wire GameEndChatIntegration references ──
        var chatSO = new SerializedObject(chatIntegration);
        chatSO.FindProperty("_chatColumn").objectReferenceValue = chatColumn.transform;

        // Try to find CoachConfig asset
        var configGuids = AssetDatabase.FindAssets("t:CoachConfig");
        if (configGuids.Length > 0)
        {
            var configPath = AssetDatabase.GUIDToAssetPath(configGuids[0]);
            var config = AssetDatabase.LoadAssetAtPath<Object>(configPath);
            chatSO.FindProperty("_coachConfig").objectReferenceValue = config;
        }
        else
        {
            Debug.LogWarning("CoachConfig asset not found — wire _coachConfig manually after creating it.");
        }

        // Try to find InvestmentSystem in scene
        var investmentSystem = Object.FindFirstObjectByType<FortuneValley.Core.InvestmentSystem>();
        if (investmentSystem != null)
        {
            chatSO.FindProperty("_investmentSystem").objectReferenceValue = investmentSystem;
        }
        else
        {
            Debug.LogWarning("InvestmentSystem not found in scene — wire _investmentSystem manually.");
        }

        chatSO.ApplyModifiedProperties();

        // Start hidden
        panelRoot.SetActive(false);

        EditorUtility.SetDirty(panel);
        Debug.Log("GameEndPanel built successfully under UI_Canvas with two-column layout!");
    }

    // ═══════════════════════════════════════════════════════════════
    // FIX 2: Speed Controls — recreate DaySpeedDisplay under TopBar
    // ═══════════════════════════════════════════════════════════════

    [MenuItem("Fortune Valley/Setup/Build Speed Controls")]
    public static void BuildSpeedControls()
    {
        // Find TopBar in the scene
        var topBar = GameObject.Find("TopBar");
        if (topBar == null)
        {
            EditorUtility.DisplayDialog("Error", "TopBar not found in scene.", "OK");
            return;
        }

        // Delete existing if present
        var existing = topBar.transform.Find("DaySpeedDisplay");
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("DaySpeedDisplay Exists",
                "A DaySpeedDisplay already exists under TopBar. Delete it and rebuild?",
                "Rebuild", "Cancel"))
            {
                return;
            }
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        // ── Root: DaySpeedDisplay with HorizontalLayoutGroup ──
        var root = UIBuilderUtils.CreateUIElement("DaySpeedDisplay", topBar.transform);
        var rootLayout = root.AddComponent<HorizontalLayoutGroup>();
        rootLayout.spacing = 8;
        rootLayout.childAlignment = TextAnchor.MiddleCenter;
        rootLayout.childControlWidth = false;
        rootLayout.childControlHeight = false;
        rootLayout.childForceExpandWidth = false;
        rootLayout.childForceExpandHeight = false;
        var rootLE = root.AddComponent<LayoutElement>();
        rootLE.preferredWidth = 220;
        rootLE.preferredHeight = 40;

        var speedScript = root.AddComponent<DaySpeedDisplay>();

        // ── Day Text ──
        var dayText = UIBuilderUtils.CreateTMPText("DayText", root.transform,
            "Day 1", 16, FontStyles.Bold, Color.white);
        dayText.alignment = TextAlignmentOptions.Center;
        var dayLE = dayText.gameObject.AddComponent<LayoutElement>();
        dayLE.preferredWidth = 70;
        dayLE.preferredHeight = 36;

        // ── Speed Buttons container ──
        var speedButtons = UIBuilderUtils.CreateUIElement("SpeedButtons", root.transform);
        var speedBtnLayout = speedButtons.AddComponent<HorizontalLayoutGroup>();
        speedBtnLayout.spacing = 4;
        speedBtnLayout.childAlignment = TextAnchor.MiddleCenter;
        speedBtnLayout.childControlWidth = false;
        speedBtnLayout.childControlHeight = false;
        speedBtnLayout.childForceExpandWidth = false;
        speedBtnLayout.childForceExpandHeight = false;

        // Pause button
        var pauseBtn = UIBuilderUtils.CreateButton("PauseButton", speedButtons.transform,
            "||", new Color(0.4f, 0.4f, 0.5f), 36, 36);

        // 1x button
        var speed1xBtn = UIBuilderUtils.CreateButton("Speed1xButton", speedButtons.transform,
            "1x", new Color(0.3f, 0.5f, 0.7f), 36, 36);

        // 2x button
        var speed2xBtn = UIBuilderUtils.CreateButton("Speed2xButton", speedButtons.transform,
            "2x", new Color(0.3f, 0.5f, 0.7f), 36, 36);

        // ── Wire DaySpeedDisplay serialized fields ──
        Undo.RegisterCreatedObjectUndo(root, "Build Speed Controls");

        var so = new SerializedObject(speedScript);
        so.FindProperty("_dayText").objectReferenceValue = dayText;
        so.FindProperty("_pauseButton").objectReferenceValue = pauseBtn;
        so.FindProperty("_speed1xButton").objectReferenceValue = speed1xBtn;
        so.FindProperty("_speed2xButton").objectReferenceValue = speed2xBtn;
        so.ApplyModifiedProperties();

        // ── Wire GameHUD._daySpeedDisplay reference ──
        var gameHUD = Object.FindFirstObjectByType<GameHUD>();
        if (gameHUD != null)
        {
            var hudSO = new SerializedObject(gameHUD);
            hudSO.FindProperty("_daySpeedDisplay").objectReferenceValue = speedScript;
            hudSO.ApplyModifiedProperties();
            Debug.Log("Wired DaySpeedDisplay to GameHUD._daySpeedDisplay.");
        }
        else
        {
            Debug.LogWarning("GameHUD not found in scene — wire _daySpeedDisplay manually.");
        }

        EditorUtility.SetDirty(root);
        Debug.Log("DaySpeedDisplay built successfully under TopBar!");
    }
}
