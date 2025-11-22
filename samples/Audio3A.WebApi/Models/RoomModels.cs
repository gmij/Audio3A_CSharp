namespace Audio3A.WebApi.Models;

/// <summary>
/// 创建房间请求
/// </summary>
public class CreateRoomRequest
{
    /// <summary>
    /// 房间名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 最大参与者数量（0 表示无限制）
    /// </summary>
    public int MaxParticipants { get; set; } = 0;

    /// <summary>
    /// 启用回声消除
    /// </summary>
    public bool EnableAec { get; set; } = true;

    /// <summary>
    /// 启用自动增益控制
    /// </summary>
    public bool EnableAgc { get; set; } = true;

    /// <summary>
    /// 启用噪声抑制
    /// </summary>
    public bool EnableAns { get; set; } = true;
}

/// <summary>
/// 加入房间请求
/// </summary>
public class JoinRoomRequest
{
    /// <summary>
    /// 参与者名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// 房间响应
/// </summary>
public class RoomResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public int ParticipantCount { get; set; }
    public int MaxParticipants { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ParticipantResponse> Participants { get; set; } = new();
}

/// <summary>
/// 参与者响应
/// </summary>
public class ParticipantResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public bool Enable3A { get; set; }
}
