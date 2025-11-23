namespace Audio3A.Web.Services;

/// <summary>
/// API 配置（使用 Options Pattern）
/// </summary>
public class ApiConfiguration
{
    /// <summary>
    /// API 基础地址
    /// </summary>
    public string ApiBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// 是否使用 Mock API（用于客户端演示）
    /// </summary>
    public bool UseMockApi { get; set; } = false;
}
