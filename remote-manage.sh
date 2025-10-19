#!/bin/bash

# ============================================================
# OneID 远程管理脚本
# ============================================================
# 用于管理远程服务器上的 OneID 服务
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
# 配置部分
# ============================================================

REMOTE_HOST="192.168.123.5"
REMOTE_PORT="2719"
REMOTE_USER="await"
REMOTE_PASSWORD="ZhangDong2580"
REMOTE_DEPLOY_DIR="/volume1/docker/1panel/apps/local/one-id"

# ============================================================
# SSH 命令封装
# ============================================================

remote_exec() {
    sshpass -p "$REMOTE_PASSWORD" ssh -p "$REMOTE_PORT" -o StrictHostKeyChecking=no "$REMOTE_USER@$REMOTE_HOST" "cd $REMOTE_DEPLOY_DIR && $1"
}

# ============================================================
# 菜单功能
# ============================================================

show_menu() {
    echo ""
    echo "============================================================"
    echo "         OneID 远程服务管理工具"
    echo "============================================================"
    echo "远程服务器: $REMOTE_USER@$REMOTE_HOST:$REMOTE_PORT"
    echo "部署目录: $REMOTE_DEPLOY_DIR"
    echo "============================================================"
    echo ""
    echo "请选择操作："
    echo "  1) 查看服务状态"
    echo "  2) 查看实时日志（所有服务）"
    echo "  3) 查看 Identity Server 日志"
    echo "  4) 查看 Admin API 日志"
    echo "  5) 查看 Nginx 日志"
    echo "  6) 重启所有服务"
    echo "  7) 重启 Identity Server"
    echo "  8) 重启 Admin API"
    echo "  9) 停止所有服务"
    echo " 10) 启动所有服务"
    echo " 11) 查看资源使用情况"
    echo " 12) 清理 Docker 缓存"
    echo " 13) 重新构建并部署"
    echo "  0) 退出"
    echo ""
    echo -n "请输入选项 [0-13]: "
}

# 1) 查看服务状态
view_status() {
    log_info "正在查看服务状态..."
    remote_exec "docker compose -f docker-compose.https.yml ps"
}

# 2) 查看实时日志（所有服务）
view_logs_all() {
    log_info "正在查看实时日志（按 Ctrl+C 退出）..."
    sshpass -p "$REMOTE_PASSWORD" ssh -p "$REMOTE_PORT" -o StrictHostKeyChecking=no "$REMOTE_USER@$REMOTE_HOST" \
        "cd $REMOTE_DEPLOY_DIR && docker compose -f docker-compose.https.yml logs -f"
}

# 3) 查看 Identity Server 日志
view_logs_identity() {
    log_info "正在查看 Identity Server 日志（按 Ctrl+C 退出）..."
    sshpass -p "$REMOTE_PASSWORD" ssh -p "$REMOTE_PORT" -o StrictHostKeyChecking=no "$REMOTE_USER@$REMOTE_HOST" \
        "cd $REMOTE_DEPLOY_DIR && docker compose -f docker-compose.https.yml logs -f identity"
}

# 4) 查看 Admin API 日志
view_logs_admin() {
    log_info "正在查看 Admin API 日志（按 Ctrl+C 退出）..."
    sshpass -p "$REMOTE_PASSWORD" ssh -p "$REMOTE_PORT" -o StrictHostKeyChecking=no "$REMOTE_USER@$REMOTE_HOST" \
        "cd $REMOTE_DEPLOY_DIR && docker compose -f docker-compose.https.yml logs -f adminapi"
}

# 5) 查看 Nginx 日志
view_logs_nginx() {
    log_info "正在查看 Nginx 日志（按 Ctrl+C 退出）..."
    sshpass -p "$REMOTE_PASSWORD" ssh -p "$REMOTE_PORT" -o StrictHostKeyChecking=no "$REMOTE_USER@$REMOTE_HOST" \
        "cd $REMOTE_DEPLOY_DIR && docker compose -f docker-compose.https.yml logs -f nginx"
}

# 6) 重启所有服务
restart_all() {
    log_info "正在重启所有服务..."
    remote_exec "docker compose -f docker-compose.https.yml restart"
    log_success "所有服务已重启"
}

# 7) 重启 Identity Server
restart_identity() {
    log_info "正在重启 Identity Server..."
    remote_exec "docker compose -f docker-compose.https.yml restart identity"
    log_success "Identity Server 已重启"
}

# 8) 重启 Admin API
restart_admin() {
    log_info "正在重启 Admin API..."
    remote_exec "docker compose -f docker-compose.https.yml restart adminapi"
    log_success "Admin API 已重启"
}

# 9) 停止所有服务
stop_all() {
    log_info "正在停止所有服务..."
    remote_exec "docker compose -f docker-compose.https.yml stop"
    log_success "所有服务已停止"
}

# 10) 启动所有服务
start_all() {
    log_info "正在启动所有服务..."
    remote_exec "docker compose -f docker-compose.https.yml up -d"
    log_success "所有服务已启动"
}

# 11) 查看资源使用情况
view_resources() {
    log_info "正在查看资源使用情况..."
    remote_exec "docker stats --no-stream"
}

# 12) 清理 Docker 缓存
clean_docker() {
    log_info "正在清理 Docker 缓存..."
    remote_exec "docker system prune -f"
    log_success "Docker 缓存已清理"
}

# 13) 重新构建并部署
rebuild_deploy() {
    log_info "正在重新构建并部署..."
    read -p "这将停止所有服务并重新构建镜像，是否继续？(y/N) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        remote_exec "docker compose -f docker-compose.https.yml down && docker compose -f docker-compose.https.yml build --no-cache && docker compose -f docker-compose.https.yml up -d"
        log_success "重新部署完成"
    else
        log_info "已取消操作"
    fi
}

# ============================================================
# 主循环
# ============================================================

# 检查依赖
if ! command -v sshpass &> /dev/null; then
    log_error "未安装 sshpass，请先运行 deploy-remote.sh 脚本"
    exit 1
fi

while true; do
    show_menu
    read -r choice

    case $choice in
        1) view_status ;;
        2) view_logs_all ;;
        3) view_logs_identity ;;
        4) view_logs_admin ;;
        5) view_logs_nginx ;;
        6) restart_all ;;
        7) restart_identity ;;
        8) restart_admin ;;
        9) stop_all ;;
        10) start_all ;;
        11) view_resources ;;
        12) clean_docker ;;
        13) rebuild_deploy ;;
        0)
            log_info "退出管理工具"
            exit 0
            ;;
        *)
            log_error "无效的选项，请重新选择"
            ;;
    esac

    echo ""
    read -p "按 Enter 键继续..."
done
