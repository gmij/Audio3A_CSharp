namespace Audio3A.Core;

/// <summary>
/// 定义 3A 处理器的处理顺序
/// </summary>
public enum ProcessingOrder
{
    /// <summary>
    /// 标准顺序：AEC -> ANS -> AGC
    /// 这是 WebRTC 推荐的标准处理顺序
    /// 先消除回声，然后抑制噪声，最后调整增益
    /// </summary>
    Standard,

    /// <summary>
    /// 噪声优先：ANS -> AEC -> AGC
    /// 适用于噪声环境很严重的场景
    /// 先降噪可以提高回声消除效果
    /// </summary>
    NoiseSuppressFirst,

    /// <summary>
    /// 增益优先：AGC -> AEC -> ANS
    /// 适用于信号很弱的场景
    /// 先增强信号可以改善后续处理效果
    /// </summary>
    GainControlFirst,

    /// <summary>
    /// 自定义顺序
    /// 用户可以通过 ProcessorPipeline 自行配置
    /// </summary>
    Custom
}
