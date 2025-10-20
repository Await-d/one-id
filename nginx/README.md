# Nginx 配置文件

本目录包含 OneID 的 Nginx 反向代理配置文件。

## 文件说明

- `oneid.conf` - 生产环境 Nginx 配置（单域名方案）

## 使用方法

### 1. 复制配置文件到 Nginx

```bash
sudo cp oneid.conf /etc/nginx/sites-available/oneid.conf
sudo ln -s /etc/nginx/sites-available/oneid.conf /etc/nginx/sites-enabled/
```

### 2. 测试配置

```bash
sudo nginx -t
```

### 3. 重载 Nginx

```bash
sudo systemctl reload nginx
```

## 配置说明

### 路由规则

| 路径 | 后端服务 | 说明 |
|------|---------|------|
| `/admin/api/*` | Admin API (localhost:10231) | 管理后台 API |
| `/admin/swagger` | Admin API (localhost:10231) | API 文档（可禁用） |
| `/*` | Identity Server (localhost:10230) | OIDC 服务 + 前端页面 |

### SSL 配置

配置文件中的 SSL 证书路径为 Let's Encrypt 默认路径：
- 证书: `/etc/letsencrypt/live/auth.awitk.cn/fullchain.pem`
- 私钥: `/etc/letsencrypt/live/auth.awitk.cn/privkey.pem`

### 安全响应头

配置已启用以下安全响应头：
- `Strict-Transport-Security` (HSTS)
- `X-Frame-Options`
- `X-Content-Type-Options`
- `X-XSS-Protection`
- `Referrer-Policy`

### 性能优化

- 启用 Gzip 压缩
- SSL Session 缓存
- 代理缓冲优化
- WebSocket 支持

## 注意事项

1. 请确保域名 `auth.awitk.cn` 已正确解析到服务器 IP
2. 生产环境建议禁用 Swagger UI
3. 定期检查 SSL 证书有效期
4. 建议配置日志轮转以避免日志文件过大

## 故障排查

### 502 Bad Gateway

检查 OneID 应用是否正常运行：
```bash
docker ps | grep oneid-app
curl http://localhost:10230/health
curl http://localhost:10231/health
```

### SSL 证书错误

检查证书状态：
```bash
sudo certbot certificates
```

### 查看错误日志

```bash
sudo tail -f /var/log/nginx/oneid-error.log
```

## 相关文档

- [生产环境部署指南](../PRODUCTION_DEPLOYMENT.md)
- [Nginx 官方文档](https://nginx.org/en/docs/)
- [Let's Encrypt 文档](https://letsencrypt.org/docs/)
