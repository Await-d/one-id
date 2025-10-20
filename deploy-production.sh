#!/bin/bash

# OneID 生产环境部署脚本
# 使用单域名 + Nginx 反向代理方案
# 域名: auth.awitk.cn

set -e  # 遇到错误立即退出

echo "====== OneID 生产环境部署脚本 ======"
echo ""

# 配置变量
DOMAIN="auth.awitk.cn"
CONTAINER_NAME="oneid-app"
IMAGE_NAME="await2719/oneid:latest"
POSTGRES_CONTAINER="oneid-postgres-prod"
REDIS_CONTAINER="oneid-redis-prod"
NETWORK_NAME="oneid-network"

# 数据库配置
DB_PASSWORD="OneID_Secure_Password_2024"  # 生产环境请修改为更强的密码
ADMIN_USERNAME="await"
ADMIN_PASSWORD="Await2580"  # 生产环境请修改为更强的密码
ADMIN_EMAIL="285283010@qq.com"

# OIDC 客户端配置
OIDC_CLIENT_ID="spa.portal"
OIDC_CLIENT_SECRET="await29_secret_oneid_foralawery"  # 生产环境请修改

echo "🔍 检查 Docker 网络..."
if ! docker network inspect $NETWORK_NAME &>/dev/null; then
    echo "创建 Docker 网络: $NETWORK_NAME"
    docker network create $NETWORK_NAME
else
    echo "✓ Docker 网络已存在"
fi

echo ""
echo "🔍 检查 PostgreSQL 容器..."
if ! docker ps -a --format '{{.Names}}' | grep -q "^${POSTGRES_CONTAINER}$"; then
    echo "启动 PostgreSQL 容器..."
    docker run -d \
        --name $POSTGRES_CONTAINER \
        --network $NETWORK_NAME \
        --restart unless-stopped \
        -e POSTGRES_DB=oneid \
        -e POSTGRES_USER=oneid \
        -e POSTGRES_PASSWORD=$DB_PASSWORD \
        -e TZ=Asia/Shanghai \
        -v oneid-postgres-data:/var/lib/postgresql/data \
        postgres:16-alpine

    echo "⏳ 等待 PostgreSQL 启动..."
    sleep 10
else
    echo "✓ PostgreSQL 容器已存在"
    if ! docker ps --format '{{.Names}}' | grep -q "^${POSTGRES_CONTAINER}$"; then
        echo "启动 PostgreSQL 容器..."
        docker start $POSTGRES_CONTAINER
        sleep 5
    fi
fi

echo ""
echo "🔍 检查 Redis 容器..."
if ! docker ps -a --format '{{.Names}}' | grep -q "^${REDIS_CONTAINER}$"; then
    echo "启动 Redis 容器..."
    docker run -d \
        --name $REDIS_CONTAINER \
        --network $NETWORK_NAME \
        --restart unless-stopped \
        -e TZ=Asia/Shanghai \
        -v oneid-redis-data:/data \
        redis:7-alpine redis-server --appendonly yes
else
    echo "✓ Redis 容器已存在"
    if ! docker ps --format '{{.Names}}' | grep -q "^${REDIS_CONTAINER}$"; then
        echo "启动 Redis 容器..."
        docker start $REDIS_CONTAINER
        sleep 2
    fi
fi

echo ""
echo "🛑 停止旧的 OneID 应用容器..."
if docker ps -a --format '{{.Names}}' | grep -q "^${CONTAINER_NAME}$"; then
    docker stop $CONTAINER_NAME 2>/dev/null || true
    docker rm $CONTAINER_NAME 2>/dev/null || true
fi

echo ""
echo "🚀 启动新的 OneID 应用容器..."
docker run -d \
  --name $CONTAINER_NAME \
  --restart unless-stopped \
  --network $NETWORK_NAME \
  -p 127.0.0.1:10230:5101 \
  -p 127.0.0.1:10231:5102 \
  -e TZ=Asia/Shanghai \
  -e ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
  -e ASPNETCORE_URLS="http://+:5101" \
  -e ConnectionStrings__Default="Host=${POSTGRES_CONTAINER};Port=5432;Database=oneid;Username=oneid;Password=${DB_PASSWORD}" \
  -e Persistence__Provider=Postgres \
  -e Redis__ConnectionString="${REDIS_CONTAINER}:6379" \
  -e Seed__Admin__Username=$ADMIN_USERNAME \
  -e Seed__Admin__Password=$ADMIN_PASSWORD \
  -e Seed__Admin__Email=$ADMIN_EMAIL \
  -e Seed__Admin__DisplayName="Platform Admin" \
  -e Seed__Oidc__ClientId=$OIDC_CLIENT_ID \
  -e Seed__Oidc__ClientSecret=$OIDC_CLIENT_SECRET \
  -e Seed__Oidc__RedirectUri=https://${DOMAIN}/callback \
  -e LOGIN_REDIRECT_URIS="https://${DOMAIN}/callback" \
  -e LOGIN_LOGOUT_URIS="https://${DOMAIN}" \
  -e ADMIN_REDIRECT_URIS="https://${DOMAIN}/admin/callback" \
  -e ADMIN_LOGOUT_URIS="https://${DOMAIN}/admin" \
  $IMAGE_NAME

echo ""
echo "⏳ 等待服务启动..."
sleep 10

echo ""
echo "✅ 部署完成！"
echo ""
echo "============================================"
echo "容器状态:"
docker ps --filter "name=${CONTAINER_NAME}" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
echo ""
echo "查看日志:"
echo "  docker logs -f $CONTAINER_NAME"
echo ""
echo "============================================"
echo "访问地址（需要先配置 Nginx）:"
echo ""
echo "【用户登录】"
echo "  https://${DOMAIN}"
echo ""
echo "【Admin Portal】"
echo "  https://${DOMAIN}/admin"
echo ""
echo "【OIDC Discovery】"
echo "  https://${DOMAIN}/.well-known/openid-configuration"
echo ""
echo "【Admin API Swagger】"
echo "  https://${DOMAIN}/admin/swagger"
echo ""
echo "管理员账号:"
echo "  用户名: $ADMIN_USERNAME"
echo "  密码:   $ADMIN_PASSWORD"
echo "  邮箱:   $ADMIN_EMAIL"
echo ""
echo "============================================"
echo "📝 下一步操作:"
echo "1. 配置 Nginx 反向代理（参考 nginx/oneid.conf）"
echo "2. 申请 SSL 证书（使用 Let's Encrypt）"
echo "3. 配置防火墙规则"
echo "4. 修改默认密码！"
echo "============================================"
