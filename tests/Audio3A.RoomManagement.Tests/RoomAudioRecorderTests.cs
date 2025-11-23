using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using Audio3A.Core;
using Audio3A.RoomManagement.Audio;

namespace Audio3A.RoomManagement.Tests;

/// <summary>
/// 房间音频录制器测试
/// </summary>
public class RoomAudioRecorderTests : IDisposable
{
    private readonly RoomAudioRecorder _recorder;
    private readonly string _testOutputDir = "test_recordings";
    private readonly string _testRoomId = "test_room_123";

    public RoomAudioRecorderTests()
    {
        // 清理测试目录
        if (Directory.Exists(_testOutputDir))
        {
            Directory.Delete(_testOutputDir, true);
        }

        _recorder = new RoomAudioRecorder(
            NullLogger<RoomAudioRecorder>.Instance,
            _testRoomId,
            _testOutputDir,
            sampleRate: 48000,
            channels: 1);
    }

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Assert
        Assert.False(_recorder.IsRecording);
        Assert.Null(_recorder.OutputFilePath);
    }

    [Fact]
    public void StartRecording_CreatesOutputFile()
    {
        // Act
        _recorder.StartRecording();

        // Assert
        Assert.True(_recorder.IsRecording);
        Assert.NotNull(_recorder.OutputFilePath);
        Assert.True(File.Exists(_recorder.OutputFilePath));
    }

    [Fact]
    public void StopRecording_ClosesFile()
    {
        // Arrange
        _recorder.StartRecording();
        var filePath = _recorder.OutputFilePath;

        // Act
        _recorder.StopRecording();

        // Assert
        Assert.False(_recorder.IsRecording);
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void AddAudioData_BeforeRecording_DoesNothing()
    {
        // Arrange
        var audioData = GenerateTestAudio(1000);

        // Act & Assert (should not throw)
        _recorder.AddAudioData(audioData);
    }

    [Fact]
    public void RecordAudio_SavesCorrectFormat()
    {
        // Arrange
        _recorder.StartRecording();
        var audioData = GenerateTestAudio(48000); // 1 秒的音频

        // Act
        _recorder.AddAudioData(audioData);
        Thread.Sleep(500); // 等待写入完成
        _recorder.StopRecording();

        // Assert
        var filePath = _recorder.OutputFilePath;
        Assert.NotNull(filePath);
        Assert.True(File.Exists(filePath));

        // 验证 WAV 文件头
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);

        // RIFF header
        var riff = new string(br.ReadChars(4));
        Assert.Equal("RIFF", riff);

        br.ReadInt32(); // file size

        var wave = new string(br.ReadChars(4));
        Assert.Equal("WAVE", wave);

        // fmt chunk
        var fmt = new string(br.ReadChars(4));
        Assert.Equal("fmt ", fmt);

        var fmtSize = br.ReadInt32();
        Assert.Equal(16, fmtSize);

        var audioFormat = br.ReadInt16();
        Assert.Equal(1, audioFormat); // PCM

        var channels = br.ReadInt16();
        Assert.Equal(1, channels);

        var sampleRate = br.ReadInt32();
        Assert.Equal(48000, sampleRate);
    }

    [Fact]
    public void RecordMultipleFrames_CombinesCorrectly()
    {
        // Arrange
        _recorder.StartRecording();

        // Act - 添加 10 帧音频
        for (int i = 0; i < 10; i++)
        {
            var audioData = GenerateTestAudio(4800); // 100ms 每帧
            _recorder.AddAudioData(audioData);
            Thread.Sleep(50);
        }

        Thread.Sleep(500); // 等待处理完成
        _recorder.StopRecording();

        // Assert
        var fileInfo = new FileInfo(_recorder.OutputFilePath!);
        Assert.True(fileInfo.Length > 1000); // 至少应该有一些数据
    }

    [Fact]
    public void StartRecording_WhileRecording_DoesNotCreateNewFile()
    {
        // Arrange
        _recorder.StartRecording();
        var firstFilePath = _recorder.OutputFilePath;

        // Act
        _recorder.StartRecording(); // 尝试再次开始

        // Assert
        Assert.Equal(firstFilePath, _recorder.OutputFilePath);
    }

    [Fact]
    public void StopRecording_WhileNotRecording_DoesNotThrow()
    {
        // Act & Assert (should not throw)
        _recorder.StopRecording();
    }

    /// <summary>
    /// 生成测试音频数据（正弦波）
    /// </summary>
    private float[] GenerateTestAudio(int sampleCount, float frequency = 440.0f)
    {
        var samples = new float[sampleCount];
        var sampleRate = 48000;

        for (int i = 0; i < sampleCount; i++)
        {
            var t = i / (float)sampleRate;
            samples[i] = (float)Math.Sin(2 * Math.PI * frequency * t) * 0.5f;
        }

        return samples;
    }

    public void Dispose()
    {
        _recorder.Dispose();

        // 清理测试目录
        if (Directory.Exists(_testOutputDir))
        {
            try
            {
                Directory.Delete(_testOutputDir, true);
            }
            catch
            {
                // 忽略清理错误
            }
        }
    }
}
