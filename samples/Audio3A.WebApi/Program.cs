using Audio3A.Core.Extensions;
using Audio3A.RoomManagement.Extensions;
using Audio3A.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Audio3A API", Version = "v1", Description = "实时语音通话房间管理 API" });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Enable WebSocket support
builder.Services.AddSingleton<AudioWebSocketHandler>();

// Add Audio3A services
builder.Services.AddAudio3A(config =>
{
    config.EnableAec = true;
    config.EnableAgc = true;
    config.EnableAns = true;
    config.SampleRate = 16000;
    config.Channels = 1;
    config.FrameSize = 160;
});

// Add Room Management services
builder.Services.AddRoomManagement(options =>
{
    options.EnableWebSocket = true;
    options.EnableWebRTC = true; // 启用 WebRTC 以注册 WebRtcAdapter
    options.EnableHybrid = true; // 启用 Hybrid 以注册 HybridAdapter
    options.DefaultMaxParticipants = 10;
    options.AutoCleanupIntervalMinutes = 30;
});

var app = builder.Build();

// Enable WebSocket middleware
var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(120)
};
app.UseWebSockets(webSocketOptions);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Audio3A API v1"));
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Map WebSocket endpoint
app.Map("/ws/audio", async (HttpContext context, AudioWebSocketHandler handler) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        await handler.HandleWebSocketAsync(context);
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("WebSocket connections only");
    }
});

app.UseAuthorization();
app.MapControllers();

app.Run();
