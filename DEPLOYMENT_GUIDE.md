# OneID 部署指南

## 快速部署

### 1. 构建 Docker 镜像

```bash
docker build -t await2719/oneid:latest .
```

### 2. 使用部署脚本

```bash
chmod +x deploy-oneid.sh
./deploy-oneid.sh
```

## 环境变量说明

### 必需环境变量

| 变量名 | 说明 | 示例 |
|--------|------|------|
| `ConnectionStrings__Default` | 数据库连接字符串 | `Host=oneid-postgres-prod;Port=5432;Database=oneid;Username=oneid;Password=xxx` |
| `Persistence__Provider` | 数据库提供商 | `Postgres` / `MySQL` / `SqlServer` / `Sqlite` |
| `Redis__ConnectionString` | Redis连接字符串 | `oneid-redis-prod:6379` |
| `Seed__Admin__Username` | 管理员用户名 | `await` |
| `Seed__Admin__Password` | 管理员密码（至少8位） | `Await2580` |
| `Seed__Admin__Email` | 管理员邮箱 | `admin@example.com` |

### OIDC 客户端配置

**Login SPA (spa.portal):**
```bash
Seed__Oidc__ClientId=spa.portal
Seed__Oidc__RedirectUri=https://auth.awitk.cn/callback
LOGIN_REDIRECT_URIS=http://localhost:10230/callback  # 额外的本地地址
LOGIN_LOGOUT_URIS=http://localhost:10230
```

**Admin Portal (spa.admin):**
```bash
ADMIN_REDIRECT_URIS=http://localhost:10230/admin/callback,https://auth.awitk.cn/admin/callback
ADMIN_LOGOUT_URIS=http://localhost:10230/admin,https://auth.awitk.cn/admin
```

### 可选环境变量

| 变量名 | 说明 | 默认值 |
|--------|------|--------|
| `ASPNETCORE_ENVIRONMENT` | 运行环境 | `Production` |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | 启用反向代理头 | `true` |
| `TZ` | 时区 | `Asia/Shanghai` |

## 访问地址

### 本地开发环境

- **Login SPA**: http://localhost:10230
- **Admin Portal**: http://localhost:10230/admin
- **Identity API**: http://localhost:10230/api
- **Admin API**: http://localhost:10231/api
- **Swagger (Identity)**: http://localhost:10230/swagger
- **Swagger (Admin API)**: http://localhost:10231/swagger

### 生产环境（反向代理）

- **Login SPA**: https://auth.awitk.cn
- **Admin Portal**: https://auth.awitk.cn/admin

## OIDC 客户端

| 应用 | Client ID | Redirect URI | 用途 |
|------|-----------|--------------|------|
| Login SPA | spa.portal | /callback | 用户登录门户 |
| Admin Portal | spa.admin | /admin/callback | 管理后台 |

## 管理员账号

- **用户名**: `await`
- **密码**: `Await2580`
- **邮箱**: `285283010@qq.com`

## 查看日志

```bash
# 查看实时日志
docker logs -f oneid-app

# 查看最近100行日志
docker logs --tail 100 oneid-app

# 查看错误日志
docker logs oneid-app 2>&1 | grep ERR
```

## 常见问题

### 1. redirect_uri 不匹配

**错误**: `The specified 'redirect_uri' is not valid for this client application`

**解决**: 确保环境变量中配置了正确的 redirect_uri：
- Login SPA: 添加 `LOGIN_REDIRECT_URIS`
- Admin Portal: 添加 `ADMIN_REDIRECT_URIS`

### 2. 静态资源 404

**错误**: Admin Portal 的 JS/CSS 文件 404

**解决**: 
- 确保 `base: '/admin/'` 在 `vite.config.ts` 中配置
- 确保 `basename: '/admin'` 在 `routes.tsx` 中配置

### 3. 依赖注入错误

**错误**: `Unable to resolve service for type 'XXX'`

**解决**:
- 确保 `builder.Services.AddHttpClient()` 已添加
- 确保 `builder.Services.AddOneIdInfrastructure()` 已添加

### 4. Admin Portal 404 错误（Analytics API 无法访问）

**错误**: Admin Portal Dashboard 显示 "Unexpected Application Error! 404 Not Found"

**原因**:
- Admin Portal 尝试访问 Analytics API (`/api/analytics/*`)
- Identity Server (10230) 没有这些端点
- Admin API (10231) 才有 Analytics 端点

**解决**:
- **方案 A（推荐）**: 配置 Nginx 路由规则，将 Admin API 请求转发到 10231 端口
- **方案 B（已实现）**: Admin Portal 智能检测端口，自动使用 10231 端口访问 Admin API

### 5. 用户名不匹配导致登录失败

**错误**: 使用正确的用户名和密码仍然显示 "Invalid username or password"

**原因**: 数据库中的用户名与环境变量中的用户名不一致（旧数据）

**解决**: 直接更新数据库中的用户名
```sql
-- 连接到数据库
docker exec -e PGPASSWORD=xxx oneid-postgres-prod psql -U oneid -d oneid

-- 更新用户名
UPDATE oneid."AspNetUsers"
SET "UserName" = 'await', "NormalizedUserName" = 'AWAIT'
WHERE "Email" = '285283010@qq.com';
```

## 文件修改清单

### 后端关键修改

1. **Program.cs** (第 113, 122 行)
   - 添加 `AddHttpClient()`
   - 添加 `AddOneIdInfrastructure()`

2. **DatabaseSeeder.cs**
   - `EnsureOidcClientAsync`: 支持 `LOGIN_REDIRECT_URIS`
   - `EnsureAdminPortalClientAsync`: 支持 `ADMIN_REDIRECT_URIS`

3. **supervisord.conf**
   - 日志输出到 `/dev/fd/1` 和 `/dev/fd/2`

### 前端关键修改

1. **admin/vite.config.ts**
   - 添加 `base: '/admin/'`

2. **admin/src/routes.tsx**
   - 添加 `basename: '/admin'`

3. **admin/src/lib/oidcConfig.ts**
   - redirect_uri 改为函数，运行时计算

4. **admin/src/lib/apiClient.ts**
   - 智能检测 Admin API 地址（10231 端口）
   - 支持本地端口访问和域名访问

## 重新部署

```bash
# 1. 重新构建镜像
docker build -t await2719/oneid:latest .

# 2. 使用部署脚本
./deploy-oneid.sh

# 或手动部署
docker rm -f oneid-app
docker run -d --name oneid-app ... await2719/oneid:latest
```
