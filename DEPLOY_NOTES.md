# OneID 部署注意事项

## 🔑 管理员密码要求

管理员密码必须满足以下条件:
- ✅ 至少 8 个字符
- ✅ 包含至少 1 个小写字母
- ✅ 包含至少 1 个数字

**示例:**
- ✅ `Admin123` (8 字符,有大写、小写和数字)
- ✅ `mypass88` (8 字符,有小写和数字)
- ✅ `SecurePass2024` (更安全)
- ❌ `admin` (太短,没有数字)
- ❌ `zd2580` (太短,只有 6 字符)

## 📁 需要挂载的目录

为了数据持久化和正常运行,请挂载以下目录:

```bash
-v /path/to/data:/app/data                    # 应用数据
-v /path/to/logs:/app/logs                    # 日志文件
-v /path/to/shared-keys:/app/shared-keys      # DataProtection 密钥(重要!)
```

### 为什么需要 shared-keys?

`/app/shared-keys` 存储 ASP.NET Core Data Protection 密钥,用于:
- 加密 Cookie
- 加密敏感配置
- MFA 密钥加密
- 外部认证密钥加密

**⚠️ 如果不挂载此目录:**
- 容器重启后用户会被强制登出
- MFA 密钥会失效
- 外部认证配置会丢失

## 🚀 完整部署命令

```bash
docker run -d \
  --name oneid-app \
  --restart unless-stopped \
  -p 10230:5101 \
  -p 10231:5102 \
  -v /path/to/data:/app/data \
  -v /path/to/logs:/app/logs \
  -v /path/to/shared-keys:/app/shared-keys \
  -e ConnectionStrings__Default="Host=your-db;Port=5432;Database=oneid;Username=user;Password=pass" \
  -e Persistence__Provider=Postgres \
  -e Seed__Admin__Username=admin \
  -e Seed__Admin__Password=YourSecurePass123 \
  -e Seed__Admin__Email=admin@example.com \
  await2719/oneid:latest
```

## 🔍 常见错误

### 1. 密码太短
```
Failed to create admin user: Passwords must be at least 8 characters.
```
**解决:** 使用至少 8 个字符的密码,包含小写字母和数字。

### 2. 数据库表已存在
```
relation "AspNetRoles" already exists
```
**解决:** 这是正常提示,应用会自动跳过迁移继续运行。

### 3. DataProtection 警告
```
Storing keys in a directory '/app/shared-keys' that may not be persisted
```
**解决:** 添加 volume 挂载: `-v /path/to/shared-keys:/app/shared-keys`

## 📞 获取帮助

- GitHub Issues: https://github.com/Await-d/one-id/issues
- 文档: 查看 README.md
