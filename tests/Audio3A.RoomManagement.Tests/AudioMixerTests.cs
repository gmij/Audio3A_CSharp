using Audio3A.RoomManagement.Audio;
using Audio3A.RoomManagement.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Audio3A.RoomManagement.Tests;

/// <summary>
/// AudioMixer 单元测试
/// </summary>
public class AudioMixerTests
{
    private readonly AudioMixer _mixer;

    public AudioMixerTests()
    {
        _mixer = new AudioMixer(NullLogger<AudioMixer>.Instance);
    }

    [Fact]
    public void Mix_NoFrames_ShouldReturnEmpty()
    {
        // Act
        var result = _mixer.Mix(Array.Empty<AudioFrame>());

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Mix_SingleFrame_ShouldReturnOriginal()
    {
        // Arrange
        var data = new short[] { 100, 200, 300 };
        var frame = new AudioFrame("user-1", data, 16000, 1, 1);

        // Act
        var result = _mixer.Mix(new[] { frame });

        // Assert
        Assert.Equal(data, result);
    }

    [Fact]
    public void Mix_TwoFrames_ShouldAverageValues()
    {
        // Arrange
        var data1 = new short[] { 100, 200, 300 };
        var data2 = new short[] { 200, 400, 600 };
        var frame1 = new AudioFrame("user-1", data1, 16000, 1, 1);
        var frame2 = new AudioFrame("user-2", data2, 16000, 1, 2);

        // Act
        var result = _mixer.Mix(new[] { frame1, frame2 });

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal(150, result[0]); // (100 + 200) / 2
        Assert.Equal(300, result[1]); // (200 + 400) / 2
        Assert.Equal(450, result[2]); // (300 + 600) / 2
    }

    [Fact]
    public void Mix_ExcludeParticipant_ShouldExcludeFrame()
    {
        // Arrange
        var data1 = new short[] { 100, 200, 300 };
        var data2 = new short[] { 200, 400, 600 };
        var frame1 = new AudioFrame("user-1", data1, 16000, 1, 1);
        var frame2 = new AudioFrame("user-2", data2, 16000, 1, 2);

        // Act
        var result = _mixer.Mix(new[] { frame1, frame2 }, "user-1");

        // Assert
        Assert.Equal(data2, result); // Only frame2 should be returned
    }

    [Fact]
    public void MixWithAutoGain_ShouldApplyEnergyWeighting()
    {
        // Arrange
        var data1 = new short[] { 1000, 2000, 3000 };
        var data2 = new short[] { 100, 200, 300 };
        var frame1 = new AudioFrame("user-1", data1, 16000, 1, 1);
        var frame2 = new AudioFrame("user-2", data2, 16000, 1, 2);

        // Act
        var result = _mixer.MixWithAutoGain(new[] { frame1, frame2 });

        // Assert
        Assert.Equal(3, result.Length);
        // frame1 has higher energy, so should dominate the mix
        Assert.True(Math.Abs(result[0] - data1[0]) < Math.Abs(result[0] - data2[0]));
    }

    [Fact]
    public void Mix_InconsistentFrameLengths_ShouldUseMinimum()
    {
        // Arrange
        var data1 = new short[] { 100, 200, 300, 400, 500 };
        var data2 = new short[] { 200, 400, 600 };
        var frame1 = new AudioFrame("user-1", data1, 16000, 1, 1);
        var frame2 = new AudioFrame("user-2", data2, 16000, 1, 2);

        // Act
        var result = _mixer.Mix(new[] { frame1, frame2 });

        // Assert
        Assert.Equal(3, result.Length); // Minimum length
    }
}
