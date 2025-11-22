using Audio3A.Core;
using Xunit;

namespace Audio3A.Tests;

public class AudioFormatTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var format = new AudioFormat();

        // Assert
        Assert.Equal(16000, format.SampleRate);
        Assert.Equal(1, format.Channels);
        Assert.Equal(16, format.BitsPerSample);
        Assert.Equal(160, format.FrameSize);
    }

    [Fact]
    public void Constructor_SetsCustomValues()
    {
        // Act
        var format = new AudioFormat(sampleRate: 48000, channels: 2, bitsPerSample: 32, frameSize: 480);

        // Assert
        Assert.Equal(48000, format.SampleRate);
        Assert.Equal(2, format.Channels);
        Assert.Equal(32, format.BitsPerSample);
        Assert.Equal(480, format.FrameSize);
    }

    [Fact]
    public void FrameDurationMs_CalculatesCorrectly()
    {
        // Arrange
        var format = new AudioFormat(sampleRate: 16000, frameSize: 160);

        // Act
        double duration = format.FrameDurationMs;

        // Assert
        Assert.Equal(10.0, duration, precision: 2);
    }

    [Theory]
    [InlineData(16000, 160, 10.0)]
    [InlineData(48000, 480, 10.0)]
    [InlineData(8000, 160, 20.0)]
    public void FrameDurationMs_VariousSampleRates(int sampleRate, int frameSize, double expectedMs)
    {
        // Arrange
        var format = new AudioFormat(sampleRate: sampleRate, frameSize: frameSize);

        // Act
        double duration = format.FrameDurationMs;

        // Assert
        Assert.Equal(expectedMs, duration, precision: 2);
    }
}
