using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

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
    [SerializeField] private int maxSubharmonicDivisor = 4;
    [SerializeField] private float candidateSelectionTolerance = 0.92f;
    [SerializeField] private float microphoneInitializationTimeoutSeconds = 8f;

    private readonly Queue<float> smoothedFrequencies = new Queue<float>();

    private AudioClip microphoneClip;
    private float[] analysisSamples;
    private PitchDetector pitchDetector;
    private int detectorSampleRate;
    private int outputSampleRate;
    private int effectiveSampleRate;
    private bool microphoneReady;
    private string microphoneStatus = "Input: microphone initializing";
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
        if (!HasMicrophoneAuthorization())
        {
            UpdateStatusText();
            yield break;
        }

        yield return StartMicrophoneRecording();
        if (!microphoneReady)
        {
            UpdateStatusText();
            yield break;
        }

        ConfigureDetectorForCurrentSource();
        UpdateStatusText();
    }

private IEnumerator RequestMicrophoneAccess()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            yield break;
        }

        bool finished = false;
        PermissionCallbacks callbacks = new PermissionCallbacks();
        callbacks.PermissionGranted += _ => finished = true;
        callbacks.PermissionDenied += _ => finished = true;
        Permission.RequestUserPermission(Permission.Microphone, callbacks);

        float timeout = Time.realtimeSinceStartup + 10f;
        while (!finished && Time.realtimeSinceStartup < timeout)
        {
            yield return null;
        }
#else
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
        }
#endif
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

        float refinedFrequency = RefineFrequencyWithSubharmonics(result.Frequency);
        float smoothedFrequency = SmoothFrequency(refinedFrequency);
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
        StopMicrophoneRecording();
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
            microphoneStatus = $"Input: playback ({outputSampleRate} Hz)";
            return;
        }

        if (microphoneReady)
        {
            string deviceLabel = string.IsNullOrEmpty(activeDeviceName) ? "default device" : activeDeviceName;
            microphoneStatus = $"Input: microphone {deviceLabel} ({effectiveSampleRate} Hz)";
            return;
        }

        if (!HasMicrophoneAuthorization())
        {
            microphoneStatus = "Input: microphone permission not granted";
            return;
        }

        if (Microphone.devices.Length == 0)
        {
            microphoneStatus = "Input: no microphone device detected";
            return;
        }

        microphoneStatus = "Input: microphone initializing";
    }

    private bool HasMicrophoneAuthorization()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return Permission.HasUserAuthorizedPermission(Permission.Microphone);
#else
        return Application.HasUserAuthorization(UserAuthorization.Microphone);
#endif
    }

    private IEnumerator StartMicrophoneRecording()
    {
        StopMicrophoneRecording();

#if UNITY_ANDROID && !UNITY_EDITOR
        // Some Android devices report the mic slightly late after runtime permission is granted.
        yield return new WaitForSecondsRealtime(0.25f);
#endif

        if (Microphone.devices.Length == 0)
        {
            microphoneReady = false;
            yield break;
        }

        string preferredDevice = Microphone.devices[0];
        int preferredRate = ResolveRequestedSampleRate(preferredDevice);
        yield return TryStartMicrophone(preferredDevice, preferredRate);

        if (!microphoneReady)
        {
            yield return TryStartMicrophone(null, requestedSampleRate);
        }
    }

    private IEnumerator TryStartMicrophone(string deviceName, int sampleRate)
    {
        activeDeviceName = deviceName;
        microphoneClip = Microphone.Start(deviceName, true, 1, sampleRate);
        if (microphoneClip == null)
        {
            microphoneReady = false;
            activeDeviceName = null;
            yield break;
        }

        float timeout = Time.realtimeSinceStartup + Mathf.Max(1f, microphoneInitializationTimeoutSeconds);
        while (Microphone.GetPosition(deviceName) <= 0 && Time.realtimeSinceStartup < timeout)
        {
            yield return null;
        }

        if (Microphone.GetPosition(deviceName) <= 0)
        {
            StopMicrophoneRecording();
            yield break;
        }

        effectiveSampleRate = microphoneClip.frequency > 0 ? microphoneClip.frequency : sampleRate;
        microphoneReady = true;
    }

    private void StopMicrophoneRecording()
    {
        if (microphoneClip != null && Microphone.IsRecording(activeDeviceName))
        {
            Microphone.End(activeDeviceName);
        }

        microphoneClip = null;
        microphoneReady = false;
        activeDeviceName = null;
    }

    private int ResolveRequestedSampleRate(string deviceName)
    {
        Microphone.GetDeviceCaps(deviceName, out int minFrequency, out int maxFrequency);
        if (minFrequency == 0 && maxFrequency == 0)
        {
            return requestedSampleRate;
        }

        if (maxFrequency > 0 && requestedSampleRate > maxFrequency)
        {
            return maxFrequency;
        }

        if (minFrequency > 0 && requestedSampleRate < minFrequency)
        {
            return minFrequency;
        }

        return requestedSampleRate;
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

    private float RefineFrequencyWithSubharmonics(float detectedFrequency)
    {
        float bestFrequency = detectedFrequency;
        float bestScore = ScoreFundamentalCandidate(detectedFrequency);
        if (bestScore <= 0f)
        {
            return detectedFrequency;
        }

        for (int divisor = 2; divisor <= maxSubharmonicDivisor; divisor++)
        {
            float candidateFrequency = detectedFrequency / divisor;
            if (candidateFrequency < minFrequency)
            {
                continue;
            }

            float candidateScore = ScoreFundamentalCandidate(candidateFrequency);
            if (candidateScore < bestScore * candidateSelectionTolerance)
            {
                continue;
            }

            if (candidateScore > bestScore)
            {
                bestFrequency = candidateFrequency;
                bestScore = candidateScore;
            }
        }

        return bestFrequency;
    }

    private float ScoreFundamentalCandidate(float frequency)
    {
        if (frequency <= 0f)
        {
            return 0f;
        }

        float[] harmonicWeights = { 1f, 0.85f, 0.65f, 0.45f };
        float totalScore = 0f;
        float totalWeight = 0f;

        for (int harmonic = 1; harmonic <= harmonicWeights.Length; harmonic++)
        {
            float harmonicFrequency = frequency * harmonic;
            if (harmonicFrequency > maxFrequency * 1.05f)
            {
                break;
            }

            float weight = harmonicWeights[harmonic - 1];
            totalScore += MeasureFrequencyStrength(harmonicFrequency) * weight;
            totalWeight += weight;
        }

        if (totalWeight <= 0f)
        {
            return 0f;
        }

        return totalScore / totalWeight;
    }

    private float MeasureFrequencyStrength(float frequency)
    {
        if (frequency <= 0f)
        {
            return 0f;
        }

        float center = MeasureSingleFrequencyStrength(frequency);
        float lowerSide = MeasureSingleFrequencyStrength(frequency * 0.985f);
        float upperSide = MeasureSingleFrequencyStrength(frequency * 1.015f);
        return Mathf.Max(center, Mathf.Max(lowerSide, upperSide));
    }

    private float MeasureSingleFrequencyStrength(float frequency)
    {
        if (frequency <= 0f)
        {
            return 0f;
        }

        float sampleRate = detectorSampleRate > 0 ? detectorSampleRate : requestedSampleRate;
        float angularStep = 2f * Mathf.PI * frequency / sampleRate;
        float stepCosine = Mathf.Cos(angularStep);
        float stepSine = Mathf.Sin(angularStep);
        float cosine = 1f;
        float sine = 0f;
        float real = 0f;
        float imaginary = 0f;

        for (int i = 0; i < analysisSamples.Length; i++)
        {
            float window = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * i / (analysisSamples.Length - 1));
            float sample = analysisSamples[i] * window;
            real += sample * cosine;
            imaginary -= sample * sine;

            float nextRealBase = cosine * stepCosine - sine * stepSine;
            float nextImaginaryBase = sine * stepCosine + cosine * stepSine;
            cosine = nextRealBase;
            sine = nextImaginaryBase;
        }

        return Mathf.Sqrt(real * real + imaginary * imaginary) / analysisSamples.Length;
    }
}
