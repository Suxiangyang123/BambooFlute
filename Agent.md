# BambooFlute Agent Guide

## Project Goal

Build a Unity-based bamboo flute practice app for Android beginners.

Current milestone:

1. Pitch and tuning practice
2. Metronome

## Current Architecture

- Engine: Unity 6
- Main scene: `Assets/Scenes/SampleScene.unity`
- Runtime audio input: `Microphone`
- Pitch detection: custom C# YIN-style detector
- Main prefabs:
  - `Assets/Prefabs/MainUI.prefab`
  - `Assets/Prefabs/TunerPanel.prefab`
  - `Assets/Prefabs/MetronomePanel.prefab`
- Tuner UI approach:
  - Runtime binds existing prefab nodes
  - Do not recreate full tuner pages in code
  - Prefab layout is the current source of truth for tuner UI

## Important Scripts

- `Assets/Scripts/Runtime/PracticeNavigationController.cs`
  - Home page navigation between tuner and metronome
- `Assets/Scripts/Runtime/TunerPanelController.cs`
  - Tuner page interaction and display updates
  - Binds existing `TunerPanel.prefab` nodes for both tabs
  - Updates both the listening-page gauge and target-page gauge
- `Assets/Scripts/Runtime/ListeningPitchTrailView.cs`
  - Pitch trail graph rendering for listening mode
- `Assets/Scripts/Runtime/BambooFluteTargetLibrary.cs`
  - Flute key, tonguing mode, and target-note option generation
- `Assets/Scripts/Runtime/MicrophonePitchTracker.cs`
  - Microphone permission, sampling, and smoothed pitch tracking
- `Assets/Scripts/Runtime/PitchDetector.cs`
  - Frequency detection core
- `Assets/Scripts/Runtime/PitchMath.cs`
  - Note mapping and cents math
- `Assets/Scripts/Runtime/Metronome.cs`
  - Audio click generation and beat scheduling
- `Assets/Scripts/Runtime/MetronomePanelController.cs`
  - Metronome page UI control

## Tuner Tabs

### Listening Tab

- Page path: `Content/Pages/ListeningPage`
- Purpose: smart listening and auto-detect mode
- Main authored nodes:
  - `TopControls/KeyScroller/Viewport/Content`
  - `TopControls/TongueFive`
  - `TopControls/TongueTwo`
  - `Gauge/NeedlePivot`
  - `ListeningTitle`
  - `SummaryRow/LowCard`
  - `SummaryRow/MidCard`
  - `SummaryRow/HighCard`
  - `GraphCard/GraphViewport`

### Target Tab

- Page path: `Content/Pages/TargetPage`
- Purpose: explicit target-note practice mode
- Main authored nodes:
  - `Gauge/NeedlePivot`
  - `NoteLabel`
  - `HintLabel`
  - `FrequencyLabel`
  - `CentsLabel`
  - `TargetLabel`
  - `CurrentNoteNameLabel`
  - `TargetFrequencyLabel`
  - `CurrentFrequencyLabel`
  - `RegisterHintLabel`
  - `TargetSelect/FluteKeyScroller/Viewport/Content`
  - `TargetSelect/ToneFiveButton`
  - `TargetSelect/ToneTwoButton`
  - `TargetSelect/LowRow/Track`
  - `TargetSelect/MidRow/Track`
  - `TargetSelect/HighRow/Track`

## Current Tuner Behavior

- The tuner is split into `听音` and `目标音` tabs
- Each tab has its own prefab-authored gauge
- `TunerPanelController` updates both gauge needles through shared gauge logic
- Listening-mode flute keys are bound from actual child order in:
  - `Content/Pages/ListeningPage/TopControls/KeyScroller/Viewport/Content`
- Target-mode flute keys are bound from actual child order in:
  - `Content/Pages/TargetPage/TargetSelect/FluteKeyScroller/Viewport/Content`
- Do not hardcode target key button names as the main source of truth
  - The current target key items happen to be `Key0..Key8`, but binding should rely on child order

## UI Editing Workflow

- For `TunerPanel`, treat the prefab plus `TunerPanelController.cs` as the working source of truth
- Do not reintroduce runtime-generated full-page UI
- Prefer editing prefab layout in Unity over hand-editing prefab YAML
- After any prefab hierarchy change, update `TunerPanelController.cs` binding paths immediately
- If someone wants to reuse `StaticUiBuilder.cs`, verify first that it will not overwrite the newer hand-tuned tuner prefab structure

## Fonts

- Main TMP font: `Assets/Font/SourceHanSansCN-Regular SDF.asset`
- Character list source: `Assets/Font/font.txt`
- `Assets/Prefabs/TunerPanel.prefab` now already uses `SourceHanSansCN-Regular SDF` directly
- `TunerPanelController.cs` no longer performs runtime TMP font replacement

If new Chinese text is added:

1. Update `Assets/Font/font.txt`
2. Regenerate or update the TMP atlas if glyphs are missing
3. Keep `TunerPanel.prefab` text components on `SourceHanSansCN-Regular SDF`

## UX Constraints

- Prefer direct point-and-select interactions over sliders for target-note practice
- Preserve beginner-friendly, obvious controls
- Circle note selectors should keep:
  - larger invisible hit area
  - smaller visible dot
  - labels visually attached to the same node

## Notes For Future Agents

- Avoid reintroducing runtime-generated full-page UI
- When touching tuner UI, check both tabs and both gauges
- After target-key layout changes, verify the scroller binding still points to `Viewport/Content`
- When touching pitch logic, verify silence handling, noisy attacks, and tuner stability
