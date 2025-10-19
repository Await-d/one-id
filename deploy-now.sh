#!/bin/bash

# ============================================================
# OneID 远程部署脚本（无需安装依赖版）
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

# ============================================================
# 配置
# ============================================================

REMOTE_HOST="192.168.123.5"
REMOTE_PORT="2719"
REMOTE_USER="await"
REMOTE_PASSWORD="ZhangDong2580"
REMOTE_DEPLOY_DIR="/volume1/docker/1panel/apps/local/one-id"
LOCAL_PROJECT_DIR="/home/await/project/OneID"

# 排除目录
EXCLUDE_DIRS=(
    "backend/bin/"
    "backend/obj/"
    "backend/.vs/"
    "backend/tests/"
    "frontend/*/node_modules/"
    "frontend/*/dist/"
    "frontend/*/.vite/"
    ".git/"
    ".idea/"
    ".vscode/"
    "*.log"
)

# ============================================================
# 检查工具
# ============================================================

log_info "检查必需工具..."

if ! command -v sshpass &> /dev/null; then
    log_error "未安装 sshpass，请手动安装: sudo apt-get install -y sshpass"
    exit 1
fi

if ! command -v rsync &> /dev/null; then
    log_error "未安装 rsync，请手动安装: sudo apt-get install -y rsync"
    exit 1
fi

log_success "工具检查通过"

# ============================================================
# 测试连接
# ============================================================

log_info "测试 SSH 连接..."

if ! sshpass -p "$REMOTE_PASSWORD" ssh -p "$REMOTE_PORT" -o StrictHostKeyChecking=no -o ConnectTimeout=10 "$REMOTE_USER@$REMOTE_HOST" "echo 'OK'" &> /dev/null; then
    log_error "SSH 连接失败，请检查：\n  - 主机: $REMOTE_HOST\n  - 端口: $REMOTE_PORT\n  - 用户: $REMOTE_USER\n  - 密码是否正确"
    exit 1
fi

log_success "SSH 连接成功"

# ============================================================
# 构建排除参数
# ============================================================

RSYNC_EXCLUDE_ARGS=""
for exclude in "${EXCLUDE_DIRS[@]}"; do
    RSYNC_EXCLUDE_ARGS="$RSYNC_EXCLUDE_ARGS --exclude=$exclude"
done

# ============================================================
# 创建远程目录
# ============================================================

log_info "创建远程目录..."
sshpass -p "$REMOTE_PASSWORD" ssh -p "$REMOTE_PORT" -o StrictHostKeyChecking=no "$REMOTE_USER@$REMOTE_HOST" \
    "mkdir -p $REMOTE_DEPLOY_DIR"

# ============================================================
# 传输文件
# ============================================================

log_info "开始传输文件（这可能需要几分钟）..."

sshpass -p "$REMOTE_PASSWORD" rsync -avz --progress \
    -e "ssh -p $REMOTE_PORT -o StrictHostKeyChecking=no" \
    $RSYNC_EXCLUDE_ARGS \
    "$LOCAL_PROJECT_DIR/" \
    "$REMOTE_USER@$REMOTE_HOST:$REMOTE_DEPLOY_DIR/"

if [ $? -eq 0 ]; then
    log_success "文件传输完成"
else
    log_error "文件传输失败"
    exit 1
fi

# ============================================================
# 上传环境配置
# ============================================================

log_info "上传环境配置..."

if [ -f ".env.remote" ]; then
    sshpass -p "$REMOTE_PASSWORD" scp -P "$REMOTE_PORT" -o StrictHostKeyChecking=no \
        .env.remote "$REMOTE_USER@$REMOTE_HOST:$REMOTE_DEPLOY_DIR/.env"
    log_success "环境配置已上传"
else
    log_error "未找到 .env.remote 文件"
    exit 1
fi

# ============================================================
# 远程部署
# ============================================================

log_info "开始在远程服务器上部署..."

sshpass -p "$REMOTE_PASSWORD" ssh -p "$REMOTE_PORT" -o StrictHostKeyChecking=no "$REMOTE_USER@$REMOTE_HOST" << 'REMOTE_COMMANDS'

set -e
cd /volume1/docker/1panel/apps/local/one-id

echo ""
echo "====================================="
echo "检查 Docker..."
echo "====================================="

if ! command -v docker &> /dev/null; then
    echo "错误: 未安装 Docker"
    exit 1
fi

if ! docker compose version &> /dev/null; then
    echo "错误: 未安装 Docker Compose"
    exit 1
fi

echo "Docker 已就绪"

echo ""
echo "====================================="
echo "检查并生成 SSL 证书..."
echo "====================================="

if [ ! -f "nginx/ssl/cert.pem" ]; then
    echo "生成自签名证书..."
    mkdir -p nginx/ssl
    cd nginx/ssl
    openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
        -keyout key.pem -out cert.pem \
        -subj "/C=CN/ST=State/L=City/O=Organization/CN=192.168.123.5"
    chmod 644 cert.pem key.pem
    cd ../..
    echo "SSL 证书已生成"
else
    echo "SSL 证书已存在"
fi

echo ""
echo "====================================="
echo "停止旧容器..."
echo "====================================="

docker compose -f docker-compose.https.yml down || true

echo ""
echo "====================================="
echo "构建镜像..."
echo "====================================="

docker compose -f docker-compose.https.yml build --no-cache

echo ""
echo "====================================="
echo "启动服务..."
echo "====================================="

docker compose -f docker-compose.https.yml up -d

echo ""
echo "====================================="
echo "等待服务启动..."
echo "====================================="

sleep 15

echo ""
echo "====================================="
echo "检查服务状态..."
echo "====================================="

docker compose -f docker-compose.https.yml ps

echo ""
echo "====================================="
echo "查看日志..."
echo "====================================="

echo ""
echo "=== Identity Server ==="
docker compose -f docker-compose.https.yml logs --tail=30 identity

echo ""
echo "=== Admin API ==="
docker compose -f docker-compose.https.yml logs --tail=30 adminapi

REMOTE_COMMANDS

if [ $? -eq 0 ]; then
    log_success "远程部署完成！"
else
    log_error "远程部署失败"
    exit 1
fi

# ============================================================
# 完成
# ============================================================

echo ""
echo "============================================================"
log_success "OneID 已成功部署到远程服务器！"
echo "============================================================"
echo ""
echo "访问地址："
echo "  Identity Server: https://$REMOTE_HOST:9443"
echo "  Admin Portal:    https://$REMOTE_HOST:9444"
echo ""
echo "管理员账号："
echo "  用户名: admin"
echo "  密码:   Admin@123456"
echo "  邮箱:   285283010@qq.com"
echo ""
echo "管理服务："
echo "  运行 ./remote-manage.sh 管理远程服务"
echo ""
echo "注意："
echo "  首次访问 HTTPS 会提示证书不受信任（自签名）"
echo "  点击「高级」→「继续访问」即可"
echo ""
echo "============================================================"
