using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Audio3A.Web.Services;

/// <summary>
/// WebSocket 音频传输服务 - 将音频数据发送到后端
/// </summary>
public class AudioWebSocketService : IAsyncDisposable
{
    private readonly string _wsBaseUrl;
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cts;
    private bool _isConnected;

    public event Action<string>? OnMessage;
    public event Action? OnConnected;
    public event Action? OnDisconnected;
    public event Action<string>? OnError;

    public bool IsConnected => _isConnected;

    public AudioWebSocketService(IConfiguration configuration)
    {
        // 从配置中读取 WebSocket 地址
        var apiBaseUrl = configuration["ApiBaseUrl"] ?? "http://localhost:5000";
        
        // 转换为 WebSocket URL
        _wsBaseUrl = apiBaseUrl.Replace("http://", "ws://").Replace("https://", "wss://");
    }

    /// <summary>
    /// 连接到后端 WebSocket
    /// </summary>
    public async Task ConnectAsync(string roomId, string participantId)
    {
        if (_isConnected)
        {
            Console.WriteLine("WebSocket already connected");
            return;
        }

        try
        {
            _webSocket = new ClientWebSocket();
            _cts = new CancellationTokenSource();

            var wsUrl = $"{_wsBaseUrl}/ws/audio?roomId={roomId}&participantId={participantId}";
            Console.WriteLine($"Connecting to WebSocket: {wsUrl}");

            await _webSocket.ConnectAsync(new Uri(wsUrl), _cts.Token);
            _isConnected = true;

            Console.WriteLine("WebSocket connected");
            OnConnected?.Invoke();

            // 启动接收消息的任务
            _ = ReceiveLoopAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebSocket connection failed: {ex.Message}");
            OnError?.Invoke(ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 发送音频数据到后端
    /// </summary>
    public async Task SendAudioDataAsync(float[] audioData)
    {
        if (!_isConnected || _webSocket == null || _cts == null)
        {
            return;
        }

        try
        {
            // 将音频数据序列化为 JSON
            var message = new
            {
                type = "audio",
                audioData = audioData,  // 修改字段名为 audioData，与后端一致
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var json = JsonSerializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);

            await _webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                _cts.Token
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send audio data: {ex.Message}");
            // 不抛出异常，避免中断音频流
        }
    }

    /// <summary>
    /// 发送文本消息
    /// </summary>
    public async Task SendMessageAsync(string message)
    {
        if (!_isConnected || _webSocket == null || _cts == null)
        {
            return;
        }

        try
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                _cts.Token
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send message: {ex.Message}");
        }
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (!_isConnected || _webSocket == null)
        {
            return;
        }

        try
        {
            _cts?.Cancel();

            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Client closing",
                    CancellationToken.None
                );
            }

            Console.WriteLine("WebSocket disconnected");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during disconnect: {ex.Message}");
        }
        finally
        {
            _isConnected = false;
            OnDisconnected?.Invoke();
        }
    }

    /// <summary>
    /// 接收消息循环
    /// </summary>
    private async Task ReceiveLoopAsync()
    {
        if (_webSocket == null || _cts == null)
        {
            return;
        }

        var buffer = new byte[1024 * 4];

        try
        {
            while (_webSocket.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    _cts.Token
                );

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await DisconnectAsync();
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Received message: {message}");
                    OnMessage?.Invoke(message);
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Receive loop cancelled");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in receive loop: {ex.Message}");
            OnError?.Invoke(ex.Message);
            await DisconnectAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();

        _cts?.Dispose();
        _webSocket?.Dispose();
    }
}
