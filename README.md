# Audio3A_CSharp

一个基于 .NET 8 的原生音频 3A (AEC, AGC, ANS) 处理 SDK。

## 功能特性

Audio 3A SDK 提供了三种核心的音频处理算法：

- **AEC (Acoustic Echo Cancellation)** - 回声消除：消除扬声器播放引起的麦克风回声
- **AGC (Automatic Gain Control)** - 自动增益控制：自动调节音频音量，保持一致的输出电平
- **ANS (Automatic Noise Suppression)** - 自动噪声抑制：减少背景噪声，同时保留语音

## 系统要求

- .NET 8.0 或更高版本

## 项目结构

```
Audio3A_CSharp/
├── src/
│   └── Audio3A.Core/          # 核心库
│       ├── AudioBuffer.cs      # 音频缓冲区
│       ├── AudioFormat.cs      # 音频格式
│       ├── Audio3AConfig.cs    # 配置类
│       ├── Audio3AProcessor.cs # 主处理器
│       ├── IAudioProcessor.cs  # 处理器接口
│       └── Processors/         # 3A 算法实现
│           ├── AecProcessor.cs # 回声消除
│           ├── AgcProcessor.cs # 自动增益控制
│           └── AnsProcessor.cs # 噪声抑制
├── samples/
│   └── Audio3A.Demo/          # 示例程序
└── tests/
    └── Audio3A.Tests/         # 单元测试
```

## 快速开始

### 1. 基本使用

```csharp
using Audio3A.Core;

// 创建配置
var config = new Audio3AConfig
{
    EnableAec = true,
    EnableAgc = true,
    EnableAns = true,
    SampleRate = 16000,
    Channels = 1
};

// 创建处理器
using var processor = new Audio3AProcessor(config);

// 处理音频数据（浮点格式）
float[] inputSamples = new float[160]; // 10ms @ 16kHz
// ... 填充音频数据 ...

var inputBuffer = new AudioBuffer(inputSamples);
var outputBuffer = processor.Process(inputBuffer);

// 获取处理后的数据
float[] outputSamples = outputBuffer.Samples;
```

### 2. 处理 16 位 PCM 数据

```csharp
using Audio3A.Core;

using var processor = new Audio3AProcessor();

// 直接处理 16 位 PCM 数据
short[] inputPcm = new short[160];
// ... 从麦克风获取 PCM 数据 ...

short[] outputPcm = processor.ProcessInt16(inputPcm);
```

### 3. 使用回声消除（带参考信号）

```csharp
using Audio3A.Core;

using var processor = new Audio3AProcessor();

// 麦克风输入
short[] micInputPcm = new short[160];

// 扬声器参考信号（播放的音频）
short[] speakerReferencePcm = new short[160];

// 处理，AEC 会消除扬声器引起的回声
short[] outputPcm = processor.ProcessInt16(micInputPcm, speakerReferencePcm);
```

### 4. 自定义配置

```csharp
using Audio3A.Core;

var config = new Audio3AConfig
{
    // 启用/禁用各个算法
    EnableAec = true,
    EnableAgc = true,
    EnableAns = true,

    // 音频格式
    SampleRate = 16000,
    Channels = 1,
    FrameSize = 160,

    // AGC 参数
    AgcTargetLevel = 0.5f,        // 目标电平 (0.0-1.0)
    AgcCompressionRatio = 3.0f,   // 压缩比

    // ANS 参数
    AnsNoiseReductionDb = 20.0f,  // 噪声抑制强度 (dB)

    // AEC 参数
    AecFilterLength = 512,        // 滤波器长度
    AecStepSize = 0.01f           // 自适应步长
};

using var processor = new Audio3AProcessor(config);
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
var config = new Audio3AConfig
{
    SampleRate = 16000,
    FrameSize = 160,  // 10ms
    EnableAec = true,  // 消除回声
    EnableAgc = true,  // 稳定音量
    EnableAns = true   // 降噪
};

using var processor = new Audio3AProcessor(config);

// 在音频采集回调中
void OnAudioCaptured(short[] micData, short[] speakerData)
{
    short[] processed = processor.ProcessInt16(micData, speakerData);
    // 发送处理后的音频到网络
}
```

### 录音降噪

```csharp
var config = new Audio3AConfig
{
    EnableAec = false,  // 录音不需要回声消除
    EnableAgc = true,   // 稳定音量
    EnableAns = true,   // 降噪
    AnsNoiseReductionDb = 25.0f  // 较强的降噪
};

using var processor = new Audio3AProcessor(config);

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
