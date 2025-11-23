using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Audio3A.RoomManagement.Models;
using Audio3A.Core;

namespace Audio3A.RoomManagement;

/// <summary>
/// 房间管理器 - 管理所有语音通话房间
/// </summary>
public class RoomManager
{
    private readonly ILogger<RoomManager> _logger;
    private readonly ConcurrentDictionary<string, Room> _rooms;
    private readonly IServiceProvider _serviceProvider;

    public RoomManager(ILogger<RoomManager> logger, IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _rooms = new ConcurrentDictionary<string, Room>();

        _logger.LogInformation("RoomManager initialized");
    }

    /// <summary>
    /// 创建新房间
    /// </summary>
    /// <param name="roomId">房间 ID（如果为空，自动生成）</param>
    /// <param name="roomName">房间名称</param>
    /// <param name="audioConfig">音频配置</param>
    /// <param name="supportedProtocols">支持的传输协议</param>
    /// <returns>创建的房间</returns>
    public Room CreateRoom(
        string? roomId = null, 
        string? roomName = null, 
        Audio3AConfig? audioConfig = null,
        TransportProtocol supportedProtocols = TransportProtocol.Hybrid)
    {
        roomId ??= Guid.NewGuid().ToString();
        roomName ??= $"Room_{roomId.Substring(0, Math.Min(roomId.Length, 8))}";

        // 使用默认音频配置或提供的配置
        var config = audioConfig ?? new Audio3AConfig
        {
            EnableAec = true,
            EnableAgc = true,
            EnableAns = true,
            SampleRate = 16000,
            Channels = 1,
            FrameSize = 160
        };

        var room = new Room(roomId, roomName, config, supportedProtocols);

        if (!_rooms.TryAdd(roomId, room))
        {
            _logger.LogError("Failed to create room {RoomId}: already exists", roomId);
            throw new InvalidOperationException($"Room with ID {roomId} already exists");
        }

        _logger.LogInformation(
            "Room created: Id={RoomId}, Name={RoomName}, Protocols={Protocols}", 
            roomId, roomName, supportedProtocols);

        return room;
    }

    /// <summary>
    /// 获取房间
    /// </summary>
    public Room? GetRoom(string roomId)
    {
        _rooms.TryGetValue(roomId, out var room);
        return room;
    }

    /// <summary>
    /// 删除房间
    /// </summary>
    public bool RemoveRoom(string roomId)
    {
        if (_rooms.TryRemove(roomId, out var room))
        {
            room.Close();
            _logger.LogInformation("Room removed: Id={RoomId}", roomId);
            return true;
        }

        _logger.LogWarning("Failed to remove room {RoomId}: not found", roomId);
        return false;
    }

    /// <summary>
    /// 获取所有房间
    /// </summary>
    public IReadOnlyCollection<Room> GetAllRooms()
    {
        return _rooms.Values.ToList().AsReadOnly();
    }

    /// <summary>
    /// 获取活跃房间数量
    /// </summary>
    public int ActiveRoomCount => _rooms.Count(r => r.Value.State == RoomState.Active);

    /// <summary>
    /// 参与者加入房间
    /// </summary>
    public bool JoinRoom(string roomId, Participant participant)
    {
        var room = GetRoom(roomId);
        if (room == null)
        {
            _logger.LogWarning("Cannot join room {RoomId}: room not found", roomId);
            return false;
        }

        if (!room.TryAddParticipant(participant))
        {
            _logger.LogWarning(
                "Failed to add participant {ParticipantId} to room {RoomId}", 
                participant.Id, roomId);
            return false;
        }

        participant.State = ParticipantState.Connected;

        _logger.LogInformation(
            "Participant joined: ParticipantId={ParticipantId}, RoomId={RoomId}, Protocol={Protocol}",
            participant.Id, roomId, participant.Protocol);

        return true;
    }

    /// <summary>
    /// 参与者离开房间
    /// </summary>
    public bool LeaveRoom(string roomId, string participantId)
    {
        var room = GetRoom(roomId);
        if (room == null)
        {
            _logger.LogWarning("Cannot leave room {RoomId}: room not found", roomId);
            return false;
        }

        var participant = room.GetParticipant(participantId);
        if (participant != null)
        {
            participant.State = ParticipantState.Disconnecting;
            
            // 清理 3A 处理器
            participant.Audio3AProcessor?.Dispose();
            participant.Audio3AProcessor = null;
        }

        if (!room.TryRemoveParticipant(participantId))
        {
            _logger.LogWarning(
                "Failed to remove participant {ParticipantId} from room {RoomId}",
                participantId, roomId);
            return false;
        }

        _logger.LogInformation(
            "Participant left: ParticipantId={ParticipantId}, RoomId={RoomId}",
            participantId, roomId);

        // 如果房间为空，可以选择自动清理
        if (room.IsEmpty)
        {
            _logger.LogInformation("Room {RoomId} is now empty", roomId);
        }

        return true;
    }

    /// <summary>
    /// 清理空房间
    /// </summary>
    public int CleanupEmptyRooms()
    {
        var emptyRooms = _rooms.Values
            .Where(r => r.IsEmpty && r.State == RoomState.Active)
            .Select(r => r.Id)
            .ToList();

        int cleanedCount = 0;
        foreach (var roomId in emptyRooms)
        {
            if (RemoveRoom(roomId))
            {
                cleanedCount++;
            }
        }

        if (cleanedCount > 0)
        {
            _logger.LogInformation("Cleaned up {Count} empty rooms", cleanedCount);
        }

        return cleanedCount;
    }

    /// <summary>
    /// 获取总参与者数量
    /// </summary>
    public int TotalParticipantCount => _rooms.Values.Sum(r => r.ParticipantCount);
}
