using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Audio3A.Web;
using Audio3A.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 读取配置值
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;
var useMockApi = builder.Configuration.GetValue<bool>("UseMockApi", false);

// 配置 HttpClient 使用 API 基础地址
builder.Services.AddScoped(sp => 
{
    var httpClient = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
    return httpClient;
});

// Add Ant Design Blazor
builder.Services.AddAntDesign();

// 添加音频服务
builder.Services.AddScoped<AudioCallService>();
builder.Services.AddScoped<AudioWebSocketService>();

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
    Console.WriteLine($"使用真实 API 服务（连接到 {apiBaseUrl}）");
}

// 保留 MockApiService 的单独注册（向后兼容）
builder.Services.AddSingleton<MockApiService>();

// Add Audio Call Service
builder.Services.AddScoped<AudioCallService>();

await builder.Build().RunAsync();
