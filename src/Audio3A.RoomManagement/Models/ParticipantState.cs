namespace Audio3A.RoomManagement.Models;

/// <summary>
/// 参与者状态
/// </summary>
public enum ParticipantState
{
    /// <summary>
    /// 连接中
    /// </summary>
    Connecting,

    /// <summary>
    /// 已连接
    /// </summary>
    Connected,

    /// <summary>
    /// 静音
    /// </summary>
    Muted,

    /// <summary>
    /// 断开连接中
    /// </summary>
    Disconnecting,

    /// <summary>
    /// 已断开
    /// </summary>
    Disconnected
}
