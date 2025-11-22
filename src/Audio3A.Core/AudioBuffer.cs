namespace Audio3A.Core;

/// <summary>
/// Represents an audio buffer containing PCM audio data
/// </summary>
public class AudioBuffer
{
    /// <summary>
    /// Audio samples as floating-point values (-1.0 to 1.0)
    /// </summary>
    public float[] Samples { get; set; }

    /// <summary>
    /// Number of channels
    /// </summary>
    public int Channels { get; set; }

    /// <summary>
    /// Sample rate in Hz
    /// </summary>
    public int SampleRate { get; set; }

    public AudioBuffer(int length, int channels = 1, int sampleRate = 16000)
    {
        Samples = new float[length];
        Channels = channels;
        SampleRate = sampleRate;
    }

    public AudioBuffer(float[] samples, int channels = 1, int sampleRate = 16000)
    {
        Samples = samples;
        Channels = channels;
        SampleRate = sampleRate;
    }

    /// <summary>
    /// Gets the length of the buffer in samples
    /// </summary>
    public int Length => Samples.Length;

    /// <summary>
    /// Converts 16-bit PCM data to floating-point samples
    /// </summary>
    public static AudioBuffer FromInt16(short[] pcmData, int channels = 1, int sampleRate = 16000)
    {
        float[] samples = new float[pcmData.Length];
        for (int i = 0; i < pcmData.Length; i++)
        {
            samples[i] = pcmData[i] / 32768.0f;
        }
        return new AudioBuffer(samples, channels, sampleRate);
    }

    /// <summary>
    /// Converts floating-point samples to 16-bit PCM data
    /// </summary>
    public short[] ToInt16()
    {
        short[] pcmData = new short[Samples.Length];
        for (int i = 0; i < Samples.Length; i++)
        {
            float clamped = Math.Max(-1.0f, Math.Min(1.0f, Samples[i]));
            pcmData[i] = (short)(clamped * 32767.0f);
        }
        return pcmData;
    }
}
