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
try
{
    var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
    var config = await httpClient.GetFromJsonAsync<Dictionary<string, string>>("appsettings.json");
    if (config != null && config.ContainsKey("ApiBaseUrl"))
    {
        apiConfig.BaseUrl = config["ApiBaseUrl"];
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

// Add Mock API Service (for GitHub Pages demo)
builder.Services.AddSingleton<MockApiService>();

// Add Audio Call Service
builder.Services.AddScoped<AudioCallService>();

await builder.Build().RunAsync();
