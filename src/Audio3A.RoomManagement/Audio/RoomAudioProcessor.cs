using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Audio3A.Core;
using Audio3A.Core.Extensions;
using Audio3A.RoomManagement.Models;

namespace Audio3A.RoomManagement.Audio;

/// <summary>
/// 房间音频处理服务 - 为参与者提供 3A 音频处理
/// </summary>
public class RoomAudioProcessor
{
    private readonly ILogger<RoomAudioProcessor> _logger;
    private readonly IServiceProvider _serviceProvider;

    public RoomAudioProcessor(ILogger<RoomAudioProcessor> logger, IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// 为参与者初始化 3A 处理器
    /// </summary>
    public Audio3AProcessor CreateProcessorForParticipant(Participant participant, Room room)
    {
        if (participant == null)
            throw new ArgumentNullException(nameof(participant));

        if (room == null)
            throw new ArgumentNullException(nameof(room));

        // 创建独立的 ServiceCollection 为每个参与者
        var services = new ServiceCollection();
        
        // 克隆房间的音频配置
        var config = new Audio3AConfig
        {
            EnableAec = room.AudioConfig.EnableAec,
            EnableAgc = room.AudioConfig.EnableAgc,
            EnableAns = room.AudioConfig.EnableAns,
            SampleRate = room.AudioConfig.SampleRate,
            Channels = room.AudioConfig.Channels,
            FrameSize = room.AudioConfig.FrameSize,
            AgcTargetLevel = room.AudioConfig.AgcTargetLevel,
            AgcCompressionRatio = room.AudioConfig.AgcCompressionRatio,
            AnsNoiseReductionDb = room.AudioConfig.AnsNoiseReductionDb,
            AecFilterLength = room.AudioConfig.AecFilterLength,
            AecStepSize = room.AudioConfig.AecStepSize,
            ProcessingOrder = room.AudioConfig.ProcessingOrder
        };

        // 注册 Audio3A 服务
        services.AddAudio3A(c =>
        {
            c.EnableAec = config.EnableAec;
            c.EnableAgc = config.EnableAgc;
            c.EnableAns = config.EnableAns;
            c.SampleRate = config.SampleRate;
            c.Channels = config.Channels;
            c.FrameSize = config.FrameSize;
            c.AgcTargetLevel = config.AgcTargetLevel;
            c.AgcCompressionRatio = config.AgcCompressionRatio;
            c.AnsNoiseReductionDb = config.AnsNoiseReductionDb;
            c.AecFilterLength = config.AecFilterLength;
            c.AecStepSize = config.AecStepSize;
            c.ProcessingOrder = config.ProcessingOrder;
        });
        
        // 添加日志记录
        var loggerFactory = _serviceProvider.GetService<ILoggerFactory>();
        if (loggerFactory != null)
        {
            services.AddLogging(builder =>
            {
                builder.Services.AddSingleton(loggerFactory);
            });
        }

        var serviceProvider = services.BuildServiceProvider();
        var processor = serviceProvider.GetRequiredService<Audio3AProcessor>();

        _logger.LogInformation(
            "Created 3A processor for participant {ParticipantId} in room {RoomId}",
            participant.Id, room.Id);

        return processor;
    }

    /// <summary>
    /// 处理参与者的音频帧（应用 3A）
    /// </summary>
    public short[] ProcessAudioFrame(Participant participant, short[] audioData, short[]? referenceData = null)
    {
        if (participant == null)
            throw new ArgumentNullException(nameof(participant));

        if (audioData == null || audioData.Length == 0)
            throw new ArgumentException("Audio data cannot be empty", nameof(audioData));

        // 如果没有启用 3A 或没有处理器，直接返回原始数据
        if (!participant.Enable3A || participant.Audio3AProcessor == null)
        {
            return audioData;
        }

        try
        {
            // 处理音频
            var processedData = participant.Audio3AProcessor.ProcessInt16(audioData, referenceData);
            
            _logger.LogTrace(
                "Processed audio frame for participant {ParticipantId}: {Length} samples",
                participant.Id, processedData.Length);

            return processedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error processing audio for participant {ParticipantId}", 
                participant.Id);
            
            // 出错时返回原始数据
            return audioData;
        }
    }

    /// <summary>
    /// 批量处理多个参与者的音频帧
    /// </summary>
    public Dictionary<string, short[]> ProcessMultipleFrames(
        IEnumerable<(Participant participant, short[] audioData, short[]? referenceData)> frames)
    {
        var results = new Dictionary<string, short[]>();

        foreach (var (participant, audioData, referenceData) in frames)
        {
            var processed = ProcessAudioFrame(participant, audioData, referenceData);
            results[participant.Id] = processed;
        }

        return results;
    }

    /// <summary>
    /// 重置参与者的 3A 处理器状态
    /// </summary>
    public void ResetProcessor(Participant participant)
    {
        if (participant?.Audio3AProcessor != null)
        {
            participant.Audio3AProcessor.Reset();
            _logger.LogDebug("Reset 3A processor for participant {ParticipantId}", participant.Id);
        }
    }
}
