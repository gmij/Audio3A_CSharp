using Microsoft.Extensions.Logging;

namespace Audio3A.Core.Processors;

/// <summary>
/// Automatic Gain Control (AGC) processor
/// Automatically adjusts audio volume to maintain consistent output level
/// </summary>
public class AgcProcessor : IAudioProcessor
{
    private readonly ILogger<AgcProcessor> _logger;
    private readonly float _targetLevel;
    private readonly float _compressionRatio;
    private readonly float _attackTime;
    private readonly float _releaseTime;
    private float _currentGain;
    private float _envelopeLevel;
    private readonly int _sampleRate;

    /// <summary>
    /// Initializes a new AGC processor
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="sampleRate">Sample rate in Hz</param>
    /// <param name="targetLevel">Target output level (0.0 to 1.0)</param>
    /// <param name="compressionRatio">Compression ratio (1.0 = no compression)</param>
    /// <param name="attackTimeMs">Attack time in milliseconds</param>
    /// <param name="releaseTimeMs">Release time in milliseconds</param>
    public AgcProcessor(
        ILogger<AgcProcessor> logger,
        int sampleRate = 16000,
        float targetLevel = 0.5f, 
        float compressionRatio = 3.0f,
        float attackTimeMs = 10.0f,
        float releaseTimeMs = 100.0f)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sampleRate = sampleRate;
        _targetLevel = targetLevel;
        _compressionRatio = compressionRatio;
        _attackTime = 1.0f - (float)Math.Exp(-1.0 / (sampleRate * attackTimeMs / 1000.0));
        _releaseTime = 1.0f - (float)Math.Exp(-1.0 / (sampleRate * releaseTimeMs / 1000.0));
        _currentGain = 1.0f;
        _envelopeLevel = 0.0f;

        _logger.LogDebug(
            "AGC processor initialized: SampleRate={SampleRate}, TargetLevel={TargetLevel}, CompressionRatio={CompressionRatio}",
            sampleRate, targetLevel, compressionRatio);
    }

    public AudioBuffer Process(AudioBuffer buffer)
    {
        float[] output = new float[buffer.Length];

        for (int i = 0; i < buffer.Length; i++)
        {
            float inputSample = buffer.Samples[i];
            float absLevel = Math.Abs(inputSample);

            // Envelope follower
            float coefficient = absLevel > _envelopeLevel ? _attackTime : _releaseTime;
            _envelopeLevel += coefficient * (absLevel - _envelopeLevel);

            // Calculate desired gain
            float desiredGain = 1.0f;
            if (_envelopeLevel > 0.001f)
            {
                float levelDiff = _targetLevel / _envelopeLevel;
                // Compression or expansion
                desiredGain = levelDiff < 1.0f
                    ? (float)Math.Pow(levelDiff, 1.0 / _compressionRatio)  // Compression
                    : Math.Min(levelDiff, 4.0f);  // Expansion (limited)
            }

            // Smooth gain changes
            _currentGain += 0.01f * (desiredGain - _currentGain);
            _currentGain = Math.Max(0.1f, Math.Min(_currentGain, 10.0f));

            // Apply gain
            output[i] = inputSample * _currentGain;
            output[i] = Math.Max(-1.0f, Math.Min(1.0f, output[i]));
        }

        return new AudioBuffer(output, buffer.Channels, buffer.SampleRate);
    }

    public void Reset()
    {
        _logger.LogDebug("Resetting AGC processor state");
        _currentGain = 1.0f;
        _envelopeLevel = 0.0f;
    }
}
