# Audio3A 完整运行指南

本指南将帮助你在本地运行完整的 Audio3A 实时语音通话管理系统，包括 WebAPI 后端和 Blazor Web 前端。

## 快速开始

### 前提条件
- .NET 8.0 SDK
- 现代浏览器（Chrome, Edge, Firefox 等）

### 方式 1：使用脚本一键启动（推荐）

#### Windows (PowerShell)
```powershell
# 在项目根目录运行
.\run-dev.ps1
```

#### Linux/macOS (Bash)
```bash
# 在项目根目录运行
chmod +x run-dev.sh
./run-dev.sh
```

### 方式 2：手动启动

#### 第 1 步：启动 WebAPI 后端

在第一个终端窗口：

```bash
cd samples/Audio3A.WebApi
dotnet run
```

WebAPI 将运行在：
- HTTPS: `https://localhost:7063`
- HTTP: `http://localhost:5273`
- Swagger UI: `https://localhost:7063/swagger`

#### 第 2 步：启动 Blazor Web 前端

在第二个终端窗口：

```bash
cd samples/Audio3A.Web
dotnet run
```

Web 应用将运行在：
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`

#### 第 3 步：访问应用

打开浏览器访问：`https://localhost:5001`

## 功能验证

### 1. 查看控制台
- 访问首页，查看系统统计信息
- 确认显示房间数量和参与者数量

### 2. 创建房间
1. 点击左侧菜单的「创建房间」
2. 输入房间名称（例如：技术讨论会）
3. 设置最大参与者数（可选，0 表示无限制）
4. 配置 3A 音频处理选项：
   - ✓ 启用回声消除 (AEC)
   - ✓ 启用自动增益控制 (AGC)
   - ✓ 启用噪声抑制 (ANS)
5. 点击「创建房间」按钮

### 3. 查看房间列表
1. 点击左侧菜单的「房间管理」
2. 查看已创建的房间卡片
3. 每个卡片显示：
   - 房间名称
   - 房间 ID（前 8 位）
   - 状态（Active/Closed）
   - 参与者数量
   - 创建时间

### 4. 加入房间
1. 在房间列表中点击「查看详情」
2. 在房间详情页面，输入您的名称
3. 点击「加入房间」按钮
4. 查看参与者列表中是否出现您的名字

### 5. 查看房间详情
在房间详情页面可以看到：
- 房间基本信息
- 参与者列表
- 参与者状态
- 3A 处理是否启用

### 6. 移除参与者
1. 在参与者列表中找到要移除的参与者
2. 点击「移除」按钮
3. 确认移除操作

### 7. 删除房间
1. 在房间列表中找到要删除的房间
2. 点击「删除」按钮
3. 确认删除操作

## API 测试

### 使用 Swagger UI

访问：`https://localhost:7063/swagger`

可以直接测试所有 API 端点：

1. **GET /api/rooms/stats** - 获取统计信息
2. **GET /api/rooms** - 获取所有房间
3. **POST /api/rooms** - 创建房间
4. **GET /api/rooms/{id}** - 获取房间详情
5. **DELETE /api/rooms/{id}** - 删除房间
6. **POST /api/rooms/{id}/participants** - 加入房间
7. **DELETE /api/rooms/{id}/participants/{pid}** - 离开房间

### 使用 curl

```bash
# 获取统计信息
curl https://localhost:7063/api/rooms/stats -k

# 创建房间
curl -X POST https://localhost:7063/api/rooms -k \
  -H "Content-Type: application/json" \
  -d '{
    "name": "测试房间",
    "maxParticipants": 10,
    "enableAec": true,
    "enableAgc": true,
    "enableAns": true
  }'

# 获取所有房间
curl https://localhost:7063/api/rooms -k

# 加入房间（替换 {roomId} 为实际房间 ID）
curl -X POST https://localhost:7063/api/rooms/{roomId}/participants -k \
  -H "Content-Type: application/json" \
  -d '{
    "name": "测试用户"
  }'
```

## 配置说明

### API 地址配置

Web 应用的 API 地址在 `samples/Audio3A.Web/wwwroot/appsettings.json` 中配置：

```json
{
  "ApiBaseUrl": "https://localhost:7063"
}
```

如果 API 运行在不同的地址，请修改此配置。

### CORS 配置

WebAPI 已配置允许所有跨域请求（仅用于开发）。生产环境请在 `samples/Audio3A.WebApi/Program.cs` 中修改 CORS 策略。

## 故障排查

### Web 应用无法连接到 API

**症状**：控制台显示 0 个房间，创建房间失败

**解决方法**：
1. 确认 WebAPI 正在运行（访问 `https://localhost:7063/swagger`）
2. 检查浏览器控制台的错误信息
3. 确认 `appsettings.json` 中的 API 地址正确
4. 如果是 HTTPS 证书问题，先访问 API 地址信任证书

### 端口冲突

**症状**：启动时提示端口已被占用

**解决方法**：
1. 修改 `samples/Audio3A.WebApi/Properties/launchSettings.json` 中的端口
2. 修改 `samples/Audio3A.Web/wwwroot/appsettings.json` 中的 API 地址
3. 或者关闭占用端口的程序

### 浏览器 CORS 错误

**症状**：浏览器控制台显示 CORS 错误

**解决方法**：
1. 确认 WebAPI 已启动
2. 确认 WebAPI 的 CORS 配置正确
3. 清除浏览器缓存并刷新

### SSL 证书警告

**症状**：浏览器显示 SSL 证书不受信任

**解决方法**：
```bash
# 信任开发证书
dotnet dev-certs https --trust
```

## 开发提示

### 热重载

两个项目都支持热重载。修改代码后：
- WebAPI：自动重新编译
- Web 应用：自动刷新浏览器

### 调试

#### Visual Studio
1. 设置多个启动项目
2. 右键解决方案 → 属性 → 启动项目
3. 选择「多个启动项目」
4. 设置 Audio3A.WebApi 和 Audio3A.Web 为「启动」

#### VS Code
使用 `launch.json` 配置多个调试会话。

### 日志

- WebAPI 日志：控制台输出
- Web 应用日志：浏览器开发者工具控制台

## 下一步

完成基本验证后，可以：

1. **添加实时通信**
   - 集成 SignalR 实现实时更新
   - 房间状态自动刷新

2. **音频功能**
   - 浏览器麦克风采集
   - WebRTC 音频通话
   - 实时 3A 处理

3. **增强功能**
   - 用户认证
   - 房间权限管理
   - 聊天消息

## 相关文档

- [Web 项目 README](samples/Audio3A.Web/README.md)
- [房间管理 README](src/Audio3A.RoomManagement/README.md)
- [部署指南](.github/DEPLOYMENT.md)

## 需要帮助？

- 查看代码注释
- 查看 Swagger API 文档
- 检查浏览器控制台错误
- 查看 WebAPI 控制台日志
