#!/bin/bash
# OneID 最终部署脚本
# 支持 Login SPA 和 Admin Portal 双应用部署

set -e

echo "====== OneID 部署脚本 ======"
echo ""
echo "停止旧容器..."
docker rm -f oneid-app 2>/dev/null || true

echo "启动新容器..."
docker run -d \
  --name oneid-app \
  --restart unless-stopped \
  --network oneid-network \
  -p 10230:5101 \
  -p 10231:5102 \
  -v /volume1/docker/1panel/apps/local/one-id/data:/app/data \
  -v /volume1/docker/1panel/apps/local/one-id/logs:/app/logs \
  -v /volume1/docker/1panel/apps/local/one-id/shared-keys:/app/shared-keys \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e TZ=Asia/Shanghai \
  -e ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
  -e ASPNETCORE_URLS="http://+:5101" \
  -e ConnectionStrings__Default="Host=oneid-postgres-prod;Port=5432;Database=oneid;Username=oneid;Password=OneID_Secure_Password_2024" \
  -e Persistence__Provider=Postgres \
  -e Redis__ConnectionString="oneid-redis-prod:6379" \
  -e Seed__Admin__Username=await \
  -e Seed__Admin__Password=Await2580 \
  -e Seed__Admin__Email=285283010@qq.com \
  -e Seed__Oidc__ClientId=spa.portal \
  -e Seed__Oidc__ClientSecret=await29_secret_oneid_foralawery \
  -e Seed__Oidc__RedirectUri=https://auth.awitk.cn/callback \
  -e LOGIN_REDIRECT_URIS="https://auth.awitk.cn/callback" \
  -e LOGIN_LOGOUT_URIS="https://auth.awitk.cn" \
  -e ADMIN_REDIRECT_URIS="https://auth.awitk.cn/admin/callback" \
  -e ADMIN_LOGOUT_URIS="https://auth.awitk.cn/admin" \
  await2719/oneid:latest

echo ""
echo "✅ 部署完成！"
echo ""
echo "容器状态:"
docker ps --filter name=oneid-app --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
echo ""
echo "查看日志:"
echo "  docker logs -f oneid-app"
echo ""
echo "访问地址:"
echo ""
echo "【Login SPA (用户登录)】"
echo "  本地:   http://localhost:10230"
echo "  生产:   https://auth.awitk.cn"
echo "  客户端: spa.portal"
echo ""
echo "【Admin Portal (管理后台)】"
echo "  本地:   http://localhost:10230/admin"
echo "  生产:   https://auth.awitk.cn/admin"
echo "  客户端: spa.admin"
echo ""
echo "管理员账号:"
echo "  用户名: await"
echo "  密码:   Await2580"
echo "  邮箱:   285283010@qq.com"
echo ""
