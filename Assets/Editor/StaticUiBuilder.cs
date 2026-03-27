using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StaticUiBuilder
{
    private const string SessionKey = "BambooFlute.StaticUiBuilder.Ran";
    private const string MainUiPath = "Assets/Prefabs/MainUI.prefab";
    private const string TunerPanelPath = "Assets/Prefabs/TunerPanel.prefab";
    private const string MetronomePanelPath = "Assets/Prefabs/MetronomePanel.prefab";
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string FontPath = "Assets/Font/SourceHanSansCN-Regular SDF.asset";
    private const string BackgroundPath = "Assets/Sprites/BG.jpg";

    [InitializeOnLoadMethod]
    private static void AutoBuildOnLoad()
    {
        if (SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        SessionState.SetBool(SessionKey, true);
        EditorApplication.delayCall += RebuildAll;
    }

    [MenuItem("Tools/BambooFlute/Rebuild Static UI")]
    public static void RebuildAll()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        Sprite background = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        if (font == null)
        {
            Debug.LogError($"Cannot find TMP font at {FontPath}");
            return;
        }

        EnsureEditorFolders();

        BuildMainUi(font, background);
        BuildTunerPanel(font);
        BuildMetronomePanel(font);
        RefreshScene();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("BambooFlute static UI rebuilt.");
    }

    private static void EnsureEditorFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Editor"))
        {
            AssetDatabase.CreateFolder("Assets", "Editor");
        }
    }

    private static void BuildMainUi(TMP_FontAsset font, Sprite background)
    {
        GameObject root = CreateRoot("MainUI");
        root.AddComponent<PracticeNavigationController>();

        Image bg = CreateImage(root.transform, "Background", StretchFull(), Color.white);
        bg.sprite = background;

        Image home = CreateImage(root.transform, "HomeContent", AnchoredRect(0.1f, 0.1f, 0.9f, 0.9f), new Color(0.08f, 0.12f, 0.1f, 0.82f));
        CreateText(home.transform, "Title", "竹笛练习助手", font, 84, FontStyles.Bold, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(720f, 120f));
        CreateText(home.transform, "Subtitle", "先把音准和节奏练稳", font, 34, FontStyles.Normal, new Color(0.94f, 0.9f, 0.82f, 0.86f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 140f), new Vector2(720f, 72f));
        CreateButton(home.transform, "TunerEntryButton", "音准检测", font, new Color(0.45f, 0.81f, 0.54f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(420f, 120f));
        CreateButton(home.transform, "MetronomeEntryButton", "节拍器", font, new Color(0.88f, 0.7f, 0.28f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -210f), new Vector2(420f, 120f));

        SavePrefab(root, MainUiPath);
    }

    private static void BuildTunerPanel(TMP_FontAsset font)
    {
        GameObject root = CreateRoot("TunerPanel");
        root.AddComponent<TunerPanelController>();
        CreateImage(root.transform, "Overlay", StretchFull(), new Color(0.04f, 0.06f, 0.06f, 0.86f));

        Image card = CreateImage(root.transform, "Card", AnchoredRect(0.07f, 0.06f, 0.93f, 0.94f), new Color(0.1f, 0.14f, 0.12f, 0.96f));
        CreateButton(card.transform, "BackButton", "切回", font, new Color(0.88f, 0.7f, 0.28f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -36f), new Vector2(160f, 72f), new Vector2(0f, 1f));
        CreateTabRow(card.transform, font);
        CreateGauge(card.transform, font);
        CreateText(card.transform, "NoteLabel", "--", font, 108, FontStyles.Bold, new Color(0.73f, 0.16f, 0.18f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -500f), new Vector2(460f, 120f));
        CreateText(card.transform, "HintLabel", "等待输入", font, 48, FontStyles.Bold, new Color(0.73f, 0.16f, 0.18f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -610f), new Vector2(320f, 60f));
        CreateText(card.transform, "FrequencyLabel", "频率 -- Hz", font, 32, FontStyles.Normal, new Color(0.73f, 0.16f, 0.18f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(46f, -690f), new Vector2(340f, 44f), new Vector2(0f, 1f), TextAlignmentOptions.Left);
        CreateText(card.transform, "CentsLabel", "偏差 -- cents", font, 32, FontStyles.Normal, new Color(0.73f, 0.16f, 0.18f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-46f, -690f), new Vector2(340f, 44f), new Vector2(1f, 1f), TextAlignmentOptions.Right);
        CreateText(card.transform, "TargetLabel", "目标音名 --", font, 28, FontStyles.Normal, new Color(0.4f, 0.36f, 0.33f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(46f, -760f), new Vector2(320f, 40f), new Vector2(0f, 1f), TextAlignmentOptions.Left);
        CreateText(card.transform, "CurrentNoteNameLabel", "当前音名 --", font, 28, FontStyles.Normal, new Color(0.73f, 0.16f, 0.18f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-46f, -760f), new Vector2(320f, 40f), new Vector2(1f, 1f), TextAlignmentOptions.Right);
        CreateText(card.transform, "TargetFrequencyLabel", "目标频率 -- Hz", font, 28, FontStyles.Normal, new Color(0.4f, 0.36f, 0.33f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(46f, -810f), new Vector2(360f, 40f), new Vector2(0f, 1f), TextAlignmentOptions.Left);
        CreateText(card.transform, "CurrentFrequencyLabel", "当前频率 -- Hz", font, 28, FontStyles.Normal, new Color(0.73f, 0.16f, 0.18f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-46f, -810f), new Vector2(360f, 40f), new Vector2(1f, 1f), TextAlignmentOptions.Right);
        CreateText(card.transform, "RegisterHintLabel", "听音模式", font, 30, FontStyles.Bold, new Color(0.73f, 0.16f, 0.18f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -870f), new Vector2(600f, 46f));
        CreateText(card.transform, "MicStatusLabel", "麦克风初始化中", font, 24, FontStyles.Normal, new Color(0.45f, 0.4f, 0.36f, 1f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(720f, 40f));
        CreateListeningPage(card.transform);
        CreateTargetPage(card.transform, font);

        SavePrefab(root, TunerPanelPath);
    }

    private static void BuildMetronomePanel(TMP_FontAsset font)
    {
        GameObject root = CreateRoot("MetronomePanel");
        root.AddComponent<MetronomePanelController>();
        CreateImage(root.transform, "Overlay", StretchFull(), new Color(0.04f, 0.06f, 0.06f, 0.86f));

        Image card = CreateImage(root.transform, "Card", AnchoredRect(0.07f, 0.08f, 0.93f, 0.92f), new Color(0.1f, 0.14f, 0.12f, 0.96f));
        CreateButton(card.transform, "BackButton", "切回", font, new Color(0.88f, 0.7f, 0.28f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -36f), new Vector2(160f, 72f), new Vector2(0f, 1f));
        CreateText(card.transform, "Title", "节拍器", font, 40, FontStyles.Bold, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(320f, 60f));
        CreateText(card.transform, "BpmLabel", "72 BPM", font, 110, FontStyles.Bold, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(600f, 130f));
        CreateText(card.transform, "MeterLabel", "4 / 4", font, 40, FontStyles.Normal, new Color(0.94f, 0.9f, 0.82f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -320f), new Vector2(320f, 60f));

        CreateButton(card.transform, "BpmMinus5Button", "-5", font, new Color(0.45f, 0.81f, 0.54f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -430f), new Vector2(180f, 88f), new Vector2(0f, 1f));
        CreateButton(card.transform, "BpmMinus1Button", "-1", font, new Color(0.45f, 0.81f, 0.54f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(240f, -430f), new Vector2(180f, 88f), new Vector2(0f, 1f));
        CreateButton(card.transform, "BpmPlus1Button", "+1", font, new Color(0.45f, 0.81f, 0.54f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-240f, -430f), new Vector2(180f, 88f), new Vector2(1f, 1f));
        CreateButton(card.transform, "BpmPlus5Button", "+5", font, new Color(0.45f, 0.81f, 0.54f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -430f), new Vector2(180f, 88f), new Vector2(1f, 1f));

        CreateButton(card.transform, "Meter2Button", "2拍", font, new Color(0.88f, 0.7f, 0.28f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -550f), new Vector2(180f, 88f), new Vector2(0f, 1f));
        CreateButton(card.transform, "Meter3Button", "3拍", font, new Color(0.88f, 0.7f, 0.28f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(240f, -550f), new Vector2(180f, 88f), new Vector2(0f, 1f));
        CreateButton(card.transform, "Meter4Button", "4拍", font, new Color(0.88f, 0.7f, 0.28f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-240f, -550f), new Vector2(180f, 88f), new Vector2(1f, 1f));
        CreateButton(card.transform, "Meter6Button", "6拍", font, new Color(0.88f, 0.7f, 0.28f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -550f), new Vector2(180f, 88f), new Vector2(1f, 1f));

        CreateButton(card.transform, "ToggleButton", "开始节拍器", font, new Color(0.45f, 0.81f, 0.54f, 1f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(420f, 96f));

        SavePrefab(root, MetronomePanelPath);
    }

    private static void CreateTabRow(Transform parent, TMP_FontAsset font)
    {
        GameObject tabRow = new GameObject("TabRow", typeof(RectTransform));
        tabRow.transform.SetParent(parent, false);
        RectTransform rect = tabRow.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -44f);
        rect.sizeDelta = new Vector2(520f, 64f);

        CreateButton(tabRow.transform, "ListeningTab", "听音", font, new Color(0.88f, 0.7f, 0.28f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(180f, 60f), new Vector2(0f, 0.5f));
        CreateButton(tabRow.transform, "TargetTab", "目标音", font, new Color(1f, 1f, 1f, 0.08f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(180f, 60f), new Vector2(1f, 0.5f));
    }

    private static void CreateGauge(Transform parent, TMP_FontAsset font)
    {
        GameObject gauge = new GameObject("Gauge", typeof(RectTransform));
        gauge.transform.SetParent(parent, false);
        RectTransform gaugeRect = gauge.GetComponent<RectTransform>();
        gaugeRect.anchorMin = new Vector2(0.5f, 1f);
        gaugeRect.anchorMax = new Vector2(0.5f, 1f);
        gaugeRect.pivot = new Vector2(0.5f, 1f);
        gaugeRect.anchoredPosition = new Vector2(0f, -120f);
        gaugeRect.sizeDelta = new Vector2(760f, 340f);

        for (int i = -5; i <= 5; i++)
        {
            float t = (i + 5f) / 10f;
            float angle = Mathf.Lerp(180f, 0f, t);
            float radians = angle * Mathf.Deg2Rad;
            float radius = 260f;
            Vector2 position = new Vector2(Mathf.Cos(radians) * radius, Mathf.Sin(radians) * radius);
            bool major = i % 5 == 0;
            CreateTick(gauge.transform, $"Tick_{i + 5}", angle - 90f, position, major ? 12f : 6f, major ? 42f : 26f, i == 0 ? new Color(0.15f, 0.74f, 0.32f, 1f) : new Color(0.67f, 0.64f, 0.58f, 1f));
            if (i != 0)
            {
                CreateText(gauge.transform, $"TickLabel_{i + 5}", $"{Mathf.Abs(i) * 10}", font, 22, FontStyles.Bold, i < 0 ? new Color(0.15f, 0.74f, 0.32f, 1f) : new Color(0.72f, 0.24f, 0.26f, 1f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), position * 1.12f + new Vector2(0f, 22f), new Vector2(80f, 30f));
            }
            else
            {
                CreateText(gauge.transform, $"TickLabel_{i + 5}", "0", font, 22, FontStyles.Bold, new Color(0.15f, 0.74f, 0.32f, 1f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), position * 1.08f + new Vector2(0f, 22f), new Vector2(80f, 30f));
            }
        }

        GameObject pivot = new GameObject("NeedlePivot", typeof(RectTransform));
        pivot.transform.SetParent(gauge.transform, false);
        RectTransform pivotRect = pivot.GetComponent<RectTransform>();
        pivotRect.anchorMin = new Vector2(0.5f, 0f);
        pivotRect.anchorMax = new Vector2(0.5f, 0f);
        pivotRect.pivot = new Vector2(0.5f, 0f);
        pivotRect.anchoredPosition = new Vector2(0f, 18f);
        pivotRect.sizeDelta = new Vector2(24f, 250f);
        pivotRect.localEulerAngles = new Vector3(0f, 0f, 0f);

        CreateImage(pivot.transform, "Needle", new RectPreset
        {
            AnchorMin = new Vector2(0.5f, 0f),
            AnchorMax = new Vector2(0.5f, 0f),
            Pivot = new Vector2(0.5f, 0f),
            AnchoredPosition = Vector2.zero,
            SizeDelta = new Vector2(10f, 240f)
        }, new Color(0.75f, 0.77f, 0.78f, 1f));
        CreateImage(pivot.transform, "Knob", new RectPreset
        {
            AnchorMin = new Vector2(0.5f, 0f),
            AnchorMax = new Vector2(0.5f, 0f),
            Pivot = new Vector2(0.5f, 0.5f),
            AnchoredPosition = new Vector2(0f, 0f),
            SizeDelta = new Vector2(34f, 34f)
        }, new Color(0.75f, 0.77f, 0.78f, 1f));
    }

    private static void CreateListeningPage(Transform parent)
    {
        GameObject page = new GameObject("ListeningPage", typeof(RectTransform));
        page.transform.SetParent(parent, false);
        RectTransform rect = page.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void CreateTargetPage(Transform parent, TMP_FontAsset font)
    {
        GameObject page = new GameObject("TargetPage", typeof(RectTransform));
        page.transform.SetParent(parent, false);
        RectTransform rect = page.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        page.SetActive(false);

        CreateText(page.transform, "TargetTitle", "调性与目标音", font, 32, FontStyles.Bold, Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -930f), new Vector2(320f, 44f), new Vector2(0f, 1f), TextAlignmentOptions.Left);
        CreateText(page.transform, "FluteKeyLabel", "笛子调性", font, 28, FontStyles.Normal, new Color(0.94f, 0.9f, 0.82f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -990f), new Vector2(180f, 40f), new Vector2(0f, 1f), TextAlignmentOptions.Left);
        CreateFluteKeyButtons(page.transform, font);

        CreateButton(page.transform, "ToneFiveButton", "筒音作5", font, new Color(0.88f, 0.7f, 0.28f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -1128f), new Vector2(220f, 72f), new Vector2(0f, 1f));
        CreateButton(page.transform, "ToneTwoButton", "筒音作2", font, new Color(1f, 1f, 1f, 0.08f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(280f, -1128f), new Vector2(220f, 72f), new Vector2(0f, 1f));
        CreateRegisterRow(page.transform, "LowRow", "低音", font, new Vector2(40f, -1245f));
        CreateRegisterRow(page.transform, "MidRow", "中音", font, new Vector2(40f, -1378f));
        CreateRegisterRow(page.transform, "HighRow", "高音", font, new Vector2(40f, -1511f));
    }

    private static void CreateTick(Transform parent, string name, float zRotation, Vector2 anchoredPosition, float width, float height, Color color)
    {
        Image tick = CreateImage(parent, name, new RectPreset
        {
            AnchorMin = new Vector2(0.5f, 0f),
            AnchorMax = new Vector2(0.5f, 0f),
            Pivot = new Vector2(0.5f, 0.5f),
            AnchoredPosition = anchoredPosition,
            SizeDelta = new Vector2(width, height)
        }, color);
        tick.rectTransform.localEulerAngles = new Vector3(0f, 0f, zRotation);
    }

    private static void CreateFluteKeyButtons(Transform parent, TMP_FontAsset font)
    {
        GameObject root = new GameObject("FluteKeyButtons", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -1038f);
        rect.sizeDelta = new Vector2(-80f, 72f);

        IReadOnlyList<string> labels = BambooFluteTargetLibrary.GetFluteKeyLabels();
        for (int i = 0; i < labels.Count; i++)
        {
            CreateButton(root.transform, $"Key{i + 1}", labels[i], font, i == 0 ? new Color(0.88f, 0.7f, 0.28f, 1f) : new Color(1f, 1f, 1f, 0.08f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(i * 180f, 0f), new Vector2(160f, 60f), new Vector2(0f, 1f));
        }
    }

    private static void CreateRegisterRow(Transform parent, string name, string title, TMP_FontAsset font, Vector2 anchoredPosition)
    {
        GameObject row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = anchoredPosition;
        rowRect.sizeDelta = new Vector2(-80f, 110f);

        CreateText(row.transform, "Title", title, font, 24, FontStyles.Bold, new Color(0.94f, 0.9f, 0.82f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(80f, 36f), new Vector2(0f, 0.5f), TextAlignmentOptions.Left);

        GameObject optionTrack = new GameObject("Track", typeof(RectTransform));
        optionTrack.transform.SetParent(row.transform, false);
        RectTransform trackRect = optionTrack.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0f, 0f);
        trackRect.anchorMax = new Vector2(1f, 1f);
        trackRect.offsetMin = new Vector2(100f, 0f);
        trackRect.offsetMax = new Vector2(0f, 0f);

        for (int i = 0; i < 7; i++)
        {
            GameObject option = new GameObject($"Option{i + 1}", typeof(RectTransform));
            option.transform.SetParent(optionTrack.transform, false);
            RectTransform optionRect = option.GetComponent<RectTransform>();
            optionRect.anchorMin = new Vector2(0.5f, 0.5f);
            optionRect.anchorMax = new Vector2(0.5f, 0.5f);
            optionRect.sizeDelta = new Vector2(96f, 104f);

            Button button = CreateButton(option.transform, "Circle", "", font, new Color(1f, 1f, 1f, 0.001f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -2f), new Vector2(56f, 56f));
            CreateText(button.transform, "TopLabel", $"{i + 1}", font, 20, FontStyles.Bold, new Color(0.94f, 0.9f, 0.82f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 18f), new Vector2(96f, 24f));
            CreateImage(button.transform, "Dot", new RectPreset
            {
                AnchorMin = new Vector2(0.5f, 0.5f),
                AnchorMax = new Vector2(0.5f, 0.5f),
                Pivot = new Vector2(0.5f, 0.5f),
                AnchoredPosition = Vector2.zero,
                SizeDelta = new Vector2(18f, 18f)
            }, new Color(1f, 1f, 1f, 0.24f));
            CreateText(button.transform, "BottomLabel", "--", font, 18, FontStyles.Normal, new Color(0.78f, 0.74f, 0.68f, 1f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -18f), new Vector2(96f, 24f));
        }
    }

    private static void RefreshScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("SampleScene does not contain a Canvas.");
            return;
        }

        foreach (Transform child in canvas.transform.Cast<Transform>().ToArray())
        {
            if (child.name == "MainUI" || child.name == "TunerPanel" || child.name == "MetronomePanel")
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        GameObject appRoot = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "AppRoot");
        if (appRoot != null)
        {
            Object.DestroyImmediate(appRoot);
        }

        InstantiatePanel(MainUiPath, canvas.transform, true);
        InstantiatePanel(TunerPanelPath, canvas.transform, false);
        InstantiatePanel(MetronomePanelPath, canvas.transform, false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void InstantiatePanel(string prefabPath, Transform parent, bool active)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        RectTransform rect = instance.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localPosition = Vector3.zero;
        instance.SetActive(active);
    }

    private static GameObject CreateRoot(string name)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return root;
    }

    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static Image CreateImage(Transform parent, string name, RectPreset preset, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        ApplyRect(obj.GetComponent<RectTransform>(), preset);
        Image image = obj.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        TMP_FontAsset font,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        return CreateButton(parent, name, label, font, color, anchorMin, anchorMax, anchoredPosition, sizeDelta, new Vector2(0.5f, 0.5f));
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        TMP_FontAsset font,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Vector2 pivot)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Image image = obj.GetComponent<Image>();
        image.color = color;

        Button button = obj.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        colors.selectedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        colors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f);
        button.colors = colors;

        CreateText(obj.transform, "Label", label, font, 32, FontStyles.Bold, new Color(0.12f, 0.18f, 0.16f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), TextAlignmentOptions.Center);
        return button;
    }

    private static TextMeshProUGUI CreateText(
        Transform parent,
        string name,
        string text,
        TMP_FontAsset font,
        float fontSize,
        FontStyles style,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        return CreateText(parent, name, text, font, fontSize, style, color, anchorMin, anchorMax, anchoredPosition, sizeDelta, new Vector2(0.5f, 0.5f), TextAlignmentOptions.Center);
    }

    private static TextMeshProUGUI CreateText(
        Transform parent,
        string name,
        string text,
        TMP_FontAsset font,
        float fontSize,
        FontStyles style,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Vector2 pivot,
        TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        TextMeshProUGUI label = obj.GetComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = color;
        label.alignment = alignment;
        label.enableWordWrapping = false;
        return label;
    }

    private static RectPreset StretchFull()
    {
        return new RectPreset
        {
            AnchorMin = Vector2.zero,
            AnchorMax = Vector2.one,
            Pivot = new Vector2(0.5f, 0.5f),
            AnchoredPosition = Vector2.zero,
            SizeDelta = Vector2.zero
        };
    }

    private static RectPreset AnchoredRect(float minX, float minY, float maxX, float maxY)
    {
        return new RectPreset
        {
            AnchorMin = new Vector2(minX, minY),
            AnchorMax = new Vector2(maxX, maxY),
            Pivot = new Vector2(0.5f, 0.5f),
            AnchoredPosition = Vector2.zero,
            SizeDelta = Vector2.zero
        };
    }

    private static void ApplyRect(RectTransform rect, RectPreset preset)
    {
        rect.anchorMin = preset.AnchorMin;
        rect.anchorMax = preset.AnchorMax;
        rect.pivot = preset.Pivot;
        rect.anchoredPosition = preset.AnchoredPosition;
        rect.sizeDelta = preset.SizeDelta;
    }

    private struct RectPreset
    {
        public Vector2 AnchorMin;
        public Vector2 AnchorMax;
        public Vector2 Pivot;
        public Vector2 AnchoredPosition;
        public Vector2 SizeDelta;
    }
}
