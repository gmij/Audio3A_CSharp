using Microsoft.Extensions.DependencyInjection;
using Audio3A.RoomManagement.Audio;
using Audio3A.RoomManagement.Protocols;

namespace Audio3A.RoomManagement.Extensions;

/// <summary>
/// 依赖注入扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册房间管理服务
    /// </summary>
    public static IServiceCollection AddRoomManagement(
        this IServiceCollection services,
        Action<RoomManagementOptions>? configure = null)
    {
        var options = new RoomManagementOptions();
        configure?.Invoke(options);

        // 注册核心服务
        services.AddSingleton<RoomManager>();
        services.AddSingleton<AudioMixer>();
        services.AddSingleton<RoomAudioProcessor>();

        // 注册协议适配器
        if (options.EnableWebSocket)
        {
            services.AddSingleton<WebSocketAdapter>();
        }

        if (options.EnableWebRTC)
        {
            services.AddSingleton<WebRtcAdapter>();
        }

        if (options.EnableHybrid)
        {
            services.AddSingleton<HybridAdapter>();
        }

        return services;
    }
}

/// <summary>
/// 房间管理配置选项
/// </summary>
public class RoomManagementOptions
{
    /// <summary>
    /// 启用 WebSocket 协议
    /// </summary>
    public bool EnableWebSocket { get; set; } = true;

    /// <summary>
    /// 启用 WebRTC 协议
    /// </summary>
    public bool EnableWebRTC { get; set; } = false;

    /// <summary>
    /// 启用混合协议
    /// </summary>
    public bool EnableHybrid { get; set; } = true;

    /// <summary>
    /// 自动清理空房间的间隔（分钟）
    /// 0 表示不自动清理
    /// </summary>
    public int AutoCleanupIntervalMinutes { get; set; } = 30;

    /// <summary>
    /// 房间最大参与者数量（0 表示无限制）
    /// </summary>
    public int DefaultMaxParticipants { get; set; } = 0;
}
