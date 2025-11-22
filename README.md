# Audio3A_CSharp

一个基于 .NET 8 的原生音频 3A (AEC, AGC, ANS) 处理 SDK，支持依赖注入和官方日志记录。

**🆕 现已支持实时语音通话房间管理系统！** 支持多房间、多人通话、WebSocket/WebRTC 协议。

## 功能特性

### 核心音频处理

Audio 3A SDK 提供了三种核心的音频处理算法：

- **AEC (Acoustic Echo Cancellation)** - 回声消除：消除扬声器播放引起的麦克风回声
- **AGC (Automatic Gain Control)** - 自动增益控制：自动调节音频音量，保持一致的输出电平
- **ANS (Automatic Noise Suppression)** - 自动噪声抑制：减少背景噪声，同时保留语音

### 🆕 房间管理系统

全新的 `Audio3A.RoomManagement` 模块提供完整的实时语音通话房间管理：

- ✅ **多房间管理** - 同时管理多个独立的语音通话房间
- ✅ **多人通话** - 每个房间支持多人同时在线
- ✅ **协议支持** - WebSocket、WebRTC 或混合协议
- ✅ **音频混音** - 实时混合多路音频流
- ✅ **3A 集成** - 每个参与者独立的音频处理

👉 [查看房间管理系统文档](src/Audio3A.RoomManagement/README.md)

### .NET 8 现代特性

- ✅ **依赖注入（DI）支持** - 使用 `Microsoft.Extensions.DependencyInjection`
- ✅ **官方日志记录** - 集成 `Microsoft.Extensions.Logging.ILogger`
- ✅ **Host Builder 模式** - 支持现代 .NET 应用架构
- ✅ **高可测试性** - 通过 DI 实现松耦合设计

## 系统要求

- .NET 8.0 或更高版本

## 项目结构

```
Audio3A_CSharp/
├── src/
│   ├── Audio3A.Core/           # 核心库
│   │   ├── AudioBuffer.cs      # 音频缓冲区
│   │   ├── AudioFormat.cs      # 音频格式
│   │   ├── Audio3AConfig.cs    # 配置类
│   │   ├── Audio3AProcessor.cs # 主处理器
│   │   ├── IAudioProcessor.cs  # 处理器接口
│   │   ├── Extensions/         # 扩展方法
│   │   │   └── ServiceCollectionExtensions.cs  # DI 注册扩展
│   │   └── Processors/         # 3A 算法实现
│   │       ├── AecProcessor.cs # 回声消除
│   │       ├── AgcProcessor.cs # 自动增益控制
│   │       └── AnsProcessor.cs # 噪声抑制
│   └── Audio3A.RoomManagement/ # 🆕 房间管理系统
│       ├── RoomManager.cs      # 房间管理器
│       ├── Models/             # 数据模型
│       ├── Audio/              # 音频处理
│       ├── Protocols/          # 协议适配器
│       └── Extensions/         # DI 扩展
├── samples/
│   ├── Audio3A.Demo/          # 3A 处理示例
│   └── Audio3A.RoomDemo/      # 🆕 房间管理示例
└── tests/
    ├── Audio3A.Tests/         # 3A 单元测试
    └── Audio3A.RoomManagement.Tests/ # 🆕 房间管理测试
```

## 快速开始

### 音频 3A 处理

#### 1. 使用依赖注入（推荐）

```csharp
using Audio3A.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// 使用 Host Builder 配置依赖注入
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // 注册 Audio 3A 服务
        services.AddAudio3A(config =>
        {
            config.EnableAec = true;
            config.EnableAgc = true;
            config.EnableAns = true;
            config.SampleRate = 16000;
            config.Channels = 1;
            config.AgcTargetLevel = 0.5f;
            config.AnsNoiseReductionDb = 20.0f;
        });
    })
    .ConfigureLogging(logging =>
    {
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .Build();

// 从 DI 容器获取处理器
using var scope = host.Services.CreateScope();
var processor = scope.ServiceProvider.GetRequiredService<Audio3AProcessor>();

// 处理音频数据
float[] inputSamples = new float[160]; // 10ms @ 16kHz
var inputBuffer = new AudioBuffer(inputSamples);
var outputBuffer = processor.Process(inputBuffer);
```

### 2. 直接实例化（适用于简单场景）

```csharp
using Audio3A.Core;
using Audio3A.Core.Processors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

// 创建配置
var config = new Audio3AConfig
{
    EnableAec = true,
    EnableAgc = true,
    EnableAns = true,
    SampleRate = 16000
};

// 创建服务集合和提供程序
var services = new ServiceCollection();
services.AddSingleton(config);

// 创建日志记录器
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
services.AddSingleton(loggerFactory.CreateLogger<Audio3AProcessor>());

// 注册处理器（带日志记录器）
if (config.EnableAec)
{
    var aecLogger = loggerFactory.CreateLogger<AecProcessor>();
    services.AddScoped<AecProcessor>(sp => 
        new AecProcessor(aecLogger, config.SampleRate, config.AecFilterLength, config.AecStepSize));
}

if (config.EnableAgc)
{
    var agcLogger = loggerFactory.CreateLogger<AgcProcessor>();
    services.AddScoped<AgcProcessor>(sp => 
        new AgcProcessor(agcLogger, config.SampleRate, config.AgcTargetLevel, config.AgcCompressionRatio));
}

if (config.EnableAns)
{
    var ansLogger = loggerFactory.CreateLogger<AnsProcessor>();
    services.AddScoped<AnsProcessor>(sp => 
        new AnsProcessor(ansLogger, config.SampleRate, noiseReductionDb: config.AnsNoiseReductionDb));
}

var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Audio3AProcessor>>();

// 创建处理器
using var processor = new Audio3AProcessor(logger, config, serviceProvider);

// 处理音频
var inputBuffer = new AudioBuffer(new float[160]);
var outputBuffer = processor.Process(inputBuffer);
```

### 3. 处理 16 位 PCM 数据
```csharp
using Audio3A.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddAudio3A(config =>
        {
            config.SampleRate = 16000;
            config.EnableAec = true;
            config.EnableAgc = true;
            config.EnableAns = true;
        });
    })
    .Build();

using var scope = host.Services.CreateScope();
var processor = scope.ServiceProvider.GetRequiredService<Audio3AProcessor>();

// 直接处理 16 位 PCM 数据
short[] inputPcm = new short[160];
// ... 从麦克风获取 PCM 数据 ...

short[] outputPcm = processor.ProcessInt16(inputPcm);
```

### 4. 使用回声消除（带参考信号）

```csharp
using Audio3A.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddAudio3A(config =>
        {
            config.EnableAec = true;
            config.SampleRate = 16000;
        });
    })
    .Build();

using var scope = host.Services.CreateScope();
var processor = scope.ServiceProvider.GetRequiredService<Audio3AProcessor>();

// 麦克风输入
short[] micInputPcm = new short[160];

// 扬声器参考信号（播放的音频）
short[] speakerReferencePcm = new short[160];

// 处理，AEC 会消除扬声器引起的回声
short[] outputPcm = processor.ProcessInt16(micInputPcm, speakerReferencePcm);
```

### 5. 自定义配置

```csharp
using Audio3A.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // 使用 AddAudio3A 注册并配置所有参数
        services.AddAudio3A(config =>
        {
            // 启用/禁用各个算法
            config.EnableAec = true;
            config.EnableAgc = true;
            config.EnableAns = true;

            // 音频格式
            config.SampleRate = 16000;
            config.Channels = 1;
            config.FrameSize = 160;

            // AGC 参数
            config.AgcTargetLevel = 0.5f;        // 目标电平 (0.0-1.0)
            config.AgcCompressionRatio = 3.0f;   // 压缩比

            // ANS 参数
            config.AnsNoiseReductionDb = 20.0f;  // 噪声抑制强度 (dB)

            // AEC 参数
            config.AecFilterLength = 512;        // 滤波器长度
            config.AecStepSize = 0.01f;          // 自适应步长
        });
    })
    .Build();

using var scope = host.Services.CreateScope();
var processor = scope.ServiceProvider.GetRequiredService<Audio3AProcessor>();
```

### 🆕 房间管理系统快速开始

```csharp
using Audio3A.Core.Extensions;
using Audio3A.RoomManagement;
using Audio3A.RoomManagement.Extensions;
using Audio3A.RoomManagement.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // 注册 Audio3A 和房间管理服务
        services.AddAudio3A(config =>
        {
            config.EnableAec = true;
            config.EnableAgc = true;
            config.EnableAns = true;
            config.SampleRate = 16000;
        });
        
        services.AddRoomManagement(options =>
        {
            options.EnableWebSocket = true;
            options.DefaultMaxParticipants = 10;
        });
    })
    .Build();

using var scope = host.Services.CreateScope();
var roomManager = scope.ServiceProvider.GetRequiredService<RoomManager>();

// 创建房间
var audioConfig = new Audio3AConfig { SampleRate = 16000, EnableAec = true, EnableAgc = true, EnableAns = true };
var room = roomManager.CreateRoom("room-001", "会议室", audioConfig);

// 参与者加入
var participant = new Participant("user-001", "张三", room.Id, TransportProtocol.WebSocket);
roomManager.JoinRoom(room.Id, participant);

Console.WriteLine($"房间 {room.Name} 已创建，{participant.Name} 已加入！");
```

👉 **更多示例和详细文档**：[房间管理系统完整文档](src/Audio3A.RoomManagement/README.md)

## 日志记录

SDK 使用 `Microsoft.Extensions.Logging.ILogger` 进行日志记录。您可以配置日志级别和输出：

```csharp
using Microsoft.Extensions.Logging;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddAudio3A(config =>
        {
            config.SampleRate = 16000;
        });
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        
        // 设置日志级别
        logging.SetMinimumLevel(LogLevel.Debug);  // 查看详细日志
        // logging.SetMinimumLevel(LogLevel.Information);  // 仅重要信息
        // logging.SetMinimumLevel(LogLevel.Warning);  // 仅警告和错误
    })
    .Build();
```

日志输出示例:
```
info: Audio3A.Core.Audio3AProcessor[0]
      Audio3A processor initialized: AEC=True, AGC=True, ANS=True, SampleRate=16000
debug: Audio3A.Core.Processors.AecProcessor[0]
      AEC processor initialized: SampleRate=16000, FilterLength=512, StepSize=0.01
```

## 构建与测试

### 构建项目

```bash
dotnet build Audio3A.sln
```

### 运行测试

```bash
dotnet test tests/Audio3A.Tests/Audio3A.Tests.csproj
```

### 运行示例

```bash
dotnet run --project samples/Audio3A.Demo/Audio3A.Demo.csproj
```

## API 文档

### Audio3AProcessor

主处理器类，整合所有 3A 算法。

#### 构造函数

```csharp
public Audio3AProcessor(Audio3AConfig? config = null)
```

#### 主要方法

- `AudioBuffer Process(AudioBuffer micInput, AudioBuffer? speakerReference = null)` - 处理浮点音频缓冲区
- `short[] ProcessInt16(short[] micInputPcm, short[]? speakerReferencePcm = null)` - 处理 16 位 PCM 数据
- `void Reset()` - 重置所有处理器状态
- `void Dispose()` - 释放资源

### AudioBuffer

音频数据容器。

#### 属性

- `float[] Samples` - 音频样本数据（范围 -1.0 到 1.0）
- `int Channels` - 声道数
- `int SampleRate` - 采样率
- `int Length` - 样本数量

#### 方法

- `static AudioBuffer FromInt16(short[] pcmData, ...)` - 从 16 位 PCM 转换
- `short[] ToInt16()` - 转换为 16 位 PCM

### Audio3AConfig

配置类，用于设置 3A 处理参数。

详见"自定义配置"章节的示例。

## 算法说明

### AEC (回声消除)

使用自适应滤波器（NLMS 算法）来估计和消除扬声器播放引起的回声。适用于：
- 视频通话
- 语音会议
- 免提通话

### AGC (自动增益控制)

动态调整音频增益，保持输出电平稳定。特性：
- 自动压缩过大的音量
- 自动放大过小的音量
- 平滑的增益过渡

### ANS (噪声抑制)

基于能量检测的噪声抑制算法，降低背景噪声。适用于：
- 语音通话
- 录音降噪
- 语音识别预处理

## 性能考虑

- 推荐的帧大小：160 样本（10ms @ 16kHz）
- 支持的采样率：8kHz, 16kHz, 32kHz, 48kHz 等
- 低延迟：每帧处理时间通常 < 1ms
- 内存占用小：纯 .NET 实现，无需外部依赖

## 最佳实践

1. **选择合适的采样率**：语音应用推荐 16kHz，音乐应用推荐 48kHz
2. **合理设置帧大小**：通常为 10ms-20ms 的音频数据
3. **提供参考信号给 AEC**：为获得最佳回声消除效果，务必提供扬声器播放的参考信号
4. **调整 AGC 目标电平**：根据应用场景调整 `AgcTargetLevel`，通常设置为 0.5-0.7
5. **循环使用处理器实例**：避免频繁创建和销毁处理器对象

## 示例场景

### 实时语音通话

```csharp
using Audio3A.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddAudio3A(config =>
        {
            config.SampleRate = 16000;
            config.FrameSize = 160;  // 10ms
            config.EnableAec = true;  // 消除回声
            config.EnableAgc = true;  // 稳定音量
            config.EnableAns = true;  // 降噪
        });
    })
    .ConfigureLogging(logging =>
    {
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .Build();

using var scope = host.Services.CreateScope();
var processor = scope.ServiceProvider.GetRequiredService<Audio3AProcessor>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

// 在音频采集回调中
void OnAudioCaptured(short[] micData, short[] speakerData)
{
    short[] processed = processor.ProcessInt16(micData, speakerData);
    // 发送处理后的音频到网络
    logger.LogDebug("Processed audio frame: {Length} samples", processed.Length);
}
```

### 录音降噪

```csharp
using Audio3A.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddAudio3A(config =>
        {
            config.EnableAec = false;  // 录音不需要回声消除
            config.EnableAgc = true;   // 稳定音量
            config.EnableAns = true;   // 降噪
            config.AnsNoiseReductionDb = 25.0f;  // 较强的降噪
        });
    })
    .Build();

using var scope = host.Services.CreateScope();
var processor = scope.ServiceProvider.GetRequiredService<Audio3AProcessor>();

// 处理录音文件
foreach (var frame in audioFrames)
{
    var processed = processor.ProcessInt16(frame);
    // 保存处理后的帧
}
```

## 许可证

MIT License

## 贡献

欢迎提交 Issue 和 Pull Request！

## 联系方式

如有问题或建议，请在 GitHub 上提交 Issue。
