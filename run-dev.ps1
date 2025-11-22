# Audio3A 实时语音通话管理系统启动脚本

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Audio3A 实时语音通话管理系统" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# 检查 .NET SDK
$dotnetVersion = dotnet --version 2>$null
if (-not $dotnetVersion) {
    Write-Host "错误：未找到 .NET SDK" -ForegroundColor Red
    Write-Host "请安装 .NET 8.0 SDK: https://dotnet.microsoft.com/download"
    exit 1
}

Write-Host "启动 WebAPI 后端..." -ForegroundColor Green
$apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run" -WorkingDirectory "samples\Audio3A.WebApi" -PassThru -WindowStyle Normal

Write-Host "等待 API 启动..."
Start-Sleep -Seconds 5

Write-Host ""
Write-Host "启动 Blazor Web 前端..." -ForegroundColor Green
$webProcess = Start-Process -FilePath "dotnet" -ArgumentList "run" -WorkingDirectory "samples\Audio3A.Web" -PassThru -WindowStyle Normal

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "✓ 系统启动成功！" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "访问地址：" -ForegroundColor Yellow
Write-Host "  Web 应用: https://localhost:5001" -ForegroundColor White
Write-Host "  API 文档: https://localhost:7063/swagger" -ForegroundColor White
Write-Host ""
Write-Host "按任意键停止服务..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

Write-Host ""
Write-Host "正在停止服务..." -ForegroundColor Yellow
Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
Stop-Process -Id $webProcess.Id -Force -ErrorAction SilentlyContinue

Write-Host "服务已停止。" -ForegroundColor Green
