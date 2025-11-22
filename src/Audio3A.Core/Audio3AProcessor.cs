using Audio3A.Core.Processors;

namespace Audio3A.Core;

/// <summary>
/// Main Audio 3A processor that combines AEC, AGC, and ANS
/// </summary>
public class Audio3AProcessor : IDisposable
{
    private readonly Audio3AConfig _config;
    private readonly AecProcessor? _aecProcessor;
    private readonly AgcProcessor? _agcProcessor;
    private readonly AnsProcessor? _ansProcessor;
    private bool _disposed;

    /// <summary>
    /// Initializes a new Audio3A processor with the specified configuration
    /// </summary>
    /// <param name="config">Configuration for 3A processing</param>
    public Audio3AProcessor(Audio3AConfig? config = null)
    {
        _config = config ?? new Audio3AConfig();

        // Initialize enabled processors
        if (_config.EnableAec)
        {
            _aecProcessor = new AecProcessor(
                _config.SampleRate,
                _config.AecFilterLength,
                _config.AecStepSize);
        }

        if (_config.EnableAgc)
        {
            _agcProcessor = new AgcProcessor(
                _config.SampleRate,
                _config.AgcTargetLevel,
                _config.AgcCompressionRatio);
        }

        if (_config.EnableAns)
        {
            _ansProcessor = new AnsProcessor(
                _config.SampleRate,
                noiseReductionDb: _config.AnsNoiseReductionDb);
        }
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

        AudioBuffer current = micInput;

        // Apply AEC first to remove echo
        if (_aecProcessor != null)
        {
            current = _aecProcessor.Process(current, speakerReference);
        }

        // Apply ANS to remove noise
        if (_ansProcessor != null)
        {
            current = _ansProcessor.Process(current);
        }

        // Apply AGC last to normalize volume
        if (_agcProcessor != null)
        {
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
            Reset();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
