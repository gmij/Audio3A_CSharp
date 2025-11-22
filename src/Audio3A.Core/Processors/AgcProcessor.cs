using Microsoft.Extensions.Logging;

namespace Audio3A.Core.Processors;

/// <summary>
/// Automatic Gain Control (AGC) processor
/// WebRTC-inspired implementation with Voice Activity Detection and adaptive gain control
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
    
    // WebRTC-inspired VAD and digital gain control
    private readonly float[] _energyHistory;
    private int _energyHistoryPos;
    private float _noiseFloor;
    private readonly float _vadThreshold = 0.03f;
    private int _speechFrameCount;
    private readonly float _minGain = 0.1f;
    private readonly float _maxGain = 30.0f;  // WebRTC allows higher gain

    /// <summary>
    /// Initializes a new AGC processor with WebRTC-inspired VAD
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
        
        // Initialize VAD components
        _energyHistory = new float[50];
        _energyHistoryPos = 0;
        _noiseFloor = 0.001f;
        _speechFrameCount = 0;

        _logger.LogDebug(
            "AGC processor initialized: SampleRate={SampleRate}, TargetLevel={TargetLevel}, CompressionRatio={CompressionRatio}",
            sampleRate, targetLevel, compressionRatio);
    }

    public AudioBuffer Process(AudioBuffer buffer)
    {
        float[] output = new float[buffer.Length];
        
        // Calculate frame energy for VAD
        float frameEnergy = SignalProcessingHelpers.CalculateFrameEnergy(buffer.Samples);
        
        // Update noise floor estimation (WebRTC-inspired)
        UpdateNoiseFloor(frameEnergy);
        
        // Voice Activity Detection
        bool isSpeech = DetectVoiceActivity(frameEnergy);

        for (int i = 0; i < buffer.Length; i++)
        {
            float inputSample = buffer.Samples[i];
            float absLevel = Math.Abs(inputSample);

            // Envelope follower with VAD-aware time constants
            float attackCoeff = isSpeech ? _attackTime : _attackTime * 0.5f;
            float releaseCoeff = isSpeech ? _releaseTime : _releaseTime * 2.0f;
            float coefficient = absLevel > _envelopeLevel ? attackCoeff : releaseCoeff;
            _envelopeLevel += coefficient * (absLevel - _envelopeLevel);

            // Calculate desired gain with VAD consideration
            float desiredGain = 1.0f;
            if (_envelopeLevel > _noiseFloor)
            {
                float levelDiff = _targetLevel / _envelopeLevel;
                
                if (isSpeech)
                {
                    // During speech: apply compression/expansion
                    desiredGain = levelDiff < 1.0f
                        ? (float)Math.Pow(levelDiff, 1.0 / _compressionRatio)  // Compression
                        : Math.Min(levelDiff, _maxGain);  // Expansion (WebRTC allows higher gain)
                }
                else
                {
                    // During silence: gentle gain adjustment to avoid amplifying noise
                    desiredGain = Math.Min(levelDiff, 2.0f);
                }
            }

            // Smooth gain changes with adaptive smoothing
            float gainSmoothingFactor = isSpeech ? 0.02f : 0.005f;
            _currentGain += gainSmoothingFactor * (desiredGain - _currentGain);
            _currentGain = SignalProcessingHelpers.Clamp(_currentGain, _minGain, _maxGain);

            // Apply gain with limiter
            output[i] = SignalProcessingHelpers.SoftLimiter(inputSample * _currentGain);
        }

        return new AudioBuffer(output, buffer.Channels, buffer.SampleRate);
    }
    

    
    /// <summary>
    /// Update noise floor estimation (WebRTC-inspired minimum statistics)
    /// </summary>
    private void UpdateNoiseFloor(float frameEnergy)
    {
        _energyHistory[_energyHistoryPos] = frameEnergy;
        _energyHistoryPos = (_energyHistoryPos + 1) % _energyHistory.Length;
        
        // Estimate noise floor as minimum of recent energy values
        float minEnergy = float.MaxValue;
        for (int i = 0; i < _energyHistory.Length; i++)
        {
            if (_energyHistory[i] < minEnergy)
                minEnergy = _energyHistory[i];
        }
        
        // Smooth noise floor update
        _noiseFloor = 0.95f * _noiseFloor + 0.05f * (minEnergy + 0.001f);
    }
    
    /// <summary>
    /// Voice Activity Detection (WebRTC-inspired energy-based VAD)
    /// </summary>
    private bool DetectVoiceActivity(float frameEnergy)
    {
        // Simple energy-based VAD with hysteresis
        float snr = frameEnergy / (_noiseFloor + 1e-10f);
        
        if (snr > _vadThreshold)
        {
            _speechFrameCount = Math.Min(_speechFrameCount + 1, 10);
        }
        else
        {
            _speechFrameCount = Math.Max(_speechFrameCount - 1, 0);
        }
        
        // Require multiple frames for speech decision (hysteresis)
        return _speechFrameCount > 3;
    }
    


    public void Reset()
    {
        _logger.LogDebug("Resetting AGC processor state");
        _currentGain = 1.0f;
        _envelopeLevel = 0.0f;
        Array.Clear(_energyHistory, 0, _energyHistory.Length);
        _energyHistoryPos = 0;
        _noiseFloor = 0.001f;
        _speechFrameCount = 0;
    }
}
