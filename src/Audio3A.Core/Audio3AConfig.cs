namespace Audio3A.Core;

/// <summary>
/// Configuration for Audio 3A processing
/// </summary>
public class Audio3AConfig
{
    /// <summary>
    /// Enable Acoustic Echo Cancellation
    /// </summary>
    public bool EnableAec { get; set; } = true;

    /// <summary>
    /// Enable Automatic Gain Control
    /// </summary>
    public bool EnableAgc { get; set; } = true;

    /// <summary>
    /// Enable Automatic Noise Suppression
    /// </summary>
    public bool EnableAns { get; set; } = true;

    /// <summary>
    /// 处理器执行顺序（默认：标准顺序 AEC -> ANS -> AGC）
    /// </summary>
    public ProcessingOrder ProcessingOrder { get; set; } = ProcessingOrder.Standard;

    /// <summary>
    /// Audio sample rate in Hz
    /// </summary>
    public int SampleRate { get; set; } = 16000;

    /// <summary>
    /// Number of audio channels
    /// </summary>
    public int Channels { get; set; } = 1;

    /// <summary>
    /// Frame size in samples
    /// </summary>
    public int FrameSize { get; set; } = 160;

    // AGC settings
    /// <summary>
    /// AGC target level (0.0 to 1.0)
    /// </summary>
    public float AgcTargetLevel { get; set; } = 0.5f;

    /// <summary>
    /// AGC compression ratio
    /// </summary>
    public float AgcCompressionRatio { get; set; } = 3.0f;

    // ANS settings
    /// <summary>
    /// Noise reduction strength in dB
    /// </summary>
    public float AnsNoiseReductionDb { get; set; } = 20.0f;

    // AEC settings
    /// <summary>
    /// AEC filter length in samples
    /// </summary>
    public int AecFilterLength { get; set; } = 512;

    /// <summary>
    /// AEC adaptation step size
    /// </summary>
    public float AecStepSize { get; set; } = 0.01f;
}
