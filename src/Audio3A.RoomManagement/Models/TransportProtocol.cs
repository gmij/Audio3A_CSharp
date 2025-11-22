namespace Audio3A.RoomManagement.Models;

/// <summary>
/// 传输协议类型
/// </summary>
public enum TransportProtocol
{
    /// <summary>
    /// WebSocket 协议（用于信令和音频传输）
    /// </summary>
    WebSocket,

    /// <summary>
    /// WebRTC 协议（用于点对点音频传输）
    /// </summary>
    WebRTC,

    /// <summary>
    /// 混合模式（WebSocket 信令 + WebRTC 音频）
    /// </summary>
    Hybrid
}
