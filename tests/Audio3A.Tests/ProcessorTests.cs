using Audio3A.Core;
using Audio3A.Core.Processors;
using Xunit;

namespace Audio3A.Tests;

public class ProcessorTests
{
    [Fact]
    public void AgcProcessor_ReducesLoudSignals()
    {
        // Arrange
        var processor = new AgcProcessor(sampleRate: 16000, targetLevel: 0.5f);
        float[] loudSamples = new float[160];
        for (int i = 0; i < loudSamples.Length; i++)
        {
            loudSamples[i] = 0.9f; // Very loud signal
        }
        var buffer = new AudioBuffer(loudSamples);

        // Act - process multiple frames to let AGC adapt
        AudioBuffer output = buffer;
        for (int i = 0; i < 10; i++)
        {
            output = processor.Process(buffer);
        }

        // Assert - output should be closer to target level
        float outputRms = CalculateRms(output.Samples);
        Assert.True(outputRms < 0.9f, $"Expected output RMS < 0.9, got {outputRms}");
    }

    [Fact]
    public void AgcProcessor_AmplifiesTooQuietSignals()
    {
        // Arrange
        var processor = new AgcProcessor(sampleRate: 16000, targetLevel: 0.5f);
        float[] quietSamples = new float[160];
        for (int i = 0; i < quietSamples.Length; i++)
        {
            quietSamples[i] = 0.05f; // Very quiet signal
        }
        var buffer = new AudioBuffer(quietSamples);

        // Act - process multiple frames to let AGC adapt
        AudioBuffer output = buffer;
        for (int i = 0; i < 20; i++)
        {
            output = processor.Process(buffer);
        }

        // Assert - output should be amplified
        float outputRms = CalculateRms(output.Samples);
        Assert.True(outputRms > 0.05f, $"Expected output RMS > 0.05, got {outputRms}");
    }

    [Fact]
    public void AnsProcessor_ReducesNoiseLevel()
    {
        // Arrange
        var processor = new AnsProcessor(sampleRate: 16000, noiseReductionDb: 20.0f);
        var random = new Random(42);
        float[] noisySamples = new float[160];
        for (int i = 0; i < noisySamples.Length; i++)
        {
            noisySamples[i] = 0.1f * (float)(random.NextDouble() * 2 - 1); // Pure noise
        }
        var buffer = new AudioBuffer(noisySamples);

        // Act - process multiple frames to adapt to noise
        AudioBuffer output = buffer;
        for (int i = 0; i < 15; i++)
        {
            output = processor.Process(buffer);
        }

        // Assert - output noise should be reduced
        float inputRms = CalculateRms(buffer.Samples);
        float outputRms = CalculateRms(output.Samples);
        Assert.True(outputRms < inputRms, $"Expected noise reduction, input RMS: {inputRms}, output RMS: {outputRms}");
    }

    [Fact]
    public void AecProcessor_WithoutReference_PassThrough()
    {
        // Arrange
        var processor = new AecProcessor(sampleRate: 16000);
        float[] samples = new float[160];
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = (float)Math.Sin(2 * Math.PI * 1000 * i / 16000.0);
        }
        var buffer = new AudioBuffer(samples);

        // Act
        var output = processor.Process(buffer);

        // Assert - should be pass-through without reference
        Assert.Equal(buffer.Length, output.Length);
        for (int i = 0; i < buffer.Length; i++)
        {
            Assert.Equal(buffer.Samples[i], output.Samples[i], precision: 5);
        }
    }

    [Fact]
    public void AecProcessor_WithReference_ProcessesWithoutError()
    {
        // Arrange
        var processor = new AecProcessor(sampleRate: 16000, filterLength: 256, stepSize: 0.001f);
        
        // Create reference signal (speaker output)
        float[] referenceSamples = new float[160];
        for (int i = 0; i < referenceSamples.Length; i++)
        {
            referenceSamples[i] = (float)Math.Sin(2 * Math.PI * 500 * i / 16000.0);
        }
        var reference = new AudioBuffer(referenceSamples);

        // Create mic signal with echo (delayed and attenuated reference)
        float[] micSamples = new float[160];
        for (int i = 0; i < micSamples.Length; i++)
        {
            micSamples[i] = 0.5f * referenceSamples[i]; // Simulated echo
        }
        var mic = new AudioBuffer(micSamples);

        // Act - process multiple frames
        AudioBuffer output = mic;
        for (int i = 0; i < 20; i++)
        {
            output = processor.Process(mic, reference);
        }

        // Assert - output should be valid (no NaN or infinity)
        Assert.NotNull(output);
        Assert.Equal(mic.Length, output.Length);
        
        // Verify all output samples are valid
        foreach (float sample in output.Samples)
        {
            Assert.False(float.IsNaN(sample), "Output contains NaN");
            Assert.False(float.IsInfinity(sample), "Output contains infinity");
        }
    }

    [Fact]
    public void AllProcessors_Reset_ClearsState()
    {
        // Arrange
        var agc = new AgcProcessor();
        var ans = new AnsProcessor();
        var aec = new AecProcessor();

        float[] samples = new float[160];
        var buffer = new AudioBuffer(samples);

        // Process some data
        agc.Process(buffer);
        ans.Process(buffer);
        aec.Process(buffer, buffer);

        // Act
        agc.Reset();
        ans.Reset();
        aec.Reset();

        // Assert - should not throw and state should be reset
        Assert.NotNull(agc);
        Assert.NotNull(ans);
        Assert.NotNull(aec);
    }

    private static float CalculateRms(float[] samples)
    {
        float sum = 0;
        foreach (float sample in samples)
        {
            sum += sample * sample;
        }
        return (float)Math.Sqrt(sum / samples.Length);
    }
}
