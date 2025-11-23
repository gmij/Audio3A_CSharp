using System.Collections.Concurrent;
using Audio3A.Core;
using Audio3A.RoomManagement.Audio;

namespace Audio3A.RoomManagement.Models;

/// <summary>
/// 语音通话房间
/// </summary>
public class Room
{
    /// <summary>
    /// 房间唯一标识符
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// 房间名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 房间状态
    /// </summary>
    public RoomState State { get; set; }

    /// <summary>
    /// 房间最大参与者数量（0 表示无限制）
    /// </summary>
    public int MaxParticipants { get; set; }

    /// <summary>
    /// 支持的传输协议
    /// </summary>
    public TransportProtocol SupportedProtocols { get; set; }

    /// <summary>
    /// 房间创建时间
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// 房间参与者集合（线程安全）
    /// </summary>
    private readonly ConcurrentDictionary<string, Participant> _participants;

    /// <summary>
    /// 房间音频配置
    /// </summary>
    public Audio3AConfig AudioConfig { get; }

    /// <summary>
    /// 房间元数据
    /// </summary>
    public Dictionary<string, object> Metadata { get; }

    /// <summary>
    /// 房间音频录制器
    /// </summary>
    private RoomAudioRecorder? _recorder;

    /// <summary>
    /// 参与者音频缓冲区（用于混音）
    /// </summary>
    private readonly ConcurrentDictionary<string, ConcurrentQueue<float[]>> _audioBuffers;

    public Room(string id, string name, Audio3AConfig audioConfig, TransportProtocol supportedProtocols = TransportProtocol.Hybrid)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Room ID cannot be empty", nameof(id));

        Id = id;
        Name = name ?? id;
        State = RoomState.Active;
        MaxParticipants = 0; // 默认无限制
        SupportedProtocols = supportedProtocols;
        CreatedAt = DateTime.UtcNow;
        _participants = new ConcurrentDictionary<string, Participant>();
        AudioConfig = audioConfig ?? throw new ArgumentNullException(nameof(audioConfig));
        Metadata = new Dictionary<string, object>();
        _audioBuffers = new ConcurrentDictionary<string, ConcurrentQueue<float[]>>();
    }

    /// <summary>
    /// 获取所有参与者
    /// </summary>
    public IReadOnlyCollection<Participant> Participants => _participants.Values.ToList().AsReadOnly();

    /// <summary>
    /// 获取参与者数量
    /// </summary>
    public int ParticipantCount => _participants.Count;

    /// <summary>
    /// 添加参与者
    /// </summary>
    public bool TryAddParticipant(Participant participant)
    {
        if (participant == null)
            throw new ArgumentNullException(nameof(participant));

        if (State != RoomState.Active)
            return false;

        if (MaxParticipants > 0 && _participants.Count >= MaxParticipants)
            return false;

        return _participants.TryAdd(participant.Id, participant);
    }

    /// <summary>
    /// 移除参与者
    /// </summary>
    public bool TryRemoveParticipant(string participantId)
    {
        return _participants.TryRemove(participantId, out _);
    }

    /// <summary>
    /// 获取参与者
    /// </summary>
    public Participant? GetParticipant(string participantId)
    {
        _participants.TryGetValue(participantId, out var participant);
        return participant;
    }

    /// <summary>
    /// 检查房间是否为空
    /// </summary>
    public bool IsEmpty => _participants.IsEmpty;

    /// <summary>
    /// 添加参与者音频数据（处理后的音频）
    /// </summary>
    public void AddParticipantAudio(string participantId, float[] audioData)
    {
        if (!_audioBuffers.ContainsKey(participantId))
        {
            _audioBuffers.TryAdd(participantId, new ConcurrentQueue<float[]>());
        }

        if (_audioBuffers.TryGetValue(participantId, out var buffer))
        {
            buffer.Enqueue(audioData);

            // 限制队列大小（防止内存泄漏）
            while (buffer.Count > 100) // 保留最近 100 帧
            {
                buffer.TryDequeue(out _);
            }
        }

        // 如果正在录音，添加到录音器
        if (_recorder?.IsRecording == true)
        {
            _recorder.AddAudioData(audioData);
            
            // 添加日志以便调试
            System.Diagnostics.Debug.WriteLine(
                $"[Room {Id}] 添加音频到录音器: ParticipantId={participantId}, Samples={audioData.Length}");
        }
    }

    /// <summary>
    /// 开始房间录音
    /// </summary>
    public void StartRecording(string outputDirectory = "recordings")
    {
        if (_recorder?.IsRecording == true)
        {
            return;
        }

        _recorder?.Dispose();
        _recorder = new RoomAudioRecorder(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<RoomAudioRecorder>.Instance,
            Id,
            outputDirectory,
            AudioConfig.SampleRate,
            AudioConfig.Channels);

        _recorder.StartRecording();
    }

    /// <summary>
    /// 停止房间录音
    /// </summary>
    public string? StopRecording()
    {
        if (_recorder?.IsRecording == true)
        {
            _recorder.StopRecording();
            var filePath = _recorder.OutputFilePath;
            
            // 保存录音文件路径到元数据，便于后续查询
            if (!string.IsNullOrEmpty(filePath))
            {
                if (!Metadata.ContainsKey("LastRecordingFile"))
                {
                    Metadata["LastRecordingFile"] = filePath;
                }
                else
                {
                    Metadata["LastRecordingFile"] = filePath;
                }
            }
            
            return filePath;
        }
        return null;
    }

    /// <summary>
    /// 获取最后一次录音文件路径
    /// </summary>
    public string? GetLastRecordingFile()
    {
        if (Metadata.TryGetValue("LastRecordingFile", out var value) && value is string filePath)
        {
            return filePath;
        }
        return null;
    }

    /// <summary>
    /// 是否正在录音
    /// </summary>
    public bool IsRecording => _recorder?.IsRecording == true;

    /// <summary>
    /// 关闭房间
    /// </summary>
    public void Close()
    {
        State = RoomState.Closed;

        // 停止录音
        if (_recorder?.IsRecording == true)
        {
            _recorder.StopRecording();
        }
        _recorder?.Dispose();
    }
}
