# OneID 单端口生产部署指南

## 🎯 部署目标

**只使用一个域名 + 一个端口（443）完成所有操作**

- ✅ 用户访问：`https://auth.awitk.cn`
- ✅ 管理后台：`https://auth.awitk.cn/admin`
- ✅ Admin API：`https://auth.awitk.cn/admin/api/*`
- ✅ 无需在 URL 中指定端口号

## 🏗️ 架构说明

```
浏览器
  ↓
https://auth.awitk.cn (443)
  ↓
Nginx 反向代理
  ├─ /admin/api/* → localhost:10231 (Admin API)
  └─ /*           → localhost:10230 (Identity Server + 前端)
```

**关键点**：
- Docker 容器端口**只绑定到 localhost**，不对外暴露
- Nginx 监听 443 端口，负责所有外部流量
- 根据 URL 路径自动转发到不同的后端服务

## 📦 前置要求

1. **已安装 Docker**
2. **已安装 Nginx**
3. **域名已解析**到服务器 IP
4. **PostgreSQL** 和 **Redis** 已运行（或使用完整部署脚本）

## 🚀 快速部署

### 步骤 1: 启动 Docker 容器（端口绑定到 localhost）

```bash
docker run -d \
  --name oneid-app \
  --restart unless-stopped \
  --network oneid-network \
  -p 127.0.0.1:10230:5101 \
  -p 127.0.0.1:10231:5102 \
  -v /your/path/data:/app/data \
  -v /your/path/logs:/app/logs \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e TZ=Asia/Shanghai \
  -e ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
  -e ASPNETCORE_URLS="http://+:5101" \
  -e ConnectionStrings__Default="Host=your-postgres;Port=5432;Database=oneid;Username=oneid;Password=your_password" \
  -e Persistence__Provider=Postgres \
  -e Redis__ConnectionString="your-redis:6379" \
  -e Seed__Admin__Username=admin \
  -e Seed__Admin__Password=YourStrongPassword \
  -e Seed__Admin__Email=admin@example.com \
  -e Seed__Oidc__ClientId=spa.portal \
  -e Seed__Oidc__ClientSecret=your_client_secret \
  -e Seed__Oidc__RedirectUri=https://auth.awitk.cn/callback \
  -e LOGIN_REDIRECT_URIS="https://auth.awitk.cn/callback" \
  -e LOGIN_LOGOUT_URIS="https://auth.awitk.cn" \
  -e ADMIN_REDIRECT_URIS="https://auth.awitk.cn/admin/callback" \
  -e ADMIN_LOGOUT_URIS="https://auth.awitk.cn/admin" \
  await2719/oneid:latest
```

**关键变化**：
- ✅ `-p 127.0.0.1:10230:5101` - 只绑定到 localhost
- ✅ `-p 127.0.0.1:10231:5102` - 只绑定到 localhost
- ✅ 外部无法直接访问这些端口，必须通过 Nginx

### 步骤 2: 配置 Nginx

复制 Nginx 配置文件：

```bash
sudo cp nginx/oneid.conf /etc/nginx/sites-available/oneid.conf
sudo ln -s /etc/nginx/sites-available/oneid.conf /etc/nginx/sites-enabled/
```

测试配置：

```bash
sudo nginx -t
```

### 步骤 3: 申请 SSL 证书

使用 Let's Encrypt 免费证书：

```bash
sudo certbot --nginx -d auth.awitk.cn
```

Certbot 会自动：
- 申请 SSL 证书
- 更新 Nginx 配置
- 设置自动续期

### 步骤 4: 重启 Nginx

```bash
sudo systemctl restart nginx
```

### 步骤 5: 验证部署

访问以下地址验证：

```bash
# OIDC Discovery
curl https://auth.awitk.cn/.well-known/openid-configuration

# 健康检查
curl https://auth.awitk.cn/health
```

浏览器访问：
- **登录页面**: https://auth.awitk.cn
- **管理后台**: https://auth.awitk.cn/admin

## 📝 完整部署脚本

如果你需要一键部署（包括 PostgreSQL、Redis），使用：

```bash
chmod +x deploy-production.sh
./deploy-production.sh
```

脚本会自动创建：
- ✅ Docker 网络
- ✅ PostgreSQL 容器
- ✅ Redis 容器
- ✅ OneID 应用容器

## 🔧 自定义配置

### 修改域名

编辑 `deploy-production.sh` 或 docker run 命令中的：
- `DOMAIN="your-domain.com"`
- 所有 `https://auth.awitk.cn` 替换为你的域名

### 修改管理员密码

修改环境变量：
```bash
-e Seed__Admin__Username=your_admin
-e Seed__Admin__Password=YourStrongPassword123
-e Seed__Admin__Email=admin@yourdomain.com
```

### 修改数据库连接

```bash
-e ConnectionStrings__Default="Host=your-host;Port=5432;Database=oneid;Username=oneid;Password=your_password"
```

## 🛠️ 常用操作

### 查看日志

```bash
# 实时日志
docker logs -f oneid-app

# 最近 100 行
docker logs --tail 100 oneid-app

# Nginx 错误日志
sudo tail -f /var/log/nginx/oneid-error.log
```

### 重启服务

```bash
# 重启 OneID
docker restart oneid-app

# 重启 Nginx
sudo systemctl restart nginx
```

### 更新应用

```bash
# 1. 构建新镜像
docker build -t await2719/oneid:latest .

# 2. 停止旧容器
docker stop oneid-app && docker rm oneid-app

# 3. 启动新容器（使用上面的 docker run 命令）
```

## ❓ 常见问题

### Q1: 为什么要绑定到 localhost？

**答**：安全性。绑定到 `127.0.0.1` 后，外部网络无法直接访问这些端口，必须通过 Nginx，这样可以：
- ✅ 统一 SSL/TLS 加密
- ✅ 统一访问控制
- ✅ 统一日志记录
- ✅ 防止端口被直接扫描攻击

### Q2: 如果不想用 Nginx 怎么办？

**答**：可以直接暴露端口，但需要：
1. 修改端口绑定：`-p 10230:5101 -p 10231:5102`（去掉 127.0.0.1）
2. 在防火墙开放这些端口
3. 访问时带端口号：`https://auth.awitk.cn:10230`
4. 需要为每个端口配置 SSL 证书

**不推荐**，因为会增加复杂度和安全风险。

### Q3: Nginx 反向代理会影响性能吗？

**答**：几乎不会。Nginx 是高性能的反向代理，还能提供：
- ✅ Gzip 压缩（减少带宽）
- ✅ 静态资源缓存
- ✅ 连接池管理
- ✅ 负载均衡能力

### Q4: 443 端口被占用怎么办？

**答**：检查是否有其他 Web 服务占用：
```bash
sudo netstat -tlnp | grep :443
sudo lsof -i :443
```

如果确实被占用，可以：
1. 停止占用的服务
2. 或者使用其他端口（如 8443）+ Nginx 配置

## 📖 相关文档

- 详细部署指南：`PRODUCTION_DEPLOYMENT.md`
- Nginx 配置说明：`nginx/README.md`
- Nginx 配置文件：`nginx/oneid.conf`

## 🆘 技术支持

- 项目地址：https://github.com/Await-d/one-id
- 问题反馈：https://github.com/Await-d/one-id/issues
