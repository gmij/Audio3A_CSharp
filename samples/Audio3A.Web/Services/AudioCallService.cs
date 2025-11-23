using Microsoft.JSInterop;

namespace Audio3A.Web.Services;

/// <summary>
/// 音频通话服务（WebRTC）
/// </summary>
public class AudioCallService : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;
    private DotNetObjectReference<AudioCallService>? _objRef;
    private bool _isInitialized;

    public event Action<string>? OnParticipantJoined;
    public event Action<string>? OnParticipantLeft;
    public event Action<string, float>? OnAudioLevel;
    public event Action<byte[]>? OnInputWaveform;
    public event Action<byte[]>? OnProcessedWaveform;
    public event Action<string>? OnError;

    public bool IsMuted { get; private set; }
    public bool IsConnected { get; private set; }

    public AudioCallService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// 初始化音频通话
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            _objRef = DotNetObjectReference.Create(this);
            // 使用绝对路径（基于base标签）导入JavaScript模块
            _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/audioCall.js");
            await _module.InvokeVoidAsync("initialize", _objRef);
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize audio call: {ex.Message}");
            OnError?.Invoke(ex.Message);
        }
    }

    /// <summary>
    /// 开始通话
    /// </summary>
    public async Task StartCallAsync(string roomId, bool enable3A)
    {
        Console.WriteLine($"AudioCallService: Starting call for room {roomId}, 3A={enable3A}");
        
        if (!_isInitialized)
        {
            Console.WriteLine("AudioCallService: Not initialized, initializing now...");
            await InitializeAsync();
        }

        try
        {
            if (_module != null)
            {
                Console.WriteLine("AudioCallService: Invoking startCall on JS module");
                await _module.InvokeVoidAsync("startCall", roomId, enable3A);
                IsConnected = true;
                Console.WriteLine("AudioCallService: Call started successfully");
            }
            else
            {
                Console.WriteLine("AudioCallService: ERROR - Module is null!");
                throw new InvalidOperationException("JavaScript module not loaded");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AudioCallService: Failed to start call: {ex.Message}");
            Console.WriteLine($"AudioCallService: Stack trace: {ex.StackTrace}");
            OnError?.Invoke(ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 结束通话
    /// </summary>
    public async Task EndCallAsync()
    {
        try
        {
            if (_module != null && IsConnected)
            {
                await _module.InvokeVoidAsync("endCall");
                IsConnected = false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to end call: {ex.Message}");
        }
    }

    /// <summary>
    /// 切换静音状态
    /// </summary>
    public async Task ToggleMuteAsync()
    {
        try
        {
            if (_module != null && IsConnected)
            {
                IsMuted = !IsMuted;
                await _module.InvokeVoidAsync("setMuted", IsMuted);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to toggle mute: {ex.Message}");
        }
    }

    // JavaScript 回调方法
    [JSInvokable]
    public void NotifyParticipantJoined(string participantId)
    {
        OnParticipantJoined?.Invoke(participantId);
    }

    [JSInvokable]
    public void NotifyParticipantLeft(string participantId)
    {
        OnParticipantLeft?.Invoke(participantId);
    }

    [JSInvokable]
    public void NotifyAudioLevel(string participantId, float level)
    {
        Console.WriteLine($"AudioCallService: Received audio level - Participant: {participantId}, Level: {level:F3}");
        OnAudioLevel?.Invoke(participantId, level);
    }

    [JSInvokable]
    public void NotifyInputWaveform(byte[] waveformData)
    {
        OnInputWaveform?.Invoke(waveformData);
    }

    [JSInvokable]
    public void NotifyProcessedWaveform(byte[] waveformData)
    {
        OnProcessedWaveform?.Invoke(waveformData);
    }

    [JSInvokable]
    public void NotifyError(string message)
    {
        OnError?.Invoke(message);
    }

    public async ValueTask DisposeAsync()
    {
        if (IsConnected)
        {
            await EndCallAsync();
        }

        if (_module != null)
        {
            await _module.DisposeAsync();
        }

        _objRef?.Dispose();
    }
}
