using Microsoft.Extensions.Logging;
using Audio3A.Core.Processors;

namespace Audio3A.Core;

/// <summary>
/// Main Audio 3A processor that combines AEC, AGC, and ANS
/// </summary>
public class Audio3AProcessor : IDisposable
{
    private readonly ILogger<Audio3AProcessor> _logger;
    private readonly Audio3AConfig _config;
    private readonly AecProcessor? _aecProcessor;
    private readonly AgcProcessor? _agcProcessor;
    private readonly AnsProcessor? _ansProcessor;
    private bool _disposed;

    /// <summary>
    /// Initializes a new Audio3A processor with dependency injection
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="config">Configuration for 3A processing</param>
    /// <param name="aecProcessor">AEC processor (injected)</param>
    /// <param name="agcProcessor">AGC processor (injected)</param>
    /// <param name="ansProcessor">ANS processor (injected)</param>
    public Audio3AProcessor(
        ILogger<Audio3AProcessor> logger,
        Audio3AConfig config,
        AecProcessor? aecProcessor = null,
        AgcProcessor? agcProcessor = null,
        AnsProcessor? ansProcessor = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));

        // Use injected processors only if enabled in config
        _aecProcessor = _config.EnableAec ? aecProcessor : null;
        _agcProcessor = _config.EnableAgc ? agcProcessor : null;
        _ansProcessor = _config.EnableAns ? ansProcessor : null;

        _logger.LogInformation(
            "Audio3A processor initialized: AEC={AecEnabled}, AGC={AgcEnabled}, ANS={AnsEnabled}, SampleRate={SampleRate}",
            _config.EnableAec, _config.EnableAgc, _config.EnableAns, _config.SampleRate);
    }

    /// <summary>
    /// Process audio with 3A algorithms
    /// </summary>
    /// <param name="micInput">Microphone input buffer</param>
    /// <param name="speakerReference">Speaker reference signal for echo cancellation (optional)</param>
    /// <returns>Processed audio buffer</returns>
    public AudioBuffer Process(AudioBuffer micInput, AudioBuffer? speakerReference = null)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(Audio3AProcessor));
        }

        _logger.LogTrace("Processing audio buffer: Length={Length} samples", micInput.Length);

        AudioBuffer current = micInput;

        // Apply AEC first to remove echo
        if (_aecProcessor != null)
        {
            _logger.LogTrace("Applying AEC processing");
            current = _aecProcessor.Process(current, speakerReference);
        }

        // Apply ANS to remove noise
        if (_ansProcessor != null)
        {
            _logger.LogTrace("Applying ANS processing");
            current = _ansProcessor.Process(current);
        }

        // Apply AGC last to normalize volume
        if (_agcProcessor != null)
        {
            _logger.LogTrace("Applying AGC processing");
            current = _agcProcessor.Process(current);
        }

        return current;
    }

    /// <summary>
    /// Process 16-bit PCM audio data
    /// </summary>
    /// <param name="micInputPcm">Microphone input as 16-bit PCM</param>
    /// <param name="speakerReferencePcm">Speaker reference as 16-bit PCM (optional)</param>
    /// <returns>Processed audio as 16-bit PCM</returns>
    public short[] ProcessInt16(short[] micInputPcm, short[]? speakerReferencePcm = null)
    {
        AudioBuffer micBuffer = AudioBuffer.FromInt16(micInputPcm, _config.Channels, _config.SampleRate);
        AudioBuffer? speakerBuffer = speakerReferencePcm != null
            ? AudioBuffer.FromInt16(speakerReferencePcm, _config.Channels, _config.SampleRate)
            : null;

        AudioBuffer processed = Process(micBuffer, speakerBuffer);
        return processed.ToInt16();
    }

    /// <summary>
    /// Reset all processors to their initial state
    /// </summary>
    public void Reset()
    {
        _logger.LogDebug("Resetting all processors");
        _aecProcessor?.Reset();
        _agcProcessor?.Reset();
        _ansProcessor?.Reset();
    }

    /// <summary>
    /// Gets the current configuration
    /// </summary>
    public Audio3AConfig Config => _config;

    public void Dispose()
    {
        if (!_disposed)
        {
            _logger.LogDebug("Disposing Audio3A processor");
            Reset();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
