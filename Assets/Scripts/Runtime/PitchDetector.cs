using UnityEngine;

public readonly struct PitchDetectionResult
{
    public PitchDetectionResult(bool isValid, float frequency, float clarity)
    {
        IsValid = isValid;
        Frequency = frequency;
        Clarity = clarity;
    }

    public bool IsValid { get; }
    public float Frequency { get; }
    public float Clarity { get; }
}

public sealed class PitchDetector
{
    private readonly int sampleRate;
    private readonly int bufferSize;
    private readonly float[] difference;
    private readonly float[] cumulativeMeanNormalizedDifference;

    public PitchDetector(int sampleRate, int bufferSize)
    {
        this.sampleRate = sampleRate;
        this.bufferSize = bufferSize;
        difference = new float[bufferSize / 2];
        cumulativeMeanNormalizedDifference = new float[bufferSize / 2];
    }

    public PitchDetectionResult Detect(float[] samples, float minFrequency, float maxFrequency, float yinThreshold)
    {
        int halfBuffer = bufferSize / 2;
        int minTau = Mathf.Max(2, Mathf.FloorToInt(sampleRate / maxFrequency));
        int maxTau = Mathf.Min(halfBuffer - 1, Mathf.CeilToInt(sampleRate / minFrequency));

        for (int tau = 0; tau < halfBuffer; tau++)
        {
            difference[tau] = 0f;
            cumulativeMeanNormalizedDifference[tau] = 1f;
        }

        for (int tau = 1; tau <= maxTau; tau++)
        {
            float sum = 0f;
            int sampleCount = bufferSize - tau;
            for (int i = 0; i < sampleCount; i++)
            {
                float delta = samples[i] - samples[i + tau];
                sum += delta * delta;
            }

            difference[tau] = sum;
        }

        float runningSum = 0f;
        for (int tau = 1; tau <= maxTau; tau++)
        {
            runningSum += difference[tau];
            cumulativeMeanNormalizedDifference[tau] = runningSum <= Mathf.Epsilon
                ? 1f
                : difference[tau] * tau / runningSum;
        }

        int bestTau = -1;
        float bestValue = 1f;
        for (int tau = minTau; tau <= maxTau; tau++)
        {
            float value = cumulativeMeanNormalizedDifference[tau];
            if (value < yinThreshold)
            {
                while (tau + 1 <= maxTau && cumulativeMeanNormalizedDifference[tau + 1] < value)
                {
                    tau++;
                    value = cumulativeMeanNormalizedDifference[tau];
                }

                bestTau = tau;
                bestValue = value;
                break;
            }

            if (value < bestValue)
            {
                bestTau = tau;
                bestValue = value;
            }
        }

        if (bestTau < 0)
        {
            return new PitchDetectionResult(false, 0f, 0f);
        }

        float refinedTau = ParabolicInterpolation(bestTau, maxTau);
        if (refinedTau <= 0f)
        {
            return new PitchDetectionResult(false, 0f, 0f);
        }

        float frequency = sampleRate / refinedTau;
        float clarity = Mathf.Clamp01(1f - bestValue);
        bool valid = frequency >= minFrequency && frequency <= maxFrequency;
        return new PitchDetectionResult(valid, frequency, clarity);
    }

    private float ParabolicInterpolation(int tau, int maxTau)
    {
        if (tau <= 1 || tau >= maxTau)
        {
            return tau;
        }

        float left = cumulativeMeanNormalizedDifference[tau - 1];
        float center = cumulativeMeanNormalizedDifference[tau];
        float right = cumulativeMeanNormalizedDifference[tau + 1];
        float denominator = 2f * (2f * center - right - left);
        if (Mathf.Abs(denominator) < 0.0001f)
        {
            return tau;
        }

        return tau + (right - left) / denominator;
    }
}
