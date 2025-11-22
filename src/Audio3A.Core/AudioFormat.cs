namespace Audio3A.Core;

/// <summary>
/// Represents audio format specifications
/// </summary>
public class AudioFormat
{
    /// <summary>
    /// Sample rate in Hz (e.g., 8000, 16000, 44100, 48000)
    /// </summary>
    public int SampleRate { get; set; }

    /// <summary>
    /// Number of audio channels (1 for mono, 2 for stereo)
    /// </summary>
    public int Channels { get; set; }

    /// <summary>
    /// Bits per sample (typically 16 or 32)
    /// </summary>
    public int BitsPerSample { get; set; }

    /// <summary>
    /// Frame size in samples (number of samples per processing frame)
    /// </summary>
    public int FrameSize { get; set; }

    public AudioFormat(int sampleRate = 16000, int channels = 1, int bitsPerSample = 16, int frameSize = 160)
    {
        SampleRate = sampleRate;
        Channels = channels;
        BitsPerSample = bitsPerSample;
        FrameSize = frameSize;
    }

    /// <summary>
    /// Gets the frame duration in milliseconds
    /// </summary>
    public double FrameDurationMs => (double)FrameSize / SampleRate * 1000;
}
