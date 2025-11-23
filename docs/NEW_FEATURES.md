# 新增功能说明

## 概述

本次更新实现了两个主要功能：

1. **服务端房间管理集成** - 将 Web 前端从浏览器内存模拟切换到真实的 WebAPI 后端
2. **实时波形可视化** - 在通话界面显示输入音频和 3A 处理后的波形图

## 1. 服务端房间管理

### 功能描述

之前的 Web 应用使用 `MockApiService` 在浏览器内存中模拟房间管理功能，现在已经升级支持连接真实的 WebAPI 后端服务。

### 配置方式

#### appsettings.json

```json
{
  "ApiBaseUrl": "https://localhost:7063",
  "UseMockApi": false
}
```

**配置项说明**：
- `ApiBaseUrl`: WebAPI 后端地址
- `UseMockApi`: 
  - `false` - 使用真实 API（连接服务端，默认）
  - `true` - 使用 Mock API（浏览器内存，用于 GitHub Pages）

## 2. 实时波形可视化

### 功能描述

在语音通话界面显示两个实时波形图：
- **输入音频波形**（蓝色）- 显示从麦克风采集的原始音频
- **3A 处理后波形**（绿色）- 显示经过回声消除、增益控制、噪声抑制后的音频

### 使用效果

1. **启动通话**：点击"开始通话"按钮
2. **授权麦克风**：浏览器会请求麦克风权限
3. **查看波形**：
   - 上方显示输入音频的实时波形（蓝色）
   - 下方显示 3A 处理后的波形（绿色）
4. **静音功能**：点击静音按钮时，波形停止更新

## 部署说明

### 开发环境（本地测试）

1. 修改 `samples/Audio3A.Web/wwwroot/appsettings.json`：
   ```json
   {
     "ApiBaseUrl": "https://localhost:7063",
     "UseMockApi": false
   }
   ```

2. 启动服务：
   ```bash
   # 终端 1
   cd samples/Audio3A.WebApi
   dotnet run

   # 终端 2
   cd samples/Audio3A.Web
   dotnet run
   ```

3. 访问 `https://localhost:5001`

