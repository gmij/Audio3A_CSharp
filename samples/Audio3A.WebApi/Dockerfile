# 使用官方 .NET 8 SDK 镜像进行构建
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 复制解决方案和项目文件
COPY ["Audio3A.sln", "./"]
COPY ["src/Audio3A.Core/Audio3A.Core.csproj", "src/Audio3A.Core/"]
COPY ["src/Audio3A.RoomManagement/Audio3A.RoomManagement.csproj", "src/Audio3A.RoomManagement/"]
COPY ["samples/Audio3A.WebApi/Audio3A.WebApi.csproj", "samples/Audio3A.WebApi/"]

# 恢复依赖
RUN dotnet restore "samples/Audio3A.WebApi/Audio3A.WebApi.csproj"

# 复制所有源代码
COPY . .

# 构建项目
WORKDIR "/src/samples/Audio3A.WebApi"
RUN dotnet build "Audio3A.WebApi.csproj" -c Release -o /app/build

# 发布项目
FROM build AS publish
RUN dotnet publish "Audio3A.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 使用 ASP.NET Core 运行时镜像
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# 设置环境变量
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

# 暴露端口
EXPOSE 80
EXPOSE 443

# 复制发布的文件
COPY --from=publish /app/publish .

# 设置入口点
ENTRYPOINT ["dotnet", "Audio3A.WebApi.dll"]
