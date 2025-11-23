using Microsoft.AspNetCore.Mvc;
using Audio3A.Core;
using Audio3A.RoomManagement;
using Audio3A.RoomManagement.Models;
using Audio3A.WebApi.Models;

namespace Audio3A.WebApi.Controllers;

/// <summary>
/// 房间管理 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly RoomManager _roomManager;
    private readonly ILogger<RoomsController> _logger;

    public RoomsController(RoomManager roomManager, ILogger<RoomsController> logger)
    {
        _roomManager = roomManager;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有房间
    /// </summary>
    [HttpGet]
    public ActionResult<IEnumerable<RoomResponse>> GetAllRooms()
    {
        var rooms = _roomManager.GetAllRooms();
        return Ok(rooms.Select(MapToResponse));
    }

    /// <summary>
    /// 获取指定房间
    /// </summary>
    [HttpGet("{roomId}")]
    public ActionResult<RoomResponse> GetRoom(string roomId)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null)
        {
            return NotFound(new { message = $"房间 {roomId} 不存在" });
        }

        return Ok(MapToResponse(room));
    }

    /// <summary>
    /// 创建房间
    /// </summary>
    [HttpPost]
    public ActionResult<RoomResponse> CreateRoom([FromBody] CreateRoomRequest request)
    {
        try
        {
            var audioConfig = new Audio3AConfig
            {
                EnableAec = request.EnableAec,
                EnableAgc = request.EnableAgc,
                EnableAns = request.EnableAns,
                SampleRate = 16000,
                Channels = 1,
                FrameSize = 160
            };

            var room = _roomManager.CreateRoom(
                roomId: null,
                roomName: request.Name,
                audioConfig: audioConfig,
                supportedProtocols: TransportProtocol.WebSocket
            );

            if (request.MaxParticipants > 0)
            {
                room.MaxParticipants = request.MaxParticipants;
            }

            _logger.LogInformation("创建房间成功: {RoomId} - {RoomName}", room.Id, room.Name);
            return CreatedAtAction(nameof(GetRoom), new { roomId = room.Id }, MapToResponse(room));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建房间失败");
            return BadRequest(new { message = "创建房间失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 删除房间
    /// </summary>
    [HttpDelete("{roomId}")]
    public ActionResult DeleteRoom(string roomId)
    {
        var success = _roomManager.RemoveRoom(roomId);
        if (!success)
        {
            return NotFound(new { message = $"房间 {roomId} 不存在" });
        }

        _logger.LogInformation("删除房间成功: {RoomId}", roomId);
        return Ok(new { message = "房间已删除" });
    }

    /// <summary>
    /// 加入房间
    /// </summary>
    [HttpPost("{roomId}/participants")]
    public ActionResult<ParticipantResponse> JoinRoom(string roomId, [FromBody] JoinRoomRequest request)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null)
        {
            return NotFound(new { message = $"房间 {roomId} 不存在" });
        }

        try
        {
            var participantId = Guid.NewGuid().ToString();
            var participant = new Participant(participantId, request.Name, roomId, TransportProtocol.WebSocket);

            var success = _roomManager.JoinRoom(roomId, participant);
            if (!success)
            {
                return BadRequest(new { message = "加入房间失败，房间可能已满" });
            }

            _logger.LogInformation("参与者 {ParticipantId} 加入房间 {RoomId}", participantId, roomId);
            return Ok(MapToParticipantResponse(participant));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加入房间失败");
            return BadRequest(new { message = "加入房间失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 离开房间
    /// </summary>
    [HttpDelete("{roomId}/participants/{participantId}")]
    public ActionResult LeaveRoom(string roomId, string participantId)
    {
        var success = _roomManager.LeaveRoom(roomId, participantId);
        if (!success)
        {
            return NotFound(new { message = "参与者或房间不存在" });
        }

        _logger.LogInformation("参与者 {ParticipantId} 离开房间 {RoomId}", participantId, roomId);
        return Ok(new { message = "已离开房间" });
    }

    /// <summary>
    /// 获取房间统计信息
    /// </summary>
    [HttpGet("stats")]
    public ActionResult<object> GetStats()
    {
        return Ok(new
        {
            totalRooms = _roomManager.GetAllRooms().Count,
            activeRooms = _roomManager.ActiveRoomCount,
            totalParticipants = _roomManager.TotalParticipantCount
        });
    }

    /// <summary>
    /// 开始房间录音
    /// </summary>
    [HttpPost("{roomId}/recording/start")]
    public ActionResult StartRecording(string roomId)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null)
        {
            return NotFound(new { message = $"房间 {roomId} 不存在" });
        }

        if (room.IsRecording)
        {
            return BadRequest(new { message = "房间已在录音中" });
        }

        room.StartRecording();
        _logger.LogInformation("房间 {RoomId} 开始录音", roomId);
        return Ok(new { message = "录音已开始" });
    }

    /// <summary>
    /// 停止房间录音
    /// </summary>
    [HttpPost("{roomId}/recording/stop")]
    public ActionResult<object> StopRecording(string roomId)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null)
        {
            return NotFound(new { message = $"房间 {roomId} 不存在" });
        }

        if (!room.IsRecording)
        {
            return BadRequest(new { message = "房间未在录音" });
        }

        var filePath = room.StopRecording();
        _logger.LogInformation("房间 {RoomId} 停止录音，文件: {FilePath}", roomId, filePath);
        return Ok(new { message = "录音已停止", filePath });
    }

    /// <summary>
    /// 获取房间录音状态
    /// </summary>
    [HttpGet("{roomId}/recording/status")]
    public ActionResult<object> GetRecordingStatus(string roomId)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null)
        {
            return NotFound(new { message = $"房间 {roomId} 不存在" });
        }

        return Ok(new { 
            isRecording = room.IsRecording,
            lastRecordingFile = room.GetLastRecordingFile()
        });
    }

    /// <summary>
    /// 下载房间录音文件
    /// </summary>
    [HttpGet("recordings/{fileName}")]
    public ActionResult DownloadRecording(string fileName)
    {
        try
        {
            var filePath = Path.Combine("recordings", fileName);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { message = "录音文件不存在" });
            }

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "audio/wav", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载录音文件失败: {FileName}", fileName);
            return BadRequest(new { message = "下载失败", error = ex.Message });
        }
    }

    private static RoomResponse MapToResponse(Room room)
    {
        return new RoomResponse
        {
            Id = room.Id,
            Name = room.Name,
            State = room.State.ToString(),
            ParticipantCount = room.ParticipantCount,
            MaxParticipants = room.MaxParticipants,
            CreatedAt = room.CreatedAt,
            Participants = room.Participants.Select(MapToParticipantResponse).ToList()
        };
    }

    private static ParticipantResponse MapToParticipantResponse(Participant participant)
    {
        return new ParticipantResponse
        {
            Id = participant.Id,
            Name = participant.Name,
            State = participant.State.ToString(),
            JoinedAt = participant.JoinedAt,
            Enable3A = participant.Enable3A
        };
    }
}
