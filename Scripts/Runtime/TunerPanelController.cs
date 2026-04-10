using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class TunerPanelController : MonoBehaviour
{
    private readonly Color okayColor = new Color(0.45f, 0.81f, 0.54f, 1f);
    private readonly Color warningColor = new Color(0.93f, 0.55f, 0.29f, 1f);
    private readonly Color dangerColor = new Color(0.78f, 0.28f, 0.24f, 1f);
    private readonly Color softTextColor = new Color(0.94f, 0.9f, 0.82f, 1f);
    private readonly Color accentColor = new Color(0.88f, 0.7f, 0.28f, 1f);

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
    private RectTransform needlePivot;
    private Button listeningTabButton;
    private Button targetTabButton;
    private Image listeningTabImage;
    private Image targetTabImage;
    private GameObject listeningPage;
    private GameObject targetPage;
    private Button sourceToggleButton;
    private readonly List<Button> fluteKeyButtons = new List<Button>();
    private readonly List<TextMeshProUGUI> fluteKeyButtonLabels = new List<TextMeshProUGUI>();
    private RectTransform fluteKeyButtonRoot;
    private RectTransform fluteKeyButtonContent;
    private Button tongueFiveButton;
    private Button tongueTwoButton;
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

    private void Awake()
    {
        pitchTracker = GetComponent<MicrophonePitchTracker>();
        if (pitchTracker == null)
        {
            pitchTracker = gameObject.AddComponent<MicrophonePitchTracker>();
        }

        navigationController = FindFirstObjectByType<PracticeNavigationController>();

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
        EnsureSourceToggleButton();
        needlePivot = FindRequiredChild("Card/Gauge/NeedlePivot") as RectTransform;
        listeningTabButton = FindRequiredButton("Card/TabRow/ListeningTab");
        targetTabButton = FindRequiredButton("Card/TabRow/TargetTab");
        listeningTabImage = listeningTabButton.GetComponent<Image>();
        targetTabImage = targetTabButton.GetComponent<Image>();
        listeningPage = FindRequiredChild("Card/ListeningPage").gameObject;
        targetPage = FindRequiredChild("Card/TargetPage").gameObject;
        fluteKeyButtonRoot = FindRequiredChild("Card/TargetPage/FluteKeyButtons") as RectTransform;
        tongueFiveButton = FindRequiredButton("Card/TargetPage/ToneFiveButton");
        tongueTwoButton = FindRequiredButton("Card/TargetPage/ToneTwoButton");
        lowRow = FindRequiredChild("Card/TargetPage/LowRow/Track") as RectTransform;
        midRow = FindRequiredChild("Card/TargetPage/MidRow/Track") as RectTransform;
        highRow = FindRequiredChild("Card/TargetPage/HighRow/Track") as RectTransform;

        FindRequiredButton("Card/BackButton").onClick.AddListener(HandleBack);
        sourceToggleButton.onClick.AddListener(HandleSourceToggle);
        listeningTabButton.onClick.AddListener(() => SetMode(TunerMode.Listening));
        targetTabButton.onClick.AddListener(() => SetMode(TunerMode.TargetSelection));
        tongueFiveButton.onClick.AddListener(() => SetTongueMode(TongueMode.Five));
        tongueTwoButton.onClick.AddListener(() => SetTongueMode(TongueMode.Two));

        SetupFluteKeyButtons();

        BindRow("Card/TargetPage/LowRow/Track", lowOptionButtons, lowTopLabels, lowBottomLabels, RegisterBand.Low);
        BindRow("Card/TargetPage/MidRow/Track", midOptionButtons, midTopLabels, midBottomLabels, RegisterBand.Mid);
        BindRow("Card/TargetPage/HighRow/Track", highOptionButtons, highTopLabels, highBottomLabels, RegisterBand.High);

        SetMode(TunerMode.Listening);
        RefreshTargetOptions();
        RefreshSourceToggleUi();
    }

    private void Update()
    {
        RenderPitchPanel();
    }

    private void HandleBack()
    {
        navigationController.ShowHome();
    }

    private void SetMode(TunerMode mode)
    {
        currentMode = mode;
        listeningPage.SetActive(mode == TunerMode.Listening);
        targetPage.SetActive(mode == TunerMode.TargetSelection);
        listeningTabImage.color = mode == TunerMode.Listening ? accentColor : new Color(1f, 1f, 1f, 0.08f);
        targetTabImage.color = mode == TunerMode.TargetSelection ? accentColor : new Color(1f, 1f, 1f, 0.08f);
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
        RefreshTargetOptions();
    }

    private void RenderPitchPanel()
    {
        PitchFrame frame = pitchTracker.CurrentFrame;
        micStatusLabel.text = pitchTracker.MicrophoneStatus;
        RefreshSourceToggleUi();

        if (!frame.HasSignal)
        {
            noteLabel.text = "--";
            hintLabel.text = GetIdleHint();
            hintLabel.color = softTextColor;
            frequencyLabel.text = "频率 -- Hz";
            centsLabel.text = "偏差 -- cents";
            targetLabel.text = currentMode == TunerMode.Listening ? "当前音名 --" : $"目标音名 {GetSelectedOption().PitchLabel}";
            targetFrequencyLabel.text = currentMode == TunerMode.Listening ? "目标频率 -- Hz" : $"目标频率 {GetSelectedOption().Frequency:0.00} Hz";
            currentNoteNameLabel.text = "当前音名 --";
            currentFrequencyLabel.text = "当前频率 -- Hz";
            registerHintLabel.text = currentMode == TunerMode.Listening ? "听音模式" : GetSelectedOption().DisplayLabel;
            UpdateGauge(0f, softTextColor);
            RefreshTargetSelectionUi();
            return;
        }

        if (!frame.HasPitch)
        {
            noteLabel.text = "--";
            hintLabel.text = GetIdleHint();
            hintLabel.color = softTextColor;
            frequencyLabel.text = "频率 -- Hz";
            centsLabel.text = "偏差 -- cents";
            targetLabel.text = currentMode == TunerMode.Listening ? "当前音名 --" : $"目标音名 {GetSelectedOption().PitchLabel}";
            targetFrequencyLabel.text = currentMode == TunerMode.Listening ? "目标频率 -- Hz" : $"目标频率 {GetSelectedOption().Frequency:0.00} Hz";
            currentNoteNameLabel.text = "当前音名 --";
            currentFrequencyLabel.text = "当前频率 -- Hz";
            registerHintLabel.text = currentMode == TunerMode.Listening ? "听音模式" : GetSelectedOption().DisplayLabel;
            UpdateGauge(0f, softTextColor);
            RefreshTargetSelectionUi();
            return;
        }

        NoteInfo displayNote = frame.NearestNote;
        float targetFrequency = currentMode == TunerMode.Listening ? displayNote.ReferenceFrequency : GetSelectedOption().Frequency;
        float centsOffset = 1200f * Mathf.Log(frame.Frequency / targetFrequency, 2f);
        string targetNoteName = currentMode == TunerMode.Listening
            ? displayNote.DisplayName
            : PitchMath.GetNearestNote(targetFrequency).DisplayName;

        noteLabel.text = displayNote.DisplayName;
        hintLabel.text = PitchMath.GetTuningHint(centsOffset);
        hintLabel.color = GetHintColor(centsOffset);
        frequencyLabel.text = $"频率 {frame.Frequency:0.0} Hz";
        centsLabel.text = $"偏差 {centsOffset:+0.0;-0.0;0.0} cents";
        targetLabel.text = $"目标音名 {targetNoteName}";
        targetFrequencyLabel.text = $"目标频率 {targetFrequency:0.00} Hz";
        currentNoteNameLabel.text = $"当前音名 {displayNote.DisplayName}";
        currentFrequencyLabel.text = $"当前频率 {frame.Frequency:0.00} Hz";
        registerHintLabel.text = currentMode == TunerMode.Listening ? "听音模式" : GetSelectedOption().DisplayLabel;
        UpdateGauge(centsOffset, GetHintColor(centsOffset));
        RefreshTargetSelectionUi();
    }

    private void UpdateGauge(float centsOffset, Color color)
    {
        float clamped = Mathf.Clamp(centsOffset, -50f, 50f);
        float angle = Mathf.Lerp(90f, -90f, Mathf.InverseLerp(-50f, 50f, clamped));
        needlePivot.localEulerAngles = new Vector3(0f, 0f, angle);
        needlePivot.Find("Needle").GetComponent<Image>().color = color;
        needlePivot.Find("Knob").GetComponent<Image>().color = color;
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

    private void RefreshTargetOptions()
    {
        currentOptions = BambooFluteTargetLibrary.BuildOptions(fluteKeyIndex, currentTongueMode);
        if (selectedOptionIndex >= currentOptions.Count)
        {
            selectedOptionIndex = Mathf.Clamp(selectedOptionIndex, 0, currentOptions.Count - 1);
        }

        IReadOnlyList<string> fluteLabels = BambooFluteTargetLibrary.GetFluteKeyLabels();
        for (int i = 0; i < fluteKeyButtons.Count; i++)
        {
            bool selected = i == fluteKeyIndex;
            fluteKeyButtons[i].GetComponent<Image>().color = selected ? accentColor : new Color(1f, 1f, 1f, 0.08f);
            fluteKeyButtonLabels[i].text = fluteLabels[i];
            fluteKeyButtonLabels[i].color = selected ? new Color(0.12f, 0.18f, 0.16f, 1f) : new Color(0.94f, 0.9f, 0.82f, 1f);
        }
        RefreshTargetSelectionUi();
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

        Button template = fluteKeyButtonRoot.GetComponentInChildren<Button>(true);
        return template;
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

    private void RefreshTargetSelectionUi()
    {
        if (currentOptions == null || currentOptions.Count == 0)
        {
            return;
        }

        tongueFiveButton.GetComponent<Image>().color = currentTongueMode == TongueMode.Five ? accentColor : new Color(1f, 1f, 1f, 0.08f);
        tongueTwoButton.GetComponent<Image>().color = currentTongueMode == TongueMode.Two ? accentColor : new Color(1f, 1f, 1f, 0.08f);

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

            int xIndex = activeCount <= 4 ? i : i;
            float x = startX + xIndex * spacing;
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
            if (Mathf.Abs(item.Frequency - option.Frequency) < 0.01f && item.RegisterBand == option.RegisterBand && item.DegreeLabel == option.DegreeLabel)
            {
                return i;
            }
        }

        return -1;
    }

    private void BindRow(string rowPath, List<Button> buttons, List<TextMeshProUGUI> topLabels, List<TextMeshProUGUI> bottomLabels, RegisterBand band)
    {
        int count = 7;
        for (int i = 0; i < count; i++)
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

    private void HandleSourceToggle()
    {
        pitchTracker.ToggleInputSource();
        RefreshSourceToggleUi();
    }

    private void RefreshSourceToggleUi()
    {
        if (sourceToggleButton == null || sourceToggleLabel == null)
        {
            return;
        }

        bool micMode = pitchTracker.CurrentInputSource == PitchInputSource.Microphone;
        sourceToggleLabel.text = micMode ? "切到播放检测" : "切到麦克风";
        sourceToggleButton.GetComponent<Image>().color = micMode ? new Color(0.88f, 0.7f, 0.28f, 1f) : new Color(0.45f, 0.81f, 0.54f, 1f);
        sourceToggleLabel.color = new Color(0.12f, 0.18f, 0.16f, 1f);
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
        buttonImage.color = new Color(0.88f, 0.7f, 0.28f, 1f);

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

    private string GetIdleHint()
    {
        if (pitchTracker.CurrentInputSource == PitchInputSource.Playback)
        {
            return "等待播放声音";
        }

        return pitchTracker.IsReady ? "请开始吹奏" : "等待麦克风";
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
