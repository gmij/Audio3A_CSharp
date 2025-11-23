using System.Collections.Concurrent;

namespace Audio3A.Web.Services;

/// <summary>
/// 模拟 API 服务（用于 GitHub Pages 演示）
/// 在客户端内存中模拟服务器端 API 的功能
/// </summary>
public class MockApiService
{
    private readonly ConcurrentDictionary<string, RoomData> _rooms = new();
    private readonly ConcurrentDictionary<string, ParticipantData> _participants = new();

    public class RoomData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string State { get; set; } = "Active";
        public int MaxParticipants { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool EnableAec { get; set; }
        public bool EnableAgc { get; set; }
        public bool EnableAns { get; set; }
        public string InviteCode { get; set; } = GenerateInviteCode();
        public List<string> ParticipantIds { get; set; } = new();

        private static string GenerateInviteCode()
        {
            var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new Random();
            return new string(Enumerable.Range(0, 6)
                .Select(_ => chars[random.Next(chars.Length)])
                .ToArray());
        }
    }

    public class ParticipantData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string RoomId { get; set; } = string.Empty;
        public string State { get; set; } = "Connected";
        public DateTime JoinedAt { get; set; } = DateTime.Now;
        public bool Enable3A { get; set; }
    }

    public class StatsData
    {
        public int TotalRooms { get; set; }
        public int ActiveRooms { get; set; }
        public int TotalParticipants { get; set; }
    }

    // 创建房间
    public Task<RoomData> CreateRoom(string name, int maxParticipants, bool enableAec, bool enableAgc, bool enableAns)
    {
        var room = new RoomData
        {
            Name = name,
            MaxParticipants = maxParticipants,
            EnableAec = enableAec,
            EnableAgc = enableAgc,
            EnableAns = enableAns
        };
        _rooms.TryAdd(room.Id, room);
        return Task.FromResult(room);
    }

    // 获取所有房间
    public Task<List<RoomInfo>> GetRooms()
    {
        var rooms = _rooms.Values.Select(r => new RoomInfo
        {
            Id = r.Id,
            Name = r.Name,
            State = r.State,
            ParticipantCount = r.ParticipantIds.Count,
            MaxParticipants = r.MaxParticipants,
            CreatedAt = r.CreatedAt
        }).ToList();
        return Task.FromResult(rooms);
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

    // 获取房间详情
    public Task<RoomDetailInfo?> GetRoom(string roomId)
    {
        if (!_rooms.TryGetValue(roomId, out var room))
            return Task.FromResult<RoomDetailInfo?>(null);

        var participants = room.ParticipantIds
            .Select(id => _participants.TryGetValue(id, out var p) ? p : null)
            .Where(p => p != null)
            .Select(p => new ParticipantInfo
            {
                Id = p!.Id,
                Name = p.Name,
                State = p.State,
                JoinedAt = p.JoinedAt,
                Enable3A = p.Enable3A
            })
            .ToList();

        var detail = new RoomDetailInfo
        {
            Id = room.Id,
            Name = room.Name,
            State = room.State,
            ParticipantCount = room.ParticipantIds.Count,
            MaxParticipants = room.MaxParticipants,
            CreatedAt = room.CreatedAt,
            InviteCode = room.InviteCode,
            EnableAec = room.EnableAec,
            EnableAgc = room.EnableAgc,
            EnableAns = room.EnableAns,
            Participants = participants
        };

        return Task.FromResult<RoomDetailInfo?>(detail);
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

    // 通过邀请码查找房间
    public Task<RoomDetailInfo?> GetRoomByInviteCode(string inviteCode)
    {
        var room = _rooms.Values.FirstOrDefault(r => r.InviteCode == inviteCode);
        if (room == null)
            return Task.FromResult<RoomDetailInfo?>(null);

        return GetRoom(room.Id);
    }

    // 删除房间
    public Task<bool> DeleteRoom(string roomId)
    {
        if (!_rooms.TryRemove(roomId, out var room))
            return Task.FromResult(false);

        // 删除所有参与者
        foreach (var participantId in room.ParticipantIds)
        {
            _participants.TryRemove(participantId, out _);
        }

        return Task.FromResult(true);
    }

    // 加入房间
    public Task<ParticipantData?> JoinRoom(string roomId, string name)
    {
        if (!_rooms.TryGetValue(roomId, out var room))
            return Task.FromResult<ParticipantData?>(null);

        if (room.MaxParticipants > 0 && room.ParticipantIds.Count >= room.MaxParticipants)
            return Task.FromResult<ParticipantData?>(null);

        var participant = new ParticipantData
        {
            Name = name,
            RoomId = roomId,
            Enable3A = room.EnableAec || room.EnableAgc || room.EnableAns
        };

        _participants.TryAdd(participant.Id, participant);
        room.ParticipantIds.Add(participant.Id);

        return Task.FromResult<ParticipantData?>(participant);
    }

    // 离开房间
    public Task<bool> LeaveRoom(string roomId, string participantId)
    {
        if (!_rooms.TryGetValue(roomId, out var room))
            return Task.FromResult(false);

        room.ParticipantIds.Remove(participantId);
        _participants.TryRemove(participantId, out _);

        return Task.FromResult(true);
    }

    // 获取统计信息
    public Task<StatsData> GetStats()
    {
        var stats = new StatsData
        {
            TotalRooms = _rooms.Count,
            ActiveRooms = _rooms.Count(r => r.Value.State == "Active"),
            TotalParticipants = _participants.Count
        };
        return Task.FromResult(stats);
    }
}
