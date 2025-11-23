using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Audio3A.Core;

namespace Audio3A.RoomManagement.Audio;

/// <summary>
/// 房间音频录制器 - 录制并保存房间的混音音频
/// 遵循单一职责原则：只负责录音，不负责混音
/// </summary>
public class RoomAudioRecorder : IDisposable
{
    private readonly ILogger<RoomAudioRecorder> _logger;
    private readonly string _roomId;
    private readonly string _outputDirectory;
    private readonly ConcurrentQueue<float[]> _audioQueue;
    private readonly CancellationTokenSource _cts;
    private readonly Task _recordingTask;
    private FileStream? _fileStream;
    private BinaryWriter? _writer;
    private bool _disposed;
    private long _totalSamples;
    private readonly int _sampleRate;
    private readonly int _channels;

    public bool IsRecording { get; private set; }
    public string? OutputFilePath { get; private set; }

    public RoomAudioRecorder(
        ILogger<RoomAudioRecorder> logger,
        string roomId,
        string outputDirectory = "recordings",
        int sampleRate = 48000,
        int channels = 1)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _roomId = roomId ?? throw new ArgumentNullException(nameof(roomId));
        _outputDirectory = outputDirectory;
        _sampleRate = sampleRate;
        _channels = channels;
        _audioQueue = new ConcurrentQueue<float[]>();
        _cts = new CancellationTokenSource();
        _recordingTask = Task.Run(RecordingLoop);
    }

    /// <summary>
    /// 开始录音
    /// </summary>
    public void StartRecording()
    {
        if (IsRecording)
        {
            _logger.LogWarning("录音已在进行中");
            return;
        }

        try
        {
            // 创建输出目录
            Directory.CreateDirectory(_outputDirectory);

            // 生成文件名
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"{_roomId}_{timestamp}.wav";
            OutputFilePath = Path.Combine(_outputDirectory, fileName);

            // 创建文件流
            _fileStream = new FileStream(OutputFilePath, FileMode.Create, FileAccess.Write);
            _writer = new BinaryWriter(_fileStream);

            // 写入 WAV 文件头（占位符，录音结束后更新）
            WriteWavHeader(_writer, 0);

            IsRecording = true;
            _totalSamples = 0;

            _logger.LogInformation("开始录音: RoomId={RoomId}, File={FilePath}", _roomId, OutputFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动录音失败");
            CleanupFileResources();
            throw;
        }
    }

    /// <summary>
    /// 停止录音
    /// </summary>
    public void StopRecording()
    {
        if (!IsRecording)
        {
            _logger.LogWarning("没有正在进行的录音");
            return;
        }

        try
        {
            IsRecording = false;

            // 等待队列处理完毕（最多等待 5 秒）
            var timeout = TimeSpan.FromSeconds(5);
            var startTime = DateTime.UtcNow;
            while (!_audioQueue.IsEmpty && DateTime.UtcNow - startTime < timeout)
            {
                Thread.Sleep(50);
            }

            // 更新 WAV 文件头
            if (_writer != null && _fileStream != null)
            {
                _fileStream.Seek(0, SeekOrigin.Begin);
                WriteWavHeader(_writer, _totalSamples);
            }

            CleanupFileResources();

            _logger.LogInformation(
                "录音完成: RoomId={RoomId}, File={FilePath}, Samples={Samples}, Duration={Duration}s",
                _roomId, OutputFilePath, _totalSamples, _totalSamples / (double)_sampleRate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止录音失败");
            CleanupFileResources();
        }
    }

    /// <summary>
    /// 添加音频数据到录音队列
    /// </summary>
    public void AddAudioData(float[] samples)
    {
        if (!IsRecording || samples == null || samples.Length == 0)
            return;

        _audioQueue.Enqueue(samples);
    }

    /// <summary>
    /// 录音循环（后台线程）
    /// </summary>
    private async Task RecordingLoop()
    {
        _logger.LogDebug("录音线程启动");

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                if (_audioQueue.TryDequeue(out var samples))
                {
                    await WriteSamplesAsync(samples);
                }
                else
                {
                    // 队列为空，短暂等待
                    await Task.Delay(10, _cts.Token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("录音线程被取消");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "录音线程异常");
        }

        _logger.LogDebug("录音线程结束");
    }

    /// <summary>
    /// 写入音频采样数据
    /// </summary>
    private Task WriteSamplesAsync(float[] samples)
    {
        if (_writer == null || !IsRecording)
            return Task.CompletedTask;

        try
        {
            // 将 float32 转换为 int16 (PCM)
            foreach (var sample in samples)
            {
                // 限幅到 [-1.0, 1.0]
                var clamped = Math.Clamp(sample, -1.0f, 1.0f);
                // 转换到 16-bit PCM
                var pcm = (short)(clamped * 32767.0f);
                _writer.Write(pcm);
            }

            _totalSamples += samples.Length;
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "写入音频数据失败");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 写入 WAV 文件头
    /// </summary>
    private void WriteWavHeader(BinaryWriter writer, long totalSamples)
    {
        int byteRate = _sampleRate * _channels * 2; // 16-bit = 2 bytes
        short blockAlign = (short)(_channels * 2);
        int dataSize = (int)(totalSamples * 2); // 16-bit = 2 bytes

        // RIFF header
        writer.Write(new[] { 'R', 'I', 'F', 'F' });
        writer.Write(36 + dataSize); // File size - 8
        writer.Write(new[] { 'W', 'A', 'V', 'E' });

        // fmt chunk
        writer.Write(new[] { 'f', 'm', 't', ' ' });
        writer.Write(16); // fmt chunk size
        writer.Write((short)1); // Audio format (1 = PCM)
        writer.Write((short)_channels);
        writer.Write(_sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write((short)16); // Bits per sample

        // data chunk
        writer.Write(new[] { 'd', 'a', 't', 'a' });
        writer.Write(dataSize);
    }

    /// <summary>
    /// 清理文件资源
    /// </summary>
    private void CleanupFileResources()
    {
        try
        {
            _writer?.Flush();
            _writer?.Dispose();
            _writer = null;

            _fileStream?.Close();
            _fileStream?.Dispose();
            _fileStream = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理文件资源失败");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _logger.LogDebug("释放 RoomAudioRecorder");

        if (IsRecording)
        {
            StopRecording();
        }

        _cts.Cancel();

        try
        {
            _recordingTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "等待录音线程结束超时");
        }

        _cts.Dispose();
        CleanupFileResources();

        _disposed = true;
    }
}
