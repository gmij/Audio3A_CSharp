using Audio3A.Core;
using Audio3A.Core.Extensions;
using Audio3A.RoomManagement;
using Audio3A.RoomManagement.Extensions;
using Audio3A.RoomManagement.Models;
using Audio3A.RoomManagement.Audio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

Console.WriteLine("Audio3A 房间管理系统演示");
Console.WriteLine("========================\n");

// 构建主机
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
            config.Channels = 1;
            config.FrameSize = 160;
        });

        // 注册房间管理服务
        services.AddRoomManagement(options =>
        {
            options.EnableWebSocket = true;
            options.EnableWebRTC = false; // 简化演示，仅使用 WebSocket
            options.EnableHybrid = false;
            options.DefaultMaxParticipants = 10;
        });
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var roomManager = services.GetRequiredService<RoomManager>();
    var audioMixer = services.GetRequiredService<AudioMixer>();
    var roomAudioProcessor = services.GetRequiredService<RoomAudioProcessor>();

    logger.LogInformation("房间管理系统已启动");
    Console.WriteLine();

    // 示例 1: 创建房间
    Console.WriteLine("示例 1: 创建房间");
    Console.WriteLine("----------------");

    var audioConfig = new Audio3AConfig
    {
        EnableAec = true,
        EnableAgc = true,
        EnableAns = true,
        SampleRate = 16000,
        Channels = 1,
        FrameSize = 160
    };

    var room1 = roomManager.CreateRoom(
        roomId: "room-001",
        roomName: "会议室 1",
        audioConfig: audioConfig,
        supportedProtocols: TransportProtocol.WebSocket
    );

    var room2 = roomManager.CreateRoom(
        roomId: "room-002",
        roomName: "会议室 2",
        audioConfig: audioConfig,
        supportedProtocols: TransportProtocol.WebSocket
    );

    Console.WriteLine($"创建房间: {room1.Name} (ID: {room1.Id})");
    Console.WriteLine($"创建房间: {room2.Name} (ID: {room2.Id})");
    Console.WriteLine($"活跃房间数: {roomManager.ActiveRoomCount}");
    Console.WriteLine();

    // 示例 2: 参与者加入房间
    Console.WriteLine("示例 2: 参与者加入房间");
    Console.WriteLine("----------------------");

    var participant1 = new Participant("user-001", "张三", room1.Id, TransportProtocol.WebSocket);
    var participant2 = new Participant("user-002", "李四", room1.Id, TransportProtocol.WebSocket);
    var participant3 = new Participant("user-003", "王五", room2.Id, TransportProtocol.WebSocket);

    roomManager.JoinRoom(room1.Id, participant1);
    roomManager.JoinRoom(room1.Id, participant2);
    roomManager.JoinRoom(room2.Id, participant3);

    Console.WriteLine($"{participant1.Name} 加入 {room1.Name}");
    Console.WriteLine($"{participant2.Name} 加入 {room1.Name}");
    Console.WriteLine($"{participant3.Name} 加入 {room2.Name}");
    Console.WriteLine($"{room1.Name} 当前参与者: {room1.ParticipantCount}");
    Console.WriteLine($"{room2.Name} 当前参与者: {room2.ParticipantCount}");
    Console.WriteLine($"总参与者数: {roomManager.TotalParticipantCount}");
    Console.WriteLine();

    // 示例 3: 为参与者创建 3A 处理器
    Console.WriteLine("示例 3: 为参与者初始化 3A 音频处理");
    Console.WriteLine("------------------------------------");

    participant1.Audio3AProcessor = roomAudioProcessor.CreateProcessorForParticipant(participant1, room1);
    participant2.Audio3AProcessor = roomAudioProcessor.CreateProcessorForParticipant(participant2, room1);

    Console.WriteLine($"为 {participant1.Name} 创建 3A 处理器");
    Console.WriteLine($"为 {participant2.Name} 创建 3A 处理器");
    Console.WriteLine();

    // 示例 4: 模拟音频数据处理
    Console.WriteLine("示例 4: 处理和混合音频数据");
    Console.WriteLine("----------------------------");

    // 创建模拟音频数据
    var random = new Random(42);
    var audioData1 = GenerateSineWave(160, 16000, 440, 0.3f, random); // 参与者 1 的音频（440Hz）
    var audioData2 = GenerateSineWave(160, 16000, 880, 0.3f, random); // 参与者 2 的音频（880Hz）

    Console.WriteLine($"参与者 1 音频: {audioData1.Length} 采样点");
    Console.WriteLine($"参与者 2 音频: {audioData2.Length} 采样点");

    // 应用 3A 处理
    var processed1 = roomAudioProcessor.ProcessAudioFrame(participant1, audioData1);
    var processed2 = roomAudioProcessor.ProcessAudioFrame(participant2, audioData2);

    Console.WriteLine("已对两个参与者的音频应用 3A 处理");

    // 创建音频帧
    var frame1 = new AudioFrame(participant1.Id, processed1, 16000, 1, 1);
    var frame2 = new AudioFrame(participant2.Id, processed2, 16000, 1, 2);

    // 混音（参与者 1 听到参与者 2 的声音）
    var mixedForUser1 = audioMixer.Mix(new[] { frame2 }, participant1.Id);
    Console.WriteLine($"为参与者 1 混音: {mixedForUser1.Length} 采样点");

    // 混音（参与者 2 听到参与者 1 的声音）
    var mixedForUser2 = audioMixer.Mix(new[] { frame1 }, participant2.Id);
    Console.WriteLine($"为参与者 2 混音: {mixedForUser2.Length} 采样点");

    // 使用能量感知混音
    var mixedWithAutoGain = audioMixer.MixWithAutoGain(new[] { frame1, frame2 });
    Console.WriteLine($"能量感知混音: {mixedWithAutoGain.Length} 采样点");
    Console.WriteLine();

    // 示例 5: 参与者离开房间
    Console.WriteLine("示例 5: 参与者离开房间");
    Console.WriteLine("----------------------");

    roomManager.LeaveRoom(room1.Id, participant1.Id);
    Console.WriteLine($"{participant1.Name} 离开 {room1.Name}");
    Console.WriteLine($"{room1.Name} 当前参与者: {room1.ParticipantCount}");
    Console.WriteLine();

    // 示例 6: 查看所有房间
    Console.WriteLine("示例 6: 查看所有房间信息");
    Console.WriteLine("------------------------");

    var allRooms = roomManager.GetAllRooms();
    foreach (var room in allRooms)
    {
        Console.WriteLine($"房间: {room.Name} (ID: {room.Id})");
        Console.WriteLine($"  状态: {room.State}");
        Console.WriteLine($"  参与者数: {room.ParticipantCount}");
        Console.WriteLine($"  支持协议: {room.SupportedProtocols}");
        Console.WriteLine($"  音频配置: AEC={room.AudioConfig.EnableAec}, AGC={room.AudioConfig.EnableAgc}, ANS={room.AudioConfig.EnableAns}");
        
        foreach (var p in room.Participants)
        {
            Console.WriteLine($"    - {p.Name} ({p.State})");
        }
        Console.WriteLine();
    }

    // 示例 7: 清理空房间
    Console.WriteLine("示例 7: 清理操作");
    Console.WriteLine("----------------");

    // 让所有参与者离开
    roomManager.LeaveRoom(room1.Id, participant2.Id);
    roomManager.LeaveRoom(room2.Id, participant3.Id);

    Console.WriteLine("所有参与者已离开房间");

    var cleanedCount = roomManager.CleanupEmptyRooms();
    Console.WriteLine($"已清理 {cleanedCount} 个空房间");
    Console.WriteLine($"剩余活跃房间数: {roomManager.ActiveRoomCount}");
    Console.WriteLine();

    logger.LogInformation("演示完成");
    Console.WriteLine("\n演示完成！");
    Console.WriteLine("\n功能总结:");
    Console.WriteLine("✓ 支持多房间管理");
    Console.WriteLine("✓ 支持多人同时在线");
    Console.WriteLine("✓ 集成 3A 音频处理");
    Console.WriteLine("✓ 支持音频混音");
    Console.WriteLine("✓ 支持 WebSocket 协议");
    Console.WriteLine("✓ 可扩展 WebRTC 协议");
}

// 辅助方法：生成正弦波音频数据
static short[] GenerateSineWave(int sampleCount, int sampleRate, double frequency, float amplitude, Random random)
{
    var samples = new short[sampleCount];
    for (int i = 0; i < sampleCount; i++)
    {
        // 正弦波 + 轻微噪声
        var signal = amplitude * Math.Sin(2 * Math.PI * frequency * i / sampleRate);
        var noise = 0.05f * (random.NextDouble() * 2 - 1);
        var value = (signal + noise) * short.MaxValue;
        samples[i] = (short)Math.Clamp(value, short.MinValue, short.MaxValue);
    }
    return samples;
}
