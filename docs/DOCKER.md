# Docker 部署指南

本文档介绍如何使用 Docker 部署 Audio3A WebAPI 服务。

## 快速开始

### 使用预构建镜像

从 Docker Hub 拉取并运行：

```bash
docker pull gmij/audio3a:latest
docker run -d -p 8080:80 --name audio3a-api gmij/audio3a:latest
```

访问 `http://localhost:8080/swagger` 查看 API 文档。

### 本地构建

从源码构建镜像：

```bash
# 在项目根目录执行
docker build -t audio3a-api -f samples/Audio3A.WebApi/Dockerfile .

# 运行容器
docker run -d -p 8080:80 --name audio3a-api audio3a-api
```

## 环境变量配置

可以通过环境变量配置服务：

```bash
docker run -d -p 8080:80 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS=http://+:80 \
  --name audio3a-api \
  gmij/audio3a:latest
```

### 常用环境变量

| 变量名 | 默认值 | 说明 |
|--------|--------|------|
| `ASPNETCORE_ENVIRONMENT` | Production | 运行环境（Development/Production） |
| `ASPNETCORE_URLS` | http://+:80 | 监听地址和端口 |

## Docker Compose

使用 Docker Compose 部署完整服务栈：

```yaml
version: '3.8'

services:
  audio3a-api:
    image: gmij/audio3a:latest
    ports:
      - "8080:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost/swagger"]
      interval: 30s
      timeout: 10s
      retries: 3
```

保存为 `docker-compose.yml` 后运行：

```bash
docker-compose up -d
```

## 持久化数据

如果需要持久化数据，可以挂载卷：

```bash
docker run -d -p 8080:80 \
  -v audio3a-data:/app/data \
  --name audio3a-api \
  gmij/audio3a:latest
```

## 健康检查

检查容器状态：

```bash
# 查看容器日志
docker logs audio3a-api

# 检查容器状态
docker ps | grep audio3a-api

# 测试 API 可用性
curl http://localhost:8080/api/rooms/stats
```

## 生产环境部署

### 使用 HTTPS

推荐使用反向代理（如 Nginx 或 Traefik）提供 HTTPS：

```nginx
server {
    listen 443 ssl http2;
    server_name api.audio3a.example.com;

    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;

    location / {
        proxy_pass http://localhost:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### 资源限制

限制容器资源使用：

```bash
docker run -d \
  -p 8080:80 \
  --memory="512m" \
  --cpus="1.0" \
  --name audio3a-api \
  gmij/audio3a:latest
```

### 自动重启

设置容器自动重启策略：

```bash
docker run -d \
  -p 8080:80 \
  --restart=unless-stopped \
  --name audio3a-api \
  gmij/audio3a:latest
```

## 故障排查

### 容器无法启动

```bash
# 查看详细日志
docker logs --tail 100 audio3a-api

# 进入容器检查
docker exec -it audio3a-api /bin/bash
```

### 端口冲突

如果 8080 端口被占用，更改映射端口：

```bash
docker run -d -p 9090:80 --name audio3a-api gmij/audio3a:latest
```

### 性能问题

监控容器资源使用：

```bash
docker stats audio3a-api
```

## 镜像标签说明

| 标签 | 说明 |
|------|------|
| `latest` | main 分支最新版本 |
| `v1.0.0` | 特定版本号 |
| `main-abc1234` | 分支名+提交 SHA |

## 清理

停止并删除容器：

```bash
docker stop audio3a-api
docker rm audio3a-api
```

删除镜像：

```bash
docker rmi gmij/audio3a:latest
```

清理未使用的镜像和卷：

```bash
docker system prune -a
```

## CI/CD 集成

GitHub Actions 会自动构建和推送镜像到 Docker Hub。

**触发条件**：
- 推送到 main 分支
- 创建版本标签（如 `v1.0.0`）
- 手动触发 workflow

**所需配置**：
在 GitHub Repository Settings → Secrets 中添加：
- `DOCKERHUB_USERNAME` - Docker Hub 用户名
- `DOCKERHUB_TOKEN` - Docker Hub 访问令牌

## 参考资料

- [Docker 官方文档](https://docs.docker.com/)
- [ASP.NET Core Docker 部署](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/)
- [Docker Compose 文档](https://docs.docker.com/compose/)
