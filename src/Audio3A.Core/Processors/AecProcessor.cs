namespace Audio3A.Core.Processors;

/// <summary>
/// Acoustic Echo Cancellation (AEC) processor
/// Removes acoustic echo from the microphone signal caused by speaker playback
/// </summary>
public class AecProcessor : IAudioProcessor
{
    private readonly int _filterLength;
    private readonly float _stepSize;
    private readonly float[] _filterCoefficients;
    private readonly float[] _referenceBuffer;
    private int _bufferPosition;
    private readonly int _sampleRate;

    /// <summary>
    /// Initializes a new AEC processor
    /// </summary>
    /// <param name="sampleRate">Sample rate in Hz</param>
    /// <param name="filterLength">Adaptive filter length (tail length in samples)</param>
    /// <param name="stepSize">Adaptation step size (learning rate)</param>
    public AecProcessor(int sampleRate = 16000, int filterLength = 512, float stepSize = 0.01f)
    {
        _sampleRate = sampleRate;
        _filterLength = filterLength;
        _stepSize = stepSize;
        _filterCoefficients = new float[filterLength];
        _referenceBuffer = new float[filterLength];
        _bufferPosition = 0;
    }

    /// <summary>
    /// Process audio with echo cancellation
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
            output[n] = error;

            // Update filter coefficients (NLMS algorithm)
            float power = 0.0f;
            for (int k = 0; k < _filterLength; k++)
            {
                int idx = (_bufferPosition - k + _filterLength) % _filterLength;
                power += _referenceBuffer[idx] * _referenceBuffer[idx];
            }
            power = power / _filterLength + 1e-6f;

            float mu = _stepSize / power;
            for (int k = 0; k < _filterLength; k++)
            {
                int idx = (_bufferPosition - k + _filterLength) % _filterLength;
                _filterCoefficients[k] += mu * error * _referenceBuffer[idx];
                // Prevent filter coefficients from growing unbounded
                _filterCoefficients[k] = Math.Max(-10.0f, Math.Min(10.0f, _filterCoefficients[k]));
            }

            _bufferPosition = (_bufferPosition + 1) % _filterLength;
        }

        // Copy remaining samples if reference was shorter
        for (int n = processLength; n < buffer.Length; n++)
        {
            output[n] = buffer.Samples[n];
        }

        return new AudioBuffer(output, buffer.Channels, buffer.SampleRate);
    }

    public AudioBuffer Process(AudioBuffer buffer)
    {
        // Process without reference (pass-through)
        return Process(buffer, null);
    }

    public void Reset()
    {
        Array.Clear(_filterCoefficients, 0, _filterLength);
        Array.Clear(_referenceBuffer, 0, _filterLength);
        _bufferPosition = 0;
    }
}
