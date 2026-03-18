using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using FortuneValley.UI;
using FortuneValley.UI.HUD;
using FortuneValley.UI.Panels;
using FortuneValley.UI.Popups;
using FortuneValley.UI.Components;
using FortuneValley.Core;

namespace FortuneValley.Editor
{
    /// <summary>
    /// Editor wizard to create and setup all UI prefabs for Fortune Valley.
    /// Run from menu: Fortune Valley > Setup UI
    /// </summary>
    public class UISetupWizard : EditorWindow
    {
        private static readonly Color PANEL_BG_COLOR = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        private static readonly Color POPUP_BG_COLOR = new Color(0.15f, 0.15f, 0.2f, 0.98f);
        private static readonly Color BUTTON_COLOR = new Color(0.2f, 0.5f, 0.8f, 1f);
        private static readonly Color BUTTON_HOVER_COLOR = new Color(0.3f, 0.6f, 0.9f, 1f);
        private static readonly Color TEXT_COLOR = Color.white;
        private static readonly Color ACCENT_COLOR = new Color(0.3f, 0.8f, 0.4f, 1f);

        [MenuItem("Fortune Valley/Setup UI")]
        public static void ShowWindow()
        {
            GetWindow<UISetupWizard>("UI Setup Wizard");
        }

        private void OnGUI()
        {
            GUILayout.Label("Fortune Valley UI Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This wizard will create all UI prefabs and wire up references.\n\n" +
                "It will create:\n" +
                "• Main UI Canvas with HUD\n" +
                "• All Panels (Portfolio, Lots)\n" +
                "• All Popups (Purchase, Buy/Sell Investment, Transfer)\n" +
                "• List item prefabs",
                MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("Create UI in Current Scene", GUILayout.Height(40)))
            {
                CreateUIInScene();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Create UI Prefabs", GUILayout.Height(30)))
            {
                CreateUIPrefabs();
            }
        }

        private static void CreateUIInScene()
        {
            // Check for existing UI
            var existingCanvas = GameObject.Find("UI_Canvas");
            if (existingCanvas != null)
            {
                if (!EditorUtility.DisplayDialog("UI Already Exists",
                    "A UI_Canvas already exists in the scene. Delete it and create new?",
                    "Yes, Replace", "Cancel"))
                {
                    return;
                }
                Undo.DestroyObjectImmediate(existingCanvas);
            }

            // Create main canvas
            GameObject canvasGO = CreateMainCanvas();

            // Create HUD
            GameObject hud = CreateHUD(canvasGO.transform);

            // Create Panels container
            GameObject panelsContainer = CreateContainer(canvasGO.transform, "Panels");

            // Create individual panels
            GameObject portfolioPanel = CreatePortfolioPanel(panelsContainer.transform);
            GameObject lotsPanel = CreateLotsPanel(panelsContainer.transform);

            // Create Popups container
            GameObject popupsContainer = CreateContainer(canvasGO.transform, "Popups");

            // Create overlay
            GameObject overlay = CreateOverlay(popupsContainer.transform);

            // Create individual popups
            GameObject lotPurchasePopup = CreateLotPurchasePopup(popupsContainer.transform);
            GameObject buyInvestmentPopup = CreateBuyInvestmentPopup(popupsContainer.transform);
            GameObject sellInvestmentPopup = CreateSellInvestmentPopup(popupsContainer.transform);
            GameObject transferPopup = CreateTransferPopup(popupsContainer.transform);

            // Create UIManager and wire up references
            var uiManager = canvasGO.AddComponent<UIManager>();
            SerializedObject uiManagerSO = new SerializedObject(uiManager);
            uiManagerSO.FindProperty("_portfolioPanel").objectReferenceValue = portfolioPanel.GetComponent<UIPanel>();
            uiManagerSO.FindProperty("_lotsPanel").objectReferenceValue = lotsPanel.GetComponent<UIPanel>();
            uiManagerSO.FindProperty("_lotPurchasePopup").objectReferenceValue = lotPurchasePopup.GetComponent<UIPopup>();
            uiManagerSO.FindProperty("_buyInvestmentPopup").objectReferenceValue = buyInvestmentPopup.GetComponent<UIPopup>();
            uiManagerSO.FindProperty("_sellInvestmentPopup").objectReferenceValue = sellInvestmentPopup.GetComponent<UIPopup>();
            uiManagerSO.FindProperty("_transferPopup").objectReferenceValue = transferPopup.GetComponent<UIPopup>();
            uiManagerSO.FindProperty("_popupOverlay").objectReferenceValue = overlay;
            uiManagerSO.ApplyModifiedProperties();

            // Hide panels and popups initially
            panelsContainer.SetActive(true);
            portfolioPanel.SetActive(false);
            lotsPanel.SetActive(false);
            popupsContainer.SetActive(true);
            overlay.SetActive(false);
            lotPurchasePopup.SetActive(false);
            buyInvestmentPopup.SetActive(false);
            sellInvestmentPopup.SetActive(false);
            transferPopup.SetActive(false);

            // Select the canvas
            Selection.activeGameObject = canvasGO;

            Debug.Log("[UISetupWizard] UI created successfully!");
            EditorUtility.DisplayDialog("Success", "UI has been created in the scene!", "OK");
        }

        private static void CreateUIPrefabs()
        {
            // Ensure prefab directory exists
            string prefabPath = "Assets/Prefabs/UI";
            if (!AssetDatabase.IsValidFolder(prefabPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                {
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                }
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            }

            // Create list item prefabs
            CreateInvestmentListItemPrefab(prefabPath);
            CreateLotListItemPrefab(prefabPath);
            CreateInvestmentRowPrefab(prefabPath);
            CreateSectionHeaderPrefab(prefabPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[UISetupWizard] Prefabs created successfully!");
            EditorUtility.DisplayDialog("Success", "UI Prefabs have been created!", "OK");
        }

        // ═══════════════════════════════════════════════════════════════
        // CANVAS & CONTAINERS
        // ═══════════════════════════════════════════════════════════════

        private static GameObject CreateMainCanvas()
        {
            GameObject canvasGO = new GameObject("UI_Canvas");
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create UI Canvas");

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            return canvasGO;
        }

        private static GameObject CreateContainer(Transform parent, string name)
        {
            GameObject container = new GameObject(name);
            container.transform.SetParent(parent, false);

            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return container;
        }

        private static GameObject CreateOverlay(Transform parent)
        {
            GameObject overlay = new GameObject("Overlay");
            overlay.transform.SetParent(parent, false);
            overlay.transform.SetAsFirstSibling();

            RectTransform rect = overlay.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image img = overlay.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.7f);

            // Add button to close popups when clicking overlay
            Button btn = overlay.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;

            return overlay;
        }

        // ═══════════════════════════════════════════════════════════════
        // HUD
        // ═══════════════════════════════════════════════════════════════

        private static GameObject CreateHUD(Transform parent)
        {
            GameObject hud = new GameObject("HUD");
            hud.transform.SetParent(parent, false);

            RectTransform hudRect = hud.AddComponent<RectTransform>();
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.offsetMin = Vector2.zero;
            hudRect.offsetMax = Vector2.zero;

            GameHUD gameHUD = hud.AddComponent<GameHUD>();
            SerializedObject hudSO = new SerializedObject(gameHUD);

            // Top Bar
            GameObject topBar = CreateTopBar(hud.transform);

            // Get references from top bar
            var checkingDisplay = topBar.transform.Find("CheckingDisplay")?.GetComponent<AccountDisplay>();
            var investingDisplay = topBar.transform.Find("InvestingDisplay")?.GetComponent<AccountDisplay>();
            var daySpeedDisplay = topBar.transform.Find("DaySpeedDisplay")?.GetComponent<DaySpeedDisplay>();
            var botProgressBar = topBar.transform.Find("BotProgressBar")?.GetComponent<BotProgressBar>();

            hudSO.FindProperty("_checkingDisplay").objectReferenceValue = checkingDisplay;
            hudSO.FindProperty("_investingDisplay").objectReferenceValue = investingDisplay;
            hudSO.FindProperty("_daySpeedDisplay").objectReferenceValue = daySpeedDisplay;
            hudSO.FindProperty("_botProgressBar").objectReferenceValue = botProgressBar;

            // Bottom Bar
            GameObject bottomBar = CreateBottomBar(hud.transform);

            // Get button references
            var portfolioBtn = bottomBar.transform.Find("PortfolioButton")?.GetComponent<Button>();
            var lotsBtn = bottomBar.transform.Find("LotsButton")?.GetComponent<Button>();
            var transferBtn = bottomBar.transform.Find("TransferButton")?.GetComponent<Button>();
            var restaurantBtn = bottomBar.transform.Find("RestaurantButton")?.GetComponent<Button>();

            hudSO.FindProperty("_portfolioButton").objectReferenceValue = portfolioBtn;
            hudSO.FindProperty("_lotsButton").objectReferenceValue = lotsBtn;
            hudSO.FindProperty("_transferButton").objectReferenceValue = transferBtn;
            hudSO.FindProperty("_restaurantButton").objectReferenceValue = restaurantBtn;

            hudSO.ApplyModifiedProperties();

            return hud;
        }

        private static GameObject CreateTopBar(Transform parent)
        {
            GameObject topBar = new GameObject("TopBar");
            topBar.transform.SetParent(parent, false);

            RectTransform rect = topBar.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0, 80);

            Image bg = topBar.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

            HorizontalLayoutGroup layout = topBar.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 10, 10);
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            // Checking Display
            CreateAccountDisplay(topBar.transform, "CheckingDisplay", "Checking", "$0.00");

            // Investing Display
            CreateAccountDisplay(topBar.transform, "InvestingDisplay", "Investing", "$0.00");

            // Spacer
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(topBar.transform, false);
            var spacerRect = spacer.AddComponent<RectTransform>();
            var spacerLayout = spacer.AddComponent<LayoutElement>();
            spacerLayout.flexibleWidth = 1;

            // Day Speed Display
            CreateDaySpeedDisplay(topBar.transform);

            // Bot Progress Bar
            CreateBotProgressBar(topBar.transform);

            return topBar;
        }

        private static void CreateAccountDisplay(Transform parent, string name, string label, string defaultValue)
        {
            GameObject display = new GameObject(name);
            display.transform.SetParent(parent, false);

            RectTransform rect = display.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(180, 60);

            VerticalLayoutGroup layout = display.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Label
            GameObject labelGO = CreateText(display.transform, "Label", label, 14, TMPro.TextAlignmentOptions.Center);
            labelGO.GetComponent<TMPro.TextMeshProUGUI>().color = new Color(0.7f, 0.7f, 0.7f);

            // Balance
            GameObject balanceGO = CreateText(display.transform, "Balance", defaultValue, 24, TMPro.TextAlignmentOptions.Center);
            balanceGO.GetComponent<TMPro.TextMeshProUGUI>().fontStyle = TMPro.FontStyles.Bold;

            // Add component
            AccountDisplay accountDisplay = display.AddComponent<AccountDisplay>();
            SerializedObject so = new SerializedObject(accountDisplay);
            so.FindProperty("_balanceText").objectReferenceValue = balanceGO.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_labelText").objectReferenceValue = labelGO.GetComponent<TMPro.TextMeshProUGUI>();
            so.ApplyModifiedProperties();
        }

        private static void CreateDaySpeedDisplay(Transform parent)
        {
            GameObject display = new GameObject("DaySpeedDisplay");
            display.transform.SetParent(parent, false);

            RectTransform rect = display.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 60);

            VerticalLayoutGroup layout = display.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            // Day text
            GameObject dayText = CreateText(display.transform, "DayText", "Day 1", 20, TMPro.TextAlignmentOptions.Center);

            // Speed buttons container
            GameObject speedButtons = new GameObject("SpeedButtons");
            speedButtons.transform.SetParent(display.transform, false);

            HorizontalLayoutGroup speedLayout = speedButtons.AddComponent<HorizontalLayoutGroup>();
            speedLayout.spacing = 5;
            speedLayout.childAlignment = TextAnchor.MiddleCenter;
            speedLayout.childControlWidth = false;
            speedLayout.childControlHeight = false;

            var pauseBtn = CreateSmallButton(speedButtons.transform, "PauseButton", "||");
            var speed1Btn = CreateSmallButton(speedButtons.transform, "Speed1Button", "1x");
            var speed2Btn = CreateSmallButton(speedButtons.transform, "Speed2Button", "2x");

            // Add component
            DaySpeedDisplay daySpeedDisplay = display.AddComponent<DaySpeedDisplay>();
            SerializedObject so = new SerializedObject(daySpeedDisplay);
            so.FindProperty("_dayText").objectReferenceValue = dayText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_pauseButton").objectReferenceValue = pauseBtn.GetComponent<Button>();
            so.FindProperty("_speed1xButton").objectReferenceValue = speed1Btn.GetComponent<Button>();
            so.FindProperty("_speed2xButton").objectReferenceValue = speed2Btn.GetComponent<Button>();
            so.ApplyModifiedProperties();
        }

        private static void CreateBotProgressBar(Transform parent)
        {
            GameObject display = new GameObject("BotProgressBar");
            display.transform.SetParent(parent, false);

            RectTransform rect = display.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 60);

            VerticalLayoutGroup layout = display.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.padding = new RectOffset(5, 5, 5, 5);
            layout.spacing = 2;

            // Label
            GameObject labelGO = CreateText(display.transform, "Label", "Rival Progress", 12, TMPro.TextAlignmentOptions.Center);
            labelGO.GetComponent<TMPro.TextMeshProUGUI>().color = new Color(0.9f, 0.3f, 0.3f);

            // Progress bar background
            GameObject progressBg = new GameObject("ProgressBackground");
            progressBg.transform.SetParent(display.transform, false);

            RectTransform progressBgRect = progressBg.AddComponent<RectTransform>();
            progressBgRect.sizeDelta = new Vector2(180, 20);

            Image progressBgImg = progressBg.AddComponent<Image>();
            progressBgImg.color = new Color(0.2f, 0.2f, 0.2f);

            // Progress fill
            GameObject progressFill = new GameObject("ProgressFill");
            progressFill.transform.SetParent(progressBg.transform, false);

            RectTransform fillRect = progressFill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0.5f, 1); // 50% for demo
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            Image fillImg = progressFill.AddComponent<Image>();
            fillImg.color = new Color(0.9f, 0.3f, 0.3f);

            // Status text
            GameObject statusText = CreateText(display.transform, "StatusText", "3 / 5 lots", 11, TMPro.TextAlignmentOptions.Center);

            // Add component
            BotProgressBar botProgress = display.AddComponent<BotProgressBar>();
            SerializedObject so = new SerializedObject(botProgress);
            so.FindProperty("_progressFillImage").objectReferenceValue = fillImg;
            so.FindProperty("_botLotsText").objectReferenceValue = statusText.GetComponent<TMPro.TextMeshProUGUI>();
            so.ApplyModifiedProperties();
        }

        private static GameObject CreateBottomBar(Transform parent)
        {
            GameObject bottomBar = new GameObject("BottomBar");
            bottomBar.transform.SetParent(parent, false);

            RectTransform rect = bottomBar.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0, 80);

            Image bg = bottomBar.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

            HorizontalLayoutGroup layout = bottomBar.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 10, 10);
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            // Navigation buttons
            CreateNavButton(bottomBar.transform, "PortfolioButton", "Portfolio");
            CreateNavButton(bottomBar.transform, "LotsButton", "Lots");
            CreateNavButton(bottomBar.transform, "TransferButton", "Transfer");
            CreateNavButton(bottomBar.transform, "RestaurantButton", "Restaurant");

            return bottomBar;
        }

        private static void CreateNavButton(Transform parent, string name, string label)
        {
            GameObject btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent, false);

            RectTransform rect = btnGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150, 50);

            Image img = btnGO.AddComponent<Image>();
            img.color = BUTTON_COLOR;

            Button btn = btnGO.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = BUTTON_COLOR;
            colors.highlightedColor = BUTTON_HOVER_COLOR;
            colors.pressedColor = new Color(0.15f, 0.4f, 0.7f);
            btn.colors = colors;

            // Text
            GameObject textGO = CreateText(btnGO.transform, "Text", label, 18, TMPro.TextAlignmentOptions.Center);
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        // ═══════════════════════════════════════════════════════════════
        // PANELS
        // ═══════════════════════════════════════════════════════════════

        private static GameObject CreatePortfolioPanel(Transform parent)
        {
            GameObject panel = CreatePanelBase(parent, "PortfolioPanel", "Investment Portfolio");
            PortfolioPanel portfolioPanel = panel.AddComponent<PortfolioPanel>();

            Transform content = panel.transform.Find("Content");

            // ── Summary row (4 stats across the top) ──
            GameObject summaryRow = new GameObject("SummaryRow");
            summaryRow.transform.SetParent(content, false);

            HorizontalLayoutGroup summaryLayout = summaryRow.AddComponent<HorizontalLayoutGroup>();
            summaryLayout.spacing = 15;
            summaryLayout.childControlWidth = true;
            summaryLayout.childControlHeight = true;
            summaryLayout.childForceExpandWidth = true;

            var summaryLayoutElem = summaryRow.AddComponent<LayoutElement>();
            summaryLayoutElem.preferredHeight = 40;

            var totalValueText = CreateText(summaryRow.transform, "TotalValueText", "Portfolio: $0", 16, TMPro.TextAlignmentOptions.Center);
            totalValueText.GetComponent<TMPro.TextMeshProUGUI>().fontStyle = TMPro.FontStyles.Bold;
            var totalGainText = CreateText(summaryRow.transform, "TotalGainText", "Gain: +$0", 16, TMPro.TextAlignmentOptions.Center);
            var balanceText = CreateText(summaryRow.transform, "BalanceText", "Cash: $0", 16, TMPro.TextAlignmentOptions.Center);
            var netWorthText = CreateText(summaryRow.transform, "NetWorthText", "Net Worth: $0", 16, TMPro.TextAlignmentOptions.Center);
            netWorthText.GetComponent<TMPro.TextMeshProUGUI>().fontStyle = TMPro.FontStyles.Bold;

            CreateSpacer(content, 5);

            // ── Main body (graph left, investments right) ──
            GameObject mainBody = new GameObject("MainBody");
            mainBody.transform.SetParent(content, false);

            HorizontalLayoutGroup mainLayout = mainBody.AddComponent<HorizontalLayoutGroup>();
            mainLayout.spacing = 15;
            mainLayout.childControlWidth = false;
            mainLayout.childControlHeight = true;
            mainLayout.childForceExpandWidth = false;
            mainLayout.childForceExpandHeight = true;

            var mainLayoutElem = mainBody.AddComponent<LayoutElement>();
            mainLayoutElem.flexibleHeight = 1;

            // ── Left side: Graph area (~35%) ──
            GameObject graphArea = new GameObject("GraphArea");
            graphArea.transform.SetParent(mainBody.transform, false);

            var graphAreaLayout = graphArea.AddComponent<LayoutElement>();
            graphAreaLayout.preferredWidth = 420;
            graphAreaLayout.flexibleHeight = 1;

            VerticalLayoutGroup graphVLayout = graphArea.AddComponent<VerticalLayoutGroup>();
            graphVLayout.spacing = 5;
            graphVLayout.childControlWidth = true;
            graphVLayout.childControlHeight = false;
            graphVLayout.childForceExpandWidth = true;

            var graphLabel = CreateText(graphArea.transform, "GraphLabel", "Performance", 14, TMPro.TextAlignmentOptions.Left);
            graphLabel.GetComponent<TMPro.TextMeshProUGUI>().fontStyle = TMPro.FontStyles.Bold;

            // RawImage for graph
            GameObject graphImageGO = new GameObject("GraphImage");
            graphImageGO.transform.SetParent(graphArea.transform, false);

            RectTransform graphRect = graphImageGO.AddComponent<RectTransform>();
            graphRect.sizeDelta = new Vector2(400, 200);

            RawImage graphRawImage = graphImageGO.AddComponent<RawImage>();
            graphRawImage.color = Color.white;

            var graphImgLayout = graphImageGO.AddComponent<LayoutElement>();
            graphImgLayout.preferredHeight = 200;
            graphImgLayout.flexibleWidth = 1;

            // Add PortfolioLineGraph component
            PortfolioLineGraph lineGraph = graphImageGO.AddComponent<PortfolioLineGraph>();
            SerializedObject lineGraphSO = new SerializedObject(lineGraph);
            lineGraphSO.FindProperty("_graphImage").objectReferenceValue = graphRawImage;
            lineGraphSO.ApplyModifiedProperties();

            // Legend
            GameObject legend = new GameObject("Legend");
            legend.transform.SetParent(graphArea.transform, false);

            HorizontalLayoutGroup legendLayout = legend.AddComponent<HorizontalLayoutGroup>();
            legendLayout.spacing = 20;
            legendLayout.childControlWidth = false;
            legendLayout.childControlHeight = true;

            var wealthLegend = CreateText(legend.transform, "WealthLegend", "--- Total Wealth", 11, TMPro.TextAlignmentOptions.Left);
            wealthLegend.GetComponent<TMPro.TextMeshProUGUI>().color = new Color(0.2f, 0.8f, 0.4f);

            var gainLegend = CreateText(legend.transform, "GainLegend", "--- Net Gain", 11, TMPro.TextAlignmentOptions.Left);
            gainLegend.GetComponent<TMPro.TextMeshProUGUI>().color = new Color(0.9f, 0.8f, 0.2f);

            // ── Right side: Investment list (~65%) ──
            GameObject listArea = new GameObject("ListArea");
            listArea.transform.SetParent(mainBody.transform, false);

            var listAreaLayout = listArea.AddComponent<LayoutElement>();
            listAreaLayout.flexibleWidth = 1;
            listAreaLayout.flexibleHeight = 1;

            VerticalLayoutGroup listVLayout = listArea.AddComponent<VerticalLayoutGroup>();
            listVLayout.spacing = 0;
            listVLayout.childControlWidth = true;
            listVLayout.childControlHeight = true;
            listVLayout.childForceExpandWidth = true;
            listVLayout.childForceExpandHeight = true;

            var investmentsLabel = CreateText(listArea.transform, "InvestmentsLabel", "Investments", 14, TMPro.TextAlignmentOptions.Left);
            investmentsLabel.GetComponent<TMPro.TextMeshProUGUI>().fontStyle = TMPro.FontStyles.Bold;
            var investLabelLayout = investmentsLabel.AddComponent<LayoutElement>();
            investLabelLayout.preferredHeight = 22;

            // Scrollable investment list
            GameObject investScroll = CreateScrollView(listArea.transform, "InvestmentScroll", 400);
            Transform investContainer = investScroll.transform.Find("Viewport/Content");

            // ── Create prefabs for InvestmentRow and SectionHeader ──
            string prefabPath = "Assets/Prefabs/UI";
            EnsurePrefabFolder(prefabPath);

            GameObject investmentRowPrefab = CreateInvestmentRowPrefab(prefabPath);
            GameObject sectionHeaderPrefab = CreateSectionHeaderPrefab(prefabPath);

            // ── Wire up all references ──
            SerializedObject so = new SerializedObject(portfolioPanel);
            so.FindProperty("_totalValueText").objectReferenceValue = totalValueText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_totalGainText").objectReferenceValue = totalGainText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_balanceText").objectReferenceValue = balanceText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_netWorthText").objectReferenceValue = netWorthText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_lineGraph").objectReferenceValue = lineGraph;
            so.FindProperty("_investmentListContainer").objectReferenceValue = investContainer;
            so.FindProperty("_investmentRowPrefab").objectReferenceValue = investmentRowPrefab != null ? investmentRowPrefab.GetComponent<InvestmentRow>() : null;
            so.FindProperty("_sectionHeaderPrefab").objectReferenceValue = sectionHeaderPrefab;
            so.FindProperty("_closeButton").objectReferenceValue = panel.transform.Find("Header/CloseButton")?.GetComponent<Button>();
            so.ApplyModifiedProperties();

            return panel;
        }

        private static void EnsurePrefabFolder(string prefabPath)
        {
            if (!AssetDatabase.IsValidFolder(prefabPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            }
        }

        /// <summary>
        /// Creates the InvestmentRow prefab with inline buy/sell buttons.
        /// </summary>
        private static GameObject CreateInvestmentRowPrefab(string prefabPath)
        {
            GameObject row = new GameObject("InvestmentRow");

            RectTransform rect = row.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 50);

            Image bg = row.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 0.6f);

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 6;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;

            var rowLayoutElem = row.AddComponent<LayoutElement>();
            rowLayoutElem.preferredHeight = 50;

            // Risk dot
            GameObject riskDot = new GameObject("RiskDot");
            riskDot.transform.SetParent(row.transform, false);
            RectTransform riskRect = riskDot.AddComponent<RectTransform>();
            riskRect.sizeDelta = new Vector2(8, 8);
            Image riskImg = riskDot.AddComponent<Image>();
            riskImg.color = Color.yellow;
            var riskLayout = riskDot.AddComponent<LayoutElement>();
            riskLayout.preferredWidth = 8;

            // Name
            var nameText = CreateText(row.transform, "NameText", "Investment", 12, TMPro.TextAlignmentOptions.Left);
            nameText.GetComponent<TMPro.TextMeshProUGUI>().fontStyle = TMPro.FontStyles.Bold;
            var nameLayout = nameText.AddComponent<LayoutElement>();
            nameLayout.preferredWidth = 130;

            // Price
            var priceText = CreateText(row.transform, "PriceText", "$0.00", 11, TMPro.TextAlignmentOptions.Right);
            var priceLayout = priceText.AddComponent<LayoutElement>();
            priceLayout.preferredWidth = 60;

            // Shares
            var sharesText = CreateText(row.transform, "SharesText", "0", 11, TMPro.TextAlignmentOptions.Center);
            var sharesLayout = sharesText.AddComponent<LayoutElement>();
            sharesLayout.preferredWidth = 35;

            // Value
            var valueText = CreateText(row.transform, "ValueText", "-", 11, TMPro.TextAlignmentOptions.Right);
            var valueLayout = valueText.AddComponent<LayoutElement>();
            valueLayout.preferredWidth = 65;

            // Gain
            var gainText = CreateText(row.transform, "GainText", "-", 11, TMPro.TextAlignmentOptions.Right);
            gainText.GetComponent<TMPro.TextMeshProUGUI>().color = ACCENT_COLOR;
            var gainLayout = gainText.AddComponent<LayoutElement>();
            gainLayout.preferredWidth = 85;

            // Fixed return text (hidden by default)
            var fixedReturnText = CreateText(row.transform, "FixedReturnText", "5.0% APY", 10, TMPro.TextAlignmentOptions.Center);
            fixedReturnText.GetComponent<TMPro.TextMeshProUGUI>().color = new Color(0.5f, 0.8f, 1f);
            var fixedReturnLayout = fixedReturnText.AddComponent<LayoutElement>();
            fixedReturnLayout.preferredWidth = 55;
            fixedReturnText.SetActive(false);

            // Buy buttons container
            GameObject buyContainer = new GameObject("BuyButtons");
            buyContainer.transform.SetParent(row.transform, false);

            HorizontalLayoutGroup buyLayout = buyContainer.AddComponent<HorizontalLayoutGroup>();
            buyLayout.spacing = 2;
            buyLayout.childControlWidth = false;
            buyLayout.childControlHeight = true;
            buyLayout.childAlignment = TextAnchor.MiddleCenter;

            var buyContainerLayout = buyContainer.AddComponent<LayoutElement>();
            buyContainerLayout.preferredWidth = 140;

            var buy1Btn = CreateSmallButton(buyContainer.transform, "Buy1", "+1");
            var buy5Btn = CreateSmallButton(buyContainer.transform, "Buy5", "+5");
            var buy50Btn = CreateSmallButton(buyContainer.transform, "Buy50", "+50");
            var buyMaxBtn = CreateSmallButton(buyContainer.transform, "BuyMax", "Max");

            // Sell buttons container
            GameObject sellContainer = new GameObject("SellButtons");
            sellContainer.transform.SetParent(row.transform, false);

            HorizontalLayoutGroup sellLayout = sellContainer.AddComponent<HorizontalLayoutGroup>();
            sellLayout.spacing = 2;
            sellLayout.childControlWidth = false;
            sellLayout.childControlHeight = true;
            sellLayout.childAlignment = TextAnchor.MiddleCenter;

            var sellContainerLayout = sellContainer.AddComponent<LayoutElement>();
            sellContainerLayout.preferredWidth = 140;

            var sell1Btn = CreateSmallButton(sellContainer.transform, "Sell1", "-1");
            var sell5Btn = CreateSmallButton(sellContainer.transform, "Sell5", "-5");
            var sell50Btn = CreateSmallButton(sellContainer.transform, "Sell50", "-50");
            var sellAllBtn = CreateSmallButton(sellContainer.transform, "SellAll", "All");

            // Color sell buttons red-ish
            Color sellBtnColor = new Color(0.35f, 0.2f, 0.2f);
            SetButtonColor(sell1Btn, sellBtnColor);
            SetButtonColor(sell5Btn, sellBtnColor);
            SetButtonColor(sell50Btn, sellBtnColor);
            SetButtonColor(sellAllBtn, sellBtnColor);

            // Add InvestmentRow component
            InvestmentRow investmentRow = row.AddComponent<InvestmentRow>();
            SerializedObject rowSO = new SerializedObject(investmentRow);
            rowSO.FindProperty("_nameText").objectReferenceValue = nameText.GetComponent<TMPro.TextMeshProUGUI>();
            rowSO.FindProperty("_priceText").objectReferenceValue = priceText.GetComponent<TMPro.TextMeshProUGUI>();
            rowSO.FindProperty("_sharesText").objectReferenceValue = sharesText.GetComponent<TMPro.TextMeshProUGUI>();
            rowSO.FindProperty("_valueText").objectReferenceValue = valueText.GetComponent<TMPro.TextMeshProUGUI>();
            rowSO.FindProperty("_gainText").objectReferenceValue = gainText.GetComponent<TMPro.TextMeshProUGUI>();
            rowSO.FindProperty("_fixedReturnText").objectReferenceValue = fixedReturnText.GetComponent<TMPro.TextMeshProUGUI>();
            rowSO.FindProperty("_riskDot").objectReferenceValue = riskImg;
            rowSO.FindProperty("_buy1Button").objectReferenceValue = buy1Btn.GetComponent<Button>();
            rowSO.FindProperty("_buy5Button").objectReferenceValue = buy5Btn.GetComponent<Button>();
            rowSO.FindProperty("_buy50Button").objectReferenceValue = buy50Btn.GetComponent<Button>();
            rowSO.FindProperty("_buyMaxButton").objectReferenceValue = buyMaxBtn.GetComponent<Button>();
            rowSO.FindProperty("_sell1Button").objectReferenceValue = sell1Btn.GetComponent<Button>();
            rowSO.FindProperty("_sell5Button").objectReferenceValue = sell5Btn.GetComponent<Button>();
            rowSO.FindProperty("_sell50Button").objectReferenceValue = sell50Btn.GetComponent<Button>();
            rowSO.FindProperty("_sellAllButton").objectReferenceValue = sellAllBtn.GetComponent<Button>();
            rowSO.FindProperty("_sellButtonsContainer").objectReferenceValue = sellContainer;
            rowSO.ApplyModifiedProperties();

            // Save prefab
            string path = $"{prefabPath}/InvestmentRow.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(row, path);
            DestroyImmediate(row);

            Debug.Log($"[UISetupWizard] Created prefab: {path}");
            return prefab;
        }

        /// <summary>
        /// Creates a simple section header prefab (bold label).
        /// </summary>
        private static GameObject CreateSectionHeaderPrefab(string prefabPath)
        {
            GameObject header = new GameObject("SectionHeader");

            RectTransform rect = header.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 28);

            Image bg = header.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.18f, 0.8f);

            var layoutElem = header.AddComponent<LayoutElement>();
            layoutElem.preferredHeight = 28;

            HorizontalLayoutGroup hlayout = header.AddComponent<HorizontalLayoutGroup>();
            hlayout.padding = new RectOffset(10, 10, 4, 4);
            hlayout.childControlWidth = true;
            hlayout.childControlHeight = true;
            hlayout.childForceExpandWidth = true;

            var labelText = CreateText(header.transform, "Label", "Category", 14, TMPro.TextAlignmentOptions.Left);
            labelText.GetComponent<TMPro.TextMeshProUGUI>().fontStyle = TMPro.FontStyles.Bold;
            labelText.GetComponent<TMPro.TextMeshProUGUI>().color = new Color(0.6f, 0.75f, 1f);

            // Save prefab
            string path = $"{prefabPath}/SectionHeader.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(header, path);
            DestroyImmediate(header);

            Debug.Log($"[UISetupWizard] Created prefab: {path}");
            return prefab;
        }

        private static void SetButtonColor(GameObject btnGO, Color color)
        {
            Image img = btnGO.GetComponent<Image>();
            if (img != null) img.color = color;

            Button btn = btnGO.GetComponent<Button>();
            if (btn != null)
            {
                ColorBlock cb = btn.colors;
                cb.normalColor = color;
                cb.highlightedColor = color * 1.2f;
                cb.pressedColor = color * 0.8f;
                btn.colors = cb;
            }
        }

        private static GameObject CreateLotsPanel(Transform parent)
        {
            GameObject panel = CreatePanelBase(parent, "LotsPanel", "City Lots");
            LotsPanel lotsPanel = panel.AddComponent<LotsPanel>();

            Transform content = panel.transform.Find("Content");

            // Filter section
            GameObject filters = new GameObject("Filters");
            filters.transform.SetParent(content, false);

            HorizontalLayoutGroup filterLayout = filters.AddComponent<HorizontalLayoutGroup>();
            filterLayout.spacing = 10;
            filterLayout.childControlWidth = false;
            filterLayout.childControlHeight = true;

            var showAllToggle = CreateToggle(filters.transform, "ShowAllToggle", "All", true);
            var showAvailableToggle = CreateToggle(filters.transform, "ShowAvailableToggle", "Available", false);
            var showOwnedToggle = CreateToggle(filters.transform, "ShowOwnedToggle", "Owned", false);

            // Sort dropdown
            var sortDropdown = CreateDropdown(filters.transform, "SortDropdown", new[] { "Price", "Income", "Name" });

            CreateSpacer(content, 10);

            // Summary text
            var summaryText = CreateText(content, "SummaryText", "Lots: 0 owned / 0 available / 0 rival", 14, TMPro.TextAlignmentOptions.Left);

            CreateSpacer(content, 10);

            // Lots list (scrollable)
            GameObject lotsScroll = CreateScrollView(content, "LotsScroll", 350);
            Transform lotsContainer = lotsScroll.transform.Find("Viewport/Content");

            // Wire up references
            SerializedObject so = new SerializedObject(lotsPanel);
            so.FindProperty("_listContainer").objectReferenceValue = lotsContainer;
            so.FindProperty("_showAllToggle").objectReferenceValue = showAllToggle.GetComponent<Toggle>();
            so.FindProperty("_showAvailableToggle").objectReferenceValue = showAvailableToggle.GetComponent<Toggle>();
            so.FindProperty("_showOwnedToggle").objectReferenceValue = showOwnedToggle.GetComponent<Toggle>();
            so.FindProperty("_sortDropdown").objectReferenceValue = sortDropdown.GetComponent<TMPro.TMP_Dropdown>();
            so.FindProperty("_summaryText").objectReferenceValue = summaryText.GetComponent<TMPro.TextMeshProUGUI>();
            so.ApplyModifiedProperties();

            return panel;
        }

        private static GameObject CreatePanelBase(Transform parent, string name, string title)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.1f);
            rect.anchorMax = new Vector2(0.9f, 0.85f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = panel.AddComponent<Image>();
            bg.color = PANEL_BG_COLOR;

            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 10, 20);
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Header
            GameObject header = new GameObject("Header");
            header.transform.SetParent(panel.transform, false);

            RectTransform headerRect = header.AddComponent<RectTransform>();
            headerRect.sizeDelta = new Vector2(0, 50);

            HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.childControlWidth = false;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = true;

            // Title
            var titleText = CreateText(header.transform, "Title", title, 28, TMPro.TextAlignmentOptions.Left);
            titleText.GetComponent<TMPro.TextMeshProUGUI>().fontStyle = TMPro.FontStyles.Bold;
            var titleLayout = titleText.AddComponent<LayoutElement>();
            titleLayout.flexibleWidth = 1;

            // Close button
            var closeBtn = CreateButton(header.transform, "CloseButton", "X", 40, 40);

            // Content area
            GameObject content = new GameObject("Content");
            content.transform.SetParent(panel.transform, false);

            RectTransform contentRect = content.AddComponent<RectTransform>();

            VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 10;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var contentLayoutElem = content.AddComponent<LayoutElement>();
            contentLayoutElem.flexibleHeight = 1;

            return panel;
        }

        // ═══════════════════════════════════════════════════════════════
        // POPUPS
        // ═══════════════════════════════════════════════════════════════

        private static GameObject CreateLotPurchasePopup(Transform parent)
        {
            GameObject popup = CreatePopupBase(parent, "LotPurchasePopup", "Purchase Lot");
            LotPurchasePopup lotPopup = popup.AddComponent<LotPurchasePopup>();

            Transform content = popup.transform.Find("Content");

            // Lot info
            var lotNameText = CreateText(content, "LotNameText", "Downtown Corner", 22, TMPro.TextAlignmentOptions.Center);
            lotNameText.GetComponent<TMPro.TextMeshProUGUI>().fontStyle = TMPro.FontStyles.Bold;

            var lotDescText = CreateText(content, "LotDescriptionText", "A prime location in the city center.", 14, TMPro.TextAlignmentOptions.Center);

            CreateSpacer(content, 15);

            // Economics
            var costText = CreateText(content, "CostText", "Cost: $2,500", 18, TMPro.TextAlignmentOptions.Left);
            var incomeBonusText = CreateText(content, "IncomeBonusText", "Income: +$15/day", 16, TMPro.TextAlignmentOptions.Left);
            var roiText = CreateText(content, "ROIText", "Payback: ~167 days", 14, TMPro.TextAlignmentOptions.Left);

            CreateSpacer(content, 15);

            // Balance info
            var balanceText = CreateText(content, "BalanceText", "Your Checking: $1,000", 16, TMPro.TextAlignmentOptions.Left);
            var affordabilityText = CreateText(content, "AffordabilityText", "You can afford this!", 14, TMPro.TextAlignmentOptions.Center);
            affordabilityText.GetComponent<TMPro.TextMeshProUGUI>().color = ACCENT_COLOR;

            CreateSpacer(content, 20);

            // Buttons
            GameObject buttons = CreateButtonRow(content);
            var buyButton = CreateButton(buttons.transform, "BuyButton", "Buy", 120, 45);
            var cancelButton = CreateButton(buttons.transform, "CancelButton", "Cancel", 120, 45);

            // Wire up
            SerializedObject so = new SerializedObject(lotPopup);
            so.FindProperty("_lotNameText").objectReferenceValue = lotNameText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_lotDescriptionText").objectReferenceValue = lotDescText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_costText").objectReferenceValue = costText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_incomeBonusText").objectReferenceValue = incomeBonusText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_roiText").objectReferenceValue = roiText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_balanceText").objectReferenceValue = balanceText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_affordabilityText").objectReferenceValue = affordabilityText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_buyButton").objectReferenceValue = buyButton.GetComponent<Button>();
            so.FindProperty("_cancelButton").objectReferenceValue = cancelButton.GetComponent<Button>();
            so.FindProperty("_buyButtonText").objectReferenceValue = buyButton.transform.Find("Text")?.GetComponent<TMPro.TextMeshProUGUI>();
            so.ApplyModifiedProperties();

            return popup;
        }

        private static GameObject CreateBuyInvestmentPopup(Transform parent)
        {
            GameObject popup = CreatePopupBase(parent, "BuyInvestmentPopup", "Buy Investment");
            BuyInvestmentPopup buyPopup = popup.AddComponent<BuyInvestmentPopup>();

            Transform content = popup.transform.Find("Content");

            // Investment dropdown
            var dropdown = CreateDropdown(content, "InvestmentDropdown", new[] { "Select Investment..." });

            CreateSpacer(content, 10);

            // Investment info
            var nameText = CreateText(content, "InvestmentNameText", "S&P 500 Index", 20, TMPro.TextAlignmentOptions.Left);
            nameText.GetComponent<TMPro.TextMeshProUGUI>().fontStyle = TMPro.FontStyles.Bold;

            var descText = CreateText(content, "DescriptionText", "Tracks the 500 largest US companies.", 14, TMPro.TextAlignmentOptions.Left);

            CreateSpacer(content, 10);

            // Risk and return
            var riskText = CreateText(content, "RiskText", "Low Risk - Stable returns", 14, TMPro.TextAlignmentOptions.Left);
            var returnText = CreateText(content, "ReturnText", "Expected Return: ~10% per year", 14, TMPro.TextAlignmentOptions.Left);
            var minimumText = CreateText(content, "MinimumText", "Minimum: $100", 14, TMPro.TextAlignmentOptions.Left);

            CreateSpacer(content, 15);

            // Amount input
            var amountLabel = CreateText(content, "AmountLabel", "Amount to Invest:", 14, TMPro.TextAlignmentOptions.Left);

            GameObject amountRow = new GameObject("AmountRow");
            amountRow.transform.SetParent(content, false);
            HorizontalLayoutGroup amountLayout = amountRow.AddComponent<HorizontalLayoutGroup>();
            amountLayout.spacing = 10;
            amountLayout.childControlWidth = false;
            amountLayout.childControlHeight = true;

            var amountInput = CreateInputField(amountRow.transform, "AmountInput", "0.00", 150, 40);
            var maxButton = CreateButton(amountRow.transform, "MaxButton", "Max", 60, 40);

            // Slider
            var amountSlider = CreateSlider(content, "AmountSlider");

            CreateSpacer(content, 10);

            // Balance and projection
            var balanceText = CreateText(content, "InvestingBalanceText", "Available: $500.00", 14, TMPro.TextAlignmentOptions.Left);
            var projectionText = CreateText(content, "ProjectionText", "Enter an amount to see projection", 12, TMPro.TextAlignmentOptions.Left);
            projectionText.GetComponent<TMPro.TextMeshProUGUI>().color = new Color(0.7f, 0.7f, 0.7f);

            CreateSpacer(content, 15);

            // Buttons
            GameObject buttons = CreateButtonRow(content);
            var buyButton = CreateButton(buttons.transform, "BuyButton", "Buy", 120, 45);
            var cancelButton = CreateButton(buttons.transform, "CancelButton", "Cancel", 120, 45);

            // Wire up
            SerializedObject so = new SerializedObject(buyPopup);
            so.FindProperty("_investmentDropdown").objectReferenceValue = dropdown.GetComponent<TMPro.TMP_Dropdown>();
            so.FindProperty("_investmentNameText").objectReferenceValue = nameText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_investmentDescriptionText").objectReferenceValue = descText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_riskText").objectReferenceValue = riskText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_returnText").objectReferenceValue = returnText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_minimumText").objectReferenceValue = minimumText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_amountInput").objectReferenceValue = amountInput.GetComponent<TMPro.TMP_InputField>();
            so.FindProperty("_amountSlider").objectReferenceValue = amountSlider.GetComponent<Slider>();
            so.FindProperty("_maxButton").objectReferenceValue = maxButton.GetComponent<Button>();
            so.FindProperty("_investingBalanceText").objectReferenceValue = balanceText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_projectionText").objectReferenceValue = projectionText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_buyButton").objectReferenceValue = buyButton.GetComponent<Button>();
            so.FindProperty("_cancelButton").objectReferenceValue = cancelButton.GetComponent<Button>();
            so.FindProperty("_buyButtonText").objectReferenceValue = buyButton.transform.Find("Text")?.GetComponent<TMPro.TextMeshProUGUI>();
            so.ApplyModifiedProperties();

            return popup;
        }

        private static GameObject CreateSellInvestmentPopup(Transform parent)
        {
            GameObject popup = CreatePopupBase(parent, "SellInvestmentPopup", "Sell Investment");
            SellInvestmentPopup sellPopup = popup.AddComponent<SellInvestmentPopup>();

            Transform content = popup.transform.Find("Content");

            // Investment info
            var nameText = CreateText(content, "InvestmentNameText", "S&P 500 Index", 22, TMPro.TextAlignmentOptions.Center);
            nameText.GetComponent<TMPro.TextMeshProUGUI>().fontStyle = TMPro.FontStyles.Bold;

            var typeText = CreateText(content, "InvestmentTypeText", "Low Risk", 14, TMPro.TextAlignmentOptions.Center);

            CreateSpacer(content, 15);

            // Value display
            var principalText = CreateText(content, "PrincipalText", "Original Investment: $100.00", 16, TMPro.TextAlignmentOptions.Left);
            var currentValueText = CreateText(content, "CurrentValueText", "Current Value: $112.50", 18, TMPro.TextAlignmentOptions.Left);
            currentValueText.GetComponent<TMPro.TextMeshProUGUI>().fontStyle = TMPro.FontStyles.Bold;

            CreateSpacer(content, 10);

            // Gain/loss
            var gainLossText = CreateText(content, "GainLossText", "Total Gain: +$12.50", 16, TMPro.TextAlignmentOptions.Left);
            gainLossText.GetComponent<TMPro.TextMeshProUGUI>().color = ACCENT_COLOR;

            var percentReturnText = CreateText(content, "PercentReturnText", "Return: +12.5%", 14, TMPro.TextAlignmentOptions.Left);
            percentReturnText.GetComponent<TMPro.TextMeshProUGUI>().color = ACCENT_COLOR;

            CreateSpacer(content, 10);

            // Time held
            var daysHeldText = CreateText(content, "DaysHeldText", "Days Held: 45", 14, TMPro.TextAlignmentOptions.Left);
            var compoundsText = CreateText(content, "CompoundsText", "Compound Events: 1", 14, TMPro.TextAlignmentOptions.Left);

            CreateSpacer(content, 10);

            // Explanation
            var explanationText = CreateText(content, "ExplanationText",
                "Your investment has grown due to compound interest. The longer you hold, the more it can grow!",
                12, TMPro.TextAlignmentOptions.Left);
            explanationText.GetComponent<TMPro.TextMeshProUGUI>().color = new Color(0.7f, 0.7f, 0.7f);

            CreateSpacer(content, 20);

            // Buttons
            GameObject buttons = CreateButtonRow(content);
            var sellButton = CreateButton(buttons.transform, "SellButton", "Sell for $112.50", 180, 45);
            var cancelButton = CreateButton(buttons.transform, "CancelButton", "Keep", 100, 45);

            // Wire up
            SerializedObject so = new SerializedObject(sellPopup);
            so.FindProperty("_investmentNameText").objectReferenceValue = nameText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_investmentTypeText").objectReferenceValue = typeText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_principalText").objectReferenceValue = principalText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_currentValueText").objectReferenceValue = currentValueText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_gainLossText").objectReferenceValue = gainLossText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_percentReturnText").objectReferenceValue = percentReturnText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_daysHeldText").objectReferenceValue = daysHeldText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_compoundsText").objectReferenceValue = compoundsText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_explanationText").objectReferenceValue = explanationText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_sellButton").objectReferenceValue = sellButton.GetComponent<Button>();
            so.FindProperty("_cancelButton").objectReferenceValue = cancelButton.GetComponent<Button>();
            so.FindProperty("_sellButtonText").objectReferenceValue = sellButton.transform.Find("Text")?.GetComponent<TMPro.TextMeshProUGUI>();
            so.ApplyModifiedProperties();

            return popup;
        }

        private static GameObject CreateTransferPopup(Transform parent)
        {
            GameObject popup = CreatePopupBase(parent, "TransferPopup", "Transfer Funds");
            TransferPopup transferPopup = popup.AddComponent<TransferPopup>();

            Transform content = popup.transform.Find("Content");

            // From account
            var fromLabel = CreateText(content, "FromLabel", "From:", 14, TMPro.TextAlignmentOptions.Left);
            var fromDropdown = CreateDropdown(content, "FromAccountDropdown", new[] { "Checking", "Investing" });
            var fromBalanceText = CreateText(content, "FromBalanceText", "Checking: $500.00", 12, TMPro.TextAlignmentOptions.Left);

            CreateSpacer(content, 10);

            // To account
            var toLabel = CreateText(content, "ToLabel", "To:", 14, TMPro.TextAlignmentOptions.Left);
            var toDropdown = CreateDropdown(content, "ToAccountDropdown", new[] { "Checking", "Investing" });
            var toBalanceText = CreateText(content, "ToBalanceText", "Investing: $500.00", 12, TMPro.TextAlignmentOptions.Left);

            CreateSpacer(content, 15);

            // Amount
            var amountLabel = CreateText(content, "AmountLabel", "Amount:", 14, TMPro.TextAlignmentOptions.Left);

            GameObject amountRow = new GameObject("AmountRow");
            amountRow.transform.SetParent(content, false);
            HorizontalLayoutGroup amountLayout = amountRow.AddComponent<HorizontalLayoutGroup>();
            amountLayout.spacing = 10;
            amountLayout.childControlWidth = false;
            amountLayout.childControlHeight = true;

            var amountInput = CreateInputField(amountRow.transform, "AmountInput", "0.00", 150, 40);
            var maxButton = CreateButton(amountRow.transform, "MaxButton", "Max", 60, 40);

            var amountSlider = CreateSlider(content, "AmountSlider");

            CreateSpacer(content, 10);

            // Preview
            var previewText = CreateText(content, "PreviewText", "Enter an amount to transfer", 14, TMPro.TextAlignmentOptions.Center);
            previewText.GetComponent<TMPro.TextMeshProUGUI>().color = new Color(0.7f, 0.7f, 0.7f);

            CreateSpacer(content, 20);

            // Buttons
            GameObject buttons = CreateButtonRow(content);
            var transferButton = CreateButton(buttons.transform, "TransferButton", "Transfer", 120, 45);
            var cancelButton = CreateButton(buttons.transform, "CancelButton", "Cancel", 120, 45);

            // Wire up
            SerializedObject so = new SerializedObject(transferPopup);
            so.FindProperty("_fromAccountDropdown").objectReferenceValue = fromDropdown.GetComponent<TMPro.TMP_Dropdown>();
            so.FindProperty("_toAccountDropdown").objectReferenceValue = toDropdown.GetComponent<TMPro.TMP_Dropdown>();
            so.FindProperty("_fromBalanceText").objectReferenceValue = fromBalanceText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_toBalanceText").objectReferenceValue = toBalanceText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_amountInput").objectReferenceValue = amountInput.GetComponent<TMPro.TMP_InputField>();
            so.FindProperty("_amountSlider").objectReferenceValue = amountSlider.GetComponent<Slider>();
            so.FindProperty("_maxButton").objectReferenceValue = maxButton.GetComponent<Button>();
            so.FindProperty("_previewText").objectReferenceValue = previewText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_transferButton").objectReferenceValue = transferButton.GetComponent<Button>();
            so.FindProperty("_cancelButton").objectReferenceValue = cancelButton.GetComponent<Button>();
            so.ApplyModifiedProperties();

            return popup;
        }

        private static GameObject CreatePopupBase(Transform parent, string name, string title)
        {
            GameObject popup = new GameObject(name);
            popup.transform.SetParent(parent, false);

            RectTransform rect = popup.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(450, 550);

            Image bg = popup.AddComponent<Image>();
            bg.color = POPUP_BG_COLOR;

            VerticalLayoutGroup layout = popup.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(25, 25, 15, 25);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Title
            var titleText = CreateText(popup.transform, "Title", title, 26, TMPro.TextAlignmentOptions.Center);
            titleText.GetComponent<TMPro.TextMeshProUGUI>().fontStyle = TMPro.FontStyles.Bold;

            // Divider
            GameObject divider = new GameObject("Divider");
            divider.transform.SetParent(popup.transform, false);
            RectTransform divRect = divider.AddComponent<RectTransform>();
            divRect.sizeDelta = new Vector2(0, 2);
            Image divImg = divider.AddComponent<Image>();
            divImg.color = new Color(0.3f, 0.3f, 0.35f);

            // Content container
            GameObject content = new GameObject("Content");
            content.transform.SetParent(popup.transform, false);

            VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 5;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandWidth = true;

            var contentLayoutElem = content.AddComponent<LayoutElement>();
            contentLayoutElem.flexibleHeight = 1;

            return popup;
        }

        // ═══════════════════════════════════════════════════════════════
        // LIST ITEM PREFABS
        // ═══════════════════════════════════════════════════════════════

        private static void CreateInvestmentListItemPrefab(string prefabPath)
        {
            GameObject item = new GameObject("InvestmentListItem");

            RectTransform rect = item.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 70);

            Image bg = item.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);

            HorizontalLayoutGroup layout = item.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(15, 15, 10, 10);
            layout.spacing = 15;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;

            // Info section
            GameObject info = new GameObject("Info");
            info.transform.SetParent(item.transform, false);

            VerticalLayoutGroup infoLayout = info.AddComponent<VerticalLayoutGroup>();
            infoLayout.childControlWidth = true;
            infoLayout.childControlHeight = true;

            var infoLayoutElem = info.AddComponent<LayoutElement>();
            infoLayoutElem.flexibleWidth = 1;

            var nameText = CreateText(info.transform, "NameText", "Investment Name", 16, TMPro.TextAlignmentOptions.Left);
            nameText.GetComponent<TMPro.TextMeshProUGUI>().fontStyle = TMPro.FontStyles.Bold;

            var valueText = CreateText(info.transform, "ValueText", "$0.00", 14, TMPro.TextAlignmentOptions.Left);

            // Gain section
            GameObject gainSection = new GameObject("GainSection");
            gainSection.transform.SetParent(item.transform, false);

            VerticalLayoutGroup gainLayout = gainSection.AddComponent<VerticalLayoutGroup>();
            gainLayout.childControlWidth = true;
            gainLayout.childControlHeight = true;
            gainLayout.childAlignment = TextAnchor.MiddleRight;

            var gainLayoutElem = gainSection.AddComponent<LayoutElement>();
            gainLayoutElem.preferredWidth = 100;

            var gainText = CreateText(gainSection.transform, "GainText", "+$0.00 (+0.0%)", 14, TMPro.TextAlignmentOptions.Right);
            gainText.GetComponent<TMPro.TextMeshProUGUI>().color = ACCENT_COLOR;

            // Sell button
            var sellBtn = CreateButton(item.transform, "SellButton", "Sell", 70, 40);

            // Add component
            InvestmentListItem listItem = item.AddComponent<InvestmentListItem>();
            SerializedObject so = new SerializedObject(listItem);
            so.FindProperty("_nameText").objectReferenceValue = nameText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_valueText").objectReferenceValue = valueText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_gainText").objectReferenceValue = gainText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_sellButton").objectReferenceValue = sellBtn.GetComponent<Button>();
            so.ApplyModifiedProperties();

            // Make clickable
            Button itemBtn = item.AddComponent<Button>();
            itemBtn.targetGraphic = bg;

            // Save prefab
            string path = $"{prefabPath}/InvestmentListItem.prefab";
            PrefabUtility.SaveAsPrefabAsset(item, path);
            DestroyImmediate(item);

            Debug.Log($"[UISetupWizard] Created prefab: {path}");
        }

        private static void CreateLotListItemPrefab(string prefabPath)
        {
            GameObject item = new GameObject("LotListItem");

            RectTransform rect = item.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 80);

            Image bg = item.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);

            HorizontalLayoutGroup layout = item.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(15, 15, 10, 10);
            layout.spacing = 15;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;

            // Status indicator
            GameObject statusIndicator = new GameObject("StatusIndicator");
            statusIndicator.transform.SetParent(item.transform, false);

            RectTransform statusRect = statusIndicator.AddComponent<RectTransform>();
            statusRect.sizeDelta = new Vector2(10, 60);

            Image statusImg = statusIndicator.AddComponent<Image>();
            statusImg.color = ACCENT_COLOR;

            // Info section
            GameObject info = new GameObject("Info");
            info.transform.SetParent(item.transform, false);

            VerticalLayoutGroup infoLayout = info.AddComponent<VerticalLayoutGroup>();
            infoLayout.childControlWidth = true;
            infoLayout.childControlHeight = true;

            var infoLayoutElem = info.AddComponent<LayoutElement>();
            infoLayoutElem.flexibleWidth = 1;

            var nameText = CreateText(info.transform, "NameText", "Lot Name", 16, TMPro.TextAlignmentOptions.Left);
            nameText.GetComponent<TMPro.TextMeshProUGUI>().fontStyle = TMPro.FontStyles.Bold;

            var priceText = CreateText(info.transform, "PriceText", "Cost: $0", 14, TMPro.TextAlignmentOptions.Left);
            var incomeText = CreateText(info.transform, "IncomeText", "Income: +$0/day", 12, TMPro.TextAlignmentOptions.Left);

            // Status text
            GameObject statusSection = new GameObject("StatusSection");
            statusSection.transform.SetParent(item.transform, false);

            var statusLayoutElem = statusSection.AddComponent<LayoutElement>();
            statusLayoutElem.preferredWidth = 80;

            var statusText = CreateText(statusSection.transform, "StatusText", "Available", 14, TMPro.TextAlignmentOptions.Center);
            RectTransform statusTextRect = statusText.GetComponent<RectTransform>();
            statusTextRect.anchorMin = Vector2.zero;
            statusTextRect.anchorMax = Vector2.one;
            statusTextRect.offsetMin = Vector2.zero;
            statusTextRect.offsetMax = Vector2.zero;

            // Add component
            LotListItem listItem = item.AddComponent<LotListItem>();
            SerializedObject so = new SerializedObject(listItem);
            so.FindProperty("_nameText").objectReferenceValue = nameText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_priceText").objectReferenceValue = priceText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_incomeText").objectReferenceValue = incomeText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_statusText").objectReferenceValue = statusText.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("_statusIcon").objectReferenceValue = statusImg;
            so.ApplyModifiedProperties();

            // Make clickable
            Button itemBtn = item.AddComponent<Button>();
            itemBtn.targetGraphic = bg;

            // Save prefab
            string path = $"{prefabPath}/LotListItem.prefab";
            PrefabUtility.SaveAsPrefabAsset(item, path);
            DestroyImmediate(item);

            Debug.Log($"[UISetupWizard] Created prefab: {path}");
        }

        // ═══════════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════════

        private static GameObject CreateText(Transform parent, string name, string text, int fontSize, TMPro.TextAlignmentOptions alignment)
        {
            GameObject textGO = new GameObject(name);
            textGO.transform.SetParent(parent, false);

            RectTransform rect = textGO.AddComponent<RectTransform>();

            TMPro.TextMeshProUGUI tmp = textGO.AddComponent<TMPro.TextMeshProUGUI>();

            // Load the default TMP font
            var defaultFont = TMPro.TMP_Settings.defaultFontAsset;
            if (defaultFont == null)
            {
                // Try to find any TMP font asset in the project
                string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    defaultFont = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(path);
                }
            }

            if (defaultFont != null)
            {
                tmp.font = defaultFont;
            }

            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = TEXT_COLOR;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TMPro.TextWrappingModes.Normal;

            return textGO;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, float width, float height)
        {
            GameObject btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent, false);

            RectTransform rect = btnGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);

            Image img = btnGO.AddComponent<Image>();
            img.color = BUTTON_COLOR;

            Button btn = btnGO.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = BUTTON_COLOR;
            colors.highlightedColor = BUTTON_HOVER_COLOR;
            colors.pressedColor = new Color(0.15f, 0.4f, 0.7f);
            btn.colors = colors;

            var layoutElem = btnGO.AddComponent<LayoutElement>();
            layoutElem.preferredWidth = width;
            layoutElem.preferredHeight = height;

            // Text
            GameObject textGO = CreateText(btnGO.transform, "Text", label, 16, TMPro.TextAlignmentOptions.Center);
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return btnGO;
        }

        private static GameObject CreateSmallButton(Transform parent, string name, string label)
        {
            GameObject btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent, false);

            RectTransform rect = btnGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(35, 25);

            Image img = btnGO.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.3f);

            Button btn = btnGO.AddComponent<Button>();

            // Text
            GameObject textGO = CreateText(btnGO.transform, "Text", label, 12, TMPro.TextAlignmentOptions.Center);
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return btnGO;
        }

        private static GameObject CreateButtonRow(Transform parent)
        {
            GameObject row = new GameObject("ButtonRow");
            row.transform.SetParent(parent, false);

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            return row;
        }

        private static GameObject CreateSpacer(Transform parent, float height)
        {
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(parent, false);

            RectTransform rect = spacer.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, height);

            var layoutElem = spacer.AddComponent<LayoutElement>();
            layoutElem.preferredHeight = height;

            return spacer;
        }

        private static GameObject CreateToggle(Transform parent, string name, string label, bool isOn)
        {
            GameObject toggleGO = new GameObject(name);
            toggleGO.transform.SetParent(parent, false);

            RectTransform rect = toggleGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, 30);

            HorizontalLayoutGroup layout = toggleGO.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;

            // Checkbox background
            GameObject checkBg = new GameObject("Background");
            checkBg.transform.SetParent(toggleGO.transform, false);

            RectTransform checkBgRect = checkBg.AddComponent<RectTransform>();
            checkBgRect.sizeDelta = new Vector2(20, 20);

            Image checkBgImg = checkBg.AddComponent<Image>();
            checkBgImg.color = new Color(0.3f, 0.3f, 0.35f);

            // Checkmark
            GameObject checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(checkBg.transform, false);

            RectTransform checkmarkRect = checkmark.AddComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(0.2f, 0.2f);
            checkmarkRect.anchorMax = new Vector2(0.8f, 0.8f);
            checkmarkRect.offsetMin = Vector2.zero;
            checkmarkRect.offsetMax = Vector2.zero;

            Image checkmarkImg = checkmark.AddComponent<Image>();
            checkmarkImg.color = ACCENT_COLOR;

            // Label
            var labelText = CreateText(toggleGO.transform, "Label", label, 14, TMPro.TextAlignmentOptions.Left);

            // Toggle component
            Toggle toggle = toggleGO.AddComponent<Toggle>();
            toggle.isOn = isOn;
            toggle.graphic = checkmarkImg;
            toggle.targetGraphic = checkBgImg;

            return toggleGO;
        }

        private static GameObject CreateDropdown(Transform parent, string name, string[] options)
        {
            GameObject dropdownGO = new GameObject(name);
            dropdownGO.transform.SetParent(parent, false);

            RectTransform rect = dropdownGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 35);

            Image img = dropdownGO.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.3f);

            TMPro.TMP_Dropdown dropdown = dropdownGO.AddComponent<TMPro.TMP_Dropdown>();

            // Caption text
            GameObject captionGO = CreateText(dropdownGO.transform, "Label", options.Length > 0 ? options[0] : "", 14, TMPro.TextAlignmentOptions.Left);
            RectTransform captionRect = captionGO.GetComponent<RectTransform>();
            captionRect.anchorMin = new Vector2(0, 0);
            captionRect.anchorMax = new Vector2(1, 1);
            captionRect.offsetMin = new Vector2(10, 5);
            captionRect.offsetMax = new Vector2(-30, -5);

            dropdown.captionText = captionGO.GetComponent<TMPro.TextMeshProUGUI>();

            // Arrow
            GameObject arrow = new GameObject("Arrow");
            arrow.transform.SetParent(dropdownGO.transform, false);

            RectTransform arrowRect = arrow.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0.5f);
            arrowRect.anchorMax = new Vector2(1, 0.5f);
            arrowRect.pivot = new Vector2(1, 0.5f);
            arrowRect.anchoredPosition = new Vector2(-10, 0);
            arrowRect.sizeDelta = new Vector2(20, 20);

            var arrowText = CreateText(arrow.transform, "ArrowText", "▼", 12, TMPro.TextAlignmentOptions.Center);
            RectTransform arrowTextRect = arrowText.GetComponent<RectTransform>();
            arrowTextRect.anchorMin = Vector2.zero;
            arrowTextRect.anchorMax = Vector2.one;
            arrowTextRect.offsetMin = Vector2.zero;
            arrowTextRect.offsetMax = Vector2.zero;

            // Template (dropdown list)
            GameObject template = new GameObject("Template");
            template.transform.SetParent(dropdownGO.transform, false);

            RectTransform templateRect = template.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.anchoredPosition = Vector2.zero;
            templateRect.sizeDelta = new Vector2(0, 150);

            Image templateImg = template.AddComponent<Image>();
            templateImg.color = new Color(0.2f, 0.2f, 0.25f);

            ScrollRect scroll = template.AddComponent<ScrollRect>();

            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(template.transform, false);

            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            viewport.AddComponent<Mask>();
            Image viewportImg = viewport.AddComponent<Image>();
            viewportImg.color = Color.white;

            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);

            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 30);

            // Item template
            GameObject item = new GameObject("Item");
            item.transform.SetParent(content.transform, false);

            RectTransform itemRect = item.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 0.5f);
            itemRect.anchorMax = new Vector2(1, 0.5f);
            itemRect.sizeDelta = new Vector2(0, 30);

            Toggle itemToggle = item.AddComponent<Toggle>();

            // Item background
            GameObject itemBg = new GameObject("Item Background");
            itemBg.transform.SetParent(item.transform, false);

            RectTransform itemBgRect = itemBg.AddComponent<RectTransform>();
            itemBgRect.anchorMin = Vector2.zero;
            itemBgRect.anchorMax = Vector2.one;
            itemBgRect.offsetMin = Vector2.zero;
            itemBgRect.offsetMax = Vector2.zero;

            Image itemBgImg = itemBg.AddComponent<Image>();
            itemBgImg.color = new Color(0.3f, 0.3f, 0.35f);

            // Item checkmark
            GameObject itemCheckmark = new GameObject("Item Checkmark");
            itemCheckmark.transform.SetParent(item.transform, false);

            RectTransform checkRect = itemCheckmark.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0, 0.5f);
            checkRect.anchorMax = new Vector2(0, 0.5f);
            checkRect.pivot = new Vector2(0, 0.5f);
            checkRect.anchoredPosition = new Vector2(5, 0);
            checkRect.sizeDelta = new Vector2(15, 15);

            Image checkImg = itemCheckmark.AddComponent<Image>();
            checkImg.color = ACCENT_COLOR;

            // Item label
            GameObject itemLabel = CreateText(item.transform, "Item Label", "Option", 14, TMPro.TextAlignmentOptions.Left);
            RectTransform itemLabelRect = itemLabel.GetComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(25, 2);
            itemLabelRect.offsetMax = new Vector2(-5, -2);

            itemToggle.targetGraphic = itemBgImg;
            itemToggle.graphic = checkImg;

            // Wire up scroll rect
            scroll.content = contentRect;
            scroll.viewport = viewportRect;

            // Wire up dropdown
            dropdown.template = templateRect;
            dropdown.itemText = itemLabel.GetComponent<TMPro.TextMeshProUGUI>();

            // Add options
            dropdown.ClearOptions();
            dropdown.AddOptions(new System.Collections.Generic.List<string>(options));

            // Hide template
            template.SetActive(false);

            return dropdownGO;
        }

        private static GameObject CreateInputField(Transform parent, string name, string placeholder, float width, float height)
        {
            GameObject inputGO = new GameObject(name);
            inputGO.transform.SetParent(parent, false);

            RectTransform rect = inputGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);

            Image img = inputGO.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.2f);

            // Text Area
            GameObject textArea = new GameObject("Text Area");
            textArea.transform.SetParent(inputGO.transform, false);

            RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(10, 5);
            textAreaRect.offsetMax = new Vector2(-10, -5);

            // Placeholder
            GameObject placeholderGO = CreateText(textArea.transform, "Placeholder", placeholder, 16, TMPro.TextAlignmentOptions.Left);
            placeholderGO.GetComponent<TMPro.TextMeshProUGUI>().color = new Color(0.5f, 0.5f, 0.5f);
            RectTransform phRect = placeholderGO.GetComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = Vector2.zero;
            phRect.offsetMax = Vector2.zero;

            // Text
            GameObject textGO = CreateText(textArea.transform, "Text", "", 16, TMPro.TextAlignmentOptions.Left);
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Input field component
            TMPro.TMP_InputField inputField = inputGO.AddComponent<TMPro.TMP_InputField>();
            inputField.textViewport = textAreaRect;
            inputField.textComponent = textGO.GetComponent<TMPro.TextMeshProUGUI>();
            inputField.placeholder = placeholderGO.GetComponent<TMPro.TextMeshProUGUI>();

            var layoutElem = inputGO.AddComponent<LayoutElement>();
            layoutElem.preferredWidth = width;
            layoutElem.preferredHeight = height;

            return inputGO;
        }

        private static GameObject CreateSlider(Transform parent, string name)
        {
            GameObject sliderGO = new GameObject(name);
            sliderGO.transform.SetParent(parent, false);

            RectTransform rect = sliderGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 20);

            Slider slider = sliderGO.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 0;

            // Background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(sliderGO.transform, false);

            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.25f);
            bgRect.anchorMax = new Vector2(1, 0.75f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.25f);

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGO.transform, false);

            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            // Fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);

            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = ACCENT_COLOR;

            // Handle Area
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderGO.transform, false);

            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10, 0);
            handleAreaRect.offsetMax = new Vector2(-10, 0);

            // Handle
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);

            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 0);

            Image handleImg = handle.AddComponent<Image>();
            handleImg.color = Color.white;

            // Wire up slider
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;

            return sliderGO;
        }

        private static GameObject CreateScrollView(Transform parent, string name, float height)
        {
            GameObject scrollGO = new GameObject(name);
            scrollGO.transform.SetParent(parent, false);

            RectTransform rect = scrollGO.AddComponent<RectTransform>();

            var layoutElem = scrollGO.AddComponent<LayoutElement>();
            layoutElem.preferredHeight = height;
            layoutElem.flexibleHeight = 1;

            ScrollRect scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGO.transform, false);

            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            viewport.AddComponent<Mask>();
            Image viewportImg = viewport.AddComponent<Image>();
            viewportImg.color = Color.clear;

            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);

            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 5;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.padding = new RectOffset(5, 5, 5, 5);

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            return scrollGO;
        }
    }
}
