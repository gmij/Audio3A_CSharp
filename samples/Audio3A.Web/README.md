# Audio3A Web 管理系统

基于 Blazor WebAssembly 和 Ant Design Blazor 构建的实时语音通话管理 Web 界面。

## 功能特性

### 🎨 用户界面
- **现代化设计**：使用 Ant Design Blazor 组件库
- **响应式布局**：适配桌面和移动设备
- **中文界面**：完整的中文本地化

### 📊 核心功能
- **控制台**：实时展示系统统计数据
  - 活跃房间数
  - 总房间数
  - 在线参与者数
  
- **房间管理**：
  - 创建新房间
  - 查看房间列表
  - 删除房间
  - 查看房间详情
  
- **参与者管理**：
  - 加入房间
  - 查看参与者列表
  - 移除参与者
  
- **3A 配置**：
  - 启用/禁用回声消除 (AEC)
  - 启用/禁用自动增益控制 (AGC)
  - 启用/禁用噪声抑制 (ANS)

## 技术栈

- **.NET 8** - 框架版本
- **Blazor WebAssembly** - 前端框架
- **Ant Design Blazor** - UI 组件库
- **Audio3A.WebApi** - 后端 API

## 本地开发

### 前提条件
- .NET 8 SDK
- 现代浏览器（Chrome, Edge, Firefox 等）

### 启动 API 服务
```bash
cd samples/Audio3A.WebApi
dotnet run
```

API 默认运行在 `https://localhost:7001`

### 启动 Web 应用
```bash
cd samples/Audio3A.Web
dotnet run
```

Web 应用默认运行在 `https://localhost:5001`

### 同时启动两个服务
```bash
# 在项目根目录
dotnet run --project samples/Audio3A.WebApi &
dotnet run --project samples/Audio3A.Web
```

## 构建和发布

### 构建 Web 应用
```bash
dotnet build samples/Audio3A.Web/Audio3A.Web.csproj -c Release
```

### 发布 Web 应用
```bash
dotnet publish samples/Audio3A.Web/Audio3A.Web.csproj -c Release -o ./publish
```

发布后的文件在 `./publish/wwwroot` 目录中，可以部署到任何静态 Web 服务器。

## GitHub Pages 部署

项目已配置 GitHub Actions 工作流，可自动部署到 GitHub Pages。

### 部署步骤

1. **启用 GitHub Pages**
   - 进入仓库 Settings
   - 点击 Pages 选项
   - Source 选择 "GitHub Actions"

2. **自动部署**
   - 推送到 `main` 或 `copilot/add-room-management-capabilities` 分支
   - GitHub Actions 会自动构建和部署
   - 部署完成后访问：`https://<username>.github.io/Audio3A_CSharp/`

3. **手动触发部署**
   - 进入 Actions 标签
   - 选择 "Deploy Web App to GitHub Pages"
   - 点击 "Run workflow"

## 页面说明

### 控制台（首页）
- 显示系统概览统计
- 展示功能特性
- 快速开始指南

### 房间管理
- 卡片式展示所有房间
- 实时显示房间状态和参与者数量
- 快速操作（查看详情、删除）

### 创建房间
- 表单输入房间信息
- 配置 3A 音频处理选项
- 实时验证和提示

### 房间详情
- 显示房间完整信息
- 参与者列表和管理
- 加入房间功能

## API 接口

Web 应用通过以下 API 与后端通信：

```csharp
GET    /api/rooms          // 获取所有房间
GET    /api/rooms/{id}     // 获取指定房间
POST   /api/rooms          // 创建房间
DELETE /api/rooms/{id}     // 删除房间
GET    /api/rooms/stats    // 获取统计信息
POST   /api/rooms/{id}/participants      // 加入房间
DELETE /api/rooms/{id}/participants/{pid} // 离开房间
```

详见 `samples/Audio3A.WebApi/Controllers/RoomsController.cs`

## 自定义配置

### API 地址配置
编辑 `wwwroot/appsettings.json`：

```json
{
  "ApiBaseUrl": "https://your-api-server.com"
}
```

### 主题定制
Ant Design Blazor 支持主题定制，参考：
https://antblazor.com/zh-CN/docs/customize-theme

## 浏览器支持

- ✅ Chrome 90+
- ✅ Edge 90+
- ✅ Firefox 88+
- ✅ Safari 14+

## 截图

### 控制台
![控制台](docs/dashboard.png)

### 房间列表
![房间列表](docs/rooms.png)

### 创建房间
![创建房间](docs/create-room.png)

## 故障排查

### 无法连接到 API
- 检查 API 服务是否运行
- 检查 `appsettings.json` 中的 API 地址
- 检查 CORS 配置

### 页面加载失败
- 检查浏览器控制台错误
- 清除浏览器缓存
- 尝试硬刷新（Ctrl+F5）

## 许可证

MIT License
