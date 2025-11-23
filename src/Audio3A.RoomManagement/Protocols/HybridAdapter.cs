using Microsoft.Extensions.Logging;
using Audio3A.RoomManagement.Models;

namespace Audio3A.RoomManagement.Protocols;

/// <summary>
/// 混合传输适配器 - 结合 WebSocket 和 WebRTC
/// WebSocket 用于信令，WebRTC 用于音频传输
/// </summary>
public class HybridAdapter : ITransportAdapter
{
    private readonly ILogger<HybridAdapter> _logger;
    private readonly WebSocketAdapter _webSocketAdapter;
    private readonly WebRtcAdapter _webRtcAdapter;

    public TransportProtocol Protocol => TransportProtocol.Hybrid;

    public event EventHandler<AudioFrameReceivedEventArgs>? AudioFrameReceived;
    public event EventHandler<SignalingMessageReceivedEventArgs>? SignalingMessageReceived;
    public event EventHandler<ParticipantConnectedEventArgs>? ParticipantConnected;
    public event EventHandler<ParticipantDisconnectedEventArgs>? ParticipantDisconnected;

    public HybridAdapter(
        ILogger<HybridAdapter> logger,
        WebSocketAdapter webSocketAdapter,
        WebRtcAdapter webRtcAdapter)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _webSocketAdapter = webSocketAdapter ?? throw new ArgumentNullException(nameof(webSocketAdapter));
        _webRtcAdapter = webRtcAdapter ?? throw new ArgumentNullException(nameof(webRtcAdapter));

        // 订阅 WebSocket 事件（用于信令）
        _webSocketAdapter.SignalingMessageReceived += OnWebSocketSignalingReceived;
        _webSocketAdapter.ParticipantConnected += OnWebSocketParticipantConnected;
        _webSocketAdapter.ParticipantDisconnected += OnWebSocketParticipantDisconnected;

        // 订阅 WebRTC 事件（用于音频）
        _webRtcAdapter.AudioFrameReceived += OnWebRtcAudioReceived;
    }

    private void OnWebSocketSignalingReceived(object? sender, SignalingMessageReceivedEventArgs e)
    {
        // 转发信令消息
        SignalingMessageReceived?.Invoke(this, e);
    }

    private void OnWebSocketParticipantConnected(object? sender, ParticipantConnectedEventArgs e)
    {
        // 转发连接事件
        ParticipantConnected?.Invoke(this, e);
    }

    private void OnWebSocketParticipantDisconnected(object? sender, ParticipantDisconnectedEventArgs e)
    {
        // 转发断开事件
        ParticipantDisconnected?.Invoke(this, e);
    }

    private void OnWebRtcAudioReceived(object? sender, AudioFrameReceivedEventArgs e)
    {
        // 转发音频接收事件
        AudioFrameReceived?.Invoke(this, e);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Hybrid adapter (WebSocket + WebRTC)");
        
        await _webSocketAdapter.StartAsync(cancellationToken);
        await _webRtcAdapter.StartAsync(cancellationToken);

        _logger.LogInformation("Hybrid adapter started successfully");
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping Hybrid adapter");

        await _webSocketAdapter.StopAsync(cancellationToken);
        await _webRtcAdapter.StopAsync(cancellationToken);

        _logger.LogInformation("Hybrid adapter stopped successfully");
    }

    public Task SendAudioAsync(string participantId, AudioFrame frame, CancellationToken cancellationToken = default)
    {
        // 通过 WebRTC 发送音频（低延迟）
        return _webRtcAdapter.SendAudioAsync(participantId, frame, cancellationToken);
    }

    public Task BroadcastAudioAsync(string roomId, AudioFrame frame, string? excludeParticipantId = null, CancellationToken cancellationToken = default)
    {
        // 通过 WebRTC 广播音频
        return _webRtcAdapter.BroadcastAudioAsync(roomId, frame, excludeParticipantId, cancellationToken);
    }

    public Task SendSignalingAsync(string participantId, string message, CancellationToken cancellationToken = default)
    {
        // 通过 WebSocket 发送信令（可靠传输）
        return _webSocketAdapter.SendSignalingAsync(participantId, message, cancellationToken);
    }
}
