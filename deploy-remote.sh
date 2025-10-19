#!/bin/bash

# ============================================================
# OneID 远程部署脚本
# ============================================================
# 自动将 OneID 项目部署到远程 Docker 服务器
# ============================================================

set -e  # 遇到错误立即退出

# 颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 打印带颜色的消息
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# ============================================================
# 配置部分 - 根据实际情况修改
# ============================================================

# 远程服务器配置
REMOTE_HOST="192.168.123.5"
REMOTE_PORT="2719"
REMOTE_USER="await"
REMOTE_PASSWORD="ZhangDong2580"
REMOTE_DEPLOY_DIR="/volume1/docker/1panel/apps/local/one-id"

# 本地项目目录
LOCAL_PROJECT_DIR="/home/await/project/OneID"

# 需要排除的目录（不传输）
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
# 检查依赖工具
# ============================================================

log_info "检查必需的工具..."

# 检查 sshpass
if ! command -v sshpass &> /dev/null; then
    log_error "未安装 sshpass，正在尝试安装..."
    if command -v apt-get &> /dev/null; then
        sudo apt-get update && sudo apt-get install -y sshpass
    elif command -v yum &> /dev/null; then
        sudo yum install -y sshpass
    else
        log_error "无法自动安装 sshpass，请手动安装后再运行此脚本"
        exit 1
    fi
fi

# 检查 rsync
if ! command -v rsync &> /dev/null; then
    log_error "未安装 rsync，正在尝试安装..."
    if command -v apt-get &> /dev/null; then
        sudo apt-get update && sudo apt-get install -y rsync
    elif command -v yum &> /dev/null; then
        sudo yum install -y rsync
    else
        log_error "无法自动安装 rsync，请手动安装后再运行此脚本"
        exit 1
    fi
fi

log_success "所有必需工具已就绪"

# ============================================================
# SSH 连接测试
# ============================================================

log_info "测试 SSH 连接..."

if sshpass -p "$REMOTE_PASSWORD" ssh -p "$REMOTE_PORT" -o StrictHostKeyChecking=no -o ConnectTimeout=10 "$REMOTE_USER@$REMOTE_HOST" "echo 'SSH连接成功'" &> /dev/null; then
    log_success "SSH 连接测试成功"
else
    log_error "无法连接到远程服务器，请检查：\n  - 主机地址: $REMOTE_HOST\n  - 端口: $REMOTE_PORT\n  - 用户名: $REMOTE_USER\n  - 密码是否正确"
    exit 1
fi

# ============================================================
# 构建 rsync 排除参数
# ============================================================

RSYNC_EXCLUDE_ARGS=""
for exclude in "${EXCLUDE_DIRS[@]}"; do
    RSYNC_EXCLUDE_ARGS="$RSYNC_EXCLUDE_ARGS --exclude=$exclude"
done

# ============================================================
# 传输项目文件
# ============================================================

log_info "开始传输项目文件到远程服务器..."
log_info "远程目录: $REMOTE_DEPLOY_DIR"

# 创建远程目录（如果不存在）
log_info "创建远程部署目录..."
sshpass -p "$REMOTE_PASSWORD" ssh -p "$REMOTE_PORT" -o StrictHostKeyChecking=no "$REMOTE_USER@$REMOTE_HOST" \
    "mkdir -p $REMOTE_DEPLOY_DIR"

# 使用 rsync 传输文件
log_info "正在同步文件（这可能需要几分钟）..."
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
# 远程部署操作
# ============================================================

log_info "开始在远程服务器上部署..."

# 执行远程命令
sshpass -p "$REMOTE_PASSWORD" ssh -p "$REMOTE_PORT" -o StrictHostKeyChecking=no "$REMOTE_USER@$REMOTE_HOST" << 'REMOTE_COMMANDS'

set -e

# 进入部署目录
cd /volume1/docker/1panel/apps/local/one-id

echo "====================================="
echo "检查 Docker 和 Docker Compose..."
echo "====================================="

# 检查 Docker
if ! command -v docker &> /dev/null; then
    echo "错误: 未安装 Docker"
    exit 1
fi

# 检查 Docker Compose
if ! docker compose version &> /dev/null; then
    echo "错误: 未安装 Docker Compose 或版本过旧"
    exit 1
fi

echo "Docker 和 Docker Compose 已就绪"

echo ""
echo "====================================="
echo "停止旧的容器（如果存在）..."
echo "====================================="

# 停止并删除旧容器
docker compose -f docker-compose.https.yml down || true

echo ""
echo "====================================="
echo "构建 Docker 镜像..."
echo "====================================="

# 构建镜像
docker compose -f docker-compose.https.yml build --no-cache

echo ""
echo "====================================="
echo "启动服务..."
echo "====================================="

# 启动服务
docker compose -f docker-compose.https.yml up -d

echo ""
echo "====================================="
echo "等待服务启动..."
echo "====================================="

# 等待服务健康检查通过
sleep 10

echo ""
echo "====================================="
echo "检查服务状态..."
echo "====================================="

# 显示容器状态
docker compose -f docker-compose.https.yml ps

echo ""
echo "====================================="
echo "检查容器日志（最后 20 行）..."
echo "====================================="

# 显示 Identity Server 日志
echo ""
echo "=== Identity Server 日志 ==="
docker compose -f docker-compose.https.yml logs --tail=20 identity

# 显示 Admin API 日志
echo ""
echo "=== Admin API 日志 ==="
docker compose -f docker-compose.https.yml logs --tail=20 adminapi

echo ""
echo "====================================="
echo "部署完成！"
echo "====================================="

REMOTE_COMMANDS

if [ $? -eq 0 ]; then
    log_success "远程部署执行成功！"
else
    log_error "远程部署执行失败"
    exit 1
fi

# ============================================================
# 显示部署信息
# ============================================================

echo ""
echo "============================================================"
log_success "OneID 已成功部署到远程服务器！"
echo "============================================================"
echo ""
echo "访问信息："
echo "  Identity Server (登录界面): https://$REMOTE_HOST:9443"
echo "  Admin Portal (管理后台):    https://$REMOTE_HOST:9444"
echo ""
echo "管理员账号（查看远程服务器的 .env 文件）："
echo "  用户名: admin"
echo "  密码:   (在 .env 文件中的 ADMIN_PASSWORD)"
echo ""
echo "常用命令（在远程服务器上执行）："
echo "  查看日志:   docker compose -f docker-compose.https.yml logs -f"
echo "  重启服务:   docker compose -f docker-compose.https.yml restart"
echo "  停止服务:   docker compose -f docker-compose.https.yml down"
echo ""
echo "注意事项："
echo "  1. 首次访问 HTTPS 时，浏览器会提示证书不受信任（自签名证书）"
echo "  2. 点击「高级」→「继续访问」即可"
echo "  3. 生产环境建议使用真实的 SSL 证书"
echo ""
echo "============================================================"
