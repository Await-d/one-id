#!/bin/bash

# ============================================================
# OneID 快速部署脚本（一键部署版本）
# ============================================================
# 此脚本会自动完成所有部署步骤，无需人工干预
# ============================================================

set -e

# 颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

# ============================================================
# 配置检查
# ============================================================

log_info "OneID 快速部署脚本"
echo ""
echo "此脚本将："
echo "  1. 构建最新的 Docker 镜像"
echo "  2. 停止并删除旧容器"
echo "  3. 启动新容器"
echo ""
read -p "确认开始部署？(y/N) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    log_info "已取消部署"
    exit 0
fi

# ============================================================
# 步骤 1: 本地构建 Docker 镜像
# ============================================================

log_info "步骤 1/3: 本地构建 Docker 镜像..."

docker build -t await2719/oneid:latest .

if [ $? -ne 0 ]; then
    log_error "Docker 镜像构建失败"
    exit 1
fi

log_success "Docker 镜像构建完成"

# ============================================================
# 步骤 2: 停止并删除旧容器
# ============================================================

log_info "步骤 2/3: 停止旧容器..."

docker stop oneid-app 2>/dev/null || true
docker rm oneid-app 2>/dev/null || true

log_success "旧容器已清理"

# ============================================================
# 步骤 3: 启动新容器
# ============================================================

log_info "步骤 3/3: 启动新容器..."

docker run -d \
  --name oneid-app \
  --restart unless-stopped \
  --network 1panel-network \
  -p 127.0.0.1:10230:5101 \
  -p 127.0.0.1:10231:5102 \
  -v /volume1/docker/1panel/apps/local/one-id/data:/app/data \
  -v /volume1/docker/1panel/apps/local/one-id/logs:/app/logs \
  -v /volume1/docker/1panel/apps/local/one-id/shared-keys:/app/shared-keys \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e TZ=Asia/Shanghai \
  -e ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
  -e ASPNETCORE_URLS="http://+:5101" \
  -e ConnectionStrings__Default="Host=1Panel-postgresql-WSXy;Port=5432;Database=oneid;Username=oneid;Password=H2GiCAJpST7ji7k3" \
  -e Persistence__Provider=Postgres \
  -e Redis__ConnectionString="1Panel-redis-2Awp:6379,password=bdtdhc6DizYQYbEZ" \
  -e Seed__Admin__Username=await \
  -e Seed__Admin__Password=Await2580 \
  -e Seed__Admin__Email=285283010@qq.com \
  -e Seed__Oidc__ClientId=spa.portal \
  -e Seed__Oidc__ClientSecret=await29_secret_oneid_foralawery \
  -e Seed__Oidc__RedirectUri=https://auth.awitk.cn/callback \
  -e LOGIN_REDIRECT_URIS="https://auth.awitk.cn/callback" \
  -e LOGIN_LOGOUT_URIS="https://auth.awitk.cn" \
  -e ADMIN_REDIRECT_URIS="https://auth.awitk.cn/admin/callback,https://auth.awitk.cn/callback" \
  -e ADMIN_LOGOUT_URIS="https://auth.awitk.cn/admin,https://auth.awitk.cn" \
  await2719/oneid:latest

if [ $? -ne 0 ]; then
    log_error "容器启动失败"
    exit 1
fi

log_success "新容器已启动"

# ============================================================
# 完成
# ============================================================

echo ""
echo "============================================================"
log_success "OneID 快速部署完成！"
echo "============================================================"
echo ""
echo "容器状态："
docker ps --filter name=oneid-app --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
echo ""
echo "查看日志："
echo "  docker logs -f oneid-app"
echo ""
echo "测试端点："
echo "  curl http://localhost:10230/.well-known/openid-configuration"
echo "  curl http://localhost:10230/health"
echo ""
echo "访问地址："
echo "  Identity Server: https://auth.awitk.cn"
echo "  Admin Portal:    https://auth.awitk.cn/admin"
echo ""
echo "============================================================"
echo "============================================================"
