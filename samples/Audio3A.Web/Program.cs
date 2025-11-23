using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Audio3A.Web;
using Audio3A.Web.Services;
using System.Net.Http.Json;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 读取 API 配置
var apiConfig = new ApiConfiguration();
var useMockApi = false; // 默认使用真实 API

try
{
    var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
    var config = await httpClient.GetFromJsonAsync<Dictionary<string, string>>("appsettings.json");
    if (config != null)
    {
        if (config.ContainsKey("ApiBaseUrl"))
        {
            apiConfig.BaseUrl = config["ApiBaseUrl"];
        }
        
        // 检查是否使用 Mock API（用于 GitHub Pages）
        if (config.ContainsKey("UseMockApi") && bool.TryParse(config["UseMockApi"], out var shouldUseMock))
        {
            useMockApi = shouldUseMock;
        }
    }
}
catch
{
    // 如果读取失败，使用默认值
    apiConfig.BaseUrl = builder.HostEnvironment.BaseAddress;
}

// 注册 API 配置
builder.Services.AddSingleton(apiConfig);

// 配置 HttpClient 使用 API 基础地址
builder.Services.AddScoped(sp => 
{
    var config = sp.GetRequiredService<ApiConfiguration>();
    var httpClient = new HttpClient { BaseAddress = new Uri(config.BaseUrl) };
    return httpClient;
});

// Add Ant Design Blazor
builder.Services.AddAntDesign();

// 根据配置注册 API 服务
if (useMockApi)
{
    // 使用 Mock API（用于 GitHub Pages 演示）
    builder.Services.AddSingleton<IApiService, MockApiService>();
    Console.WriteLine("使用 Mock API 服务（客户端内存模拟）");
}
else
{
    // 使用真实 API（连接服务端）
    builder.Services.AddScoped<IApiService>(sp =>
    {
        var httpClient = sp.GetRequiredService<HttpClient>();
        var logger = sp.GetService<ILogger<RealApiService>>();
        return new RealApiService(httpClient, logger);
    });
    Console.WriteLine($"使用真实 API 服务（连接到 {apiConfig.BaseUrl}）");
}

// 保留 MockApiService 的单独注册（向后兼容）
builder.Services.AddSingleton<MockApiService>();

// Add Audio Call Service
builder.Services.AddScoped<AudioCallService>();

await builder.Build().RunAsync();
