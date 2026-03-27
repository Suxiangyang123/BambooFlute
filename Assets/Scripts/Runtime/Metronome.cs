using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public sealed class Metronome : MonoBehaviour
{
    [SerializeField] private int bpm = 72;
    [SerializeField] private int beatsPerBar = 4;
    [SerializeField] private float clickVolume = 0.8f;
    [SerializeField] private float nextBeatLeadTime = 0.1f;

    private AudioSource audioSource;
    private AudioClip accentClip;
    private AudioClip regularClip;
    private double nextBeatDspTime;
    private int beatInBar;

    public int Bpm => bpm;
    public int BeatsPerBar => beatsPerBar;
    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        accentClip = CreateClickClip(1760f, 0.05f);
        regularClip = CreateClickClip(1320f, 0.04f);
    }

    private void Update()
    {
        if (!IsPlaying)
        {
            return;
        }

        while (AudioSettings.dspTime + nextBeatLeadTime >= nextBeatDspTime)
        {
            AudioClip clip = beatInBar == 0 ? accentClip : regularClip;
            audioSource.PlayOneShot(clip, clickVolume);
            nextBeatDspTime += 60d / bpm;
            beatInBar = (beatInBar + 1) % beatsPerBar;
        }
    }

    public void Toggle()
    {
        if (IsPlaying)
        {
            Stop();
        }
        else
        {
            Play();
        }
    }

    public void Play()
    {
        IsPlaying = true;
        beatInBar = 0;
        nextBeatDspTime = AudioSettings.dspTime + 0.05d;
    }

    public void Stop()
    {
        IsPlaying = false;
        beatInBar = 0;
    }

    public void AdjustBpm(int delta)
    {
        bpm = Mathf.Clamp(bpm + delta, 30, 220);
    }

    public void SetBeatsPerBar(int value)
    {
        beatsPerBar = Mathf.Clamp(value, 2, 6);
        beatInBar = 0;
    }

    private static AudioClip CreateClickClip(float frequency, float duration)
    {
        const int sampleRate = 44100;
        int length = Mathf.CeilToInt(sampleRate * duration);
        float[] data = new float[length];

        for (int i = 0; i < length; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = Mathf.Exp(-24f * t);
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope;
        }

        AudioClip clip = AudioClip.Create($"click_{frequency}", length, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
