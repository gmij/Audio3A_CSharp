# GitHub Pages 部署说明

本项目已配置自动部署 Blazor WebAssembly 应用到 GitHub Pages。

## 启用 GitHub Pages

### 步骤 1: 配置 Pages 设置
1. 进入仓库 **Settings** 页面
2. 在左侧菜单找到 **Pages** 选项
3. 在 **Source** 下拉框选择 **GitHub Actions**
4. 保存设置

### 步骤 2: 触发部署
部署会在以下情况自动触发：
- 推送到 `main` 分支
- 推送到 `copilot/add-room-management-capabilities` 分支
- 手动触发（Actions 标签 → "Deploy Web App to GitHub Pages" → "Run workflow"）

## 查看部署状态

1. 进入仓库的 **Actions** 标签
2. 查看 "Deploy Web App to GitHub Pages" 工作流
3. 点击最新的运行查看详细日志

## 访问部署的应用

部署成功后，应用将在以下地址可用：
```
https://<username>.github.io/Audio3A_CSharp/
```

例如：`https://gmij.github.io/Audio3A_CSharp/`

## 工作流说明

工作流文件：`.github/workflows/deploy-web.yml`

### 构建步骤
1. **Checkout** - 检出代码
2. **Setup .NET** - 安装 .NET 8 SDK
3. **Restore** - 恢复 Web 项目依赖
4. **Build** - 构建 Release 版本
5. **Publish** - 发布 Blazor 应用
6. **Change base-tag** - 修改 index.html 的 base 标签以适配 GitHub Pages 子路径
7. **Copy 404.html** - 复制 404.html 用于 SPA 路由支持
8. **Add .nojekyll** - 添加 .nojekyll 文件防止 Jekyll 处理
9. **Upload artifact** - 上传构建产物

### 部署步骤
1. **Deploy to GitHub Pages** - 部署到 GitHub Pages

### SPA 路由支持

Blazor WebAssembly 是单页应用（SPA），所有路由都在客户端处理。为了在 GitHub Pages 上正确支持客户端路由：

1. **404.html** - 当用户直接访问子路由（如 `/Audio3A_CSharp/rooms`）时，GitHub Pages 会返回 404 错误。404.html 会捕获这个请求并重定向到 index.html，同时保留原始路径。

2. **index.html 重定向脚本** - index.html 中的脚本会检测从 404.html 传来的重定向参数，并使用 `history.replaceState` 恢复原始 URL，这样 Blazor Router 就能正确处理路由。

3. **base 标签** - 设置为 `/Audio3A_CSharp/` 确保所有资源路径正确。

这种方案使得用户可以：
- 直接访问任何页面 URL（如 `https://gmij.github.io/Audio3A_CSharp/rooms`）
- 刷新页面不会丢失当前路由
- 在应用内导航正常工作

## 本地测试部署构建

```bash
# 1. 恢复依赖
dotnet restore samples/Audio3A.Web/Audio3A.Web.csproj

# 2. 构建 Release 版本
dotnet build samples/Audio3A.Web/Audio3A.Web.csproj --configuration Release --no-restore

# 3. 发布应用
dotnet publish samples/Audio3A.Web/Audio3A.Web.csproj -c Release -o ./publish --nologo

# 4. 修改 base 标签（用于 GitHub Pages）
sed -i 's/<base href="\/" \/>/<base href="\/Audio3A_CSharp\/" \/>/g' ./publish/wwwroot/index.html

# 5. 添加 .nojekyll 文件
touch ./publish/wwwroot/.nojekyll

# 6. 查看发布结果
ls -la ./publish/wwwroot/
```

## 故障排查

### 部署失败
1. 检查 Actions 日志中的错误信息
2. 确保 Pages 设置正确（Source: GitHub Actions）
3. 确保仓库有 Pages 权限

### 应用无法访问
1. 检查 URL 是否正确（包含仓库名称）
2. 等待几分钟让部署完全生效
3. 清除浏览器缓存并刷新

### 页面显示空白
1. 检查浏览器控制台的错误信息
2. 确认 base 标签路径正确
3. 检查资源文件是否正确加载

## 注意事项

- **base 标签**：GitHub Pages 部署的应用需要修改 base 标签以匹配子路径
- **.nojekyll**：添加此文件防止 Jekyll 处理 Blazor 文件
- **API 访问**：发布的 Web 应用为静态文件，需要配置 API 地址到实际的后端服务

## 相关文档

- [GitHub Pages 文档](https://docs.github.com/pages)
- [Blazor WebAssembly 部署](https://learn.microsoft.com/aspnet/core/blazor/host-and-deploy/webassembly)
- [GitHub Actions 工作流语法](https://docs.github.com/actions/using-workflows/workflow-syntax-for-github-actions)
