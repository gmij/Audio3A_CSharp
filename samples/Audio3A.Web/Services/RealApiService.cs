using System.Net.Http.Json;
using System.Text.Json;

namespace Audio3A.Web.Services;

/// <summary>
/// 真实 API 服务（连接到服务端 WebAPI）
/// </summary>
public class RealApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RealApiService>? _logger;

    public RealApiService(HttpClient httpClient, ILogger<RealApiService>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    // DTO 类 - 与 API 响应匹配
    private class RoomResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public int ParticipantCount { get; set; }
        public int MaxParticipants { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ParticipantResponse> Participants { get; set; } = new();
    }

    private class ParticipantResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
        public bool Enable3A { get; set; }
    }

    private class StatsResponse
    {
        public int TotalRooms { get; set; }
        public int ActiveRooms { get; set; }
        public int TotalParticipants { get; set; }
    }

    // 请求 DTO
    private class CreateRoomRequest
    {
        public string Name { get; set; } = string.Empty;
        public int MaxParticipants { get; set; }
        public bool EnableAec { get; set; }
        public bool EnableAgc { get; set; }
        public bool EnableAns { get; set; }
    }

    private class JoinRoomRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    // API 方法实现
    public async Task<StatsData> GetStats()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<StatsResponse>("/api/rooms/stats");
            if (response == null)
                return new StatsData();
            
            return new StatsData
            {
                TotalRooms = response.TotalRooms,
                ActiveRooms = response.ActiveRooms,
                TotalParticipants = response.TotalParticipants
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "获取统计信息失败");
            throw;
        }
    }

    public async Task<List<RoomInfo>> GetRooms()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<RoomResponse>>("/api/rooms");
            if (response == null)
                return new List<RoomInfo>();

            return response.Select(r => new RoomInfo
            {
                Id = r.Id,
                Name = r.Name,
                State = r.State,
                ParticipantCount = r.ParticipantCount,
                MaxParticipants = r.MaxParticipants,
                CreatedAt = r.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "获取房间列表失败");
            throw;
        }
    }

    public async Task<RoomDetailInfo?> GetRoom(string roomId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<RoomResponse>($"/api/rooms/{roomId}");
            if (response == null)
                return null;

            // WebAPI 不返回 InviteCode，生成一个临时的
            // 注意：这是一个临时解决方案，生产环境应在服务端实现 InviteCode 功能
            // 或者考虑移除此功能以保持与 API 的一致性
            var inviteCode = roomId.Length >= 6 ? roomId.Substring(0, 6).ToUpper() : roomId.ToUpper();

            return new RoomDetailInfo
            {
                Id = response.Id,
                Name = response.Name,
                State = response.State,
                ParticipantCount = response.ParticipantCount,
                MaxParticipants = response.MaxParticipants,
                CreatedAt = response.CreatedAt,
                InviteCode = inviteCode,
                EnableAec = response.Participants.Any(p => p.Enable3A), // 从参与者推断
                EnableAgc = response.Participants.Any(p => p.Enable3A),
                EnableAns = response.Participants.Any(p => p.Enable3A),
                Participants = response.Participants.Select(p => new ParticipantInfo
                {
                    Id = p.Id,
                    Name = p.Name,
                    State = p.State,
                    JoinedAt = p.JoinedAt,
                    Enable3A = p.Enable3A
                }).ToList()
            };
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "获取房间详情失败: {RoomId}", roomId);
            throw;
        }
    }

    public async Task<RoomData> CreateRoom(string name, int maxParticipants, bool enableAec, bool enableAgc, bool enableAns)
    {
        try
        {
            var request = new CreateRoomRequest
            {
                Name = name,
                MaxParticipants = maxParticipants,
                EnableAec = enableAec,
                EnableAgc = enableAgc,
                EnableAns = enableAns
            };

            var response = await _httpClient.PostAsJsonAsync("/api/rooms", request);
            response.EnsureSuccessStatusCode();

            var room = await response.Content.ReadFromJsonAsync<RoomResponse>();
            if (room == null)
                throw new InvalidOperationException("创建房间返回空响应");

            return new RoomData
            {
                Id = room.Id,
                Name = room.Name,
                State = room.State,
                MaxParticipants = room.MaxParticipants,
                CreatedAt = room.CreatedAt,
                EnableAec = enableAec,
                EnableAgc = enableAgc,
                EnableAns = enableAns
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "创建房间失败");
            throw;
        }
    }

    public async Task<bool> DeleteRoom(string roomId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/rooms/{roomId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "删除房间失败: {RoomId}", roomId);
            return false;
        }
    }

    public async Task<ParticipantData?> JoinRoom(string roomId, string name)
    {
        try
        {
            var request = new JoinRoomRequest { Name = name };
            var response = await _httpClient.PostAsJsonAsync($"/api/rooms/{roomId}/participants", request);
            response.EnsureSuccessStatusCode();

            var participant = await response.Content.ReadFromJsonAsync<ParticipantResponse>();
            if (participant == null)
                return null;

            return new ParticipantData
            {
                Id = participant.Id,
                Name = participant.Name,
                RoomId = roomId,
                State = participant.State,
                JoinedAt = participant.JoinedAt,
                Enable3A = participant.Enable3A
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "加入房间失败: {RoomId}", roomId);
            throw;
        }
    }

    public async Task<bool> LeaveRoom(string roomId, string participantId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/rooms/{roomId}/participants/{participantId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "离开房间失败: {RoomId}, {ParticipantId}", roomId, participantId);
            return false;
        }
    }
}
