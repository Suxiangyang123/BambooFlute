using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public readonly struct PitchFrame
{
    public PitchFrame(bool hasSignal, bool hasPitch, float rms, float frequency, float clarity, NoteInfo nearestNote)
    {
        HasSignal = hasSignal;
        HasPitch = hasPitch;
        Rms = rms;
        Frequency = frequency;
        Clarity = clarity;
        NearestNote = nearestNote;
    }

    public bool HasSignal { get; }
    public bool HasPitch { get; }
    public float Rms { get; }
    public float Frequency { get; }
    public float Clarity { get; }
    public NoteInfo NearestNote { get; }
}

public enum PitchInputSource
{
    Microphone,
    Playback,
}

public sealed class MicrophonePitchTracker : MonoBehaviour
{
    [SerializeField] private PitchInputSource inputSource = PitchInputSource.Microphone;
    [SerializeField] private int requestedSampleRate = 48000;
    [SerializeField] private int analysisWindowSize = 4096;
    [SerializeField] private float minFrequency = 120f;
    [SerializeField] private float maxFrequency = 1400f;
    [SerializeField] private float yinThreshold = 0.12f;
    [SerializeField] private float minSignalRms = 0.008f;
    [SerializeField] private int smoothingWindow = 5;

    private readonly Queue<float> smoothedFrequencies = new Queue<float>();

    private AudioClip microphoneClip;
    private float[] analysisSamples;
    private PitchDetector pitchDetector;
    private int detectorSampleRate;
    private int outputSampleRate;
    private int effectiveSampleRate;
    private bool microphoneReady;
    private string microphoneStatus = "检测源初始化中";
    private string activeDeviceName;

    public PitchFrame CurrentFrame { get; private set; }
    public bool IsReady => microphoneReady;
    public string MicrophoneStatus => microphoneStatus;
    public PitchInputSource CurrentInputSource => inputSource;

    private IEnumerator Start()
    {
        analysisSamples = new float[analysisWindowSize];
        outputSampleRate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : requestedSampleRate;
        ConfigureDetectorForCurrentSource();
        UpdateStatusText();

        yield return RequestMicrophoneAccess();
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            UpdateStatusText();
            yield break;
        }

        if (Microphone.devices.Length == 0)
        {
            UpdateStatusText();
            yield break;
        }

        activeDeviceName = Microphone.devices[0];
        microphoneClip = Microphone.Start(activeDeviceName, true, 1, requestedSampleRate);
        float timeout = Time.realtimeSinceStartup + 3f;
        while (Microphone.GetPosition(activeDeviceName) <= 0 && Time.realtimeSinceStartup < timeout)
        {
            yield return null;
        }

        if (Microphone.GetPosition(activeDeviceName) <= 0)
        {
            UpdateStatusText();
            yield break;
        }

        effectiveSampleRate = microphoneClip.frequency > 0 ? microphoneClip.frequency : requestedSampleRate;
        microphoneReady = true;
        ConfigureDetectorForCurrentSource();
        UpdateStatusText();
    }

    private IEnumerator RequestMicrophoneAccess()
    {
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
        }
    }

    private void Update()
    {
        if (analysisSamples == null || pitchDetector == null)
        {
            return;
        }

        if (!TryReadInputSamples())
        {
            return;
        }

        float rms = CalculateRms(analysisSamples);
        if (rms < minSignalRms)
        {
            smoothedFrequencies.Clear();
            return;
        }

        PitchDetectionResult result = pitchDetector.Detect(analysisSamples, minFrequency, maxFrequency, yinThreshold);
        if (!result.IsValid)
        {
            return;
        }

        float smoothedFrequency = SmoothFrequency(result.Frequency);
        CurrentFrame = new PitchFrame(true, true, rms, smoothedFrequency, result.Clarity, PitchMath.GetNearestNote(smoothedFrequency));
    }

    public void ToggleInputSource()
    {
        SetInputSource(inputSource == PitchInputSource.Microphone ? PitchInputSource.Playback : PitchInputSource.Microphone);
    }

    public void SetInputSource(PitchInputSource source)
    {
        if (inputSource == source)
        {
            return;
        }

        inputSource = source;
        smoothedFrequencies.Clear();
        ConfigureDetectorForCurrentSource();
        UpdateStatusText();
    }

    private void OnDisable()
    {
        if (microphoneClip != null && !string.IsNullOrEmpty(activeDeviceName) && Microphone.IsRecording(activeDeviceName))
        {
            Microphone.End(activeDeviceName);
        }
    }

    private bool TryReadInputSamples()
    {
        if (inputSource == PitchInputSource.Microphone)
        {
            if (!microphoneReady || microphoneClip == null)
            {
                return false;
            }

            int position = Microphone.GetPosition(activeDeviceName);
            if (position < analysisWindowSize)
            {
                return false;
            }

            microphoneClip.GetData(analysisSamples, position - analysisWindowSize);
            return true;
        }

        AudioListener.GetOutputData(analysisSamples, 0);
        return true;
    }

    private void ConfigureDetectorForCurrentSource()
    {
        int sampleRate = inputSource == PitchInputSource.Microphone && microphoneReady
            ? effectiveSampleRate
            : outputSampleRate;
        sampleRate = sampleRate > 0 ? sampleRate : requestedSampleRate;

        if (pitchDetector != null && detectorSampleRate == sampleRate)
        {
            return;
        }

        detectorSampleRate = sampleRate;
        pitchDetector = new PitchDetector(sampleRate, analysisWindowSize);
    }

    private void UpdateStatusText()
    {
        if (inputSource == PitchInputSource.Playback)
        {
            microphoneStatus = $"检测源: 播放检测 ({outputSampleRate} Hz)";
            return;
        }

        if (microphoneReady)
        {
            microphoneStatus = $"检测源: 麦克风 {activeDeviceName} ({effectiveSampleRate} Hz)";
            return;
        }

        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            microphoneStatus = "检测源: 麦克风未授权（可切到播放检测）";
            return;
        }

        if (Microphone.devices.Length == 0)
        {
            microphoneStatus = "检测源: 没有可用麦克风（可切到播放检测）";
            return;
        }

        microphoneStatus = "检测源: 麦克风初始化中";
    }

    private float SmoothFrequency(float frequency)
    {
        smoothedFrequencies.Enqueue(frequency);
        while (smoothedFrequencies.Count > smoothingWindow)
        {
            smoothedFrequencies.Dequeue();
        }

        float[] ordered = new float[smoothedFrequencies.Count];
        smoothedFrequencies.CopyTo(ordered, 0);
        System.Array.Sort(ordered);
        return ordered[ordered.Length / 2];
    }

    private static float CalculateRms(float[] samples)
    {
        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += samples[i] * samples[i];
        }

        return Mathf.Sqrt(sum / samples.Length);
    }
}
