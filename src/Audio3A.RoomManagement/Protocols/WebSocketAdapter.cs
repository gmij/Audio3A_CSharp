using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Audio3A.RoomManagement.Models;

namespace Audio3A.RoomManagement.Protocols;

/// <summary>
/// WebSocket 传输适配器
/// </summary>
public class WebSocketAdapter : ITransportAdapter
{
    private readonly ILogger<WebSocketAdapter> _logger;
    private readonly ConcurrentDictionary<string, WebSocket> _connections;
    private readonly ConcurrentDictionary<string, string> _participantRooms; // participantId -> roomId

    public TransportProtocol Protocol => TransportProtocol.WebSocket;

    public event EventHandler<AudioFrameReceivedEventArgs>? AudioFrameReceived;
    public event EventHandler<SignalingMessageReceivedEventArgs>? SignalingMessageReceived;
    public event EventHandler<ParticipantConnectedEventArgs>? ParticipantConnected;
    public event EventHandler<ParticipantDisconnectedEventArgs>? ParticipantDisconnected;

    public WebSocketAdapter(ILogger<WebSocketAdapter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connections = new ConcurrentDictionary<string, WebSocket>();
        _participantRooms = new ConcurrentDictionary<string, string>();
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("WebSocket adapter started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping WebSocket adapter");

        // 关闭所有连接
        foreach (var connection in _connections.Values)
        {
            if (connection.State == WebSocketState.Open)
            {
                connection.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", cancellationToken)
                    .Wait(cancellationToken);
            }
        }

        _connections.Clear();
        _participantRooms.Clear();

        return Task.CompletedTask;
    }

    /// <summary>
    /// 注册 WebSocket 连接
    /// </summary>
    public async Task RegisterConnectionAsync(string participantId, string roomId, WebSocket webSocket, CancellationToken cancellationToken = default)
    {
        if (!_connections.TryAdd(participantId, webSocket))
        {
            _logger.LogWarning("Participant {ParticipantId} already has a connection", participantId);
            return;
        }

        _participantRooms[participantId] = roomId;

        _logger.LogInformation("WebSocket registered: ParticipantId={ParticipantId}, RoomId={RoomId}", 
            participantId, roomId);

        // 触发连接事件
        ParticipantConnected?.Invoke(this, new ParticipantConnectedEventArgs(participantId, roomId));

        // 开始接收消息
        await ReceiveMessagesAsync(participantId, webSocket, cancellationToken);
    }

    private async Task ReceiveMessagesAsync(string participantId, WebSocket webSocket, CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 64]; // 64KB buffer

        try
        {
            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await HandleDisconnectAsync(participantId, webSocket, cancellationToken);
                    break;
                }

                await ProcessMessageAsync(participantId, buffer, result, cancellationToken);
            }
        }
        catch (WebSocketException ex)
        {
            _logger.LogWarning(ex, "WebSocket error for participant {ParticipantId}", participantId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Receive operation cancelled for participant {ParticipantId}", participantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving messages for participant {ParticipantId}", participantId);
        }
        finally
        {
            await HandleDisconnectAsync(participantId, webSocket, cancellationToken);
        }
    }

    private async Task ProcessMessageAsync(string participantId, byte[] buffer, WebSocketReceiveResult result, CancellationToken cancellationToken)
    {
        if (result.MessageType == WebSocketMessageType.Binary)
        {
            // 处理音频数据
            await ProcessAudioDataAsync(participantId, buffer, result.Count, cancellationToken);
        }
        else if (result.MessageType == WebSocketMessageType.Text)
        {
            // 处理信令消息
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            ProcessSignalingMessage(participantId, message);
        }
    }

    private Task ProcessAudioDataAsync(string participantId, byte[] buffer, int count, CancellationToken cancellationToken)
    {
        try
        {
            // 将字节数组转换为 short 数组（16位 PCM）
            var audioData = new short[count / 2];
            Buffer.BlockCopy(buffer, 0, audioData, 0, count);

            // 获取房间 ID
            if (!_participantRooms.TryGetValue(participantId, out var roomId))
            {
                _logger.LogWarning("No room found for participant {ParticipantId}", participantId);
                return Task.CompletedTask;
            }

            // 创建音频帧
            var frame = new AudioFrame(
                participantId,
                audioData,
                16000, // 默认采样率
                1,     // 默认单声道
                DateTime.UtcNow.Ticks
            );

            // 触发音频接收事件
            AudioFrameReceived?.Invoke(this, new AudioFrameReceivedEventArgs(participantId, frame));

            _logger.LogTrace("Received audio frame from {ParticipantId}: {Length} samples", 
                participantId, audioData.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing audio data from {ParticipantId}", participantId);
        }

        return Task.CompletedTask;
    }

    private void ProcessSignalingMessage(string participantId, string message)
    {
        try
        {
            _logger.LogDebug("Received signaling message from {ParticipantId}: {Message}", 
                participantId, message);

            // 触发信令消息接收事件
            SignalingMessageReceived?.Invoke(this, 
                new SignalingMessageReceivedEventArgs(participantId, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing signaling message from {ParticipantId}", participantId);
        }
    }

    public async Task SendAudioAsync(string participantId, AudioFrame frame, CancellationToken cancellationToken = default)
    {
        if (!_connections.TryGetValue(participantId, out var webSocket))
        {
            _logger.LogWarning("No connection found for participant {ParticipantId}", participantId);
            return;
        }

        if (webSocket.State != WebSocketState.Open)
        {
            _logger.LogWarning("WebSocket not open for participant {ParticipantId}", participantId);
            return;
        }

        try
        {
            // 将 short 数组转换为字节数组
            var buffer = new byte[frame.Data.Length * 2];
            Buffer.BlockCopy(frame.Data, 0, buffer, 0, buffer.Length);

            await webSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Binary,
                true,
                cancellationToken);

            _logger.LogTrace("Sent audio frame to {ParticipantId}: {Length} samples", 
                participantId, frame.Data.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending audio to participant {ParticipantId}", participantId);
        }
    }

    public async Task BroadcastAudioAsync(string roomId, AudioFrame frame, string? excludeParticipantId = null, CancellationToken cancellationToken = default)
    {
        var participantsInRoom = _participantRooms
            .Where(p => p.Value == roomId && p.Key != excludeParticipantId)
            .Select(p => p.Key)
            .ToList();

        var tasks = participantsInRoom.Select(participantId => 
            SendAudioAsync(participantId, frame, cancellationToken));

        await Task.WhenAll(tasks);

        _logger.LogTrace("Broadcasted audio to {Count} participants in room {RoomId}", 
            participantsInRoom.Count, roomId);
    }

    public async Task SendSignalingAsync(string participantId, string message, CancellationToken cancellationToken = default)
    {
        if (!_connections.TryGetValue(participantId, out var webSocket))
        {
            _logger.LogWarning("No connection found for participant {ParticipantId}", participantId);
            return;
        }

        if (webSocket.State != WebSocketState.Open)
        {
            _logger.LogWarning("WebSocket not open for participant {ParticipantId}", participantId);
            return;
        }

        try
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                cancellationToken);

            _logger.LogDebug("Sent signaling message to {ParticipantId}", participantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending signaling message to {ParticipantId}", participantId);
        }
    }

    private async Task HandleDisconnectAsync(string participantId, WebSocket webSocket, CancellationToken cancellationToken)
    {
        _connections.TryRemove(participantId, out _);
        
        if (_participantRooms.TryRemove(participantId, out var roomId))
        {
            _logger.LogInformation("WebSocket disconnected: ParticipantId={ParticipantId}, RoomId={RoomId}", 
                participantId, roomId);

            // 触发断开事件
            ParticipantDisconnected?.Invoke(this, 
                new ParticipantDisconnectedEventArgs(participantId, roomId));
        }

        if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
        {
            try
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Connection closed",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing WebSocket for participant {ParticipantId}", participantId);
            }
        }

        webSocket.Dispose();
    }
}
