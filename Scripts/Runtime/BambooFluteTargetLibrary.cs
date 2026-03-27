using System;
using System.Collections.Generic;
using UnityEngine;

public enum TunerMode
{
    Listening,
    TargetSelection,
}

public enum TongueMode
{
    Five,
    Two,
}

public enum RegisterBand
{
    Low,
    Mid,
    High,
}

public readonly struct TargetNoteOption
{
    public TargetNoteOption(RegisterBand registerBand, string degreeLabel, string pitchLabel, float frequency)
    {
        RegisterBand = registerBand;
        DegreeLabel = degreeLabel;
        PitchLabel = pitchLabel;
        Frequency = frequency;
    }

    public RegisterBand RegisterBand { get; }
    public string DegreeLabel { get; }
    public string PitchLabel { get; }
    public float Frequency { get; }
    public string DisplayLabel => $"{RegisterText(RegisterBand)}{DegreeLabel}  {PitchLabel}";
    public string CompactLabel => $"{DegreeLabel}";

    private static string RegisterText(RegisterBand band)
    {
        return band switch
        {
            RegisterBand.Low => "低音",
            RegisterBand.Mid => "中音",
            _ => "高音",
        };
    }
}

public static class BambooFluteTargetLibrary
{
    private static readonly string[] FluteKeys = { "G调", "F调", "E调", "D调" };
    private static readonly int[] BaseDoMidi = { 67, 65, 64, 62 };
    private static readonly int[] MajorScale = { 0, 2, 4, 5, 7, 9, 11 };
    private static readonly int[] ClosedDegreeOffsets = { 0, 2, 4, 5, 7, 9, 11 };
    private static readonly string[] DegreeTexts = { "1", "2", "3", "4", "5", "6", "7" };
    private static readonly TargetDescriptor[] ToneFiveTargets =
    {
        new(RegisterBand.Low, 5, -1),
        new(RegisterBand.Low, 6, -1),
        new(RegisterBand.Low, 7, -1),
        new(RegisterBand.Mid, 1, 0),
        new(RegisterBand.Mid, 2, 0),
        new(RegisterBand.Mid, 3, 0),
        new(RegisterBand.Mid, 4, 0),
        new(RegisterBand.Mid, 5, 0),
        new(RegisterBand.Mid, 6, 0),
        new(RegisterBand.Mid, 7, 0),
        new(RegisterBand.High, 1, 1),
        new(RegisterBand.High, 2, 1),
        new(RegisterBand.High, 3, 1),
        new(RegisterBand.High, 4, 1),
    };
    private static readonly TargetDescriptor[] ToneTwoTargets =
    {
        new(RegisterBand.Low, 2, -1),
        new(RegisterBand.Low, 3, -1),
        new(RegisterBand.Low, 4, -1),
        new(RegisterBand.Mid, 5, -1),
        new(RegisterBand.Mid, 6, -1),
        new(RegisterBand.Mid, 7, -1),
        new(RegisterBand.Mid, 1, 0),
        new(RegisterBand.Mid, 2, 0),
        new(RegisterBand.Mid, 3, 0),
        new(RegisterBand.Mid, 4, 0),
        new(RegisterBand.High, 5, 0),
        new(RegisterBand.High, 6, 0),
        new(RegisterBand.High, 7, 0),
        new(RegisterBand.High, 1, 1),
    };

    public static int FluteKeyCount => FluteKeys.Length;

    public static string GetFluteKeyLabel(int index)
    {
        return FluteKeys[Mathf.Clamp(index, 0, FluteKeys.Length - 1)];
    }

    public static IReadOnlyList<TargetNoteOption> BuildOptions(int fluteKeyIndex, TongueMode tongueMode)
    {
        fluteKeyIndex = Mathf.Clamp(fluteKeyIndex, 0, FluteKeys.Length - 1);
        int baseDoMidi = BaseDoMidi[fluteKeyIndex];
        TargetDescriptor[] descriptors = tongueMode == TongueMode.Five ? ToneFiveTargets : ToneTwoTargets;

        List<TargetNoteOption> options = new List<TargetNoteOption>(descriptors.Length);
        foreach (TargetDescriptor descriptor in descriptors)
        {
            int midi = GetMidiForDegree(baseDoMidi, descriptor.Degree, descriptor.OctaveShift);
            string noteName = PitchMath.GetNearestNote(PitchMath.MidiToFrequency(midi)).DisplayName;
            options.Add(new TargetNoteOption(descriptor.RegisterBand, DegreeTexts[descriptor.Degree - 1], noteName, PitchMath.MidiToFrequency(midi)));
        }

        return options;
    }

    public static IReadOnlyList<string> GetFluteKeyLabels()
    {
        return FluteKeys;
    }

    private static int GetMidiForDegree(int baseDoMidi, int degree, int octaveShift)
    {
        int semitoneOffset = MajorScale[degree - 1];
        return baseDoMidi + semitoneOffset + octaveShift * 12;
    }

    private readonly struct TargetDescriptor
    {
        public TargetDescriptor(RegisterBand registerBand, int degree, int octaveShift)
        {
            RegisterBand = registerBand;
            Degree = degree;
            OctaveShift = octaveShift;
        }

        public RegisterBand RegisterBand { get; }
        public int Degree { get; }
        public int OctaveShift { get; }
    }
}
