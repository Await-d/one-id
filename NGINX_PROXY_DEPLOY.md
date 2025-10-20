# OneID Nginx 反向代理部署指南

## 🎯 部署目标

通过 Nginx 反向代理实现：
- ✅ 用户只访问一个域名 `https://auth.awitk.cn` (443 端口)
- ✅ Nginx 处理 SSL/TLS 加密
- ✅ Docker 容器只监听 localhost，不对外暴露端口
- ✅ 根据 URL 路径自动路由到不同的后端服务

## 🏗️ 架构说明

```
互联网用户
  ↓
https://auth.awitk.cn (443)
  ↓
Nginx (宿主机，监听 0.0.0.0:443)
  ├─ SSL 终止
  ├─ /admin/api/* → http://127.0.0.1:10231 (Admin API)
  └─ /*           → http://127.0.0.1:10230 (Identity Server)
       ↓
Docker 容器 (监听 127.0.0.1:10230, 127.0.0.1:10231)
```

**关键点**：
1. **Nginx 在宿主机上运行**（不在 Docker 内）
2. **Docker 端口只绑定到 127.0.0.1**（本地回环地址）
3. **Nginx 通过 localhost 访问 Docker 服务**
4. **外部流量只能通过 Nginx 进入**

## 📦 步骤 1: 部署 Docker 容器

### 1.1 完整的 docker run 命令

```bash
docker run -d \
  --name oneid-app \
  --restart unless-stopped \
  --network bridge \
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

### 1.2 关键配置说明

#### 端口绑定（最重要）
```bash
-p 127.0.0.1:10230:5101  # ✅ 正确：只绑定到 localhost
-p 127.0.0.1:10231:5102  # ✅ 正确：只绑定到 localhost

# ❌ 错误示例
-p 10230:5101            # 会绑定到 0.0.0.0，对外暴露端口
-p 0.0.0.0:10230:5101    # 同样会对外暴露端口
```

**为什么要绑定到 127.0.0.1？**
- ✅ 安全：外部无法直接访问这些端口
- ✅ 统一入口：所有流量必须通过 Nginx
- ✅ SSL 由 Nginx 统一处理
- ✅ 防止端口扫描攻击

#### 网络模式
```bash
--network bridge  # 使用默认 bridge 网络，可以访问宿主机的 localhost
```

**如果你的数据库在其他 Docker 容器中**：
```bash
--network oneid-network  # 使用自定义网络与其他容器通信
```

#### 转发头配置
```bash
-e ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
```

这个配置非常重要！确保 ASP.NET Core 能正确识别：
- 客户端真实 IP（X-Forwarded-For）
- 原始协议（X-Forwarded-Proto: https）
- 原始域名（X-Forwarded-Host）

#### Redirect URIs
```bash
-e Seed__Oidc__RedirectUri=https://auth.awitk.cn/callback
-e LOGIN_REDIRECT_URIS="https://auth.awitk.cn/callback"
-e ADMIN_REDIRECT_URIS="https://auth.awitk.cn/admin/callback"
```

**注意**：
- ✅ 使用 `https://`（由 Nginx 处理 SSL）
- ✅ 使用域名而不是 IP
- ✅ 不包含端口号（默认 443）

### 1.3 验证 Docker 容器

```bash
# 查看容器状态
docker ps | grep oneid-app

# 查看端口绑定
docker port oneid-app
# 应该输出：
# 5101/tcp -> 127.0.0.1:10230
# 5102/tcp -> 127.0.0.1:10231

# 测试本地访问（从宿主机）
curl http://localhost:10230/health
curl http://localhost:10231/health

# 测试外部无法访问（从另一台机器）
curl http://<服务器IP>:10230/health  # 应该超时或拒绝连接
```

## 🌐 步骤 2: 配置 Nginx

### 2.1 安装 Nginx

```bash
sudo apt update
sudo apt install nginx -y
```

### 2.2 复制配置文件

```bash
# 复制配置文件
sudo cp nginx/oneid.conf /etc/nginx/sites-available/oneid.conf

# 创建软链接
sudo ln -s /etc/nginx/sites-available/oneid.conf /etc/nginx/sites-enabled/

# 测试配置
sudo nginx -t
```

### 2.3 Nginx 配置详解

`nginx/oneid.conf` 的关键部分：

```nginx
# HTTPS 配置
server {
    listen 443 ssl http2;
    server_name auth.awitk.cn;

    # SSL 证书（Let's Encrypt 会自动配置）
    ssl_certificate /etc/letsencrypt/live/auth.awitk.cn/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/auth.awitk.cn/privkey.pem;

    # Admin API 路由
    location /admin/api/ {
        # 重写路径：/admin/api/xxx → /api/xxx
        rewrite ^/admin/api/(.*)$ /api/$1 break;

        # 转发到 Docker 容器
        proxy_pass http://localhost:10231;

        # 转发头（非常重要！）
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;  # https
        proxy_set_header X-Forwarded-Host $host;
        proxy_set_header X-Forwarded-Port $server_port;  # 443
    }

    # Identity Server（默认路由）
    location / {
        proxy_pass http://localhost:10230;

        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-Host $host;
        proxy_set_header X-Forwarded-Port $server_port;
    }
}
```

**关键点**：
1. **proxy_pass 使用 localhost**：因为 Docker 绑定到 127.0.0.1
2. **X-Forwarded-Proto 设置为 https**：让后端知道原始请求是 HTTPS
3. **路径重写**：`/admin/api/xxx` → `/api/xxx`

## 🔐 步骤 3: 申请 SSL 证书

### 3.1 安装 Certbot

```bash
sudo apt install certbot python3-certbot-nginx -y
```

### 3.2 申请证书

```bash
sudo certbot --nginx -d auth.awitk.cn
```

Certbot 会：
- ✅ 自动申请 SSL 证书
- ✅ 自动更新 Nginx 配置
- ✅ 设置自动续期（每 90 天）

### 3.3 验证证书

```bash
# 查看证书状态
sudo certbot certificates

# 测试自动续期
sudo certbot renew --dry-run
```

## 🚀 步骤 4: 启动服务

### 4.1 启动 Nginx

```bash
sudo systemctl enable nginx
sudo systemctl restart nginx
```

### 4.2 检查服务状态

```bash
# Nginx 状态
sudo systemctl status nginx

# Docker 容器状态
docker ps | grep oneid-app

# 查看 Nginx 日志
sudo tail -f /var/log/nginx/oneid-access.log
sudo tail -f /var/log/nginx/oneid-error.log
```

## ✅ 步骤 5: 验证部署

### 5.1 测试 HTTPS 访问

```bash
# OIDC Discovery
curl https://auth.awitk.cn/.well-known/openid-configuration

# 健康检查
curl https://auth.awitk.cn/health

# Admin API 健康检查
curl https://auth.awitk.cn/admin/api/health
```

### 5.2 浏览器访问

- **用户登录**: https://auth.awitk.cn
- **管理后台**: https://auth.awitk.cn/admin
- **API 文档**: https://auth.awitk.cn/admin/swagger

### 5.3 验证端口安全性

```bash
# 从外部机器测试（应该失败）
curl http://<服务器IP>:10230/health  # 应该超时或拒绝连接
curl http://<服务器IP>:10231/health  # 应该超时或拒绝连接

# 只有 443 端口可以访问
curl https://auth.awitk.cn/health  # ✅ 成功
```

## 🔒 安全检查清单

- [ ] Docker 端口只绑定到 127.0.0.1
- [ ] 外部无法直接访问 10230、10231 端口
- [ ] SSL 证书有效且自动续期
- [ ] Nginx 配置了安全响应头（HSTS、X-Frame-Options）
- [ ] 所有 Redirect URIs 使用 HTTPS
- [ ] 转发头正确配置（X-Forwarded-Proto 等）

## 🛠️ 常见问题

### Q1: 502 Bad Gateway

**可能原因**：
- Docker 容器未启动
- 端口绑定不正确
- Nginx 配置错误

**解决方法**：
```bash
# 检查容器状态
docker ps | grep oneid-app

# 检查端口监听
sudo netstat -tlnp | grep -E '10230|10231'

# 应该看到：
# tcp  0  0  127.0.0.1:10230  0.0.0.0:*  LISTEN
# tcp  0  0  127.0.0.1:10231  0.0.0.0:*  LISTEN

# 测试本地连接
curl http://localhost:10230/health
```

### Q2: OIDC Redirect URI 不匹配

**错误信息**：`The specified 'redirect_uri' is not valid`

**解决方法**：
确保环境变量中的 URI 使用 HTTPS 和域名：
```bash
-e Seed__Oidc__RedirectUri=https://auth.awitk.cn/callback
-e LOGIN_REDIRECT_URIS="https://auth.awitk.cn/callback"
-e ADMIN_REDIRECT_URIS="https://auth.awitk.cn/admin/callback"
```

### Q3: 登录后无限重定向

**可能原因**：X-Forwarded-Proto 未正确设置

**解决方法**：
1. 确保 Docker 容器中启用了：
   ```bash
   -e ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
   ```

2. 确保 Nginx 配置中设置了：
   ```nginx
   proxy_set_header X-Forwarded-Proto $scheme;  # https
   ```

### Q4: 外部仍可访问 Docker 端口

**问题**：可以从外部访问 `http://<IP>:10230`

**原因**：端口绑定到 0.0.0.0 而不是 127.0.0.1

**解决方法**：
```bash
# 停止容器
docker stop oneid-app && docker rm oneid-app

# 重新运行，确保端口绑定到 127.0.0.1
docker run -d ... \
  -p 127.0.0.1:10230:5101 \
  -p 127.0.0.1:10231:5102 \
  ...
```

### Q5: SSL 证书过期

**检查证书**：
```bash
sudo certbot certificates
```

**手动续期**：
```bash
sudo certbot renew
sudo systemctl reload nginx
```

## 📊 监控和日志

### Nginx 日志
```bash
# 访问日志
sudo tail -f /var/log/nginx/oneid-access.log

# 错误日志
sudo tail -f /var/log/nginx/oneid-error.log
```

### Docker 日志
```bash
# 实时日志
docker logs -f oneid-app

# 最近 100 行
docker logs --tail 100 oneid-app

# 错误日志
docker logs oneid-app 2>&1 | grep ERR
```

### 性能监控
```bash
# Nginx 连接数
sudo netstat -an | grep :443 | wc -l

# Docker 资源使用
docker stats oneid-app
```

## 🔄 更新应用

```bash
# 1. 拉取最新镜像
docker pull await2719/oneid:latest

# 2. 停止旧容器
docker stop oneid-app && docker rm oneid-app

# 3. 启动新容器（使用相同的 docker run 命令）
docker run -d --name oneid-app ... await2719/oneid:latest

# 4. 无需重启 Nginx（会自动连接到新容器）
```

## 📚 相关文档

- 简洁部署指南：`README_DEPLOY.md`
- 完整部署指南：`PRODUCTION_DEPLOYMENT.md`
- Nginx 配置说明：`nginx/README.md`
- Nginx 配置文件：`nginx/oneid.conf`

## 🎯 快速部署脚本

如果你已经配置好数据库和 Redis，可以使用：

```bash
# 快速部署（使用已有的数据库）
chmod +x quick-deploy.sh
./quick-deploy.sh

# 完整部署（包含数据库、Redis）
chmod +x deploy-production.sh
./deploy-production.sh
```

然后配置 Nginx：

```bash
# 1. 复制配置
sudo cp nginx/oneid.conf /etc/nginx/sites-available/
sudo ln -s /etc/nginx/sites-available/oneid.conf /etc/nginx/sites-enabled/

# 2. 申请证书
sudo certbot --nginx -d auth.awitk.cn

# 3. 重启 Nginx
sudo systemctl restart nginx
```

## ✨ 总结

**反向代理部署的核心要点**：

1. ✅ Docker 端口绑定到 `127.0.0.1`（不对外暴露）
2. ✅ Nginx 监听 `0.0.0.0:443`（对外提供服务）
3. ✅ Nginx 通过 `localhost` 访问 Docker
4. ✅ 启用转发头：`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`
5. ✅ 所有 Redirect URIs 使用 `https://` + 域名
6. ✅ SSL 由 Nginx 统一处理

**用户体验**：
- ✅ 只需访问 `https://auth.awitk.cn`（无需端口号）
- ✅ 自动 HTTPS 加密
- ✅ 统一的访问入口
- ✅ 更好的安全性

现在你的 OneID 已经通过专业的反向代理部署完成！🚀
