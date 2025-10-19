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

log_info "OneID 一键部署脚本"
echo ""
echo "目标服务器: 192.168.123.5:2719"
echo "部署目录: /volume1/docker/1panel/apps/local/one-id"
echo ""
read -p "确认开始部署？(y/N) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    log_info "已取消部署"
    exit 0
fi

# ============================================================
# 步骤 1: 准备远程环境配置文件
# ============================================================

log_info "步骤 1/4: 准备远程环境配置..."

if [ ! -f ".env.remote" ]; then
    log_error "未找到 .env.remote 文件，请先创建此文件"
    exit 1
fi

# 临时复制 .env.remote 为 .env（用于传输）
cp .env.remote .env.remote.backup
log_success "环境配置准备完成"

# ============================================================
# 步骤 2: 执行主部署脚本
# ============================================================

log_info "步骤 2/4: 开始传输文件和部署..."

./deploy-remote.sh

if [ $? -ne 0 ]; then
    log_error "部署失败"
    exit 1
fi

log_success "部署脚本执行完成"

# ============================================================
# 步骤 3: 更新远程服务器的 .env 文件
# ============================================================

log_info "步骤 3/4: 更新远程服务器的环境配置..."

REMOTE_HOST="192.168.123.5"
REMOTE_PORT="2719"
REMOTE_USER="await"
REMOTE_PASSWORD="ZhangDong2580"
REMOTE_DEPLOY_DIR="/volume1/docker/1panel/apps/local/one-id"

# 上传 .env.remote 作为远程服务器的 .env
sshpass -p "$REMOTE_PASSWORD" scp -P "$REMOTE_PORT" -o StrictHostKeyChecking=no \
    .env.remote "$REMOTE_USER@$REMOTE_HOST:$REMOTE_DEPLOY_DIR/.env"

log_success "环境配置已更新"

# ============================================================
# 步骤 4: 检查 SSL 证书
# ============================================================

log_info "步骤 4/4: 检查 SSL 证书..."

# 检查远程服务器是否有 SSL 证书
HAS_CERT=$(sshpass -p "$REMOTE_PASSWORD" ssh -p "$REMOTE_PORT" -o StrictHostKeyChecking=no "$REMOTE_USER@$REMOTE_HOST" \
    "[ -f $REMOTE_DEPLOY_DIR/nginx/ssl/cert.pem ] && echo 'yes' || echo 'no'")

if [ "$HAS_CERT" == "no" ]; then
    log_warning "未找到 SSL 证书，正在生成自签名证书..."

    # 在远程服务器上生成自签名证书
    sshpass -p "$REMOTE_PASSWORD" ssh -p "$REMOTE_PORT" -o StrictHostKeyChecking=no "$REMOTE_USER@$REMOTE_HOST" << 'CERT_GEN'
cd /volume1/docker/1panel/apps/local/one-id
mkdir -p nginx/ssl
cd nginx/ssl

# 生成自签名证书
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout key.pem -out cert.pem \
    -subj "/C=CN/ST=State/L=City/O=Organization/CN=192.168.123.5"

chmod 644 cert.pem key.pem

echo "SSL 证书已生成"
CERT_GEN

    log_success "SSL 证书已生成（自签名，有效期 365 天）"
else
    log_success "SSL 证书已存在"
fi

# ============================================================
# 步骤 5: 重启服务以应用配置
# ============================================================

log_info "重启服务以应用新配置..."

sshpass -p "$REMOTE_PASSWORD" ssh -p "$REMOTE_PORT" -o StrictHostKeyChecking=no "$REMOTE_USER@$REMOTE_HOST" \
    "cd $REMOTE_DEPLOY_DIR && docker compose -f docker-compose.https.yml restart"

log_success "服务已重启"

# ============================================================
# 完成
# ============================================================

echo ""
echo "============================================================"
log_success "OneID 部署完成！"
echo "============================================================"
echo ""
echo "访问地址："
echo "  Identity Server: https://192.168.123.5:9443"
echo "  Admin Portal:    https://192.168.123.5:9444"
echo ""
echo "管理员账号："
echo "  用户名: admin"
echo "  密码:   Admin@123456 (请尽快修改)"
echo "  邮箱:   285283010@qq.com"
echo ""
echo "管理工具："
echo "  运行 ./remote-manage.sh 可以管理远程服务"
echo "  - 查看日志"
echo "  - 重启服务"
echo "  - 查看状态"
echo ""
echo "注意事项："
echo "  1. 首次访问 HTTPS 时会提示证书不受信任（自签名证书）"
echo "  2. 点击「高级」→「继续访问」即可"
echo "  3. 生产环境建议替换为真实的 SSL 证书"
echo ""
echo "============================================================"
