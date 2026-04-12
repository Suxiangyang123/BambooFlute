using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MetronomePanelController : MonoBehaviour
{
    private readonly Color okayColor = new Color(0.45f, 0.81f, 0.54f, 1f);
    private readonly Color accentColor = new Color(0.88f, 0.7f, 0.28f, 1f);

    private PracticeNavigationController navigationController;
    private Metronome metronome;

    private TextMeshProUGUI bpmLabel;
    private TextMeshProUGUI meterLabel;
    private TextMeshProUGUI toggleButtonLabel;
    private Image toggleButtonImage;

private void Awake()
    {
        navigationController = FindAnyObjectByType<PracticeNavigationController>();
        metronome = GetComponent<Metronome>();
        if (metronome == null)
        {
            metronome = gameObject.AddComponent<Metronome>();
        }

        bpmLabel = FindLabel("Card/BpmLabel");
        meterLabel = FindLabel("Card/MeterLabel");
        toggleButtonLabel = FindLabel("Card/ToggleButton/Label");
        toggleButtonImage = FindRequiredButton("Card/ToggleButton").GetComponent<Image>();

        FindRequiredButton("Card/BackButton").onClick.AddListener(HandleBack);
        FindRequiredButton("Card/ToggleButton").onClick.AddListener(() => metronome.Toggle());
        FindRequiredButton("Card/BpmMinus5Button").onClick.AddListener(() => metronome.AdjustBpm(-5));
        FindRequiredButton("Card/BpmMinus1Button").onClick.AddListener(() => metronome.AdjustBpm(-1));
        FindRequiredButton("Card/BpmPlus1Button").onClick.AddListener(() => metronome.AdjustBpm(1));
        FindRequiredButton("Card/BpmPlus5Button").onClick.AddListener(() => metronome.AdjustBpm(5));
        FindRequiredButton("Card/Meter2Button").onClick.AddListener(() => metronome.SetBeatsPerBar(2));
        FindRequiredButton("Card/Meter3Button").onClick.AddListener(() => metronome.SetBeatsPerBar(3));
        FindRequiredButton("Card/Meter4Button").onClick.AddListener(() => metronome.SetBeatsPerBar(4));
        FindRequiredButton("Card/Meter6Button").onClick.AddListener(() => metronome.SetBeatsPerBar(6));
    }

    private void Update()
    {
        bpmLabel.text = $"{metronome.Bpm} BPM";
        meterLabel.text = $"{metronome.BeatsPerBar} / 4";
        toggleButtonLabel.text = metronome.IsPlaying ? "停止节拍器" : "开始节拍器";
        toggleButtonImage.color = metronome.IsPlaying ? accentColor : okayColor;
    }

    private void HandleBack()
    {
        metronome.Stop();
        navigationController.ShowHome();
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
            throw new MissingReferenceException($"Cannot find child '{path}' under MetronomePanel.");
        }

        return target;
    }
}
