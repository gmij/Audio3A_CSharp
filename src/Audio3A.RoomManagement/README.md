# Audio3A 房间管理系统

基于 Audio3A.Core 构建的实时语音通话房间管理系统，支持多房间、多人语音通话，集成 3A (AEC, AGC, ANS) 音频处理。

## 功能特性

### 核心功能

- ✅ **多房间管理**：同时管理多个独立的语音通话房间
- ✅ **多人通话**：每个房间支持多人同时在线
- ✅ **协议支持**：WebSocket、WebRTC 或混合协议
- ✅ **3A 集成**：每个参与者独立的音频处理（回声消除、增益控制、噪声抑制）
- ✅ **音频混音**：实时混合多路音频流
- ✅ **线程安全**：并发访问和操作的完全支持

### 协议支持

#### WebSocket 协议
- **信令传输**：实时传输控制消息
- **音频传输**：通过二进制消息传输音频数据
- **连接管理**：自动处理连接和断开
- **适用场景**：简单实时通信、移动端、Web 端

#### WebRTC 协议（扩展框架）
- **点对点连接**：低延迟的音频传输
- **NAT 穿透**：支持 ICE、STUN、TURN
- **SDP 协商**：自动协商编解码器
- **适用场景**：高质量实时通信、大规模会议

#### 混合协议
- **信令与音频分离**：WebSocket 传输信令，WebRTC 传输音频
- **最佳实践**：结合两种协议的优势
- **灵活切换**：根据网络条件自动选择

## 快速开始

### 1. 安装依赖

```bash
dotnet add package Audio3A.RoomManagement
```

### 2. 基本使用

```csharp
using Audio3A.Core;
using Audio3A.Core.Extensions;
using Audio3A.RoomManagement;
using Audio3A.RoomManagement.Extensions;
using Audio3A.RoomManagement.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// 配置服务
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // 注册 Audio3A 服务
        services.AddAudio3A(config =>
        {
            config.EnableAec = true;
            config.EnableAgc = true;
            config.EnableAns = true;
            config.SampleRate = 16000;
        });

        // 注册房间管理服务
        services.AddRoomManagement(options =>
        {
            options.EnableWebSocket = true;
            options.EnableWebRTC = false;
            options.DefaultMaxParticipants = 10;
        });
    })
    .Build();

// 使用服务
using var scope = host.Services.CreateScope();
var roomManager = scope.ServiceProvider.GetRequiredService<RoomManager>();

// 创建房间
var audioConfig = new Audio3AConfig
{
    EnableAec = true,
    EnableAgc = true,
    EnableAns = true,
    SampleRate = 16000
};

var room = roomManager.CreateRoom(
    roomId: "room-001",
    roomName: "会议室",
    audioConfig: audioConfig,
    supportedProtocols: TransportProtocol.WebSocket
);

// 参与者加入
var participant = new Participant("user-001", "张三", room.Id, TransportProtocol.WebSocket);
roomManager.JoinRoom(room.Id, participant);
```

### 3. 音频处理和混音

```csharp
using Audio3A.RoomManagement.Audio;

var roomAudioProcessor = scope.ServiceProvider.GetRequiredService<RoomAudioProcessor>();
var audioMixer = scope.ServiceProvider.GetRequiredService<AudioMixer>();

// 为参与者创建 3A 处理器
participant.Audio3AProcessor = roomAudioProcessor.CreateProcessorForParticipant(participant, room);

// 处理音频数据
short[] inputAudio = GetAudioFromMicrophone(); // 从麦克风获取音频
short[] processedAudio = roomAudioProcessor.ProcessAudioFrame(participant, inputAudio);

// 创建音频帧
var frame1 = new AudioFrame("user-001", processedAudio1, 16000, 1, 1);
var frame2 = new AudioFrame("user-002", processedAudio2, 16000, 1, 2);

// 混音（排除自己）
var mixedAudio = audioMixer.Mix(new[] { frame1, frame2 }, excludeParticipantId: "user-001");

// 使用能量感知混音
var autoGainMixed = audioMixer.MixWithAutoGain(new[] { frame1, frame2 });
```

## 架构设计

### 核心组件

#### RoomManager
房间管理器，负责房间的生命周期管理：

```csharp
public class RoomManager
{
    // 创建房间
    Room CreateRoom(string? roomId, string? roomName, Audio3AConfig? audioConfig, 
                   TransportProtocol supportedProtocols);
    
    // 获取房间
    Room? GetRoom(string roomId);
    
    // 删除房间
    bool RemoveRoom(string roomId);
    
    // 参与者管理
    bool JoinRoom(string roomId, Participant participant);
    bool LeaveRoom(string roomId, string participantId);
    
    // 清理空房间
    int CleanupEmptyRooms();
}
```

#### Room
房间模型，表示一个语音通话房间：

```csharp
public class Room
{
    string Id { get; }                          // 房间 ID
    string Name { get; set; }                   // 房间名称
    RoomState State { get; set; }               // 房间状态
    int MaxParticipants { get; set; }           // 最大参与者数
    TransportProtocol SupportedProtocols { get; } // 支持的协议
    Audio3AConfig AudioConfig { get; }          // 音频配置
    IReadOnlyCollection<Participant> Participants { get; } // 参与者列表
}
```

#### Participant
参与者模型，表示房间中的一个用户：

```csharp
public class Participant
{
    string Id { get; }                    // 参与者 ID
    string Name { get; set; }             // 显示名称
    string RoomId { get; }                // 所在房间
    ParticipantState State { get; set; }  // 当前状态
    TransportProtocol Protocol { get; }   // 使用的协议
    bool Enable3A { get; set; }           // 是否启用 3A
    Audio3AProcessor? Audio3AProcessor { get; set; } // 3A 处理器
}
```

#### AudioMixer
音频混音器，混合多路音频流：

```csharp
public class AudioMixer
{
    // 简单混音
    short[] Mix(IEnumerable<AudioFrame> frames, string? excludeParticipantId = null);
    
    // 能量感知混音
    short[] MixWithAutoGain(IEnumerable<AudioFrame> frames, string? excludeParticipantId = null);
}
```

#### RoomAudioProcessor
房间音频处理器，管理参与者的 3A 处理：

```csharp
public class RoomAudioProcessor
{
    // 创建 3A 处理器
    Audio3AProcessor CreateProcessorForParticipant(Participant participant, Room room);
    
    // 处理音频帧
    short[] ProcessAudioFrame(Participant participant, short[] audioData, 
                             short[]? referenceData = null);
    
    // 重置处理器
    void ResetProcessor(Participant participant);
}
```

### 协议适配器

所有协议适配器实现 `ITransportAdapter` 接口：

```csharp
public interface ITransportAdapter
{
    TransportProtocol Protocol { get; }
    
    // 发送音频
    Task SendAudioAsync(string participantId, AudioFrame frame, CancellationToken ct);
    Task BroadcastAudioAsync(string roomId, AudioFrame frame, string? excludeId, CancellationToken ct);
    
    // 发送信令
    Task SendSignalingAsync(string participantId, string message, CancellationToken ct);
    
    // 事件
    event EventHandler<AudioFrameReceivedEventArgs> AudioFrameReceived;
    event EventHandler<SignalingMessageReceivedEventArgs> SignalingMessageReceived;
    event EventHandler<ParticipantConnectedEventArgs> ParticipantConnected;
    event EventHandler<ParticipantDisconnectedEventArgs> ParticipantDisconnected;
    
    // 生命周期
    Task StartAsync(CancellationToken ct);
    Task StopAsync(CancellationToken ct);
}
```

## 使用场景

### 场景 1: 多人会议室

```csharp
// 创建会议室
var room = roomManager.CreateRoom("meeting-001", "技术讨论会", audioConfig);

// 多人加入
var participants = new[]
{
    new Participant("user-001", "张三", room.Id, TransportProtocol.WebSocket),
    new Participant("user-002", "李四", room.Id, TransportProtocol.WebSocket),
    new Participant("user-003", "王五", room.Id, TransportProtocol.WebSocket)
};

foreach (var p in participants)
{
    roomManager.JoinRoom(room.Id, p);
    p.Audio3AProcessor = roomAudioProcessor.CreateProcessorForParticipant(p, room);
}

// 音频处理循环
while (room.State == RoomState.Active)
{
    // 收集所有参与者的音频
    var frames = new List<AudioFrame>();
    foreach (var p in room.Participants)
    {
        var audio = GetAudioFrom(p); // 获取音频数据
        var processed = roomAudioProcessor.ProcessAudioFrame(p, audio);
        frames.Add(new AudioFrame(p.Id, processed, 16000, 1, frameNumber++));
    }
    
    // 为每个参与者混音（排除自己）
    foreach (var p in room.Participants)
    {
        var mixed = audioMixer.Mix(frames, p.Id);
        SendAudioTo(p, mixed); // 发送混音后的音频
    }
}
```

### 场景 2: 一对一通话

```csharp
// 创建私密房间
var room = roomManager.CreateRoom("call-001", "私密通话", audioConfig);
room.MaxParticipants = 2;

var caller = new Participant("user-001", "呼叫者", room.Id, TransportProtocol.WebRTC);
var receiver = new Participant("user-002", "接收者", room.Id, TransportProtocol.WebRTC);

roomManager.JoinRoom(room.Id, caller);
roomManager.JoinRoom(room.Id, receiver);

// 启用 AEC（回声消除）
caller.Audio3AProcessor = roomAudioProcessor.CreateProcessorForParticipant(caller, room);
receiver.Audio3AProcessor = roomAudioProcessor.CreateProcessorForParticipant(receiver, room);
```

### 场景 3: 语音聊天室

```csharp
// 创建聊天室
var chatRoom = roomManager.CreateRoom("chat-001", "语音聊天室", audioConfig);
chatRoom.MaxParticipants = 50; // 最多 50 人

// 动态加入/离开
void OnUserJoin(string userId, string userName)
{
    var participant = new Participant(userId, userName, chatRoom.Id, TransportProtocol.WebSocket);
    if (roomManager.JoinRoom(chatRoom.Id, participant))
    {
        participant.Audio3AProcessor = roomAudioProcessor.CreateProcessorForParticipant(participant, chatRoom);
        BroadcastMessage($"{userName} 加入了聊天室");
    }
}

void OnUserLeave(string userId)
{
    var participant = chatRoom.GetParticipant(userId);
    if (participant != null)
    {
        roomManager.LeaveRoom(chatRoom.Id, userId);
        BroadcastMessage($"{participant.Name} 离开了聊天室");
    }
}
```

## 配置选项

### RoomManagementOptions

```csharp
services.AddRoomManagement(options =>
{
    // 启用 WebSocket 协议
    options.EnableWebSocket = true;
    
    // 启用 WebRTC 协议
    options.EnableWebRTC = false;
    
    // 启用混合协议
    options.EnableHybrid = true;
    
    // 自动清理空房间的间隔（分钟）
    options.AutoCleanupIntervalMinutes = 30;
    
    // 默认最大参与者数（0 表示无限制）
    options.DefaultMaxParticipants = 0;
});
```

### Audio3AConfig

每个房间都有独立的音频配置：

```csharp
var audioConfig = new Audio3AConfig
{
    // 启用回声消除
    EnableAec = true,
    AecFilterLength = 512,
    AecStepSize = 0.01f,
    
    // 启用自动增益控制
    EnableAgc = true,
    AgcTargetLevel = 0.5f,
    AgcCompressionRatio = 3.0f,
    
    // 启用噪声抑制
    EnableAns = true,
    AnsNoiseReductionDb = 20.0f,
    
    // 音频格式
    SampleRate = 16000,
    Channels = 1,
    FrameSize = 160,
    
    // 处理顺序
    ProcessingOrder = ProcessingOrder.Standard // AEC -> ANS -> AGC
};
```

## 性能优化

### 1. 并发处理

房间管理器使用 `ConcurrentDictionary` 实现线程安全：

```csharp
// 可以从多个线程同时操作
Parallel.ForEach(rooms, room =>
{
    ProcessRoomAudio(room);
});
```

### 2. 音频缓冲

使用合适的帧大小减少处理开销：

```csharp
// 推荐配置
config.FrameSize = 160;  // 10ms @ 16kHz
config.SampleRate = 16000; // 语音应用推荐
```

### 3. 条件性 3A 处理

根据需要启用 3A 处理：

```csharp
// 仅在需要时启用
participant.Enable3A = room.ParticipantCount > 2; // 多人时才启用
```

## 测试

项目包含完整的单元测试：

```bash
# 运行所有测试
dotnet test

# 运行房间管理测试
dotnet test tests/Audio3A.RoomManagement.Tests

# 测试覆盖率
dotnet test /p:CollectCoverage=true
```

## 示例程序

运行完整示例：

```bash
dotnet run --project samples/Audio3A.RoomDemo
```

示例包含：
- 创建多个房间
- 参与者加入和离开
- 3A 音频处理
- 音频混音演示
- 房间清理

## 最佳实践

1. **房间隔离**：为不同场景创建独立房间
2. **资源管理**：及时清理空房间和离线参与者
3. **错误处理**：监听适配器事件处理连接错误
4. **性能监控**：跟踪房间数量和参与者数量
5. **音频质量**：根据网络条件调整采样率和帧大小

## 扩展 WebRTC

完整的 WebRTC 实现需要：

```csharp
// 1. 引入 SIPSorcery 或其他 WebRTC 库
// 2. 实现 PeerConnection 管理
// 3. 配置 STUN/TURN 服务器
// 4. 处理 ICE candidate 交换
// 5. 实现音频编解码器

// 示例框架已在 WebRtcAdapter 中提供
public class WebRtcAdapter : ITransportAdapter
{
    // TODO: 完整实现
    public Task HandleOfferAsync(string participantId, string sdp);
    public Task HandleAnswerAsync(string participantId, string sdp);
    public Task HandleIceCandidateAsync(string participantId, string candidate);
}
```

## 常见问题

**Q: 如何限制房间人数？**
```csharp
room.MaxParticipants = 10; // 最多 10 人
```

**Q: 如何实现静音功能？**
```csharp
participant.State = ParticipantState.Muted;
// 在混音时检查状态
```

**Q: 如何处理网络断线？**
```csharp
adapter.ParticipantDisconnected += (sender, e) =>
{
    roomManager.LeaveRoom(e.RoomId, e.ParticipantId);
};
```

**Q: 如何实现房间锁定？**
```csharp
room.Metadata["locked"] = true;
// 在加入时检查
if (room.Metadata.ContainsKey("locked")) return false;
```

## 许可证

MIT License

## 贡献

欢迎提交 Issue 和 Pull Request！
