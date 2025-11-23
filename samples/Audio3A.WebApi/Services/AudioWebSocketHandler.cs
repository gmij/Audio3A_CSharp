using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Audio3A.Core;
using Audio3A.RoomManagement;
using Microsoft.Extensions.Logging;

namespace Audio3A.WebApi.Services;

/// <summary>
/// WebSocket 音频处理器
/// 负责接收客户端音频流，应用 Audio3A 处理，并转发到房间管理器
/// </summary>
public class AudioWebSocketHandler
{
    private readonly ILogger<AudioWebSocketHandler> _logger;
    private readonly RoomManager _roomManager;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();

    public AudioWebSocketHandler(
        ILogger<AudioWebSocketHandler> logger,
        RoomManager roomManager,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _roomManager = roomManager;
        _serviceScopeFactory = serviceScopeFactory;
    }

    /// <summary>
    /// 处理 WebSocket 连接
    /// </summary>
    public async Task HandleWebSocketAsync(HttpContext context)
    {
        var roomId = context.Request.Query["roomId"].ToString();
        var participantId = context.Request.Query["participantId"].ToString();

        if (string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(participantId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Missing roomId or participantId");
            return;
        }

        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var connectionId = $"{roomId}:{participantId}";
        _connections.TryAdd(connectionId, webSocket);

        _logger.LogInformation("WebSocket 连接建立: RoomId={RoomId}, ParticipantId={ParticipantId}", roomId, participantId);

        try
        {
            await ReceiveAudioLoop(webSocket, roomId, participantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket 处理异常: RoomId={RoomId}, ParticipantId={ParticipantId}", roomId, participantId);
        }
        finally
        {
            _connections.TryRemove(connectionId, out _);
            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
            }
            webSocket.Dispose();
            _logger.LogInformation("WebSocket 连接关闭: RoomId={RoomId}, ParticipantId={ParticipantId}", roomId, participantId);
        }
    }

    /// <summary>
    /// 接收音频数据循环
    /// </summary>
    private async Task ReceiveAudioLoop(WebSocket webSocket, string roomId, string participantId)
    {
        var buffer = new byte[64 * 1024]; // 64KB buffer
        var messageBuffer = new StringBuilder();

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                _logger.LogInformation("收到关闭消息: RoomId={RoomId}, ParticipantId={ParticipantId}", roomId, participantId);
                break;
            }

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var messageChunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
                messageBuffer.Append(messageChunk);

                // 如果消息结束，处理完整消息
                if (result.EndOfMessage)
                {
                    var message = messageBuffer.ToString();
                    messageBuffer.Clear();

                    await ProcessAudioMessage(message, roomId, participantId);
                }
            }
        }
    }

    /// <summary>
    /// 处理音频消息
    /// </summary>
    private async Task ProcessAudioMessage(string message, string roomId, string participantId)
    {
        try
        {
            // 解析 JSON 消息
            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;

            if (!root.TryGetProperty("audioData", out var audioDataElement))
            {
                _logger.LogWarning("音频消息缺少 audioData 字段");
                return;
            }

            // 转换 JSON 数组为 float[]
            var audioData = new List<float>();
            foreach (var element in audioDataElement.EnumerateArray())
            {
                audioData.Add(element.GetSingle());
            }

            var samples = audioData.ToArray();
            _logger.LogDebug("收到音频数据: {SampleCount} 个采样点", samples.Length);

            // 应用 Audio3A 处理（使用新的 scope）
            var processedSamples = await ProcessAudioAsync(samples);

            // 发送到房间进行混音和录制
            var room = _roomManager.GetRoom(roomId);
            if (room != null)
            {
                room.AddParticipantAudio(participantId, processedSamples);
                _logger.LogDebug("音频已添加到房间: RoomId={RoomId}, ParticipantId={ParticipantId}", roomId, participantId);
            }
            else
            {
                _logger.LogWarning("房间不存在: RoomId={RoomId}", roomId);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON 解析失败: {Message}", message.Substring(0, Math.Min(100, message.Length)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理音频消息异常");
        }
    }

    /// <summary>
    /// 应用 Audio3A 处理
    /// </summary>
    private Task<float[]> ProcessAudioAsync(float[] samples)
    {
        try
        {
            // 创建新的 scope 来使用 scoped 服务
            using var scope = _serviceScopeFactory.CreateScope();
            var audioProcessor = scope.ServiceProvider.GetRequiredService<Audio3AProcessor>();

            // 创建音频缓冲区（Audio3AProcessor 使用同步方法）
            var inputBuffer = new AudioBuffer(samples, channels: 1, sampleRate: 48000);

            // 应用 3A 处理（同步调用）
            var outputBuffer = audioProcessor.Process(inputBuffer);

            // 返回处理后的数据
            return Task.FromResult(outputBuffer.Samples);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audio3A 处理异常，返回原始音频");
            return Task.FromResult(samples);
        }
    }

    /// <summary>
    /// 广播消息到指定房间的所有参与者（除了发送者）
    /// </summary>
    private async Task BroadcastToRoom(string roomId, string excludeParticipantId, byte[] data)
    {
        var tasks = new List<Task>();

        foreach (var (connectionId, socket) in _connections)
        {
            if (socket.State == WebSocketState.Open)
            {
                var parts = connectionId.Split(':');
                if (parts.Length == 2 && parts[0] == roomId && parts[1] != excludeParticipantId)
                {
                    tasks.Add(socket.SendAsync(
                        new ArraySegment<byte>(data),
                        WebSocketMessageType.Binary,
                        true,
                        CancellationToken.None));
                }
            }
        }

        await Task.WhenAll(tasks);
    }
}
