namespace Audio3A.Core;

/// <summary>
/// Base interface for audio processors
/// </summary>
public interface IAudioProcessor
{
    /// <summary>
    /// Process an audio buffer
    /// </summary>
    /// <param name="buffer">Input audio buffer</param>
    /// <returns>Processed audio buffer</returns>
    AudioBuffer Process(AudioBuffer buffer);

    /// <summary>
    /// Reset the processor state
    /// </summary>
    void Reset();
}
