# Audio3A 快速启动指南

## 前置要求

- .NET 8.0 SDK
- Visual Studio 2022 或 VS Code
- 支持麦克风的浏览器（Chrome、Edge、Firefox）

## 快速开始

### 1. 启动后端 API

```powershell
cd samples/Audio3A.WebApi
dotnet run
```

后端将在 `https://localhost:7096` 启动。

Swagger UI 地址：`https://localhost:7096/swagger`

### 2. 启动前端应用

新开一个终端：

```powershell
cd samples/Audio3A.Web
dotnet run
```

前端将在 `https://localhost:7116` 启动。

### 3. 创建房间并测试

1. 打开浏览器访问 `https://localhost:7116`
2. 点击"创建房间"
3. 配置房间设置：
   - 启用 AEC（回声消除）✓
   - 启用 AGC（自动增益）✓
   - 启用 ANS（噪声抑制）✓
4. 创建后进入房间详情页
5. 输入您的名称并加入房间
6. 允许浏览器访问麦克风

### 4. 录音和下载

在房间详情页，您会看到"房间录音"卡片：

1. **开始录音**：点击"开始录音"按钮
2. **开始通话**：点击"开始通话"按钮进入通话界面
3. **说话测试**：对着麦克风说话 30 秒以上
4. **结束通话**：返回房间详情页
5. **停止录音**：点击"停止录音"按钮
6. **下载录音**：录音停止后，会显示下载按钮，点击即可下载 WAV 文件
7. **播放验证**：使用媒体播放器打开下载的文件，检查音质

**注意**：录音功能在房间详情页控制，通话页面只负责实时音频传输。

## 使用 API 测试

### 使用 PowerShell 测试完整流程

```powershell
# 设置变量
$baseUrl = "https://localhost:7096"

# 1. 创建房间
$createRoom = @{
    name = "测试房间"
    enableAec = $true
    enableAgc = $true
    enableAns = $true
    maxParticipants = 10
} | ConvertTo-Json

$room = Invoke-RestMethod -Uri "$baseUrl/api/rooms" `
    -Method Post `
    -ContentType "application/json" `
    -Body $createRoom `
    -SkipCertificateCheck

Write-Host "✓ 房间已创建: $($room.id)" -ForegroundColor Green
$roomId = $room.id

# 2. 加入房间
$joinRoom = @{
    name = "测试用户"
} | ConvertTo-Json

$participant = Invoke-RestMethod -Uri "$baseUrl/api/rooms/$roomId/participants" `
    -Method Post `
    -ContentType "application/json" `
    -Body $joinRoom `
    -SkipCertificateCheck

Write-Host "✓ 参与者已加入: $($participant.id)" -ForegroundColor Green
$participantId = $participant.id

# 3. 开始录音
Invoke-RestMethod -Uri "$baseUrl/api/rooms/$roomId/recording/start" `
    -Method Post `
    -SkipCertificateCheck | Out-Null

Write-Host "✓ 录音已开始" -ForegroundColor Green

# 4. 打开前端页面进行实际通话
Write-Host "`n在浏览器中打开:" -ForegroundColor Yellow
Write-Host "https://localhost:7116/rooms/$roomId" -ForegroundColor Cyan
Write-Host "`n1. 输入名称加入房间" -ForegroundColor Yellow
Write-Host "2. 点击'开始录音'按钮" -ForegroundColor Yellow
Write-Host "3. 点击'开始通话'按钮" -ForegroundColor Yellow
Write-Host "4. 说话 30 秒后按回车继续..." -ForegroundColor Yellow
Read-Host

# 5. 停止录音
$result = Invoke-RestMethod -Uri "$baseUrl/api/rooms/$roomId/recording/stop" `
    -Method Post `
    -SkipCertificateCheck

Write-Host "✓ 录音已停止: $($result.filePath)" -ForegroundColor Green

# 6. 下载录音文件
$fileName = Split-Path $result.filePath -Leaf
$outputFile = "recording_$fileName"

Invoke-RestMethod -Uri "$baseUrl/api/rooms/recordings/$fileName" `
    -Method Get `
    -OutFile $outputFile `
    -SkipCertificateCheck

Write-Host "✓ 录音文件已下载: $outputFile" -ForegroundColor Green
Write-Host "`n用媒体播放器打开文件验证 3A 处理效果" -ForegroundColor Yellow
```

### 使用 curl 测试

```bash
# 创建房间
curl -k -X POST https://localhost:7096/api/rooms \
  -H "Content-Type: application/json" \
  -d '{"name":"测试房间","enableAec":true,"enableAgc":true,"enableAns":true}'

# 开始录音
curl -k -X POST https://localhost:7096/api/rooms/{roomId}/recording/start

# 停止录音
curl -k -X POST https://localhost:7096/api/rooms/{roomId}/recording/stop

# 下载录音
curl -k -O https://localhost:7096/api/rooms/recordings/{fileName}
```

## 验证 Audio3A 功能

### 测试回声消除 (AEC)

1. 使用扬声器播放音乐
2. 同时对着麦克风说话
3. 停止录音并播放文件
4. **预期结果**：音乐声应该被消除，只保留人声

### 测试自动增益 (AGC)

1. 先小声说话（距离麦克风远）
2. 再大声说话（距离麦克风近）
3. 停止录音并播放文件
4. **预期结果**：音量应该保持相对稳定

### 测试噪声抑制 (ANS)

1. 在嘈杂环境中说话（开风扇、放白噪音）
2. 停止录音并播放文件
3. **预期结果**：背景噪声应该明显减少

## 故障排查

### 问题：浏览器无法访问麦克风

**解决方案**：
- 确保使用 HTTPS（localhost 除外）
- 在浏览器设置中允许麦克风权限
- Chrome：`chrome://settings/content/microphone`
- Edge：`edge://settings/content/microphone`

### 问题：WebSocket 连接失败

**解决方案**：
1. 检查后端是否正常运行：`curl -k https://localhost:7096/api/rooms`
2. 检查防火墙设置
3. 查看浏览器控制台错误信息
4. 查看后端日志输出

### 问题：录音文件为空

**解决方案**：
1. 检查麦克风是否正常工作
2. 查看后端日志确认音频数据是否接收
3. 确认录音在接收音频数据后才停止
4. 检查 `recordings/` 目录权限

### 问题：音质很差

**可能原因**：
1. 麦克风质量差
2. 网络延迟导致音频丢包
3. CPU 占用过高导致处理延迟

**解决方案**：
- 使用更好的麦克风
- 检查网络连接质量
- 降低 3A 处理强度（部分禁用）

## 性能监控

### 查看日志

后端日志会显示：
```
[Information] WebSocket 连接建立: RoomId=xxx, ParticipantId=yyy
[Debug] 收到音频数据: 2048 个采样点
[Debug] 音频已添加到房间: RoomId=xxx, ParticipantId=yyy
[Information] 开始录音: RoomId=xxx, File=recordings/xxx_xxx.wav
[Information] 录音完成: Samples=960000, Duration=20s
```

### 性能指标

正常运行时：
- **CPU 使用率**：每个参与者 5-10%
- **内存使用**：每个参与者 5-10 MB
- **网络带宽**：每个参与者 192 KB/s
- **延迟**：< 50ms

## 运行测试

```powershell
# 运行所有测试
dotnet test

# 只运行特定测试
cd tests/Audio3A.Tests
dotnet test

cd tests/Audio3A.RoomManagement.Tests
dotnet test
```

预期结果：
```
测试摘要: 总计: 51, 失败: 0, 成功: 51, 已跳过: 0
```

## 下一步

- 查看 [RECORDING_GUIDE.md](RECORDING_GUIDE.md) 了解详细使用说明
- 查看 [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md) 了解实现细节
- 查看 [ARCHITECTURE.md](../samples/Audio3A.Web/ARCHITECTURE.md) 了解系统架构

## 技术支持

如有问题，请查看：
1. 后端日志输出
2. 浏览器控制台错误
3. Swagger UI 测试 API 是否正常
4. GitHub Issues

## 许可证

本项目使用 MIT 许可证。详见 [LICENSE](../LICENSE) 文件。
