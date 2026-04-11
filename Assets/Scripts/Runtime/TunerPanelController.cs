using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public sealed class TunerPanelController : MonoBehaviour
{
    private sealed class RegisterSummaryUi
    {
        public Image Card;
        public TextMeshProUGUI Title;
        public TextMeshProUGUI Main;
        public TextMeshProUGUI Detail;
    }

    private readonly Color okayColor = new Color(0.45f, 0.81f, 0.54f, 1f);
    private readonly Color warningColor = new Color(0.93f, 0.55f, 0.29f, 1f);
    private readonly Color dangerColor = new Color(0.78f, 0.28f, 0.24f, 1f);
    private readonly Color accentColor = new Color(0.88f, 0.7f, 0.28f, 1f);
    private readonly Color softTextColor = new Color(0.94f, 0.9f, 0.82f, 1f);
    private readonly Color dimCardColor = new Color(1f, 1f, 1f, 0.08f);

    private MicrophonePitchTracker pitchTracker;
    private PracticeNavigationController navigationController;
    private TMP_FontAsset uiFont;

    private TextMeshProUGUI noteLabel;
    private TextMeshProUGUI hintLabel;
    private TextMeshProUGUI frequencyLabel;
    private TextMeshProUGUI centsLabel;
    private TextMeshProUGUI targetLabel;
    private TextMeshProUGUI micStatusLabel;
    private TextMeshProUGUI targetFrequencyLabel;
    private TextMeshProUGUI currentNoteNameLabel;
    private TextMeshProUGUI currentFrequencyLabel;
    private TextMeshProUGUI registerHintLabel;
    private RectTransform needlePivot;

    private Button listeningTabButton;
    private Button targetTabButton;
    private Image listeningTabImage;
    private Image targetTabImage;
    private GameObject listeningPage;
    private GameObject targetPage;
    private Button sourceToggleButton;
    private TextMeshProUGUI sourceToggleLabel;

    private RectTransform listeningRoot;
    private RectTransform keyScrollerContent;
    private readonly List<Button> keyButtons = new List<Button>();
    private readonly List<TextMeshProUGUI> keyButtonLabels = new List<TextMeshProUGUI>();
    private Button tongueFiveButton;
    private Button tongueTwoButton;
    private TextMeshProUGUI tongueFiveLabel;
    private TextMeshProUGUI tongueTwoLabel;
    private TextMeshProUGUI listeningTitleLabel;
    private readonly Dictionary<RegisterBand, RegisterSummaryUi> registerSummary = new Dictionary<RegisterBand, RegisterSummaryUi>();
    private ListeningPitchTrailView trailView;

    private TongueMode currentTongueMode = TongueMode.Five;
    private int fluteKeyIndex = BambooFluteTargetLibrary.DefaultFluteKeyIndex;
    private IReadOnlyList<TargetNoteOption> currentOptions;

    private void OnEnable()
    {
        if (listeningTabButton == null || targetTabButton == null)
        {
            return;
        }

        listeningTabButton.onClick.RemoveListener(ShowListeningMode);
        listeningTabButton.onClick.AddListener(ShowListeningMode);
        targetTabButton.onClick.RemoveListener(ShowTargetMode);
        targetTabButton.onClick.AddListener(ShowTargetMode);
    }

    private void Awake()
    {
        pitchTracker = GetComponent<MicrophonePitchTracker>() ?? gameObject.AddComponent<MicrophonePitchTracker>();
        navigationController = FindAnyObjectByType<PracticeNavigationController>();
        uiFont = CreateRuntimeFont();

        noteLabel = FindLabel("Card/NoteLabel");
        hintLabel = FindLabel("Card/HintLabel");
        frequencyLabel = FindLabel("Card/FrequencyLabel");
        centsLabel = FindLabel("Card/CentsLabel");
        targetLabel = FindLabel("Card/TargetLabel");
        micStatusLabel = FindLabel("Card/MicStatusLabel");
        targetFrequencyLabel = FindLabel("Card/TargetFrequencyLabel");
        currentNoteNameLabel = FindLabel("Card/CurrentNoteNameLabel");
        currentFrequencyLabel = FindLabel("Card/CurrentFrequencyLabel");
        registerHintLabel = FindLabel("Card/RegisterHintLabel");
        needlePivot = FindRequiredChild("Card/Gauge/NeedlePivot") as RectTransform;

        listeningPage = FindRequiredChild("Card/ListeningPage").gameObject;
        targetPage = FindRequiredChild("Card/TargetPage").gameObject;
        listeningTabButton = FindRequiredButton("Card/TabRow/ListeningTab");
        targetTabButton = FindRequiredButton("Card/TabRow/TargetTab");
        listeningTabImage = listeningTabButton.GetComponent<Image>();
        targetTabImage = targetTabButton.GetComponent<Image>();

        FindRequiredButton("Card/BackButton").onClick.AddListener(() => navigationController?.ShowHome());
        listeningTabButton.onClick.AddListener(ShowListeningMode);
        targetTabButton.onClick.AddListener(ShowTargetMode);

        EnsureSourceToggleButton();
        sourceToggleButton.onClick.AddListener(() =>
        {
            pitchTracker.ToggleInputSource();
            trailView?.Clear();
        });

        ApplyRuntimeFont();
        ConfigureLegacyGaugeLayout();
        BuildListeningUi();
        RefreshScale();
        ShowListeningMode();
    }

    private void Update()
    {
        micStatusLabel.text = pitchTracker.MicrophoneStatus;
        RefreshSourceToggleUi();
        RenderListening();
    }

    private void ShowListeningMode()
    {
        listeningPage.SetActive(true);
        targetPage.SetActive(false);
        if (listeningRoot != null)
        {
            listeningRoot.gameObject.SetActive(true);
        }

        listeningTabImage.color = accentColor;
        targetTabImage.color = dimCardColor;
    }

    private void ShowTargetMode()
    {
        listeningPage.SetActive(false);
        targetPage.SetActive(true);
        if (listeningRoot != null)
        {
            listeningRoot.gameObject.SetActive(false);
        }

        listeningTabImage.color = dimCardColor;
        targetTabImage.color = accentColor;
    }

    private void RefreshScale()
    {
        currentOptions = BambooFluteTargetLibrary.BuildOptions(fluteKeyIndex, currentTongueMode);
        UpdateKeyButtons();
        UpdateTongueButtons();
        trailView?.SetScale(currentOptions);
    }

    private void RenderListening()
    {
        if (currentOptions == null || currentOptions.Count == 0)
        {
            return;
        }

        PitchFrame frame = pitchTracker.CurrentFrame;
        if (!frame.HasSignal || !frame.HasPitch)
        {
            noteLabel.text = "--";
            hintLabel.text = GetIdleHint();
            hintLabel.color = softTextColor;
            frequencyLabel.text = "频率 -- Hz";
            centsLabel.text = "偏差 -- cents";
            targetLabel.text = "当前目标 --";
            targetFrequencyLabel.text = "目标频率 -- Hz";
            currentNoteNameLabel.text = "当前音名 --";
            currentFrequencyLabel.text = "当前频率 -- Hz";
            registerHintLabel.text = "长音听音";
            UpdateGauge(0f, softTextColor);

            foreach (RegisterBand band in registerSummary.Keys.ToList())
            {
                RegisterSummaryUi ui = registerSummary[band];
                ui.Main.text = "--";
                ui.Detail.text = "等待输入";
                ui.Card.color = dimCardColor;
            }

            if (listeningTitleLabel != null)
            {
                listeningTitleLabel.text = "请开始吹奏";
            }

            trailView?.PushFrame(Time.unscaledTime, false, default, 0f);
            return;
        }

        TargetNoteOption nearest = FindNearestOption(frame.Frequency);
        float cents = GetCents(frame.Frequency, nearest.Frequency);
        string registerText = TargetNoteOption.GetRegisterText(nearest.RegisterBand);

        noteLabel.text = nearest.DegreeLabel;
        hintLabel.text = PitchMath.GetTuningHint(cents);
        hintLabel.color = GetHintColor(cents);
        frequencyLabel.text = $"频率 {frame.Frequency:0.0} Hz";
        centsLabel.text = $"偏差 {cents:+0.0;-0.0;0.0} cents";
        targetLabel.text = $"当前目标 {registerText}音 {nearest.DegreeLabel}";
        targetFrequencyLabel.text = $"目标频率 {nearest.Frequency:0.00} Hz";
        currentNoteNameLabel.text = $"当前音名 {nearest.PitchLabel}";
        currentFrequencyLabel.text = $"参考音高 {nearest.PitchLabel}";
        registerHintLabel.text = $"{registerText}音 {nearest.DegreeLabel}  {nearest.PitchLabel}";
        UpdateGauge(cents, GetHintColor(cents));

        if (listeningTitleLabel != null)
        {
            listeningTitleLabel.text = $"当前最接近 {registerText}音 {nearest.DegreeLabel}";
        }

        foreach (RegisterBand band in registerSummary.Keys.ToList())
        {
            RegisterSummaryUi ui = registerSummary[band];
            TargetNoteOption option = FindNearestOption(frame.Frequency, band);
            float bandCents = GetCents(frame.Frequency, option.Frequency);
            ui.Main.text = $"{option.DegreeLabel}  {option.PitchLabel}";
            ui.Main.color = GetHintColor(bandCents);
            ui.Detail.text = $"{bandCents:+0.0;-0.0;0.0} cents";
            ui.Card.color = option.RegisterBand == nearest.RegisterBand && option.DegreeLabel == nearest.DegreeLabel
                ? new Color(0.11f, 0.29f, 0.25f, 0.95f)
                : dimCardColor;
        }

        trailView?.PushFrame(Time.unscaledTime, true, nearest, cents);
    }

    private TargetNoteOption FindNearestOption(float frequency)
    {
        return FindNearestOption(frequency, null);
    }

    private TargetNoteOption FindNearestOption(float frequency, RegisterBand? band)
    {
        TargetNoteOption best = currentOptions[0];
        float bestDistance = float.MaxValue;
        for (int i = 0; i < currentOptions.Count; i++)
        {
            TargetNoteOption option = currentOptions[i];
            if (band.HasValue && option.RegisterBand != band.Value)
            {
                continue;
            }

            float distance = Mathf.Abs(GetCents(frequency, option.Frequency));
            if (distance < bestDistance)
            {
                best = option;
                bestDistance = distance;
            }
        }

        return best;
    }

    private float GetCents(float frequency, float targetFrequency)
    {
        if (frequency <= 0f || targetFrequency <= 0f)
        {
            return 0f;
        }

        return 1200f * Mathf.Log(frequency / targetFrequency, 2f);
    }

    private Color GetHintColor(float centsOffset)
    {
        float abs = Mathf.Abs(centsOffset);
        if (abs <= 8f)
        {
            return okayColor;
        }

        if (abs <= 20f)
        {
            return warningColor;
        }

        return dangerColor;
    }

    private string GetIdleHint()
    {
        if (pitchTracker.CurrentInputSource == PitchInputSource.Playback)
        {
            return "等待播放声音";
        }

        return pitchTracker.IsReady ? "请开始吹奏长音" : "等待麦克风";
    }

    private void BuildListeningUi()
    {
        RectTransform page = listeningPage.GetComponent<RectTransform>();
        listeningRoot = CreateRect("ListeningRoot", page, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        RectTransform topCard = CreatePanel("TopControls", listeningRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -116f), new Vector2(-24f, 152f), new Color(0.12f, 0.18f, 0.2f, 0.92f));
        topCard.offsetMin = new Vector2(24f, topCard.offsetMin.y);
        CreateText("KeyTitle", topCard, "笛子调性", 22f, softTextColor, new Vector2(18f, -12f), new Vector2(180f, 28f), TextAlignmentOptions.Left);

        RectTransform scrollerRoot = CreatePanel("KeyScroller", topCard, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(-24f, 42f), dimCardColor);
        scrollerRoot.offsetMin = new Vector2(18f, scrollerRoot.offsetMin.y);
        RectTransform viewport = CreateRect("Viewport", scrollerRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        viewport.gameObject.AddComponent<RectMask2D>();
        keyScrollerContent = CreateRect("Content", viewport, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        ScrollRect scrollRect = scrollerRoot.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 20f;
        scrollRect.viewport = viewport;
        scrollRect.content = keyScrollerContent;

        CreateText("TongueTitle", topCard, "筒音模式", 22f, softTextColor, new Vector2(18f, -100f), new Vector2(180f, 28f), TextAlignmentOptions.Left);
        tongueFiveButton = CreateButton(topCard, "TongueFive", "筒音作5", new Vector2(18f, -132f), new Vector2(150f, 36f), () => { currentTongueMode = TongueMode.Five; RefreshScale(); });
        tongueTwoButton = CreateButton(topCard, "TongueTwo", "筒音作2", new Vector2(180f, -132f), new Vector2(150f, 36f), () => { currentTongueMode = TongueMode.Two; RefreshScale(); });
        tongueFiveLabel = tongueFiveButton.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        tongueTwoLabel = tongueTwoButton.transform.Find("Label").GetComponent<TextMeshProUGUI>();

        listeningTitleLabel = CreateText("ListeningTitle", listeningRoot, "请开始吹奏", 28f, Color.white, new Vector2(24f, -590f), new Vector2(540f, 36f), TextAlignmentOptions.Left);

        RectTransform summaryRow = CreateRect("SummaryRow", listeningRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -634f), new Vector2(-24f, 110f));
        summaryRow.offsetMin = new Vector2(24f, summaryRow.offsetMin.y);
        CreateRegisterCard(summaryRow, RegisterBand.Low, 0);
        CreateRegisterCard(summaryRow, RegisterBand.Mid, 1);
        CreateRegisterCard(summaryRow, RegisterBand.High, 2);

        RectTransform graphCard = CreatePanel("GraphCard", listeningRoot, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 68f), new Vector2(-24f, 286f), new Color(0.12f, 0.18f, 0.2f, 0.92f));
        graphCard.offsetMin = new Vector2(24f, graphCard.offsetMin.y);
        CreateText("GraphTitle", graphCard, "音高轨迹", 24f, Color.white, new Vector2(20f, -14f), new Vector2(180f, 28f), TextAlignmentOptions.Left);
        CreateText("GraphHint", graphCard, "每段线都以当前最近目标音为中心", 18f, softTextColor, new Vector2(196f, -14f), new Vector2(420f, 24f), TextAlignmentOptions.Left);
        CreateText("AxisTop", graphCard, "+35c", 16f, softTextColor, new Vector2(8f, -54f), new Vector2(46f, 18f), TextAlignmentOptions.Left);
        CreateText("AxisUpper", graphCard, "+20c", 16f, softTextColor, new Vector2(8f, -96f), new Vector2(46f, 18f), TextAlignmentOptions.Left);
        CreateText("AxisCenter", graphCard, "0c", 16f, Color.white, new Vector2(16f, -152f), new Vector2(38f, 18f), TextAlignmentOptions.Left);
        CreateText("AxisLower", graphCard, "-20c", 16f, softTextColor, new Vector2(8f, -210f), new Vector2(46f, 18f), TextAlignmentOptions.Left);
        CreateText("AxisBottom", graphCard, "-35c", 16f, softTextColor, new Vector2(8f, -252f), new Vector2(46f, 18f), TextAlignmentOptions.Left);

        RectTransform graphViewport = CreatePanel("GraphViewport", graphCard, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(-60f, 234f), new Color(0.05f, 0.09f, 0.12f, 1f));
        graphViewport.offsetMin = new Vector2(54f, graphViewport.offsetMin.y);
        trailView = graphViewport.gameObject.AddComponent<ListeningPitchTrailView>();
        trailView.Configure(uiFont);

        BuildKeyButtons();
    }

    private void BuildKeyButtons()
    {
        for (int i = 0; i < keyScrollerContent.childCount; i++)
        {
            Destroy(keyScrollerContent.GetChild(i).gameObject);
        }

        keyButtons.Clear();
        keyButtonLabels.Clear();

        IReadOnlyList<string> labels = BambooFluteTargetLibrary.GetFluteKeyLabels();
        const float buttonWidth = 110f;
        const float gap = 12f;
        for (int i = 0; i < labels.Count; i++)
        {
            int keyIndex = i;
            Button button = CreateButton(keyScrollerContent, $"Key{keyIndex}", labels[i], new Vector2(i * (buttonWidth + gap), -2f), new Vector2(buttonWidth, 34f), () =>
            {
                fluteKeyIndex = keyIndex;
                RefreshScale();
            });
            button.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 1f);
            button.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 1f);
            button.GetComponent<RectTransform>().pivot = new Vector2(0f, 1f);
            keyButtons.Add(button);
            keyButtonLabels.Add(button.transform.Find("Label").GetComponent<TextMeshProUGUI>());
        }

        keyScrollerContent.sizeDelta = new Vector2(labels.Count * (buttonWidth + gap), 40f);
    }

    private void UpdateKeyButtons()
    {
        IReadOnlyList<string> labels = BambooFluteTargetLibrary.GetFluteKeyLabels();
        for (int i = 0; i < keyButtons.Count; i++)
        {
            bool selected = i == fluteKeyIndex;
            keyButtons[i].GetComponent<Image>().color = selected ? accentColor : dimCardColor;
            keyButtonLabels[i].text = labels[i];
            keyButtonLabels[i].color = selected ? new Color(0.12f, 0.18f, 0.16f, 1f) : softTextColor;
        }
    }

    private void UpdateTongueButtons()
    {
        bool five = currentTongueMode == TongueMode.Five;
        tongueFiveButton.GetComponent<Image>().color = five ? okayColor : dimCardColor;
        tongueTwoButton.GetComponent<Image>().color = five ? dimCardColor : okayColor;
        tongueFiveLabel.color = five ? new Color(0.12f, 0.18f, 0.16f, 1f) : softTextColor;
        tongueTwoLabel.color = five ? softTextColor : new Color(0.12f, 0.18f, 0.16f, 1f);
    }

    private void CreateRegisterCard(RectTransform parent, RegisterBand band, int index)
    {
        RectTransform cardRect = CreatePanel($"{band}Card", parent, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(index * 246f, 0f), new Vector2(228f, 120f), dimCardColor);
        RegisterSummaryUi ui = new RegisterSummaryUi
        {
            Card = cardRect.GetComponent<Image>(),
            Title = CreateText("Title", cardRect, $"{TargetNoteOption.GetRegisterText(band)}音", 22f, softTextColor, new Vector2(16f, -12f), new Vector2(120f, 26f), TextAlignmentOptions.Left),
            Main = CreateText("Main", cardRect, "--", 34f, Color.white, new Vector2(16f, -44f), new Vector2(180f, 38f), TextAlignmentOptions.Left),
            Detail = CreateText("Detail", cardRect, "等待输入", 18f, softTextColor, new Vector2(16f, -82f), new Vector2(180f, 22f), TextAlignmentOptions.Left),
        };
        registerSummary[band] = ui;
    }

    private TMP_FontAsset CreateRuntimeFont()
    {
        Font resourceFont = Resources.Load<Font>("SourceHanSansCN-Regular");
        if (resourceFont == null)
        {
            return micStatusLabel != null ? micStatusLabel.font : null;
        }

        return TMP_FontAsset.CreateFontAsset(resourceFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
    }

    private void ApplyRuntimeFont()
    {
        if (uiFont == null)
        {
            return;
        }

        TextMeshProUGUI[] labels = GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            labels[i].font = uiFont;
        }
    }

    private void ConfigureLegacyGaugeLayout()
    {
        RectTransform gauge = FindRequiredChild("Card/Gauge") as RectTransform;
        if (gauge != null)
        {
            gauge.anchoredPosition = new Vector2(0f, -246f);
            gauge.sizeDelta = new Vector2(600f, 248f);
        }

        noteLabel.font = uiFont;
        hintLabel.font = uiFont;
        frequencyLabel.font = uiFont;
        centsLabel.font = uiFont;
        targetLabel.font = uiFont;
        targetFrequencyLabel.font = uiFont;
        currentNoteNameLabel.font = uiFont;
        currentFrequencyLabel.font = uiFont;
        registerHintLabel.font = uiFont;

        SetTextRect(noteLabel.rectTransform, new Vector2(0f, -424f), new Vector2(220f, 76f));
        noteLabel.fontSize = 58f;
        noteLabel.alignment = TextAlignmentOptions.Center;

        SetTextRect(hintLabel.rectTransform, new Vector2(0f, -482f), new Vector2(260f, 42f));
        hintLabel.fontSize = 30f;
        hintLabel.alignment = TextAlignmentOptions.Center;

        SetTextRect(registerHintLabel.rectTransform, new Vector2(0f, -520f), new Vector2(420f, 30f));
        registerHintLabel.fontSize = 20f;
        registerHintLabel.alignment = TextAlignmentOptions.Center;

        SetTextRect(frequencyLabel.rectTransform, new Vector2(36f, -556f), new Vector2(260f, 26f), new Vector2(0f, 1f));
        frequencyLabel.fontSize = 20f;
        frequencyLabel.alignment = TextAlignmentOptions.Left;

        SetTextRect(centsLabel.rectTransform, new Vector2(-36f, -556f), new Vector2(260f, 26f), new Vector2(1f, 1f));
        centsLabel.fontSize = 20f;
        centsLabel.alignment = TextAlignmentOptions.Right;

        SetTextRect(targetLabel.rectTransform, new Vector2(36f, -618f), new Vector2(300f, 26f), new Vector2(0f, 1f));
        targetLabel.fontSize = 20f;
        targetLabel.alignment = TextAlignmentOptions.Left;

        SetTextRect(currentNoteNameLabel.rectTransform, new Vector2(-36f, -618f), new Vector2(300f, 26f), new Vector2(1f, 1f));
        currentNoteNameLabel.fontSize = 20f;
        currentNoteNameLabel.alignment = TextAlignmentOptions.Right;

        SetTextRect(targetFrequencyLabel.rectTransform, new Vector2(36f, -646f), new Vector2(320f, 24f), new Vector2(0f, 1f));
        targetFrequencyLabel.fontSize = 18f;
        targetFrequencyLabel.alignment = TextAlignmentOptions.Left;

        SetTextRect(currentFrequencyLabel.rectTransform, new Vector2(-36f, -646f), new Vector2(320f, 24f), new Vector2(1f, 1f));
        currentFrequencyLabel.fontSize = 18f;
        currentFrequencyLabel.alignment = TextAlignmentOptions.Right;

        targetLabel.gameObject.SetActive(false);
        targetFrequencyLabel.gameObject.SetActive(false);
        currentNoteNameLabel.gameObject.SetActive(false);
        currentFrequencyLabel.gameObject.SetActive(false);
    }

    private void SetTextRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size, Vector2? pivotOverride = null)
    {
        Vector2 pivot = pivotOverride ?? new Vector2(0.5f, 1f);
        rect.anchorMin = new Vector2(pivot.x, 1f);
        rect.anchorMax = new Vector2(pivot.x, 1f);
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private void EnsureSourceToggleButton()
    {
        RectTransform card = FindRequiredChild("Card") as RectTransform;
        Transform existing = transform.Find("Card/SourceToggleButton");
        if (existing != null)
        {
            sourceToggleButton = existing.GetComponent<Button>() ?? existing.gameObject.AddComponent<Button>();
            sourceToggleLabel = existing.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (sourceToggleLabel == null)
            {
                sourceToggleLabel = CreateFillLabel(existing as RectTransform, "切到播放检测");
            }
            return;
        }

        sourceToggleButton = CreateButton(card, "SourceToggleButton", "切到播放检测", new Vector2(1f, 0f), new Vector2(-40f, 26f), new Vector2(240f, 54f), null);
        sourceToggleLabel = sourceToggleButton.transform.Find("Label").GetComponent<TextMeshProUGUI>();
    }

    private void RefreshSourceToggleUi()
    {
        bool micMode = pitchTracker.CurrentInputSource == PitchInputSource.Microphone;
        sourceToggleButton.GetComponent<Image>().color = micMode ? accentColor : okayColor;
        sourceToggleLabel.text = micMode ? "切到播放检测" : "切到麦克风";
        sourceToggleLabel.color = new Color(0.12f, 0.18f, 0.16f, 1f);
    }

    private void UpdateGauge(float centsOffset, Color color)
    {
        if (needlePivot == null)
        {
            return;
        }

        float clamped = Mathf.Clamp(centsOffset, -50f, 50f);
        float angle = Mathf.Lerp(90f, -90f, Mathf.InverseLerp(-50f, 50f, clamped));
        needlePivot.localEulerAngles = new Vector3(0f, 0f, angle);

        Image needle = needlePivot.Find("Needle")?.GetComponent<Image>();
        Image knob = needlePivot.Find("Knob")?.GetComponent<Image>();
        if (needle != null)
        {
            needle.color = color;
        }
        if (knob != null)
        {
            knob.color = color;
        }
    }

    private TextMeshProUGUI FindLabel(string path)
    {
        return FindRequiredChild(path).GetComponent<TextMeshProUGUI>();
    }

    private Button FindRequiredButton(string path)
    {
        return FindRequiredChild(path).GetComponent<Button>();
    }

    private Transform FindRequiredChild(string path)
    {
        Transform target = transform.Find(path);
        if (target == null)
        {
            throw new MissingReferenceException($"Cannot find child '{path}' under TunerPanel.");
        }
        return target;
    }

    private RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    private RectTransform CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        go.GetComponent<Image>().color = color;
        return rect;
    }

    private TextMeshProUGUI CreateText(string name, RectTransform parent, string text, float fontSize, Color color, Vector2 anchoredPosition, Vector2 size, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        label.font = uiFont;
        label.fontSize = fontSize;
        label.color = color;
        label.text = text;
        label.alignment = alignment;
        return label;
    }

    private TextMeshProUGUI CreateFillLabel(RectTransform parent, string text)
    {
        GameObject go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        label.font = uiFont;
        label.fontSize = 22f;
        label.alignment = TextAlignmentOptions.Center;
        label.text = text;
        label.color = Color.white;
        label.fontStyle = FontStyles.Bold;
        return label;
    }

    private Button CreateButton(RectTransform parent, string name, string text, Vector2 anchoredPosition, Vector2 size, UnityAction onClick)
    {
        return CreateButton(parent, name, text, new Vector2(0f, 1f), anchoredPosition, size, onClick);
    }

    private Button CreateButton(RectTransform parent, string name, string text, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, UnityAction onClick)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.color = dimCardColor;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }

        CreateFillLabel(rect, text);
        return button;
    }
}
