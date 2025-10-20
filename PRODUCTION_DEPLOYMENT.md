# OneID 生产环境部署指南

本指南介绍如何在生产环境中使用**单域名 + Nginx 反向代理**方案部署 OneID。

## 架构说明

```
互联网
  │
  ▼
域名: https://auth.awitk.cn
  │
  ▼
Nginx 反向代理 (443/80)
  │
  ├── /admin/api/* ──────► Admin API (localhost:10231)
  │
  └── /* ────────────────► Identity Server (localhost:10230)
                            ├── Login SPA (用户登录)
                            ├── Admin Portal (管理前端)
                            └── OIDC Endpoints
```

### URL 路径规划

| 功能 | URL | 后端服务 |
|------|-----|----------|
| 用户登录 | `https://auth.awitk.cn/login` | Identity Server:10230 |
| Admin Portal | `https://auth.awitk.cn/admin` | Identity Server:10230 (静态文件) |
| Admin API | `https://auth.awitk.cn/admin/api/*` | Admin API:10231 |
| OIDC Discovery | `https://auth.awitk.cn/.well-known/openid-configuration` | Identity Server:10230 |
| Account API | `https://auth.awitk.cn/api/account/*` | Identity Server:10230 |

## 前置要求

### 服务器配置
- **操作系统**: Ubuntu 20.04 LTS 或更高版本
- **CPU**: 2核心或更高
- **内存**: 4GB RAM 或更高
- **硬盘**: 20GB 可用空间
- **域名**: 已解析到服务器 IP（如 auth.awitk.cn）

### 软件依赖
- Docker 20.10+
- Docker Compose 2.0+
- Nginx 1.18+
- Git

### 网络要求
- 开放端口 80 (HTTP)
- 开放端口 443 (HTTPS)
- 服务器可以访问 Docker Hub
- 服务器可以访问 Let's Encrypt

## 部署步骤

### 步骤 1: 安装基础软件

```bash
# 更新系统
sudo apt update && sudo apt upgrade -y

# 安装 Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker $USER

# 安装 Docker Compose
sudo apt install docker-compose-plugin -y

# 安装 Nginx
sudo apt install nginx -y

# 安装 Certbot (Let's Encrypt)
sudo apt install certbot python3-certbot-nginx -y

# 退出重新登录以应用 Docker 组权限
# exit 后重新 ssh 登录
```

### 步骤 2: 克隆项目代码

```bash
cd ~
git clone https://github.com/Await-d/one-id.git
cd one-id
```

### 步骤 3: 配置环境变量

编辑生产环境部署脚本：

```bash
nano deploy-production.sh
```

**重要：修改以下默认配置**

```bash
# 数据库密码（务必修改！）
DB_PASSWORD="你的强密码"

# 管理员账号（务必修改！）
ADMIN_USERNAME="你的用户名"
ADMIN_PASSWORD="你的强密码"
ADMIN_EMAIL="你的邮箱"

# OIDC 客户端密钥（务必修改！）
OIDC_CLIENT_SECRET="你的随机密钥"
```

### 步骤 4: 构建 Docker 镜像

```bash
# 构建最新镜像
docker build -t await2719/oneid:latest -f Dockerfile .
```

### 步骤 5: 运行部署脚本

```bash
chmod +x deploy-production.sh
./deploy-production.sh
```

脚本会自动完成：
- ✅ 创建 Docker 网络
- ✅ 启动 PostgreSQL 数据库
- ✅ 启动 Redis 缓存
- ✅ 启动 OneID 应用
- ✅ 初始化数据库
- ✅ 创建管理员账号

### 步骤 6: 配置 Nginx 反向代理

```bash
# 复制 Nginx 配置文件
sudo cp nginx/oneid.conf /etc/nginx/sites-available/oneid.conf

# 创建软链接
sudo ln -s /etc/nginx/sites-available/oneid.conf /etc/nginx/sites-enabled/

# 测试配置
sudo nginx -t

# 重载 Nginx（先不要重启，因为还没有 SSL 证书）
# sudo systemctl reload nginx
```

### 步骤 7: 申请 SSL 证书

使用 Let's Encrypt 免费 SSL 证书：

```bash
# 方式 1: 使用 Certbot 自动配置（推荐）
sudo certbot --nginx -d auth.awitk.cn

# Certbot 会自动：
# - 申请 SSL 证书
# - 修改 Nginx 配置
# - 设置自动续期

# 方式 2: 手动申请证书
sudo certbot certonly --nginx -d auth.awitk.cn

# 然后手动更新 nginx/oneid.conf 中的证书路径
```

### 步骤 8: 配置防火墙

```bash
# 如果使用 UFW
sudo ufw allow 'Nginx Full'
sudo ufw enable

# 不需要开放 10230 和 10231 端口（仅供 Nginx 内部访问）
```

### 步骤 9: 启动 Nginx

```bash
sudo systemctl enable nginx
sudo systemctl restart nginx
```

### 步骤 10: 验证部署

```bash
# 检查容器状态
docker ps

# 应该看到三个运行中的容器：
# - oneid-app (OneID 应用)
# - oneid-postgres-prod (PostgreSQL)
# - oneid-redis-prod (Redis)

# 查看应用日志
docker logs -f oneid-app

# 测试 HTTPS 访问
curl https://auth.awitk.cn/health
# 应返回: healthy

# 测试 OIDC Discovery
curl https://auth.awitk.cn/.well-known/openid-configuration
# 应返回 JSON 配置
```

### 步骤 11: 访问系统

在浏览器中访问：
- **用户登录**: https://auth.awitk.cn/login
- **管理后台**: https://auth.awitk.cn/admin

使用部署脚本中配置的管理员账号登录。

## 安全加固

### 1. 修改默认密码

登录后立即修改：
- ✅ 管理员密码
- ✅ 数据库密码
- ✅ OIDC 客户端密钥

### 2. 配置生产证书

```bash
# 在 Admin Portal 中配置签名密钥
# 访问: https://auth.awitk.cn/admin
# 导航到: Authentication & Security > Signing Keys
# 生成并激活生产环境的 RSA 签名密钥
```

### 3. 限制 Swagger 访问

编辑 `nginx/oneid.conf`，禁用 Swagger：

```nginx
location /admin/swagger {
    return 403;
    access_log off;
}
```

### 4. 配置日志轮转

```bash
# 创建日志轮转配置
sudo nano /etc/logrotate.d/nginx-oneid

# 添加内容：
/var/log/nginx/oneid-*.log {
    daily
    rotate 14
    compress
    delaycompress
    notifempty
    create 0640 www-data adm
    sharedscripts
    postrotate
        [ -f /var/run/nginx.pid ] && kill -USR1 `cat /var/run/nginx.pid`
    endscript
}
```

### 5. 配置备份

```bash
# 创建备份脚本
nano ~/backup-oneid.sh
```

```bash
#!/bin/bash
BACKUP_DIR="/backup/oneid"
DATE=$(date +%Y%m%d_%H%M%S)

mkdir -p $BACKUP_DIR

# 备份数据库
docker exec oneid-postgres-prod pg_dump -U oneid oneid | gzip > "$BACKUP_DIR/db_$DATE.sql.gz"

# 备份 Docker volumes
docker run --rm -v oneid-postgres-data:/data -v $BACKUP_DIR:/backup alpine tar czf /backup/postgres_volume_$DATE.tar.gz /data

# 删除 30 天前的备份
find $BACKUP_DIR -type f -mtime +30 -delete

echo "Backup completed: $DATE"
```

```bash
# 设置定时备份（每天凌晨 2 点）
chmod +x ~/backup-oneid.sh
(crontab -l 2>/dev/null; echo "0 2 * * * ~/backup-oneid.sh") | crontab -
```

## 监控和维护

### 查看日志

```bash
# OneID 应用日志
docker logs -f oneid-app

# 查看最近 100 行
docker logs --tail 100 oneid-app

# Nginx 访问日志
sudo tail -f /var/log/nginx/oneid-access.log

# Nginx 错误日志
sudo tail -f /var/log/nginx/oneid-error.log
```

### 健康检查

```bash
# 应用健康检查
curl https://auth.awitk.cn/health

# 数据库连接检查
docker exec oneid-postgres-prod pg_isready -U oneid

# Redis 连接检查
docker exec oneid-redis-prod redis-cli ping
```

### 更新应用

```bash
# 拉取最新代码
cd ~/one-id
git pull

# 重新构建镜像
docker build -t await2719/oneid:latest -f Dockerfile .

# 重新部署
./deploy-production.sh

# 查看启动日志
docker logs -f oneid-app
```

### 证书续期

Let's Encrypt 证书有效期 90 天，Certbot 会自动续期。

```bash
# 测试自动续期
sudo certbot renew --dry-run

# 手动续期（如需要）
sudo certbot renew

# 查看证书信息
sudo certbot certificates
```

## 故障排查

### 问题 1: 502 Bad Gateway

**可能原因**:
- OneID 容器未启动
- 端口 10230/10231 未监听

**解决方法**:
```bash
# 检查容器状态
docker ps -a | grep oneid

# 如果容器已退出，查看日志
docker logs oneid-app

# 重启容器
docker restart oneid-app

# 检查端口监听
netstat -tlnp | grep -E '10230|10231'
```

### 问题 2: 403 Forbidden (Admin API)

**可能原因**:
- JWT token 中缺少 Admin 角色
- 用户未分配 Admin 角色

**解决方法**:
```bash
# 进入数据库
docker exec -it oneid-postgres-prod psql -U oneid -d oneid

# 检查角色
SELECT "Name" FROM oneid."AspNetRoles";

# 检查用户角色关联
SELECT u."UserName", r."Name"
FROM oneid."AspNetUsers" u
JOIN oneid."AspNetUserRoles" ur ON u."Id" = ur."UserId"
JOIN oneid."AspNetRoles" r ON ur."RoleId" = r."Id";

# 退出
\q
```

### 问题 3: CORS 错误

**可能原因**:
- Nginx 反向代理配置错误
- 前端 API 路径配置错误

**解决方法**:
```bash
# 检查 Nginx 配置
sudo nginx -t

# 查看 Nginx 错误日志
sudo tail -f /var/log/nginx/oneid-error.log

# 重载 Nginx
sudo systemctl reload nginx
```

### 问题 4: SSL 证书错误

**可能原因**:
- 证书过期
- 证书路径配置错误

**解决方法**:
```bash
# 检查证书状态
sudo certbot certificates

# 续期证书
sudo certbot renew --force-renewal

# 重启 Nginx
sudo systemctl restart nginx
```

## 性能优化

### 1. 启用 Nginx 缓存

在 `nginx/oneid.conf` 中添加：

```nginx
# 在 http 块中添加
proxy_cache_path /var/cache/nginx levels=1:2 keys_zone=oneid_cache:10m max_size=100m inactive=60m;

# 在 location / 中添加
location / {
    proxy_cache oneid_cache;
    proxy_cache_valid 200 5m;
    proxy_cache_use_stale error timeout http_500 http_502 http_503 http_504;

    # ... 其他配置
}
```

### 2. 配置 Redis 持久化

编辑 Redis 配置：

```bash
docker exec -it oneid-redis-prod redis-cli CONFIG SET save "900 1 300 10 60 10000"
```

### 3. PostgreSQL 性能调优

```bash
# 编辑 PostgreSQL 配置（根据服务器内存调整）
docker exec -it oneid-postgres-prod bash
nano /var/lib/postgresql/data/postgresql.conf

# 修改以下参数（示例为 4GB RAM）
shared_buffers = 1GB
effective_cache_size = 3GB
maintenance_work_mem = 256MB
work_mem = 16MB
```

## 扩展和高可用

### 多实例部署

使用 Docker Compose 和负载均衡器（如 HAProxy）部署多个 OneID 实例。

### 数据库主从复制

配置 PostgreSQL 主从复制以提高可用性。

### Redis 集群

使用 Redis Cluster 或 Redis Sentinel 提高缓存可用性。

## 技术支持

- **项目地址**: https://github.com/Await-d/one-id
- **问题反馈**: https://github.com/Await-d/one-id/issues
- **文档**: https://github.com/Await-d/one-id/wiki

## 许可证

本项目采用 MIT 许可证。
