using Audio3A.Core;
using Xunit;

namespace Audio3A.Tests;

public class AudioBufferTests
{
    [Fact]
    public void Constructor_WithLength_CreatesBufferWithCorrectSize()
    {
        // Arrange & Act
        var buffer = new AudioBuffer(160, channels: 1, sampleRate: 16000);

        // Assert
        Assert.Equal(160, buffer.Length);
        Assert.Equal(1, buffer.Channels);
        Assert.Equal(16000, buffer.SampleRate);
        Assert.NotNull(buffer.Samples);
    }

    [Fact]
    public void Constructor_WithSamples_StoresSamples()
    {
        // Arrange
        float[] samples = new float[] { 0.1f, 0.2f, 0.3f };

        // Act
        var buffer = new AudioBuffer(samples, channels: 1, sampleRate: 16000);

        // Assert
        Assert.Equal(3, buffer.Length);
        Assert.Equal(samples, buffer.Samples);
    }

    [Fact]
    public void FromInt16_ConvertsCorrectly()
    {
        // Arrange
        short[] pcmData = new short[] { 0, 16384, -16384, 32767, -32768 };

        // Act
        var buffer = AudioBuffer.FromInt16(pcmData);

        // Assert
        Assert.Equal(5, buffer.Length);
        Assert.Equal(0.0f, buffer.Samples[0], precision: 4);
        Assert.True(Math.Abs(buffer.Samples[1] - 0.5f) < 0.01f);
        Assert.True(Math.Abs(buffer.Samples[2] + 0.5f) < 0.01f);
        Assert.True(Math.Abs(buffer.Samples[3] - 1.0f) < 0.01f);
        Assert.True(Math.Abs(buffer.Samples[4] + 1.0f) < 0.01f);
    }

    [Fact]
    public void ToInt16_ConvertsCorrectly()
    {
        // Arrange
        float[] samples = new float[] { 0.0f, 0.5f, -0.5f, 1.0f, -1.0f };
        var buffer = new AudioBuffer(samples);

        // Act
        short[] pcmData = buffer.ToInt16();

        // Assert
        Assert.Equal(5, pcmData.Length);
        Assert.Equal(0, pcmData[0]);
        Assert.True(Math.Abs(pcmData[1] - 16383) < 100);
        Assert.True(Math.Abs(pcmData[2] + 16383) < 100);
        Assert.True(Math.Abs(pcmData[3] - 32767) < 100);
        Assert.True(Math.Abs(pcmData[4] + 32767) < 100);
    }

    [Fact]
    public void ToInt16_ClampsOutOfRangeValues()
    {
        // Arrange
        float[] samples = new float[] { 2.0f, -2.0f };
        var buffer = new AudioBuffer(samples);

        // Act
        short[] pcmData = buffer.ToInt16();

        // Assert
        Assert.Equal(32767, pcmData[0]); // Clamped to max
        Assert.Equal(-32767, pcmData[1]); // Clamped to min
    }
}
