#!/bin/bash

echo "====================================="
echo "Audio3A 实时语音通话管理系统"
echo "====================================="
echo ""

# 检查 .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo "错误：未找到 .NET SDK"
    echo "请安装 .NET 8.0 SDK: https://dotnet.microsoft.com/download"
    exit 1
fi

echo "启动 WebAPI 后端..."
cd samples/Audio3A.WebApi
dotnet run &
API_PID=$!
cd ../..

echo "等待 API 启动..."
sleep 5

echo ""
echo "启动 Blazor Web 前端..."
cd samples/Audio3A.Web
dotnet run &
WEB_PID=$!
cd ../..

echo ""
echo "====================================="
echo "✓ 系统启动成功！"
echo "====================================="
echo ""
echo "访问地址："
echo "  Web 应用: https://localhost:5001"
echo "  API 文档: https://localhost:7063/swagger"
echo ""
echo "按 Ctrl+C 停止服务..."
echo ""

# 捕获 Ctrl+C 信号
trap "echo ''; echo '正在停止服务...'; kill $API_PID $WEB_PID 2>/dev/null; exit" INT

# 等待进程
wait
