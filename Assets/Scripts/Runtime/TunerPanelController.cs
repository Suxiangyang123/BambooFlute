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
    private readonly Color softTextColor = new Color(0.94f, 0.9f, 0.82f, 1f);
    private readonly Color accentColor = new Color(0.88f, 0.7f, 0.28f, 1f);
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
    private TextMeshProUGUI sourceToggleLabel;
    private RectTransform needlePivot;
    private Button listeningTabButton;
    private Button targetTabButton;
    private Image listeningTabImage;
    private Image targetTabImage;
    private GameObject listeningPage;
    private GameObject targetPage;
    private Button sourceToggleButton;

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

    private readonly List<Button> fluteKeyButtons = new List<Button>();
    private readonly List<TextMeshProUGUI> fluteKeyButtonLabels = new List<TextMeshProUGUI>();
    private RectTransform fluteKeyButtonRoot;
    private RectTransform fluteKeyButtonContent;
    private Button targetTongueFiveButton;
    private Button targetTongueTwoButton;
    private readonly List<Button> lowOptionButtons = new List<Button>();
    private readonly List<Button> midOptionButtons = new List<Button>();
    private readonly List<Button> highOptionButtons = new List<Button>();
    private readonly List<TextMeshProUGUI> lowTopLabels = new List<TextMeshProUGUI>();
    private readonly List<TextMeshProUGUI> midTopLabels = new List<TextMeshProUGUI>();
    private readonly List<TextMeshProUGUI> highTopLabels = new List<TextMeshProUGUI>();
    private readonly List<TextMeshProUGUI> lowBottomLabels = new List<TextMeshProUGUI>();
    private readonly List<TextMeshProUGUI> midBottomLabels = new List<TextMeshProUGUI>();
    private readonly List<TextMeshProUGUI> highBottomLabels = new List<TextMeshProUGUI>();
    private RectTransform lowRow;
    private RectTransform midRow;
    private RectTransform highRow;

    private TunerMode currentMode = TunerMode.Listening;
    private TongueMode currentTongueMode = TongueMode.Five;
    private int fluteKeyIndex = BambooFluteTargetLibrary.DefaultFluteKeyIndex;
    private int selectedOptionIndex = 4;
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

        fluteKeyButtonRoot = FindRequiredChild("Card/TargetPage/FluteKeyButtons") as RectTransform;
        targetTongueFiveButton = FindRequiredButton("Card/TargetPage/ToneFiveButton");
        targetTongueTwoButton = FindRequiredButton("Card/TargetPage/ToneTwoButton");
        lowRow = FindRequiredChild("Card/TargetPage/LowRow/Track") as RectTransform;
        midRow = FindRequiredChild("Card/TargetPage/MidRow/Track") as RectTransform;
        highRow = FindRequiredChild("Card/TargetPage/HighRow/Track") as RectTransform;

        FindRequiredButton("Card/BackButton").onClick.AddListener(HandleBack);
        listeningTabButton.onClick.AddListener(ShowListeningMode);
        targetTabButton.onClick.AddListener(ShowTargetMode);

        EnsureSourceToggleButton();
        sourceToggleButton.onClick.AddListener(HandleSourceToggle);
        targetTongueFiveButton.onClick.AddListener(() => SetTongueMode(TongueMode.Five));
        targetTongueTwoButton.onClick.AddListener(() => SetTongueMode(TongueMode.Two));

        ApplyRuntimeFont();
        ConfigureLegacyGaugeLayout();
        BuildListeningUi();
        SetupFluteKeyButtons();
        BindRow("Card/TargetPage/LowRow/Track", lowOptionButtons, lowTopLabels, lowBottomLabels, RegisterBand.Low);
        BindRow("Card/TargetPage/MidRow/Track", midOptionButtons, midTopLabels, midBottomLabels, RegisterBand.Mid);
        BindRow("Card/TargetPage/HighRow/Track", highOptionButtons, highTopLabels, highBottomLabels, RegisterBand.High);

        RefreshTargetOptions();
        RefreshSourceToggleUi();
        ShowListeningMode();
    }

    private void Update()
    {
        micStatusLabel.text = pitchTracker.MicrophoneStatus;
        RefreshSourceToggleUi();

        if (currentMode == TunerMode.Listening)
        {
            RenderListening();
        }
        else
        {
            RenderTargetSelection();
        }
    }

    private void HandleBack()
    {
        navigationController?.ShowHome();
    }

    private void HandleSourceToggle()
    {
        pitchTracker.ToggleInputSource();
        trailView?.Clear();
        RefreshSourceToggleUi();
    }

    private void ShowListeningMode()
    {
        currentMode = TunerMode.Listening;
        listeningPage.SetActive(true);
        targetPage.SetActive(false);
        if (listeningRoot != null)
        {
            listeningRoot.gameObject.SetActive(true);
        }

        targetLabel.gameObject.SetActive(false);
        targetFrequencyLabel.gameObject.SetActive(false);
        currentNoteNameLabel.gameObject.SetActive(false);
        currentFrequencyLabel.gameObject.SetActive(false);

        listeningTabImage.color = accentColor;
        targetTabImage.color = dimCardColor;
    }

    private void ShowTargetMode()
    {
        currentMode = TunerMode.TargetSelection;
        listeningPage.SetActive(false);
        targetPage.SetActive(true);
        if (listeningRoot != null)
        {
            listeningRoot.gameObject.SetActive(false);
        }

        targetLabel.gameObject.SetActive(true);
        targetFrequencyLabel.gameObject.SetActive(true);
        currentNoteNameLabel.gameObject.SetActive(true);
        currentFrequencyLabel.gameObject.SetActive(true);

        listeningTabImage.color = dimCardColor;
        targetTabImage.color = accentColor;
    }

    private void SetTongueMode(TongueMode tongueMode)
    {
        currentTongueMode = tongueMode;
        RefreshTargetOptions();
    }

    private void SelectFluteKey(int value)
    {
        fluteKeyIndex = value;
        RefreshTargetOptions();
    }

    private void SelectTargetOption(int optionIndex)
    {
        selectedOptionIndex = optionIndex;
        RefreshTargetSelectionUi();
    }

    private void RefreshTargetOptions()
    {
        currentOptions = BambooFluteTargetLibrary.BuildOptions(fluteKeyIndex, currentTongueMode);
        if (selectedOptionIndex >= currentOptions.Count)
        {
            selectedOptionIndex = Mathf.Clamp(selectedOptionIndex, 0, currentOptions.Count - 1);
        }

        UpdateListeningKeyButtons();
        UpdateListeningTongueButtons();
        UpdateTargetKeyButtons();
        RefreshTargetSelectionUi();
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

    private void RenderTargetSelection()
    {
        if (currentOptions == null || currentOptions.Count == 0)
        {
            return;
        }

        PitchFrame frame = pitchTracker.CurrentFrame;
        TargetNoteOption selected = GetSelectedOption();

        if (!frame.HasSignal || !frame.HasPitch)
        {
            noteLabel.text = selected.DegreeLabel;
            hintLabel.text = GetIdleHint();
            hintLabel.color = softTextColor;
            frequencyLabel.text = "频率 -- Hz";
            centsLabel.text = "偏差 -- cents";
            targetLabel.text = $"目标音名 {selected.PitchLabel}";
            targetFrequencyLabel.text = $"目标频率 {selected.Frequency:0.00} Hz";
            currentNoteNameLabel.text = "当前音名 --";
            currentFrequencyLabel.text = "当前频率 -- Hz";
            registerHintLabel.text = selected.DisplayLabel;
            UpdateGauge(0f, softTextColor);
            return;
        }

        float centsOffset = GetCents(frame.Frequency, selected.Frequency);
        NoteInfo displayNote = frame.NearestNote;

        noteLabel.text = selected.DegreeLabel;
        hintLabel.text = PitchMath.GetTuningHint(centsOffset);
        hintLabel.color = GetHintColor(centsOffset);
        frequencyLabel.text = $"频率 {frame.Frequency:0.0} Hz";
        centsLabel.text = $"偏差 {centsOffset:+0.0;-0.0;0.0} cents";
        targetLabel.text = $"目标音名 {selected.PitchLabel}";
        targetFrequencyLabel.text = $"目标频率 {selected.Frequency:0.00} Hz";
        currentNoteNameLabel.text = $"当前音名 {displayNote.DisplayName}";
        currentFrequencyLabel.text = $"当前频率 {frame.Frequency:0.00} Hz";
        registerHintLabel.text = selected.DisplayLabel;
        UpdateGauge(centsOffset, GetHintColor(centsOffset));
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
        tongueFiveButton = CreateButton(topCard, "TongueFive", "筒音作5", new Vector2(18f, -132f), new Vector2(150f, 36f), () => SetTongueMode(TongueMode.Five));
        tongueTwoButton = CreateButton(topCard, "TongueTwo", "筒音作2", new Vector2(180f, -132f), new Vector2(150f, 36f), () => SetTongueMode(TongueMode.Two));
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

        BuildListeningKeyButtons();
    }

    private void BuildListeningKeyButtons()
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
            Button button = CreateButton(keyScrollerContent, $"Key{keyIndex}", labels[i], new Vector2(i * (buttonWidth + gap), -2f), new Vector2(buttonWidth, 34f), () => SelectFluteKey(keyIndex));
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            keyButtons.Add(button);
            keyButtonLabels.Add(button.transform.Find("Label").GetComponent<TextMeshProUGUI>());
        }

        keyScrollerContent.sizeDelta = new Vector2(labels.Count * (buttonWidth + gap), 40f);
    }

    private void UpdateListeningKeyButtons()
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

    private void UpdateListeningTongueButtons()
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

    private void SetupFluteKeyButtons()
    {
        if (fluteKeyButtonRoot == null)
        {
            return;
        }

        IReadOnlyList<string> fluteLabels = BambooFluteTargetLibrary.GetFluteKeyLabels();
        Button templateButton = FindTemplateFluteKeyButton();
        TextMeshProUGUI templateLabel = templateButton != null ? templateButton.transform.Find("Label")?.GetComponent<TextMeshProUGUI>() : null;

        EnsureFluteKeyScroller();

        for (int i = fluteKeyButtonContent.childCount - 1; i >= 0; i--)
        {
            Destroy(fluteKeyButtonContent.GetChild(i).gameObject);
        }

        fluteKeyButtons.Clear();
        fluteKeyButtonLabels.Clear();

        const float buttonWidth = 160f;
        const float buttonHeight = 60f;
        const float buttonGap = 20f;

        for (int i = 0; i < fluteLabels.Count; i++)
        {
            Button button = CreateRuntimeFluteKeyButton(fluteKeyButtonContent, templateLabel != null ? templateLabel.font : micStatusLabel.font);
            button.name = $"Key{i + 1}";
            button.onClick.RemoveAllListeners();

            RectTransform buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 0.5f);
            buttonRect.anchorMax = new Vector2(0f, 0.5f);
            buttonRect.pivot = new Vector2(0f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(i * (buttonWidth + buttonGap), 0f);
            buttonRect.sizeDelta = new Vector2(buttonWidth, buttonHeight);

            TextMeshProUGUI label = button.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (label == null)
            {
                label = CreateRuntimeFluteKeyLabel(button.transform, templateLabel != null ? templateLabel.font : micStatusLabel.font);
            }

            int keyIndex = i;
            button.onClick.AddListener(() => SelectFluteKey(keyIndex));
            fluteKeyButtons.Add(button);
            fluteKeyButtonLabels.Add(label);
        }

        float contentWidth = fluteLabels.Count * buttonWidth + Mathf.Max(0, fluteLabels.Count - 1) * buttonGap;
        fluteKeyButtonContent.anchorMin = new Vector2(0f, 0f);
        fluteKeyButtonContent.anchorMax = new Vector2(0f, 1f);
        fluteKeyButtonContent.pivot = new Vector2(0f, 0.5f);
        fluteKeyButtonContent.anchoredPosition = Vector2.zero;
        fluteKeyButtonContent.sizeDelta = new Vector2(contentWidth, 0f);
    }

    private void EnsureFluteKeyScroller()
    {
        Transform viewportTransform = fluteKeyButtonRoot.Find("Viewport");
        RectTransform viewport;
        if (viewportTransform == null)
        {
            GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportGo.transform.SetParent(fluteKeyButtonRoot, false);
            viewport = viewportGo.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;

            List<Transform> childrenToMove = new List<Transform>();
            for (int i = 0; i < fluteKeyButtonRoot.childCount; i++)
            {
                Transform child = fluteKeyButtonRoot.GetChild(i);
                if (child != viewport)
                {
                    childrenToMove.Add(child);
                }
            }

            GameObject contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewport, false);
            fluteKeyButtonContent = contentGo.GetComponent<RectTransform>();

            foreach (Transform child in childrenToMove)
            {
                child.SetParent(fluteKeyButtonContent, false);
            }
        }
        else
        {
            viewport = viewportTransform as RectTransform;
            Transform contentTransform = viewport.Find("Content");
            if (contentTransform == null)
            {
                GameObject contentGo = new GameObject("Content", typeof(RectTransform));
                contentGo.transform.SetParent(viewport, false);
                fluteKeyButtonContent = contentGo.GetComponent<RectTransform>();

                List<Transform> childrenToMove = new List<Transform>();
                for (int i = 0; i < fluteKeyButtonRoot.childCount; i++)
                {
                    Transform child = fluteKeyButtonRoot.GetChild(i);
                    if (child != viewport)
                    {
                        childrenToMove.Add(child);
                    }
                }

                foreach (Transform child in childrenToMove)
                {
                    child.SetParent(fluteKeyButtonContent, false);
                }
            }
            else
            {
                fluteKeyButtonContent = contentTransform as RectTransform;
            }
        }

        ScrollRect scrollRect = fluteKeyButtonRoot.GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            scrollRect = fluteKeyButtonRoot.gameObject.AddComponent<ScrollRect>();
        }

        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.scrollSensitivity = 24f;
        scrollRect.viewport = viewport;
        scrollRect.content = fluteKeyButtonContent;
        scrollRect.horizontalScrollbar = null;
        scrollRect.verticalScrollbar = null;
    }

    private Button FindTemplateFluteKeyButton()
    {
        if (fluteKeyButtonRoot == null)
        {
            return null;
        }

        return fluteKeyButtonRoot.GetComponentInChildren<Button>(true);
    }

    private Button CreateRuntimeFluteKeyButton(Transform parent, TMP_FontAsset font)
    {
        GameObject buttonGo = new GameObject("Key", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(parent, false);

        Image buttonImage = buttonGo.GetComponent<Image>();
        buttonImage.color = new Color(1f, 1f, 1f, 0.08f);

        Button button = buttonGo.GetComponent<Button>();
        button.targetGraphic = buttonImage;

        CreateRuntimeFluteKeyLabel(buttonGo.transform, font);
        return button;
    }

    private TextMeshProUGUI CreateRuntimeFluteKeyLabel(Transform parent, TMP_FontAsset font)
    {
        GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(parent, false);

        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = 26f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = softTextColor;
        return label;
    }

    private void UpdateTargetKeyButtons()
    {
        IReadOnlyList<string> fluteLabels = BambooFluteTargetLibrary.GetFluteKeyLabels();
        for (int i = 0; i < fluteKeyButtons.Count; i++)
        {
            bool selected = i == fluteKeyIndex;
            fluteKeyButtons[i].GetComponent<Image>().color = selected ? accentColor : new Color(1f, 1f, 1f, 0.08f);
            fluteKeyButtonLabels[i].text = fluteLabels[i];
            fluteKeyButtonLabels[i].color = selected ? new Color(0.12f, 0.18f, 0.16f, 1f) : softTextColor;
        }
    }

    private void RefreshTargetSelectionUi()
    {
        if (currentOptions == null || currentOptions.Count == 0)
        {
            return;
        }

        targetTongueFiveButton.GetComponent<Image>().color = currentTongueMode == TongueMode.Five ? accentColor : new Color(1f, 1f, 1f, 0.08f);
        targetTongueTwoButton.GetComponent<Image>().color = currentTongueMode == TongueMode.Two ? accentColor : new Color(1f, 1f, 1f, 0.08f);

        LayoutRegisterRow(lowRow, lowOptionButtons, lowTopLabels, lowBottomLabels, currentOptions.Where(option => option.RegisterBand == RegisterBand.Low).ToList(), selectedOptionIndex);
        LayoutRegisterRow(midRow, midOptionButtons, midTopLabels, midBottomLabels, currentOptions.Where(option => option.RegisterBand == RegisterBand.Mid).ToList(), selectedOptionIndex);
        LayoutRegisterRow(highRow, highOptionButtons, highTopLabels, highBottomLabels, currentOptions.Where(option => option.RegisterBand == RegisterBand.High).ToList(), selectedOptionIndex);
    }

    private void LayoutRegisterRow(
        RectTransform row,
        List<Button> buttons,
        List<TextMeshProUGUI> topLabels,
        List<TextMeshProUGUI> bottomLabels,
        List<TargetNoteOption> rowOptions,
        int selectedGlobalIndex)
    {
        int activeCount = rowOptions.Count;
        float rowWidth = row.rect.width > 1f ? row.rect.width : 760f;
        float spacing = activeCount <= 1 ? 0f : Mathf.Min(120f, rowWidth / Mathf.Max(activeCount, 2));
        float totalWidth = spacing * Mathf.Max(0, activeCount - 1);
        float startX = -totalWidth * 0.5f;

        for (int i = 0; i < buttons.Count; i++)
        {
            bool active = i < activeCount;
            buttons[i].gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            float x = startX + i * spacing;
            RectTransform buttonRect = buttons[i].GetComponent<RectTransform>();
            buttonRect.anchoredPosition = new Vector2(x, 0f);

            topLabels[i].text = rowOptions[i].DegreeLabel;
            bottomLabels[i].text = rowOptions[i].PitchLabel;

            int globalIndex = IndexOfOption(rowOptions[i]);
            bool selected = globalIndex == selectedGlobalIndex;
            Image buttonImage = buttons[i].GetComponent<Image>();
            buttonImage.color = new Color(1f, 1f, 1f, 0.001f);

            Transform dot = buttons[i].transform.Find("Dot");
            if (dot != null)
            {
                dot.GetComponent<Image>().color = selected ? okayColor : new Color(1f, 1f, 1f, 0.24f);
            }

            topLabels[i].color = selected ? okayColor : softTextColor;
            bottomLabels[i].color = selected ? accentColor : new Color(0.78f, 0.74f, 0.68f, 1f);
        }
    }

    private TargetNoteOption GetSelectedOption()
    {
        return currentOptions[Mathf.Clamp(selectedOptionIndex, 0, currentOptions.Count - 1)];
    }

    private int IndexOfOption(TargetNoteOption option)
    {
        for (int i = 0; i < currentOptions.Count; i++)
        {
            TargetNoteOption item = currentOptions[i];
            if (Mathf.Abs(item.Frequency - option.Frequency) < 0.01f &&
                item.RegisterBand == option.RegisterBand &&
                item.DegreeLabel == option.DegreeLabel)
            {
                return i;
            }
        }

        return -1;
    }

    private void BindRow(string rowPath, List<Button> buttons, List<TextMeshProUGUI> topLabels, List<TextMeshProUGUI> bottomLabels, RegisterBand band)
    {
        for (int i = 0; i < 7; i++)
        {
            Button button = FindRequiredButton($"{rowPath}/Option{i + 1}/Circle");
            buttons.Add(button);
            topLabels.Add(FindLabel($"{rowPath}/Option{i + 1}/Circle/TopLabel"));
            bottomLabels.Add(FindLabel($"{rowPath}/Option{i + 1}/Circle/BottomLabel"));
            int slotIndex = i;
            button.onClick.AddListener(() => SelectOptionFromBand(band, slotIndex));
        }
    }

    private void SelectOptionFromBand(RegisterBand band, int slotIndex)
    {
        List<TargetNoteOption> rowOptions = currentOptions.Where(option => option.RegisterBand == band).ToList();
        if (slotIndex >= rowOptions.Count)
        {
            return;
        }

        selectedOptionIndex = IndexOfOption(rowOptions[slotIndex]);
        RefreshTargetSelectionUi();
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
        Transform existing = transform.Find("Card/SourceToggleButton");
        if (existing != null)
        {
            sourceToggleButton = existing.GetComponent<Button>();
            sourceToggleLabel = existing.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (sourceToggleButton == null)
            {
                sourceToggleButton = existing.gameObject.AddComponent<Button>();
            }

            if (existing.GetComponent<Image>() == null)
            {
                existing.gameObject.AddComponent<Image>();
            }

            if (sourceToggleLabel == null)
            {
                GameObject existingLabelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                existingLabelGo.transform.SetParent(existing, false);
                RectTransform existingLabelRect = existingLabelGo.GetComponent<RectTransform>();
                existingLabelRect.anchorMin = Vector2.zero;
                existingLabelRect.anchorMax = Vector2.one;
                existingLabelRect.offsetMin = Vector2.zero;
                existingLabelRect.offsetMax = Vector2.zero;
                sourceToggleLabel = existingLabelGo.GetComponent<TextMeshProUGUI>();
            }

            sourceToggleLabel.alignment = TextAlignmentOptions.Center;
            sourceToggleLabel.font = micStatusLabel.font;
            sourceToggleLabel.fontSize = 24f;
            sourceToggleLabel.color = new Color(0.12f, 0.18f, 0.16f, 1f);
            return;
        }

        RectTransform card = FindRequiredChild("Card") as RectTransform;
        GameObject buttonGo = new GameObject("SourceToggleButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(card, false);

        RectTransform buttonRect = buttonGo.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.anchoredPosition = new Vector2(-40f, 26f);
        buttonRect.sizeDelta = new Vector2(240f, 54f);

        Image buttonImage = buttonGo.GetComponent<Image>();
        buttonImage.color = accentColor;

        sourceToggleButton = buttonGo.GetComponent<Button>();

        GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(buttonGo.transform, false);
        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        sourceToggleLabel = labelGo.GetComponent<TextMeshProUGUI>();
        sourceToggleLabel.alignment = TextAlignmentOptions.Center;
        sourceToggleLabel.font = micStatusLabel.font;
        sourceToggleLabel.fontSize = 24f;
        sourceToggleLabel.text = "切到播放检测";
        sourceToggleLabel.color = new Color(0.12f, 0.18f, 0.16f, 1f);
    }

    private void RefreshSourceToggleUi()
    {
        if (sourceToggleButton == null || sourceToggleLabel == null)
        {
            return;
        }

        bool micMode = pitchTracker.CurrentInputSource == PitchInputSource.Microphone;
        sourceToggleLabel.text = micMode ? "切到播放检测" : "切到麦克风";
        sourceToggleButton.GetComponent<Image>().color = micMode ? accentColor : okayColor;
        sourceToggleLabel.color = new Color(0.12f, 0.18f, 0.16f, 1f);
    }

    private TextMeshProUGUI FindLabel(string path)
    {
        TextMeshProUGUI label = FindRequiredChild(path).GetComponent<TextMeshProUGUI>();
        if (label == null)
        {
            throw new MissingComponentException($"Missing TextMeshProUGUI on '{path}'.");
        }

        return label;
    }

    private Button FindRequiredButton(string path)
    {
        Button button = FindRequiredChild(path).GetComponent<Button>();
        if (button == null)
        {
            throw new MissingComponentException($"Missing Button on '{path}'.");
        }

        return button;
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
}
