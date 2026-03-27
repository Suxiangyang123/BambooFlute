using UnityEngine;

public readonly struct NoteInfo
{
    public NoteInfo(int midiNote, float referenceFrequency, string displayName, float centsOffset)
    {
        MidiNote = midiNote;
        ReferenceFrequency = referenceFrequency;
        DisplayName = displayName;
        CentsOffset = centsOffset;
    }

    public int MidiNote { get; }
    public float ReferenceFrequency { get; }
    public string DisplayName { get; }
    public float CentsOffset { get; }
}

public static class PitchMath
{
    private static readonly string[] NoteNames = { "C", "C#", "D", "Eb", "E", "F", "F#", "G", "Ab", "A", "Bb", "B" };

    public static NoteInfo GetNearestNote(float frequency)
    {
        if (frequency <= 0f)
        {
            return new NoteInfo(0, 0f, "--", 0f);
        }

        float midi = 69f + 12f * Mathf.Log(frequency / 440f, 2f);
        int midiRounded = Mathf.RoundToInt(midi);
        float reference = MidiToFrequency(midiRounded);
        float cents = 1200f * Mathf.Log(frequency / reference, 2f);
        string noteName = $"{NoteNames[PositiveModulo(midiRounded, 12)]}{(midiRounded / 12) - 1}";
        return new NoteInfo(midiRounded, reference, noteName, cents);
    }

    public static float MidiToFrequency(int midiNote)
    {
        return 440f * Mathf.Pow(2f, (midiNote - 69) / 12f);
    }

    public static string GetTuningHint(float centsOffset, float inTuneThreshold = 8f)
    {
        if (Mathf.Abs(centsOffset) <= inTuneThreshold)
        {
            return "准";
        }

        return centsOffset < 0f ? "偏低" : "偏高";
    }

    private static int PositiveModulo(int value, int modulo)
    {
        int result = value % modulo;
        return result < 0 ? result + modulo : result;
    }
}
