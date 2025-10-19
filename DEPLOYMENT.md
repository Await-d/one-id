# OneID 远程部署指南

本文档说明如何将 OneID 项目部署到远程 Docker 服务器。

## 📋 部署概览

- **远程服务器**: 192.168.123.5:2719
- **部署目录**: `/volume1/docker/1panel/apps/local/one-id`
- **部署方式**: Docker Compose (HTTPS)

## 🚀 快速开始（推荐）

### 方式一：一键部署（最简单）

```bash
# 1. 直接运行一键部署脚本
./quick-deploy.sh
```

这个脚本会自动完成以下操作：
- ✅ 传输项目文件到远程服务器
- ✅ 更新远程环境配置（使用 .env.remote）
- ✅ 构建 Docker 镜像
- ✅ 生成 SSL 证书（如果不存在）
- ✅ 启动所有服务

**总耗时**: 约 5-10 分钟（取决于网络速度）

---

### 方式二：分步部署（可控性更强）

#### 步骤 1: 编辑远程配置文件

编辑 `.env.remote` 文件，确认配置信息：

```bash
# 域名/IP（必须与远程服务器 IP 一致）
DOMAIN=192.168.123.5

# 数据库密码（建议修改）
POSTGRES_PASSWORD=OneID_Remote_Secure_Password_2024

# 管理员账号
ADMIN_USERNAME=admin
ADMIN_PASSWORD=Admin@123456
ADMIN_EMAIL=285283010@qq.com

# 端口配置
IDENTITY_PORT=9443
ADMIN_PORT=9444
HTTP_PORT=9080
```

#### 步骤 2: 运行部署脚本

```bash
# 传输文件并部署
./deploy-remote.sh
```

#### 步骤 3: 更新远程环境配置

```bash
# 将 .env.remote 上传到远程服务器
sshpass -p "ZhangDong2580" scp -P 2719 \
  .env.remote await@192.168.123.5:/volume1/docker/1panel/apps/local/one-id/.env
```

#### 步骤 4: 生成 SSL 证书（如果需要）

```bash
# SSH 登录到远程服务器
sshpass -p "ZhangDong2580" ssh -p 2719 await@192.168.123.5

# 进入部署目录
cd /volume1/docker/1panel/apps/local/one-id

# 生成自签名证书
mkdir -p nginx/ssl
cd nginx/ssl
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout key.pem -out cert.pem \
  -subj "/C=CN/ST=State/L=City/O=Organization/CN=192.168.123.5"
chmod 644 cert.pem key.pem

# 退出 SSH
exit
```

#### 步骤 5: 重启服务

```bash
# 使用管理脚本重启服务
./remote-manage.sh
# 选择选项 6 (重启所有服务)
```

---

## 🛠 管理工具

### 远程服务管理脚本

运行 `./remote-manage.sh` 可以方便地管理远程服务：

```bash
./remote-manage.sh
```

**功能菜单**:
- `1` - 查看服务状态
- `2` - 查看实时日志（所有服务）
- `3` - 查看 Identity Server 日志
- `4` - 查看 Admin API 日志
- `5` - 查看 Nginx 日志
- `6` - 重启所有服务
- `7` - 重启 Identity Server
- `8` - 重启 Admin API
- `9` - 停止所有服务
- `10` - 启动所有服务
- `11` - 查看资源使用情况
- `12` - 清理 Docker 缓存
- `13` - 重新构建并部署

---

## 📝 脚本说明

### 1. `quick-deploy.sh` - 一键部署脚本

**用途**: 自动化完成所有部署步骤

**特点**:
- 全自动，无需手动干预
- 包含环境配置、文件传输、构建、启动
- 自动生成 SSL 证书（如果不存在）

**使用场景**: 首次部署或完全重新部署

---

### 2. `deploy-remote.sh` - 部署脚本

**用途**: 传输项目文件并在远程服务器上构建部署

**功能**:
- ✅ 检查必需工具（sshpass, rsync）
- ✅ 测试 SSH 连接
- ✅ 同步项目文件到远程服务器
- ✅ 停止旧容器
- ✅ 构建 Docker 镜像
- ✅ 启动服务
- ✅ 显示服务状态和日志

**排除的文件/目录**:
- `backend/bin/`, `backend/obj/` - 编译输出
- `backend/tests/` - 测试项目
- `frontend/*/node_modules/` - Node 依赖
- `frontend/*/dist/` - 前端构建输出
- `.git/`, `.idea/`, `.vscode/` - 版本控制和 IDE 文件

---

### 3. `remote-manage.sh` - 服务管理脚本

**用途**: 远程管理 OneID 服务（查看日志、重启服务等）

**功能**:
- 查看服务状态和日志
- 重启/停止/启动服务
- 查看资源使用情况
- 清理 Docker 缓存
- 重新构建部署

**使用场景**: 日常运维管理

---

## 🔧 配置文件说明

### `.env.remote` - 远程环境配置

这是远程服务器使用的环境变量文件，包含：

- `DOMAIN` - 远程服务器的 IP/域名
- `POSTGRES_PASSWORD` - 数据库密码
- `ADMIN_*` - 管理员账号信息
- `*_PORT` - 服务端口配置
- `ADMIN_REDIRECT_URIS` - OIDC 回调地址
- `IDENTITY_CORS_ALLOWED_ORIGINS` - CORS 白名单

**注意**:
- 必须确保 `DOMAIN` 与实际访问地址一致
- 生产环境请修改默认密码

---

## 📦 部署架构

```
┌─────────────────────────────────────────────────────────┐
│                   Nginx (HTTPS)                         │
│  - Identity Server: https://192.168.123.5:9443         │
│  - Admin Portal:    https://192.168.123.5:9444         │
└─────────────────────────────────────────────────────────┘
                        │
        ┌───────────────┴───────────────┐
        │                               │
┌───────▼──────┐              ┌─────────▼────────┐
│   Identity   │              │    Admin API     │
│    Server    │◄────────────►│   + Admin SPA    │
│  + Login SPA │              │                  │
└──────┬───────┘              └─────────┬────────┘
       │                                │
       └────────────┬───────────────────┘
                    │
       ┌────────────┼────────────┐
       │                         │
┌──────▼──────┐          ┌───────▼──────┐
│  PostgreSQL │          │    Redis     │
│   (数据库)  │          │   (缓存)     │
└─────────────┘          └──────────────┘
```

---

## 🌐 访问信息

部署完成后，通过以下地址访问：

### Identity Server (登录界面)
- **HTTPS**: https://192.168.123.5:9443
- **用途**: 用户登录、注册、OIDC 认证

### Admin Portal (管理后台)
- **HTTPS**: https://192.168.123.5:9444
- **用途**: 用户管理、客户端管理、系统配置

### 管理员账号
- **用户名**: admin
- **密码**: Admin@123456 (请尽快修改)
- **邮箱**: 285283010@qq.com

---

## 🔐 SSL 证书

### 自签名证书（开发/测试环境）

脚本会自动生成自签名证书，浏览器会提示不受信任。

**解决方法**:
1. 访问 https://192.168.123.5:9443
2. 浏览器提示「不安全」或「证书错误」
3. 点击「高级」→「继续访问」或「继续前往」
4. 正常使用

### 真实证书（生产环境）

如果有真实的 SSL 证书，替换以下文件：

```bash
# 在远程服务器上
cd /volume1/docker/1panel/apps/local/one-id/nginx/ssl

# 替换证书文件
# cert.pem - 证书文件
# key.pem  - 私钥文件

# 重启 Nginx
docker compose -f docker-compose.https.yml restart nginx
```

---

## 🐛 故障排查

### 1. SSH 连接失败

**症状**: `deploy-remote.sh` 提示无法连接到远程服务器

**解决方法**:
```bash
# 手动测试 SSH 连接
ssh -p 2719 await@192.168.123.5

# 检查：
# - IP 地址是否正确
# - 端口是否正确
# - 用户名/密码是否正确
# - 远程服务器是否可访问
```

---

### 2. 服务启动失败

**症状**: 容器无法启动或健康检查失败

**解决方法**:
```bash
# 使用管理脚本查看日志
./remote-manage.sh
# 选择选项 2 或 3 查看详细日志

# 或直接在远程服务器上查看
ssh -p 2719 await@192.168.123.5
cd /volume1/docker/1panel/apps/local/one-id
docker compose -f docker-compose.https.yml logs -f identity
```

**常见问题**:
- 端口冲突：检查 9443/9444 端口是否被占用
- 数据库连接失败：检查 PostgreSQL 是否正常启动
- 权限问题：检查部署目录的文件权限

---

### 3. 无法访问 HTTPS

**症状**: 浏览器无法打开 https://192.168.123.5:9443

**解决方法**:
```bash
# 1. 检查服务状态
./remote-manage.sh
# 选择选项 1

# 2. 检查防火墙
ssh -p 2719 await@192.168.123.5
sudo iptables -L -n | grep 9443

# 3. 检查端口监听
netstat -tlnp | grep 9443

# 4. 检查 Nginx 配置
docker compose -f docker-compose.https.yml logs nginx
```

---

### 4. OIDC 认证失败

**症状**: 登录后重定向失败或提示 CORS 错误

**解决方法**:
1. 检查 `.env` 中的 `DOMAIN` 是否与浏览器访问地址一致
2. 检查 `ADMIN_REDIRECT_URIS` 是否正确
3. 检查 `IDENTITY_CORS_ALLOWED_ORIGINS` 是否包含 Admin Portal 地址
4. 重启服务以应用配置

```bash
# 更新 .env 后重启服务
./remote-manage.sh
# 选择选项 6 (重启所有服务)
```

---

## 📊 监控与日志

### 实时查看日志

```bash
# 方式一：使用管理脚本
./remote-manage.sh
# 选择选项 2-5

# 方式二：直接 SSH 登录
ssh -p 2719 await@192.168.123.5
cd /volume1/docker/1panel/apps/local/one-id

# 查看所有服务日志
docker compose -f docker-compose.https.yml logs -f

# 查看特定服务日志
docker compose -f docker-compose.https.yml logs -f identity
docker compose -f docker-compose.https.yml logs -f adminapi
docker compose -f docker-compose.https.yml logs -f nginx
```

### 查看资源使用

```bash
./remote-manage.sh
# 选择选项 11

# 或直接在远程服务器上
docker stats
```

---

## 🔄 更新部署

### 更新代码并重新部署

```bash
# 1. 拉取最新代码（如果使用 git）
git pull

# 2. 重新部署
./quick-deploy.sh

# 或分步执行
./deploy-remote.sh
```

### 仅重启服务（不重新构建）

```bash
./remote-manage.sh
# 选择选项 6 (重启所有服务)
```

---

## 📚 相关文档

- [CLAUDE.md](./CLAUDE.md) - 项目架构和开发指南
- [README.md](./README.md) - 项目介绍
- [docker-compose.https.yml](./docker-compose.https.yml) - Docker Compose 配置

---

## 💡 最佳实践

1. **安全**:
   - 修改默认的管理员密码
   - 使用强密码（数据库、管理员）
   - 定期备份数据库
   - 生产环境使用真实 SSL 证书

2. **性能**:
   - 定期清理 Docker 缓存（选项 12）
   - 监控资源使用情况
   - 根据负载调整数据库连接池

3. **维护**:
   - 定期查看日志
   - 关注容器健康状态
   - 及时更新依赖版本

4. **备份**:
   - 备份 PostgreSQL 数据卷
   - 备份 `.env` 配置文件
   - 备份 SSL 证书

---

## 📞 支持

如有问题，请查看：
- [GitHub Issues](https://github.com/your-repo/OneID/issues)
- [项目文档](./CLAUDE.md)
- 查看容器日志进行故障排查
