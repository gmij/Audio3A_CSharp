namespace Audio3A.Web.Services;

/// <summary>
/// API 服务接口 - 统一 Mock 和 Real API 的接口
/// </summary>
public interface IApiService
{
    // 统计信息
    Task<StatsData> GetStats();
    
    // 房间管理
    Task<List<RoomInfo>> GetRooms();
    Task<RoomDetailInfo?> GetRoom(string roomId);
    Task<RoomData> CreateRoom(string name, int maxParticipants, bool enableAec, bool enableAgc, bool enableAns);
    Task<bool> DeleteRoom(string roomId);
    
    // 参与者管理
    Task<ParticipantData?> JoinRoom(string roomId, string name);
    Task<bool> LeaveRoom(string roomId, string participantId);
}

// 共享数据模型
public class StatsData
{
    public int TotalRooms { get; set; }
    public int ActiveRooms { get; set; }
    public int TotalParticipants { get; set; }
}

public class RoomInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public int ParticipantCount { get; set; }
    public int MaxParticipants { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RoomData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public int MaxParticipants { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool EnableAec { get; set; }
    public bool EnableAgc { get; set; }
    public bool EnableAns { get; set; }
}

public class RoomDetailInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public int ParticipantCount { get; set; }
    public int MaxParticipants { get; set; }
    public DateTime CreatedAt { get; set; }
    public string InviteCode { get; set; } = string.Empty;
    public bool EnableAec { get; set; }
    public bool EnableAgc { get; set; }
    public bool EnableAns { get; set; }
    public List<ParticipantInfo> Participants { get; set; } = new();
}

public class ParticipantInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public bool Enable3A { get; set; }
}

public class ParticipantData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; } = DateTime.Now;
    public bool Enable3A { get; set; }
}
