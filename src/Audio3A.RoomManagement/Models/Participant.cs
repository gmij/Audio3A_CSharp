using Audio3A.Core;

namespace Audio3A.RoomManagement.Models;

/// <summary>
/// 房间参与者
/// </summary>
public class Participant
{
    /// <summary>
    /// 参与者唯一标识符
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// 参与者显示名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 所在房间 ID
    /// </summary>
    public string RoomId { get; }

    /// <summary>
    /// 当前状态
    /// </summary>
    public ParticipantState State { get; set; }

    /// <summary>
    /// 使用的传输协议
    /// </summary>
    public TransportProtocol Protocol { get; }

    /// <summary>
    /// 加入时间
    /// </summary>
    public DateTime JoinedAt { get; }

    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTime LastActivityAt { get; set; }

    /// <summary>
    /// 是否启用 3A 处理
    /// </summary>
    public bool Enable3A { get; set; }

    /// <summary>
    /// 3A 处理器（如果启用）
    /// </summary>
    public Audio3AProcessor? Audio3AProcessor { get; set; }

    /// <summary>
    /// 用户自定义数据
    /// </summary>
    public Dictionary<string, object> Metadata { get; }

    public Participant(string id, string name, string roomId, TransportProtocol protocol)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Participant ID cannot be empty", nameof(id));
        
        if (string.IsNullOrWhiteSpace(roomId))
            throw new ArgumentException("Room ID cannot be empty", nameof(roomId));

        Id = id;
        Name = name ?? id;
        RoomId = roomId;
        Protocol = protocol;
        State = ParticipantState.Connecting;
        JoinedAt = DateTime.UtcNow;
        LastActivityAt = DateTime.UtcNow;
        Enable3A = true;
        Metadata = new Dictionary<string, object>();
    }

    /// <summary>
    /// 更新最后活动时间
    /// </summary>
    public void UpdateActivity()
    {
        LastActivityAt = DateTime.UtcNow;
    }
}
