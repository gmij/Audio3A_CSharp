using Audio3A.RoomManagement.Models;

namespace Audio3A.RoomManagement.Protocols;

/// <summary>
/// 传输协议适配器接口
/// </summary>
public interface ITransportAdapter
{
    /// <summary>
    /// 协议类型
    /// </summary>
    TransportProtocol Protocol { get; }

    /// <summary>
    /// 发送音频帧到指定参与者
    /// </summary>
    Task SendAudioAsync(string participantId, AudioFrame frame, CancellationToken cancellationToken = default);

    /// <summary>
    /// 广播音频帧到房间所有参与者（可排除特定参与者）
    /// </summary>
    Task BroadcastAudioAsync(string roomId, AudioFrame frame, string? excludeParticipantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送信令消息
    /// </summary>
    Task SendSignalingAsync(string participantId, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// 接收音频帧事件
    /// </summary>
    event EventHandler<AudioFrameReceivedEventArgs>? AudioFrameReceived;

    /// <summary>
    /// 接收信令消息事件
    /// </summary>
    event EventHandler<SignalingMessageReceivedEventArgs>? SignalingMessageReceived;

    /// <summary>
    /// 参与者连接事件
    /// </summary>
    event EventHandler<ParticipantConnectedEventArgs>? ParticipantConnected;

    /// <summary>
    /// 参与者断开事件
    /// </summary>
    event EventHandler<ParticipantDisconnectedEventArgs>? ParticipantDisconnected;

    /// <summary>
    /// 启动适配器
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止适配器
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 音频帧接收事件参数
/// </summary>
public class AudioFrameReceivedEventArgs : EventArgs
{
    public string ParticipantId { get; }
    public AudioFrame Frame { get; }

    public AudioFrameReceivedEventArgs(string participantId, AudioFrame frame)
    {
        ParticipantId = participantId;
        Frame = frame;
    }
}

/// <summary>
/// 信令消息接收事件参数
/// </summary>
public class SignalingMessageReceivedEventArgs : EventArgs
{
    public string ParticipantId { get; }
    public string Message { get; }

    public SignalingMessageReceivedEventArgs(string participantId, string message)
    {
        ParticipantId = participantId;
        Message = message;
    }
}

/// <summary>
/// 参与者连接事件参数
/// </summary>
public class ParticipantConnectedEventArgs : EventArgs
{
    public string ParticipantId { get; }
    public string RoomId { get; }

    public ParticipantConnectedEventArgs(string participantId, string roomId)
    {
        ParticipantId = participantId;
        RoomId = roomId;
    }
}

/// <summary>
/// 参与者断开事件参数
/// </summary>
public class ParticipantDisconnectedEventArgs : EventArgs
{
    public string ParticipantId { get; }
    public string RoomId { get; }

    public ParticipantDisconnectedEventArgs(string participantId, string roomId)
    {
        ParticipantId = participantId;
        RoomId = roomId;
    }
}
