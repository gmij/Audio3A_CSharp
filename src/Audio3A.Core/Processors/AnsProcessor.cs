using Microsoft.Extensions.Logging;

namespace Audio3A.Core.Processors;

/// <summary>
/// Automatic Noise Suppression (ANS) processor
/// Reduces background noise while preserving speech
/// </summary>
public class AnsProcessor : IAudioProcessor
{
    private readonly ILogger<AnsProcessor> _logger;
    private readonly int _sampleRate;
    private readonly float _noiseReductionDb;
    private float _noiseFloor;
    private int _frameCount;

    /// <summary>
    /// Initializes a new ANS processor
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="sampleRate">Sample rate in Hz</param>
    /// <param name="fftSize">FFT size for frequency analysis (reserved for future use)</param>
    /// <param name="noiseReductionDb">Noise reduction strength in dB</param>
    public AnsProcessor(
        ILogger<AnsProcessor> logger,
        int sampleRate = 16000,
        int fftSize = 256,
        float noiseReductionDb = 20.0f)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sampleRate = sampleRate;
        _noiseReductionDb = noiseReductionDb;
        _noiseFloor = 0.001f;
        _frameCount = 0;

        _logger.LogDebug(
            "ANS processor initialized: SampleRate={SampleRate}, NoiseReductionDb={NoiseReductionDb}",
            sampleRate, noiseReductionDb);
    }

    public AudioBuffer Process(AudioBuffer buffer)
    {
        float[] output = new float[buffer.Length];

        // Simple energy-based noise suppression
        // Note: Future versions may implement FFT-based spectral subtraction
        
        // Calculate signal energy
        float signalEnergy = 0;
        for (int i = 0; i < buffer.Length; i++)
        {
            signalEnergy += buffer.Samples[i] * buffer.Samples[i];
        }
        signalEnergy /= buffer.Length;

        // Update noise profile during initial frames or low energy periods
        if (_frameCount < 10 || signalEnergy < _noiseFloor * 2)
        {
            _noiseFloor = 0.9f * _noiseFloor + 0.1f * signalEnergy;
            _frameCount++;
        }

        // Calculate noise reduction gain
        float snr = signalEnergy / (_noiseFloor + 1e-10f);
        float gain;

        if (snr < 2.0f)
        {
            // Low SNR - strong suppression
            float reductionLinear = (float)Math.Pow(10.0, -_noiseReductionDb / 20.0);
            gain = reductionLinear;
        }
        else if (snr < 10.0f)
        {
            // Medium SNR - moderate suppression
            gain = 0.3f + 0.7f * (snr - 2.0f) / 8.0f;
        }
        else
        {
            // High SNR - minimal suppression
            gain = 1.0f;
        }

        // Apply smoothed gain
        for (int i = 0; i < buffer.Length; i++)
        {
            output[i] = buffer.Samples[i] * gain;
        }

        return new AudioBuffer(output, buffer.Channels, buffer.SampleRate);
    }

    public void Reset()
    {
        _logger.LogDebug("Resetting ANS processor state");
        _noiseFloor = 0.001f;
        _frameCount = 0;
    }
}
