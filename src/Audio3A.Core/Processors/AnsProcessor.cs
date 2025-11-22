using Microsoft.Extensions.Logging;

namespace Audio3A.Core.Processors;

/// <summary>
/// Automatic Noise Suppression (ANS) processor
/// WebRTC-inspired implementation with improved noise estimation and spectral suppression
/// </summary>
public class AnsProcessor : IAudioProcessor
{
    private readonly ILogger<AnsProcessor> _logger;
    private readonly int _sampleRate;
    private readonly float _noiseReductionDb;
    private float _noiseFloor;
    private int _frameCount;
    
    // WebRTC-inspired enhancements
    private readonly float[] _noiseEstimate;
    private readonly int _smoothingFrames = 20;
    private float _speechProbability;
    private readonly float _overSubtractionFactor = 2.0f;  // WebRTC uses over-subtraction
    
    /// <summary>
    /// Initializes a new ANS processor with WebRTC-inspired noise estimation
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="sampleRate">Sample rate in Hz</param>
    /// <param name="noiseReductionDb">Noise reduction strength in dB</param>
    public AnsProcessor(
        ILogger<AnsProcessor> logger,
        int sampleRate = 16000,
        float noiseReductionDb = 20.0f)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sampleRate = sampleRate;
        _noiseReductionDb = noiseReductionDb;
        _noiseFloor = 0.001f;
        _frameCount = 0;
        
        // Initialize WebRTC-inspired components
        _noiseEstimate = new float[_smoothingFrames];
        _speechProbability = 0.0f;

        _logger.LogDebug(
            "ANS processor initialized: SampleRate={SampleRate}, NoiseReductionDb={NoiseReductionDb}",
            sampleRate, noiseReductionDb);
    }

    public AudioBuffer Process(AudioBuffer buffer)
    {
        float[] output = new float[buffer.Length];

        // Calculate signal energy and spectral characteristics
        float signalEnergy = SignalProcessingHelpers.CalculateFrameEnergy(buffer.Samples);
        float zeroCrossings = 0;
        
        // Count zero crossings
        for (int i = 1; i < buffer.Length; i++)
        {
            if (buffer.Samples[i] * buffer.Samples[i-1] < 0)
                zeroCrossings++;
        }
        float zcr = zeroCrossings / buffer.Length;

        // Update noise estimation using WebRTC-inspired minimum statistics
        UpdateNoiseEstimate(signalEnergy);
        
        // Estimate speech probability (WebRTC-inspired)
        _speechProbability = EstimateSpeechProbability(signalEnergy, zcr);

        // Calculate SNR
        float snr = SignalProcessingHelpers.CalculateSnr(signalEnergy, _noiseFloor);
        
        // WebRTC-inspired spectral subtraction with over-subtraction
        float gain = CalculateSuppressionGain(snr, _speechProbability);
        
        // Apply gain with spectral smoothing
        for (int i = 0; i < buffer.Length; i++)
        {
            // Apply noise suppression with comfort noise injection
            float suppressedSample = buffer.Samples[i] * gain;
            
            // Add minimal comfort noise during silence (WebRTC-inspired)
            if (_speechProbability < 0.3f)
            {
                float comfortNoise = SignalProcessingHelpers.GenerateComfortNoise();
                suppressedSample += comfortNoise;
            }
            
            output[i] = suppressedSample;
        }

        return new AudioBuffer(output, buffer.Channels, buffer.SampleRate);
    }
    
    /// <summary>
    /// Update noise estimate using minimum statistics (WebRTC-inspired)
    /// </summary>
    private void UpdateNoiseEstimate(float signalEnergy)
    {
        int idx = _frameCount % _smoothingFrames;
        _noiseEstimate[idx] = signalEnergy;
        _frameCount++;
        
        // Calculate noise floor as minimum of recent frames
        if (_frameCount >= _smoothingFrames)
        {
            float minEnergy = float.MaxValue;
            for (int i = 0; i < _smoothingFrames; i++)
            {
                if (_noiseEstimate[i] < minEnergy)
                    minEnergy = _noiseEstimate[i];
            }
            
            // Smooth noise floor update
            _noiseFloor = 0.95f * _noiseFloor + 0.05f * (minEnergy + 0.0001f);
        }
        else
        {
            // Initial adaptation
            _noiseFloor = 0.9f * _noiseFloor + 0.1f * signalEnergy;
        }
    }
    
    /// <summary>
    /// Estimate speech probability using energy and spectral features (WebRTC-inspired)
    /// </summary>
    private float EstimateSpeechProbability(float signalEnergy, float zeroCrossingRate)
    {
        // Energy-based probability
        float snr = SignalProcessingHelpers.CalculateSnr(signalEnergy, _noiseFloor);
        float energyProb = SignalProcessingHelpers.Clamp((snr - 1.0f) / 5.0f, 0.0f, 1.0f);
        
        // Zero-crossing rate feature (speech typically has moderate ZCR)
        float zcrProb = SignalProcessingHelpers.Clamp(1.0f - Math.Abs(zeroCrossingRate - 0.15f) / 0.15f, 0.0f, 1.0f);
        
        // Combine features
        float probability = 0.7f * energyProb + 0.3f * zcrProb;
        
        // Smooth probability over time
        _speechProbability = 0.8f * _speechProbability + 0.2f * probability;
        
        return _speechProbability;
    }
    
    /// <summary>
    /// Calculate suppression gain with WebRTC-inspired over-subtraction
    /// </summary>
    private float CalculateSuppressionGain(float snr, float speechProbability)
    {
        float gain;
        
        if (speechProbability > 0.7f)
        {
            // High speech probability - minimal suppression
            gain = 1.0f;
        }
        else if (speechProbability > 0.3f)
        {
            // Uncertain - moderate suppression
            float baseGain = Math.Min(1.0f, snr / 3.0f);
            gain = 0.3f + 0.7f * baseGain;
        }
        else
        {
            // Low speech probability - aggressive suppression with over-subtraction
            float reductionLinear = (float)Math.Pow(10.0, -_noiseReductionDb / 20.0);
            
            // Over-subtraction factor (WebRTC technique)
            float overSubtraction = _overSubtractionFactor * reductionLinear;
            gain = Math.Max(overSubtraction, snr / 10.0f);
            
            // Apply spectral floor to avoid musical noise
            float spectralFloor = 0.01f;
            gain = Math.Max(gain, spectralFloor);
        }
        
        return Math.Min(1.0f, gain);
    }


    public void Reset()
    {
        _logger.LogDebug("Resetting ANS processor state");
        _noiseFloor = 0.001f;
        _frameCount = 0;
        _speechProbability = 0.0f;
        Array.Clear(_noiseEstimate, 0, _noiseEstimate.Length);
    }
}
