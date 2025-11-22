using Audio3A.Core;
using Xunit;

namespace Audio3A.Tests;

public class Audio3AProcessorTests
{
    [Fact]
    public void Constructor_WithDefaultConfig_CreatesProcessor()
    {
        // Act
        using var processor = new Audio3AProcessor();

        // Assert
        Assert.NotNull(processor);
        Assert.NotNull(processor.Config);
        Assert.True(processor.Config.EnableAec);
        Assert.True(processor.Config.EnableAgc);
        Assert.True(processor.Config.EnableAns);
    }

    [Fact]
    public void Constructor_WithCustomConfig_UsesConfig()
    {
        // Arrange
        var config = new Audio3AConfig
        {
            EnableAec = false,
            EnableAgc = true,
            EnableAns = false,
            SampleRate = 48000
        };

        // Act
        using var processor = new Audio3AProcessor(config);

        // Assert
        Assert.False(processor.Config.EnableAec);
        Assert.True(processor.Config.EnableAgc);
        Assert.False(processor.Config.EnableAns);
        Assert.Equal(48000, processor.Config.SampleRate);
    }

    [Fact]
    public void Process_WithValidBuffer_ReturnsProcessedBuffer()
    {
        // Arrange
        using var processor = new Audio3AProcessor();
        float[] samples = new float[160];
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = (float)Math.Sin(2 * Math.PI * 1000 * i / 16000.0);
        }
        var buffer = new AudioBuffer(samples);

        // Act
        var output = processor.Process(buffer);

        // Assert
        Assert.NotNull(output);
        Assert.Equal(buffer.Length, output.Length);
    }

    [Fact]
    public void Process_WithReference_ProcessesWithAec()
    {
        // Arrange
        using var processor = new Audio3AProcessor();
        float[] micSamples = new float[160];
        float[] refSamples = new float[160];
        
        for (int i = 0; i < 160; i++)
        {
            micSamples[i] = (float)Math.Sin(2 * Math.PI * 1000 * i / 16000.0);
            refSamples[i] = (float)Math.Sin(2 * Math.PI * 500 * i / 16000.0);
        }
        
        var micBuffer = new AudioBuffer(micSamples);
        var refBuffer = new AudioBuffer(refSamples);

        // Act
        var output = processor.Process(micBuffer, refBuffer);

        // Assert
        Assert.NotNull(output);
        Assert.Equal(micBuffer.Length, output.Length);
    }

    [Fact]
    public void ProcessInt16_WithPcmData_ReturnsProcessedPcm()
    {
        // Arrange
        using var processor = new Audio3AProcessor();
        short[] pcmData = new short[160];
        for (int i = 0; i < pcmData.Length; i++)
        {
            pcmData[i] = (short)(10000 * Math.Sin(2 * Math.PI * 1000 * i / 16000.0));
        }

        // Act
        short[] output = processor.ProcessInt16(pcmData);

        // Assert
        Assert.NotNull(output);
        Assert.Equal(pcmData.Length, output.Length);
    }

    [Fact]
    public void ProcessInt16_WithReference_ProcessesWithAec()
    {
        // Arrange
        using var processor = new Audio3AProcessor();
        short[] micPcm = new short[160];
        short[] refPcm = new short[160];

        // Act
        short[] output = processor.ProcessInt16(micPcm, refPcm);

        // Assert
        Assert.NotNull(output);
        Assert.Equal(micPcm.Length, output.Length);
    }

    [Fact]
    public void Reset_ClearsProcessorState()
    {
        // Arrange
        using var processor = new Audio3AProcessor();
        var buffer = new AudioBuffer(new float[160]);
        processor.Process(buffer);

        // Act
        processor.Reset();

        // Assert - should not throw
        var output = processor.Process(buffer);
        Assert.NotNull(output);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var processor = new Audio3AProcessor();

        // Act & Assert - should not throw
        processor.Dispose();
        processor.Dispose();
    }

    [Fact]
    public void Process_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var processor = new Audio3AProcessor();
        var buffer = new AudioBuffer(new float[160]);
        processor.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => processor.Process(buffer));
    }

    [Fact]
    public void Process_DisabledProcessors_StillWorks()
    {
        // Arrange
        var config = new Audio3AConfig
        {
            EnableAec = false,
            EnableAgc = false,
            EnableAns = false
        };
        using var processor = new Audio3AProcessor(config);
        var buffer = new AudioBuffer(new float[160]);

        // Act
        var output = processor.Process(buffer);

        // Assert
        Assert.NotNull(output);
        Assert.Equal(buffer.Length, output.Length);
    }
}
