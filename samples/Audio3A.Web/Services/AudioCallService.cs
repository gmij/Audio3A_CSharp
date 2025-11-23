using Microsoft.JSInterop;

namespace Audio3A.Web.Services;

/// <summary>
/// 音频通话服务 - 只负责采集麦克风音频并通过回调传递
/// 录制、3A处理、混音等都在后端（WebApi）完成
/// </summary>
public class AudioCallService : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;
    private DotNetObjectReference<AudioCallService>? _objRef;
    private bool _isInitialized;

    /// <summary>
    /// 音频数据回调 - 用于实时传输到后端
    /// </summary>
    public event Action<float[]>? OnAudioData;
    
    /// <summary>
    /// 音频级别回调 - 用于 UI 显示
    /// </summary>
    public event Action<string, float>? OnAudioLevel;
    
    /// <summary>
    /// 错误回调
    /// </summary>
    public event Action<string>? OnError;

    public bool IsMuted { get; private set; }
    public bool IsConnected { get; private set; }

    public AudioCallService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// 初始化音频采集
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            _objRef = DotNetObjectReference.Create(this);
            _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/audioCall.js");
            await _module.InvokeVoidAsync("initialize", _objRef);
            _isInitialized = true;
            Console.WriteLine("AudioCallService initialized");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize audio call: {ex.Message}");
            OnError?.Invoke(ex.Message);
        }
    }

    /// <summary>
    /// 开始采集音频
    /// </summary>
    public async Task StartCaptureAsync(string roomId)
    {
        Console.WriteLine($"Starting audio capture for room {roomId}");
        
        if (!_isInitialized)
        {
            await InitializeAsync();
        }

        try
        {
            if (_module != null)
            {
                await _module.InvokeVoidAsync("startCapture", roomId);
                IsConnected = true;
                Console.WriteLine("Audio capture started");
            }
            else
            {
                throw new InvalidOperationException("JavaScript module not loaded");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start capture: {ex.Message}");
            OnError?.Invoke(ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 停止采集音频
    /// </summary>
    public async Task StopCaptureAsync()
    {
        try
        {
            if (_module != null && IsConnected)
            {
                await _module.InvokeVoidAsync("stopCapture");
                IsConnected = false;
                Console.WriteLine("Audio capture stopped");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to stop capture: {ex.Message}");
        }
    }

    /// <summary>
    /// 切换静音
    /// </summary>
    public async Task ToggleMuteAsync()
    {
        try
        {
            if (_module != null && IsConnected)
            {
                IsMuted = !IsMuted;
                await _module.InvokeVoidAsync("setMuted", IsMuted);
                Console.WriteLine($"Muted: {IsMuted}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to toggle mute: {ex.Message}");
        }
    }

    // ===== JavaScript 回调方法 =====

    /// <summary>
    /// 接收音频数据（从 JavaScript 回调） - 实时传输到后端
    /// </summary>
    [JSInvokable]
    public void NotifyAudioData(float[] audioData)
    {
        OnAudioData?.Invoke(audioData);
    }

    /// <summary>
    /// 接收音量级别
    /// </summary>
    [JSInvokable]
    public void NotifyAudioLevel(string participantId, float level)
    {
        OnAudioLevel?.Invoke(participantId, level);
    }

    /// <summary>
    /// 接收错误信息
    /// </summary>
    [JSInvokable]
    public void NotifyError(string message)
    {
        OnError?.Invoke(message);
    }

    public async ValueTask DisposeAsync()
    {
        if (IsConnected)
        {
            await StopCaptureAsync();
        }

        if (_module != null)
        {
            await _module.DisposeAsync();
        }

        _objRef?.Dispose();
    }
}
