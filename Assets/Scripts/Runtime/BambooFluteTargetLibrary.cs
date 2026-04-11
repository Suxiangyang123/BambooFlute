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
    public string DisplayLabel => $"{GetRegisterText(RegisterBand)}音 {DegreeLabel}  {PitchLabel}";
    public string CompactLabel => DegreeLabel;

    public static string GetRegisterText(RegisterBand band)
    {
        return band switch
        {
            RegisterBand.Low => "低",
            RegisterBand.Mid => "中",
            _ => "高",
        };
    }
}

public static class BambooFluteTargetLibrary
{
    private static readonly string[] FluteKeys = { "C调", "D调", "E调", "F调", "G调", "A调", "降B调", "大A调", "大G调" };
    private static readonly int[] BaseDoMidi = { 72, 74, 76, 77, 79, 81, 70, 69, 67 };
    private static readonly int[] MajorScale = { 0, 2, 4, 5, 7, 9, 11 };
    private static readonly string[] DegreeTexts = { "1", "2", "3", "4", "5", "6", "7" };
    private static readonly int[] ToneTwoDegreeMap = { 5, 6, 7, 1, 2, 3, 4 };
    private static readonly TargetDescriptor[] Targets = CreateTargets();

    public static int FluteKeyCount => FluteKeys.Length;
    public static int DefaultFluteKeyIndex => Array.IndexOf(FluteKeys, "D调");

    public static string GetFluteKeyLabel(int index)
    {
        return FluteKeys[Mathf.Clamp(index, 0, FluteKeys.Length - 1)];
    }

    public static IReadOnlyList<string> GetFluteKeyLabels()
    {
        return FluteKeys;
    }

    public static IReadOnlyList<TargetNoteOption> BuildOptions(int fluteKeyIndex, TongueMode tongueMode)
    {
        fluteKeyIndex = Mathf.Clamp(fluteKeyIndex, 0, FluteKeys.Length - 1);
        int baseDoMidi = BaseDoMidi[fluteKeyIndex];
        List<TargetNoteOption> options = new List<TargetNoteOption>(Targets.Length);

        for (int i = 0; i < Targets.Length; i++)
        {
            TargetDescriptor descriptor = Targets[i];
            int midi = GetMidiForDegree(baseDoMidi, descriptor.PitchDegree, descriptor.OctaveShift);
            int displayDegree = GetDisplayDegree(descriptor.PitchDegree, tongueMode);
            float frequency = PitchMath.MidiToFrequency(midi);
            string noteName = PitchMath.GetNearestNote(frequency).DisplayName;
            options.Add(new TargetNoteOption(descriptor.RegisterBand, DegreeTexts[displayDegree - 1], noteName, frequency));
        }

        return options;
    }

    private static int GetMidiForDegree(int baseDoMidi, int degree, int octaveShift)
    {
        return baseDoMidi + MajorScale[degree - 1] + octaveShift * 12;
    }

    private static int GetDisplayDegree(int pitchDegree, TongueMode tongueMode)
    {
        return tongueMode == TongueMode.Five ? pitchDegree : ToneTwoDegreeMap[pitchDegree - 1];
    }

    private static TargetDescriptor[] CreateTargets()
    {
        List<TargetDescriptor> targets = new List<TargetDescriptor>(17);
        AddDegreeRange(targets, RegisterBand.Low, 5, 7, -1);
        AddDegreeRange(targets, RegisterBand.Mid, 1, 7, 0);
        AddDegreeRange(targets, RegisterBand.High, 1, 7, 1);
        return targets.ToArray();
    }

    private static void AddDegreeRange(List<TargetDescriptor> targets, RegisterBand registerBand, int startDegree, int endDegree, int octaveShift)
    {
        for (int degree = startDegree; degree <= endDegree; degree++)
        {
            targets.Add(new TargetDescriptor(registerBand, degree, octaveShift));
        }
    }

    private readonly struct TargetDescriptor
    {
        public TargetDescriptor(RegisterBand registerBand, int pitchDegree, int octaveShift)
        {
            RegisterBand = registerBand;
            PitchDegree = pitchDegree;
            OctaveShift = octaveShift;
        }

        public RegisterBand RegisterBand { get; }
        public int PitchDegree { get; }
        public int OctaveShift { get; }
    }
}
