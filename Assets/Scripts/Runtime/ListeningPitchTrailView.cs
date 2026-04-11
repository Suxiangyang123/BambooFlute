using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ListeningPitchTrailView : MonoBehaviour
{
    private const float WindowSeconds = 10f;
    private const float VisibleCentsRange = 35f;
    private const float RedrawInterval = 0.05f;
    private const int TextureWidth = 1024;
    private const int TextureHeight = 360;
    private static readonly float[] AxisTickValues = { 35f, 20f, 0f, -20f, -35f };

    private readonly struct TrailSample
    {
        public TrailSample(float time, float centsOffset, bool valid, string label)
        {
            Time = time;
            CentsOffset = centsOffset;
            Valid = valid;
            Label = label;
        }

        public float Time { get; }
        public float CentsOffset { get; }
        public bool Valid { get; }
        public string Label { get; }
    }

    private readonly struct TrailMarker
    {
        public TrailMarker(float time, string label)
        {
            Time = time;
            Label = label;
        }

        public float Time { get; }
        public string Label { get; }
    }

    private readonly List<TrailSample> samples = new List<TrailSample>(512);
    private readonly List<TrailMarker> markers = new List<TrailMarker>(64);
    private readonly List<TextMeshProUGUI> markerPool = new List<TextMeshProUGUI>(16);
    private readonly List<TextMeshProUGUI> axisLabelPool = new List<TextMeshProUGUI>(8);

    private RectTransform rectTransform;
    private RawImage graphImage;
    private RectTransform markerRoot;
    private RectTransform axisRoot;
    private Texture2D graphTexture;
    private Color32[] pixels;
    private TMP_FontAsset fontAsset;
    private float nextRedrawTime;

    private readonly Color32 backgroundColor = new Color32(14, 24, 33, 255);
    private readonly Color32 gridColor = new Color32(36, 60, 77, 255);
    private readonly Color32 centerColor = new Color32(98, 224, 177, 255);
    private readonly Color32 lineColor = new Color32(125, 233, 190, 255);
    private readonly Color32 markerColor = new Color32(220, 235, 240, 255);
    private readonly Color32 axisColor = new Color32(160, 184, 196, 255);

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        EnsureVisuals();
    }

    public void Configure(TMP_FontAsset font)
    {
        fontAsset = font;
        EnsureVisuals();
    }

    public void SetScale(IReadOnlyList<TargetNoteOption> options)
    {
        Clear();
    }

    public void Clear()
    {
        samples.Clear();
        markers.Clear();
        HideMarkers();
        ForceRedraw();
    }

    public void PushFrame(float time, bool hasPitch, TargetNoteOption nearestOption, float centsOffset)
    {
        Prune(time);

        if (!hasPitch)
        {
            samples.Add(new TrailSample(time, 0f, false, string.Empty));
            RequestRedraw(time);
            return;
        }

        string label = $"{TargetNoteOption.GetRegisterText(nearestOption.RegisterBand)}音 {nearestOption.DegreeLabel} {nearestOption.PitchLabel}";
        samples.Add(new TrailSample(time, centsOffset, true, label));
        if (markers.Count == 0 || markers[markers.Count - 1].Label != label)
        {
            markers.Add(new TrailMarker(time, label));
        }

        RequestRedraw(time);
    }

    private void EnsureVisuals()
    {
        if (graphImage == null)
        {
            GameObject graphGo = new GameObject("Graph", typeof(RectTransform), typeof(RawImage));
            graphGo.transform.SetParent(transform, false);
            RectTransform graphRect = graphGo.GetComponent<RectTransform>();
            graphRect.anchorMin = Vector2.zero;
            graphRect.anchorMax = Vector2.one;
            graphRect.offsetMin = Vector2.zero;
            graphRect.offsetMax = Vector2.zero;
            graphImage = graphGo.GetComponent<RawImage>();
        }

        if (axisRoot == null)
        {
            GameObject axisGo = new GameObject("AxisLabels", typeof(RectTransform));
            axisGo.transform.SetParent(transform, false);
            axisRoot = axisGo.GetComponent<RectTransform>();
            axisRoot.anchorMin = Vector2.zero;
            axisRoot.anchorMax = Vector2.one;
            axisRoot.offsetMin = Vector2.zero;
            axisRoot.offsetMax = Vector2.zero;
        }

        if (markerRoot == null)
        {
            GameObject markerGo = new GameObject("Markers", typeof(RectTransform));
            markerGo.transform.SetParent(transform, false);
            markerRoot = markerGo.GetComponent<RectTransform>();
            markerRoot.anchorMin = Vector2.zero;
            markerRoot.anchorMax = Vector2.one;
            markerRoot.offsetMin = Vector2.zero;
            markerRoot.offsetMax = Vector2.zero;
        }

        if (graphTexture == null)
        {
            graphTexture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false);
            graphTexture.filterMode = FilterMode.Bilinear;
            graphTexture.wrapMode = TextureWrapMode.Clamp;
            pixels = new Color32[TextureWidth * TextureHeight];
            graphImage.texture = graphTexture;
        }

        ForceRedraw();
    }

    private void RequestRedraw(float time)
    {
        if (time < nextRedrawTime)
        {
            return;
        }

        nextRedrawTime = time + RedrawInterval;
        Redraw(time);
    }

    private void ForceRedraw()
    {
        nextRedrawTime = 0f;
        Redraw(Time.unscaledTime);
    }

    private void Redraw(float currentTime)
    {
        if (graphTexture == null)
        {
            return;
        }

        Fill(backgroundColor);
        DrawGrid();
        DrawSamples();
        graphTexture.SetPixels32(pixels);
        graphTexture.Apply(false, false);
        RefreshAxisLabels();
        RefreshMarkers(currentTime);
    }

    private void Fill(Color32 color)
    {
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
    }

    private void DrawGrid()
    {
        for (int x = 0; x < TextureWidth; x += TextureWidth / 10)
        {
            DrawVertical(x, gridColor);
        }

        foreach (float tickValue in AxisTickValues)
        {
            int y = Mathf.RoundToInt(Mathf.Lerp(18f, TextureHeight - 18f, CentsToY01(tickValue)));
            DrawHorizontal(y, Mathf.Approximately(tickValue, 0f) ? centerColor : gridColor);
        }
    }

    private void DrawSamples()
    {
        if (samples.Count < 2)
        {
            return;
        }

        for (int i = 1; i < samples.Count; i++)
        {
            TrailSample previous = samples[i - 1];
            TrailSample current = samples[i];
            if (!previous.Valid || !current.Valid || previous.Label != current.Label)
            {
                continue;
            }

            Vector2Int a = GetPoint(previous);
            Vector2Int b = GetPoint(current);
            DrawLine(a.x, a.y, b.x, b.y, lineColor);
        }
    }

    private Vector2Int GetPoint(TrailSample sample)
    {
        float latestTime = samples.Count > 0 ? samples[samples.Count - 1].Time : Time.unscaledTime;
        float startTime = latestTime - WindowSeconds;
        float x01 = Mathf.InverseLerp(startTime, latestTime, sample.Time);
        float y01 = CentsToY01(sample.CentsOffset);
        int x = Mathf.RoundToInt(x01 * (TextureWidth - 1));
        int y = Mathf.RoundToInt(Mathf.Lerp(18f, TextureHeight - 18f, y01));
        return new Vector2Int(Mathf.Clamp(x, 0, TextureWidth - 1), Mathf.Clamp(y, 0, TextureHeight - 1));
    }

    private float CentsToY01(float centsOffset)
    {
        float clamped = Mathf.Clamp(centsOffset, -VisibleCentsRange, VisibleCentsRange);
        return Mathf.InverseLerp(-VisibleCentsRange, VisibleCentsRange, clamped);
    }

    private void RefreshAxisLabels()
    {
        for (int i = 0; i < axisLabelPool.Count; i++)
        {
            axisLabelPool[i].gameObject.SetActive(false);
        }
    }

    private void RefreshMarkers(float currentTime)
    {
        float startTime = currentTime - WindowSeconds;
        int visibleIndex = 0;
        for (int i = 0; i < markers.Count; i++)
        {
            TrailMarker marker = markers[i];
            if (marker.Time < startTime)
            {
                continue;
            }

            TextMeshProUGUI label = GetMarkerLabel(visibleIndex++);
            float x01 = Mathf.InverseLerp(startTime, currentTime, marker.Time);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.anchoredPosition = new Vector2(x01 * rectTransform.rect.width + 8f, -6f - (visibleIndex % 2) * 20f);
            labelRect.sizeDelta = new Vector2(180f, 20f);
            label.text = marker.Label;
            label.gameObject.SetActive(true);
        }

        for (int i = visibleIndex; i < markerPool.Count; i++)
        {
            markerPool[i].gameObject.SetActive(false);
        }
    }

    private TextMeshProUGUI GetMarkerLabel(int index)
    {
        while (markerPool.Count <= index)
        {
            GameObject labelGo = new GameObject($"Marker{markerPool.Count + 1}", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(markerRoot, false);
            TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
            label.font = fontAsset;
            label.fontSize = 16f;
            label.color = markerColor;
            label.alignment = TextAlignmentOptions.Left;
            markerPool.Add(label);
        }

        return markerPool[index];
    }

    private TextMeshProUGUI GetAxisLabel(int index)
    {
        while (axisLabelPool.Count <= index)
        {
            GameObject labelGo = new GameObject($"AxisLabel{axisLabelPool.Count + 1}", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(axisRoot, false);
            TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
            label.font = fontAsset;
            label.fontSize = 14f;
            label.color = axisColor;
            label.alignment = TextAlignmentOptions.Left;
            axisLabelPool.Add(label);
        }

        return axisLabelPool[index];
    }

    private void HideMarkers()
    {
        for (int i = 0; i < markerPool.Count; i++)
        {
            markerPool[i].gameObject.SetActive(false);
        }
    }

    private void Prune(float currentTime)
    {
        float cutoff = currentTime - WindowSeconds;
        while (samples.Count > 0 && samples[0].Time < cutoff)
        {
            samples.RemoveAt(0);
        }

        while (markers.Count > 0 && markers[0].Time < cutoff)
        {
            markers.RemoveAt(0);
        }
    }

    private void DrawHorizontal(int y, Color32 color)
    {
        if (y < 0 || y >= TextureHeight)
        {
            return;
        }

        int offset = y * TextureWidth;
        for (int x = 0; x < TextureWidth; x++)
        {
            pixels[offset + x] = color;
        }
    }

    private void DrawVertical(int x, Color32 color)
    {
        if (x < 0 || x >= TextureWidth)
        {
            return;
        }

        for (int y = 0; y < TextureHeight; y++)
        {
            pixels[y * TextureWidth + x] = color;
        }
    }

    private void DrawLine(int x0, int y0, int x1, int y1, Color32 color)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            Plot(x0, y0, color);
            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            int e2 = err * 2;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    private void Plot(int x, int y, Color32 color)
    {
        for (int oy = -1; oy <= 1; oy++)
        {
            for (int ox = -1; ox <= 1; ox++)
            {
                int px = x + ox;
                int py = y + oy;
                if (px < 0 || px >= TextureWidth || py < 0 || py >= TextureHeight)
                {
                    continue;
                }

                pixels[py * TextureWidth + px] = color;
            }
        }
    }
}
