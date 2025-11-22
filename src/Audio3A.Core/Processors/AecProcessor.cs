using Microsoft.Extensions.Logging;

namespace Audio3A.Core.Processors;

/// <summary>
/// Acoustic Echo Cancellation (AEC) processor
/// WebRTC-inspired implementation with improved echo suppression and double-talk detection
/// </summary>
public class AecProcessor : IAudioProcessor
{
    private readonly ILogger<AecProcessor> _logger;
    private readonly int _filterLength;
    private readonly float _stepSize;
    private readonly float[] _filterCoefficients;
    private readonly float[] _referenceBuffer;
    private int _bufferPosition;
    private readonly int _sampleRate;
    
    // WebRTC-inspired enhancements
    private readonly float[] _errorHistory;
    private int _errorHistoryPos;
    private float _echoReturnLoss;
    private readonly float _dtdThreshold = 0.3f;  // Double-talk detection threshold
    
    /// <summary>
    /// Initializes a new AEC processor with WebRTC-inspired improvements
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="sampleRate">Sample rate in Hz</param>
    /// <param name="filterLength">Adaptive filter length (tail length in samples)</param>
    /// <param name="stepSize">Adaptation step size (learning rate)</param>
    public AecProcessor(
        ILogger<AecProcessor> logger,
        int sampleRate = 16000,
        int filterLength = 512,
        float stepSize = 0.01f)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sampleRate = sampleRate;
        _filterLength = filterLength;
        _stepSize = stepSize;
        _filterCoefficients = new float[filterLength];
        _referenceBuffer = new float[filterLength];
        _bufferPosition = 0;
        
        // Initialize enhancement features
        _errorHistory = new float[100];
        _errorHistoryPos = 0;
        _echoReturnLoss = 0.0f;

        _logger.LogDebug(
            "AEC processor initialized: SampleRate={SampleRate}, FilterLength={FilterLength}, StepSize={StepSize}",
            sampleRate, filterLength, stepSize);
    }

    /// <summary>
    /// Process audio with enhanced echo cancellation using WebRTC-inspired techniques
    /// </summary>
    /// <param name="buffer">Microphone input buffer</param>
    /// <param name="reference">Speaker reference signal (null if not available)</param>
    /// <returns>Echo-cancelled audio buffer</returns>
    public AudioBuffer Process(AudioBuffer buffer, AudioBuffer? reference = null)
    {
        float[] output = new float[buffer.Length];

        if (reference == null || reference.Length == 0)
        {
            // No reference signal - pass through
            Array.Copy(buffer.Samples, output, buffer.Length);
            return new AudioBuffer(output, buffer.Channels, buffer.SampleRate);
        }

        // Ensure reference is same length as input
        int processLength = Math.Min(buffer.Length, reference.Length);

        for (int n = 0; n < processLength; n++)
        {
            // Update reference buffer (circular buffer)
            _referenceBuffer[_bufferPosition] = reference.Samples[n];
            
            // Estimate echo using adaptive filter
            float echoEstimate = 0.0f;
            for (int k = 0; k < _filterLength; k++)
            {
                int idx = (_bufferPosition - k + _filterLength) % _filterLength;
                echoEstimate += _filterCoefficients[k] * _referenceBuffer[idx];
            }

            // Calculate error (mic signal minus echo estimate)
            float error = buffer.Samples[n] - echoEstimate;
            
            // Double-talk detection (WebRTC-inspired)
            float micPower = buffer.Samples[n] * buffer.Samples[n];
            float refPower = reference.Samples[n] * reference.Samples[n];
            bool isDoubleTalk = DetectDoubleTalk(micPower, refPower, error * error);
            
            // Update Echo Return Loss (ERL) estimation
            UpdateEchoReturnLoss(micPower, echoEstimate * echoEstimate);
            
            // Apply non-linear processing for residual echo suppression
            float suppressedError = ApplyNonLinearSuppression(error, echoEstimate, isDoubleTalk);
            output[n] = suppressedError;

            // Update filter coefficients (NLMS algorithm with double-talk handling)
            if (!isDoubleTalk)
            {
                // Calculate normalized power
                float power = 0.0f;
                for (int k = 0; k < _filterLength; k++)
                {
                    int idx = (_bufferPosition - k + _filterLength) % _filterLength;
                    power += _referenceBuffer[idx] * _referenceBuffer[idx];
                }
                power = power / _filterLength + 1e-6f;

                float mu = _stepSize / power;
                
                // Adaptive step size based on echo return loss
                mu *= Math.Min(1.0f, _echoReturnLoss / 10.0f);
                
                for (int k = 0; k < _filterLength; k++)
                {
                    int idx = (_bufferPosition - k + _filterLength) % _filterLength;
                    _filterCoefficients[k] += mu * error * _referenceBuffer[idx];
                    // Prevent filter coefficients from growing unbounded
                    _filterCoefficients[k] = SignalProcessingHelpers.Clamp(_filterCoefficients[k], -10.0f, 10.0f);
                }
            }

            // Store error for history
            _errorHistory[_errorHistoryPos] = error * error;
            _errorHistoryPos = (_errorHistoryPos + 1) % _errorHistory.Length;
            
            _bufferPosition = (_bufferPosition + 1) % _filterLength;
        }

        // Copy remaining samples if reference was shorter
        for (int n = processLength; n < buffer.Length; n++)
        {
            output[n] = buffer.Samples[n];
        }

        return new AudioBuffer(output, buffer.Channels, buffer.SampleRate);
    }
    
    /// <summary>
    /// Detect double-talk scenario (when both near-end and far-end are talking)
    /// Inspired by WebRTC's Geigel double-talk detector
    /// </summary>
    private bool DetectDoubleTalk(float micPower, float refPower, float errorPower)
    {
        // Require minimum reference power for meaningful detection
        const float minRefPower = 1e-6f;
        if (refPower < minRefPower) return false;
        
        float ratio = micPower / refPower;
        return ratio > _dtdThreshold;
    }
    
    /// <summary>
    /// Update Echo Return Loss estimation (WebRTC-inspired)
    /// </summary>
    private void UpdateEchoReturnLoss(float micPower, float echoPower)
    {
        if (echoPower > 1e-10f)
        {
            float currentErl = 10.0f * (float)Math.Log10(micPower / (echoPower + 1e-10f));
            // Smooth ERL estimation
            _echoReturnLoss = 0.95f * _echoReturnLoss + 0.05f * currentErl;
            _echoReturnLoss = SignalProcessingHelpers.Clamp(_echoReturnLoss, 0.0f, 50.0f);
        }
    }
    
    /// <summary>
    /// Apply non-linear processing for residual echo suppression
    /// Inspired by WebRTC's comfort noise injection and suppression
    /// </summary>
    private float ApplyNonLinearSuppression(float error, float echoEstimate, bool isDoubleTalk)
    {
        if (isDoubleTalk)
        {
            // During double-talk, don't suppress aggressively
            return error;
        }
        
        // Calculate suppression gain based on echo estimate strength
        float echoLevel = Math.Abs(echoEstimate);
        float errorLevel = Math.Abs(error);
        
        if (echoLevel > 1e-6f)
        {
            // Suppression factor based on residual echo
            float suppressionFactor = Math.Min(1.0f, errorLevel / (echoLevel + 1e-6f));
            
            // Apply smooth suppression with noise floor
            float noiseFloor = 0.001f;
            float suppressedLevel = Math.Max(errorLevel * suppressionFactor, noiseFloor);
            
            return error >= 0 ? suppressedLevel : -suppressedLevel;
        }
        
        return error;
    }

    public AudioBuffer Process(AudioBuffer buffer)
    {
        // Process without reference (pass-through)
        return Process(buffer, null);
    }

    public void Reset()
    {
        _logger.LogDebug("Resetting AEC processor state");
        Array.Clear(_filterCoefficients, 0, _filterLength);
        Array.Clear(_referenceBuffer, 0, _filterLength);
        Array.Clear(_errorHistory, 0, _errorHistory.Length);
        _bufferPosition = 0;
        _errorHistoryPos = 0;
        _echoReturnLoss = 0.0f;
    }
}
