# Audio3A 录音功能使用指南

## 概述

本项目已实现完整的 Audio3A 音频处理和房间录音功能。音频流从客户端采集，通过 WebSocket 传输到后端，应用 3A 处理（回声消除、自动增益、噪声抑制），最后保存到房间录音文件。

## 架构

```
前端 (Blazor WASM)
    ↓ [采集音频 48kHz]
audioCall.js
    ↓ [Float32Array]
AudioWebSocketService
    ↓ [WebSocket JSON]
Audio3A.WebApi (/ws/audio)
    ↓
AudioWebSocketHandler
    ↓ [应用 3A 处理: AEC → ANS → AGC]
Audio3AProcessor
    ↓ [处理后的音频]
Room.AddParticipantAudio()
    ↓
RoomAudioRecorder (WAV 文件)
```

## 使用流程

### 1. 启动后端服务

```powershell
cd samples/Audio3A.WebApi
dotnet run
```

后端将在 `https://localhost:7096` 启动。

### 2. 启动前端应用

```powershell
cd samples/Audio3A.Web
dotnet run
```

前端将在 `https://localhost:7116` 启动。

### 3. 创建房间

使用 Swagger UI 或 API 创建房间：

```bash
POST https://localhost:7096/api/rooms
Content-Type: application/json

{
  "name": "测试房间",
  "enableAec": true,
  "enableAgc": true,
  "enableAns": true,
  "maxParticipants": 10
}
```

响应示例：
```json
{
  "id": "abc123",
  "name": "测试房间",
  "state": "Active",
  "participantCount": 0,
  "maxParticipants": 10,
  "createdAt": "2025-11-23T10:00:00Z"
}
```

### 4. 加入房间

```bash
POST https://localhost:7096/api/rooms/{roomId}/participants
Content-Type: application/json

{
  "name": "用户1"
}
```

响应示例：
```json
{
  "id": "participant123",
  "name": "用户1",
  "state": "Connected",
  "joinedAt": "2025-11-23T10:01:00Z",
  "enable3A": true
}
```

### 5. 开始录音

```bash
POST https://localhost:7096/api/rooms/{roomId}/recording/start
```

响应：
```json
{
  "message": "录音已开始"
}
```

**或者在 Web 界面操作**：
1. 访问 `https://localhost:7116/rooms/{roomId}`
2. 在"房间录音"卡片中点击"开始录音"按钮

### 6. 建立 WebSocket 连接并发送音频

前端自动建立 WebSocket 连接：

```
wss://localhost:7096/ws/audio?roomId={roomId}&participantId={participantId}
```

发送音频数据格式（JSON）：
```json
{
  "audioData": [0.1, 0.2, -0.1, ...] // Float32Array 转换为 JSON 数组
}
```

### 7. 停止录音并下载

停止录音：
```bash
POST https://localhost:7096/api/rooms/{roomId}/recording/stop
```

响应：
```json
{
  "message": "录音已停止",
  "filePath": "recordings/abc123_20251123_100500.wav"
}
```

**或者在 Web 界面操作**：
1. 在"房间录音"卡片中点击"停止录音"按钮
2. 录音停止后会显示下载按钮
3. 点击"下载录音"按钮即可下载 WAV 文件

下载录音文件（API 方式）：
```bash
GET https://localhost:7096/api/rooms/recordings/abc123_20251123_100500.wav
```

## API 端点总览

### 房间管理

| 方法 | 端点 | 说明 |
|------|------|------|
| GET | `/api/rooms` | 获取所有房间 |
| GET | `/api/rooms/{roomId}` | 获取指定房间 |
| POST | `/api/rooms` | 创建房间 |
| DELETE | `/api/rooms/{roomId}` | 删除房间 |
| GET | `/api/rooms/stats` | 获取统计信息 |

### 参与者管理

| 方法 | 端点 | 说明 |
|------|------|------|
| POST | `/api/rooms/{roomId}/participants` | 加入房间 |
| DELETE | `/api/rooms/{roomId}/participants/{participantId}` | 离开房间 |

### 录音管理

| 方法 | 端点 | 说明 |
|------|------|------|
| POST | `/api/rooms/{roomId}/recording/start` | 开始录音 |
| POST | `/api/rooms/{roomId}/recording/stop` | 停止录音 |
| GET | `/api/rooms/{roomId}/recording/status` | 获取录音状态 |
| GET | `/api/rooms/recordings/{fileName}` | 下载录音文件 |

### WebSocket

| 端点 | 说明 |
|------|------|
| `/ws/audio?roomId={id}&participantId={id}` | 音频流 WebSocket |

## Audio3A 配置

在创建房间时可以配置 3A 处理：

```json
{
  "name": "房间名称",
  "enableAec": true,  // 回声消除 (Echo Cancellation)
  "enableAgc": true,  // 自动增益控制 (Automatic Gain Control)
  "enableAns": true,  // 噪声抑制 (Noise Suppression)
  "maxParticipants": 10
}
```

### 处理顺序

默认使用 WebRTC 推荐的处理顺序：**AEC → ANS → AGC**

1. **AEC (回声消除)**：首先移除回声，避免影响后续处理
2. **ANS (噪声抑制)**：抑制背景噪声
3. **AGC (自动增益)**：调整音量到合适的水平

## 录音文件格式

- **格式**：WAV (PCM)
- **采样率**：48000 Hz（与前端采集一致）
- **通道数**：1（单声道）
- **位深度**：16-bit
- **文件命名**：`{roomId}_{timestamp}.wav`
- **存储位置**：`recordings/` 目录

## 测试示例

### 完整测试流程（PowerShell）

```powershell
# 1. 创建房间
$createRoom = @{
    name = "测试房间"
    enableAec = $true
    enableAgc = $true
    enableAns = $true
    maxParticipants = 10
} | ConvertTo-Json

$room = Invoke-RestMethod -Uri "https://localhost:7096/api/rooms" `
    -Method Post `
    -ContentType "application/json" `
    -Body $createRoom `
    -SkipCertificateCheck

$roomId = $room.id
Write-Host "房间已创建: $roomId"

# 2. 加入房间
$joinRoom = @{
    name = "测试用户"
} | ConvertTo-Json

$participant = Invoke-RestMethod -Uri "https://localhost:7096/api/rooms/$roomId/participants" `
    -Method Post `
    -ContentType "application/json" `
    -Body $joinRoom `
    -SkipCertificateCheck

$participantId = $participant.id
Write-Host "参与者已加入: $participantId"

# 3. 开始录音
Invoke-RestMethod -Uri "https://localhost:7096/api/rooms/$roomId/recording/start" `
    -Method Post `
    -SkipCertificateCheck
Write-Host "录音已开始"

# 4. 等待一段时间（实际应用中，用户在此期间说话）
Start-Sleep -Seconds 30

# 5. 停止录音
$result = Invoke-RestMethod -Uri "https://localhost:7096/api/rooms/$roomId/recording/stop" `
    -Method Post `
    -SkipCertificateCheck
Write-Host "录音已停止: $($result.filePath)"

# 6. 下载录音文件
$fileName = Split-Path $result.filePath -Leaf
Invoke-RestMethod -Uri "https://localhost:7096/api/rooms/recordings/$fileName" `
    -Method Get `
    -OutFile "downloaded_recording.wav" `
    -SkipCertificateCheck
Write-Host "录音文件已下载: downloaded_recording.wav"
```

## 验证 Audio3A 功能

### 验证回声消除 (AEC)

1. 在有扬声器的环境中测试
2. 播放音乐，同时说话
3. 检查录音文件，音乐声应该被消除

### 验证自动增益 (AGC)

1. 先小声说话，再大声说话
2. 检查录音文件，音量应该保持相对稳定

### 验证噪声抑制 (ANS)

1. 在嘈杂环境中说话
2. 检查录音文件，背景噪声应该明显减少

## 注意事项

1. **采样率转换**：前端采集使用 48kHz，后端 Audio3A 默认配置为 16kHz，需要注意采样率匹配问题
2. **延迟**：WebSocket 传输 + 3A 处理会引入一定延迟（通常 < 100ms）
3. **资源占用**：3A 处理是 CPU 密集型操作，高并发时注意服务器资源
4. **文件管理**：录音文件会累积，需要定期清理旧文件
5. **并发录音**：每个房间独立录音，多房间同时录音会增加磁盘 I/O

## 故障排查

### 问题：录音文件为空或很小

- **原因**：可能没有音频数据发送到房间
- **解决**：检查 WebSocket 连接日志，确认音频数据正在发送

### 问题：录音有回声

- **原因**：AEC 未生效
- **解决**：确认创建房间时 `enableAec: true`，检查日志确认 AEC 处理器已加载

### 问题：录音音量太小或太大

- **原因**：AGC 配置问题
- **解决**：调整 `Audio3AConfig` 中的 AGC 参数

### 问题：录音有杂音

- **原因**：ANS 参数不当或音频质量差
- **解决**：调整 ANS 阈值，或检查麦克风质量

## 性能优化建议

1. **采样率统一**：将前端采集改为 16kHz，避免重采样开销
2. **批量处理**：累积多帧音频再处理，减少函数调用
3. **异步 I/O**：使用异步文件写入，避免阻塞
4. **内存池**：重用音频缓冲区，减少 GC 压力
5. **压缩传输**：WebSocket 使用二进制格式代替 JSON

## 下一步改进

- [ ] 添加实时混音功能（多参与者音频实时混合）
- [ ] 支持 MP3/AAC 等压缩格式
- [ ] 添加音频可视化（波形图、频谱图）
- [ ] 实现音频回放功能
- [ ] 添加录音文件列表和管理界面
- [ ] 支持暂停/继续录音
- [ ] 添加录音质量选项（采样率、比特率）

## 相关文档

- [ARCHITECTURE.md](../../samples/Audio3A.Web/ARCHITECTURE.md) - 系统架构设计
- [RUNNING.md](../../RUNNING.md) - 运行指南
- [README.md](../../README.md) - 项目概览
