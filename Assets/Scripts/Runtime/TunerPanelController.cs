using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
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
    private RectTransform listeningNeedlePivot;
    private RectTransform targetNeedlePivot;
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

        noteLabel = FindLabel("Content/Pages/TargetPage/NoteLabel");
        hintLabel = FindLabel("Content/Pages/TargetPage/HintLabel");
        frequencyLabel = FindLabel("Content/Pages/TargetPage/FrequencyLabel");
        centsLabel = FindLabel("Content/Pages/TargetPage/CentsLabel");
        targetLabel = FindLabel("Content/Pages/TargetPage/TargetLabel");
        micStatusLabel = FindLabel("Content/MicStatusLabel");
        targetFrequencyLabel = FindLabel("Content/Pages/TargetPage/TargetFrequencyLabel");
        currentNoteNameLabel = FindLabel("Content/Pages/TargetPage/CurrentNoteNameLabel");
        currentFrequencyLabel = FindLabel("Content/Pages/TargetPage/CurrentFrequencyLabel");
        registerHintLabel = FindLabel("Content/Pages/TargetPage/RegisterHintLabel");
        listeningNeedlePivot = FindRequiredChild("Content/Pages/ListeningPage/Gauge/NeedlePivot") as RectTransform;
        targetNeedlePivot = FindRequiredChild("Content/Pages/TargetPage/Gauge/NeedlePivot") as RectTransform;

        listeningPage = FindRequiredChild("Content/Pages/ListeningPage").gameObject;
        targetPage = FindRequiredChild("Content/Pages/TargetPage").gameObject;
        listeningTabButton = FindRequiredButton("Content/TabRow/ListeningTab");
        targetTabButton = FindRequiredButton("Content/TabRow/TargetTab");
        listeningTabImage = listeningTabButton.GetComponent<Image>();
        targetTabImage = targetTabButton.GetComponent<Image>();

        fluteKeyButtonRoot = FindRequiredChild("Content/Pages/TargetPage/TargetSelect/FluteKeyScroller") as RectTransform;
        targetTongueFiveButton = FindRequiredButton("Content/Pages/TargetPage/TargetSelect/ToneFiveButton");
        targetTongueTwoButton = FindRequiredButton("Content/Pages/TargetPage/TargetSelect/ToneTwoButton");
        lowRow = FindRequiredChild("Content/Pages/TargetPage/TargetSelect/LowRow/Track") as RectTransform;
        midRow = FindRequiredChild("Content/Pages/TargetPage/TargetSelect/MidRow/Track") as RectTransform;
        highRow = FindRequiredChild("Content/Pages/TargetPage/TargetSelect/HighRow/Track") as RectTransform;

        FindRequiredButton("Content/BackButton").onClick.AddListener(HandleBack);
        listeningTabButton.onClick.AddListener(ShowListeningMode);
        targetTabButton.onClick.AddListener(ShowTargetMode);

        EnsureSourceToggleButton();
        sourceToggleButton.onClick.AddListener(HandleSourceToggle);
        targetTongueFiveButton.onClick.AddListener(() => SetTongueMode(TongueMode.Five));
        targetTongueTwoButton.onClick.AddListener(() => SetTongueMode(TongueMode.Two));

        BindListeningUi();
        SetupFluteKeyButtons();
        BindRow("Content/Pages/TargetPage/TargetSelect/LowRow/Track", lowOptionButtons, lowTopLabels, lowBottomLabels, RegisterBand.Low);
        BindRow("Content/Pages/TargetPage/TargetSelect/MidRow/Track", midOptionButtons, midTopLabels, midBottomLabels, RegisterBand.Mid);
        BindRow("Content/Pages/TargetPage/TargetSelect/HighRow/Track", highOptionButtons, highTopLabels, highBottomLabels, RegisterBand.High);

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
        float clamped = Mathf.Clamp(centsOffset, -50f, 50f);
        float angle = Mathf.Lerp(90f, -90f, Mathf.InverseLerp(-50f, 50f, clamped));
        UpdateGaugeNeedle(listeningNeedlePivot, angle, color);
        UpdateGaugeNeedle(targetNeedlePivot, angle, color);
    }

    private static void UpdateGaugeNeedle(RectTransform needlePivot, float angle, Color color)
    {
        if (needlePivot == null)
        {
            return;
        }

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

    private void BindListeningUi()
    {
        listeningRoot = FindRequiredChild("Content/Pages/ListeningPage") as RectTransform;
        keyScrollerContent = FindRequiredChild("Content/Pages/ListeningPage/TopControls/KeyScroller/Viewport/Content") as RectTransform;
        tongueFiveButton = FindRequiredButton("Content/Pages/ListeningPage/TopControls/TongueFive");
        tongueTwoButton = FindRequiredButton("Content/Pages/ListeningPage/TopControls/TongueTwo");
        tongueFiveLabel = FindLabel("Content/Pages/ListeningPage/TopControls/TongueFive/Label");
        tongueTwoLabel = FindLabel("Content/Pages/ListeningPage/TopControls/TongueTwo/Label");
        listeningTitleLabel = FindLabel("Content/Pages/ListeningPage/ListeningTitle");

        registerSummary.Clear();
        BindRegisterCard(RegisterBand.Low, "Content/Pages/ListeningPage/SummaryRow/LowCard");
        BindRegisterCard(RegisterBand.Mid, "Content/Pages/ListeningPage/SummaryRow/MidCard");
        BindRegisterCard(RegisterBand.High, "Content/Pages/ListeningPage/SummaryRow/HighCard");

        trailView = FindRequiredChild("Content/Pages/ListeningPage/GraphCard/GraphViewport").GetComponent<ListeningPitchTrailView>();
        if (trailView == null)
        {
            throw new MissingComponentException("Missing ListeningPitchTrailView on 'Content/Pages/ListeningPage/GraphCard/GraphViewport'.");
        }

        trailView.Configure(listeningTitleLabel.font);
        BindListeningKeyButtons();
    }

    private void BindListeningKeyButtons()
    {
        keyButtons.Clear();
        keyButtonLabels.Clear();

        for (int i = 0; i < keyScrollerContent.childCount; i++)
        {
            Transform child = keyScrollerContent.GetChild(i);
            Button button = child.GetComponent<Button>();
            TextMeshProUGUI label = child.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (button == null || label == null)
            {
                continue;
            }

            int keyIndex = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectFluteKey(keyIndex));
            keyButtons.Add(button);
            keyButtonLabels.Add(label);
        }
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

    private void BindRegisterCard(RegisterBand band, string path)
    {
        RegisterSummaryUi ui = new RegisterSummaryUi
        {
            Card = FindRequiredChild(path).GetComponent<Image>(),
            Title = FindLabel($"{path}/Title"),
            Main = FindLabel($"{path}/Main"),
            Detail = FindLabel($"{path}/Detail"),
        };
        registerSummary[band] = ui;
    }

    private void SetupFluteKeyButtons()
    {
        if (fluteKeyButtonRoot == null)
        {
            return;
        }

        fluteKeyButtonContent = FindRequiredChild("Content/Pages/TargetPage/TargetSelect/FluteKeyScroller/Viewport/Content") as RectTransform;
        ScrollRect existingScrollRect = fluteKeyButtonRoot.GetComponent<ScrollRect>();
        if (existingScrollRect != null)
        {
            existingScrollRect.horizontal = true;
            existingScrollRect.vertical = false;
            existingScrollRect.movementType = ScrollRect.MovementType.Clamped;
            existingScrollRect.inertia = true;
            existingScrollRect.scrollSensitivity = 24f;
            existingScrollRect.viewport = FindRequiredChild("Content/Pages/TargetPage/TargetSelect/FluteKeyScroller/Viewport") as RectTransform;
            existingScrollRect.content = fluteKeyButtonContent;
            existingScrollRect.horizontalScrollbar = null;
            existingScrollRect.verticalScrollbar = null;
        }

        fluteKeyButtons.Clear();
        fluteKeyButtonLabels.Clear();

        for (int i = 0; i < fluteKeyButtonContent.childCount; i++)
        {
            Transform child = fluteKeyButtonContent.GetChild(i);
            Button button = child.GetComponent<Button>();
            TextMeshProUGUI label = child.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (button == null || label == null)
            {
                continue;
            }

            int keyIndex = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectFluteKey(keyIndex));
            fluteKeyButtons.Add(button);
            fluteKeyButtonLabels.Add(label);
        }
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

    private void EnsureSourceToggleButton()
    {
        Transform existing = transform.Find("Content/SourceToggleButton");
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

        RectTransform card = FindRequiredChild("Content") as RectTransform;
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

}
