using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SettlementPanelEditModeBuilder
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";

    [InitializeOnLoadMethod]
    private static void ScheduleSafeAutoBuild()
    {
        EditorApplication.delayCall += TryAutoBuildOpenMainScene;
    }

    private static void TryAutoBuildOpenMainScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.isLoaded || activeScene.path != ScenePath || activeScene.isDirty)
        {
            return;
        }

        GameObject settlementPanel = FindByName("SettlementPanel");
        if (settlementPanel == null || FindChild(settlementPanel.transform, "Grid_MonthCards") != null)
        {
            return;
        }

        try
        {
            Rebuild();
        }
        catch (Exception exception)
        {
            Debug.LogError($"SettlementPanel automatic Edit Mode build failed: {exception}");
        }
    }

    [MenuItem("Tools/Quik Delivery/Rebuild Settlement Panel")]
    public static void RebuildFromMenu()
    {
        Rebuild();
    }

    public static void Rebuild()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject settlementPanel = FindByName("SettlementPanel");

        if (settlementPanel == null)
        {
            throw new InvalidOperationException("SettlementPanel was not found in MainScene.");
        }

        SettlementPanelUI settlementUI = settlementPanel.GetComponent<SettlementPanelUI>();
        if (settlementUI == null)
        {
            throw new InvalidOperationException("SettlementPanelUI is missing from SettlementPanel.");
        }

        Transform panelTransform = settlementPanel.transform;
        Transform oldTitle = FindChild(panelTransform, "Txt_SettlementTitle");
        if (oldTitle != null)
        {
            oldTitle.gameObject.name = "Txt_SettlementTitle_Legacy";
            oldTitle.gameObject.SetActive(false);
        }

        GameObject headerRow = GetOrCreate(panelTransform, "HeaderRow");
        ConfigureHorizontalRow(headerRow, 12f, 18, 18);
        LayoutElement headerElement = GetOrAdd<LayoutElement>(headerRow);
        headerElement.preferredHeight = 92f;
        headerElement.flexibleHeight = 0f;

        Button prevYear = GetOrCreateButton(headerRow.transform, "Btn_PrevYear", "◀", 105f, 30f);
        TMP_Text yearTitle = GetOrCreateText(headerRow.transform, "Txt_YearTitle", 38f, FontStyles.Bold);
        LayoutElement yearElement = GetOrAdd<LayoutElement>(yearTitle.gameObject);
        yearElement.flexibleWidth = 1f;
        yearElement.preferredHeight = 72f;
        yearTitle.alignment = TextAlignmentOptions.Center;
        Button nextYear = GetOrCreateButton(headerRow.transform, "Btn_NextYear", "▶", 105f, 30f);

        Transform actionTransform = FindChild(panelTransform, "ActionRow");
        if (actionTransform == null)
        {
            actionTransform = GetOrCreate(panelTransform, "ActionRow").transform;
        }
        ConfigureHorizontalRow(actionTransform.gameObject, 14f, 18, 18);
        LayoutElement actionElement = GetOrAdd<LayoutElement>(actionTransform.gameObject);
        actionElement.preferredHeight = 84f;
        actionElement.flexibleHeight = 0f;

        Button refresh = FindChild(actionTransform, "Btn_Refresh")?.GetComponent<Button>();
        if (refresh == null)
        {
            refresh = GetOrCreateButton(actionTransform, "Btn_Refresh", "새로고침", 0f, 28f);
        }
        SetButtonLabel(refresh, "새로고침", 28f);
        GetOrAdd<LayoutElement>(refresh.gameObject).flexibleWidth = 1f;

        Button backupToggle = GetOrCreateButton(actionTransform, "Btn_BackupToggle", "백업 보기", 0f, 28f);
        GetOrAdd<LayoutElement>(backupToggle.gameObject).flexibleWidth = 1f;

        ScrollRect scrollRect = FindChild(panelTransform, "ScrollView_Settlement")?.GetComponent<ScrollRect>();
        if (scrollRect == null || scrollRect.content == null)
        {
            throw new InvalidOperationException("ScrollView_Settlement or its Content reference is missing.");
        }
        LayoutElement scrollElement = GetOrAdd<LayoutElement>(scrollRect.gameObject);
        scrollElement.flexibleHeight = 1f;
        scrollElement.minHeight = 600f;

        GameObject backupPanel = FindBackupPanel(scrollRect.content);
        backupPanel.name = "Panel_Backup";
        backupPanel.transform.SetParent(panelTransform, false);
        ConfigureVerticalPanel(backupPanel, 12f, 18, 18);
        LayoutElement backupElement = GetOrAdd<LayoutElement>(backupPanel);
        backupElement.preferredHeight = 390f;
        backupElement.flexibleHeight = 0f;
        backupPanel.SetActive(false);

        BuildGrid(scrollRect.content, out MonthlySummaryCardUI[] cards);
        ConfigurePanelLayout(settlementPanel, headerRow.transform, actionTransform, backupPanel.transform, scrollRect.transform);
        BindInspector(settlementUI, prevYear, yearTitle, nextYear, refresh, backupToggle, backupPanel, cards);

        EditorUtility.SetDirty(settlementPanel);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("SettlementPanel Edit Mode layout rebuilt successfully.");
    }

    private static void BuildGrid(RectTransform content, out MonthlySummaryCardUI[] cards)
    {
        for (int i = 0; i < content.childCount; i++)
        {
            Transform child = content.GetChild(i);
            if (child.name != "Grid_MonthCards") child.gameObject.SetActive(false);
        }

        VerticalLayoutGroup oldContentLayout = content.GetComponent<VerticalLayoutGroup>();
        if (oldContentLayout != null)
        {
            oldContentLayout.enabled = true;
            oldContentLayout.padding = new RectOffset(12, 12, 12, 12);
            oldContentLayout.childControlWidth = true;
            oldContentLayout.childControlHeight = true;
            oldContentLayout.childForceExpandWidth = true;
            oldContentLayout.childForceExpandHeight = false;
        }

        ContentSizeFitter contentFitter = GetOrAdd<ContentSizeFitter>(content.gameObject);
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject gridObject = GetOrCreate(content, "Grid_MonthCards");
        gridObject.SetActive(true);
        GridLayoutGroup grid = GetOrAdd<GridLayoutGroup>(gridObject);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.cellSize = new Vector2(485f, 455f);
        grid.spacing = new Vector2(16f, 16f);
        grid.padding = new RectOffset(0, 0, 0, 0);
        grid.childAlignment = TextAnchor.UpperCenter;

        LayoutElement gridElement = GetOrAdd<LayoutElement>(gridObject);
        gridElement.preferredHeight = 6f * 455f + 5f * 16f;
        gridElement.flexibleHeight = 0f;

        cards = new MonthlySummaryCardUI[12];
        for (int month = 1; month <= 12; month++)
        {
            GameObject cardObject = GetOrCreate(gridObject.transform, $"Card_{month:00}");
            cardObject.SetActive(true);
            Image image = GetOrAdd<Image>(cardObject);
            image.color = new Color(0.96f, 0.945f, 0.91f, 1f);
            MonthlySummaryCardUI card = GetOrAdd<MonthlySummaryCardUI>(cardObject);
            BuildCardRows(cardObject.transform, card);
            cards[month - 1] = card;
        }
    }

    private static void BuildCardRows(Transform cardTransform, MonthlySummaryCardUI card)
    {
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(cardTransform.gameObject);
        layout.padding = new RectOffset(20, 20, 16, 16);
        layout.spacing = 3f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TMP_Text title = GetOrCreateText(cardTransform, "Txt_MonthTitle", 27f, FontStyles.Bold);
        TMP_Text count = GetOrCreateText(cardTransform, "Txt_DeliveryCount", 20f, FontStyles.Normal);
        TMP_Text gross = GetOrCreateText(cardTransform, "Txt_GrossRevenue", 20f, FontStyles.Normal);
        TMP_Text company = GetOrCreateText(cardTransform, "Txt_CompanyDeposit", 20f, FontStyles.Normal);
        TMP_Text data = GetOrCreateText(cardTransform, "Txt_DataUsageFee", 20f, FontStyles.Normal);
        TMP_Text toll = GetOrCreateText(cardTransform, "Txt_TollFee", 20f, FontStyles.Normal);
        TMP_Text electric = GetOrCreateText(cardTransform, "Txt_ElectricCharge", 20f, FontStyles.Normal);
        TMP_Text other = GetOrCreateText(cardTransform, "Txt_OtherExpense", 20f, FontStyles.Normal);
        TMP_Text expense = GetOrCreateText(cardTransform, "Txt_TotalExpense", 21f, FontStyles.Bold);
        TMP_Text net = GetOrCreateText(cardTransform, "Txt_NetRevenue", 22f, FontStyles.Bold);

        SerializedObject serializedCard = new SerializedObject(card);
        SetReference(serializedCard, "txtMonthTitle", title);
        SetReference(serializedCard, "txtDeliveryCount", count);
        SetReference(serializedCard, "txtGrossRevenue", gross);
        SetReference(serializedCard, "txtCompanyDeposit", company);
        SetReference(serializedCard, "txtDataUsageFee", data);
        SetReference(serializedCard, "txtTollFee", toll);
        SetReference(serializedCard, "txtElectricCharge", electric);
        SetReference(serializedCard, "txtOtherExpense", other);
        SetReference(serializedCard, "txtTotalExpense", expense);
        SetReference(serializedCard, "txtNetRevenue", net);
        serializedCard.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BindInspector(
        SettlementPanelUI ui,
        Button prevYear,
        TMP_Text yearTitle,
        Button nextYear,
        Button refresh,
        Button backupToggle,
        GameObject backupPanel,
        MonthlySummaryCardUI[] cards)
    {
        SerializedObject serializedUI = new SerializedObject(ui);
        SetReference(serializedUI, "btnPrevYear", prevYear);
        SetReference(serializedUI, "txtYearTitle", yearTitle);
        SetReference(serializedUI, "btnNextYear", nextYear);
        SetReference(serializedUI, "btnRefresh", refresh);
        SetReference(serializedUI, "btnBackupToggle", backupToggle);
        SetReference(serializedUI, "panelBackup", backupPanel);
        SerializedProperty cardArray = serializedUI.FindProperty("monthlyCards");
        cardArray.arraySize = cards.Length;
        for (int i = 0; i < cards.Length; i++) cardArray.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
        serializedUI.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject FindBackupPanel(RectTransform content)
    {
        Transform current = FindChild(content, "Card_Backup") ?? FindChild(content, "Panel_Backup");
        if (current != null) return current.gameObject;
        return GetOrCreate(content, "Panel_Backup");
    }

    private static void ConfigurePanelLayout(GameObject panel, Transform header, Transform action, Transform backup, Transform scroll)
    {
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(panel);
        layout.padding = new RectOffset(34, 34, 28, 28);
        layout.spacing = 16f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        header.SetSiblingIndex(0);
        action.SetSiblingIndex(1);
        backup.SetSiblingIndex(2);
        scroll.SetSiblingIndex(3);
    }

    private static void ConfigureHorizontalRow(GameObject row, float spacing, int left, int right)
    {
        HorizontalLayoutGroup layout = GetOrAdd<HorizontalLayoutGroup>(row);
        layout.padding = new RectOffset(left, right, 0, 0);
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
    }

    private static void ConfigureVerticalPanel(GameObject panel, float spacing, int left, int right)
    {
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(panel);
        layout.padding = new RectOffset(left, right, 14, 14);
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private static Button GetOrCreateButton(Transform parent, string name, string text, float preferredWidth, float fontSize)
    {
        Transform existing = FindChild(parent, name);
        GameObject buttonObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);
        GetOrAdd<Image>(buttonObject).color = new Color(0.84f, 0.91f, 0.96f, 1f);
        Button button = GetOrAdd<Button>(buttonObject);
        LayoutElement element = GetOrAdd<LayoutElement>(buttonObject);
        element.preferredWidth = preferredWidth;
        element.preferredHeight = 72f;
        SetButtonLabel(button, text, fontSize);
        return button;
    }

    private static void SetButtonLabel(Button button, string value, float fontSize)
    {
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null) label = GetOrCreateText(button.transform, "Txt_ButtonLabel", fontSize, FontStyles.Bold);
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.text = value;
    }

    private static TMP_Text GetOrCreateText(Transform parent, string name, float fontSize, FontStyles style)
    {
        Transform existing = FindChild(parent, name);
        GameObject textObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = GetOrAdd<TextMeshProUGUI>(textObject);
        if (TMP_Settings.defaultFontAsset != null) text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = new Color(0.12f, 0.11f, 0.09f, 1f);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;
        LayoutElement element = GetOrAdd<LayoutElement>(textObject);
        element.minHeight = 32f;
        element.preferredHeight = 36f;
        return text;
    }

    private static GameObject GetOrCreate(Transform parent, string name)
    {
        Transform existing = FindChild(parent, name);
        if (existing != null) return existing.gameObject;
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private static Transform FindChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
        }
        return null;
    }

    private static GameObject FindByName(string name)
    {
        Transform[] all = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform transform in all)
        {
            if (transform.name == name) return transform.gameObject;
        }
        return null;
    }

    private static void SetReference(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null) throw new InvalidOperationException($"Missing serialized property: {propertyName}");
        property.objectReferenceValue = value;
    }
}
