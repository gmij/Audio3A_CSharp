using System.Collections.Concurrent;
using Audio3A.Core;

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
    /// 关闭房间
    /// </summary>
    public void Close()
    {
        State = RoomState.Closed;
    }
}
