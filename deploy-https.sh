#!/bin/bash

# ============================================================
# OneID HTTPS 快速部署脚本
# ============================================================
# 
# 用途：一键部署 OneID HTTPS 服务
# 作者：OneID Team
# 版本：2.1 (优化版 - 添加前端检查和构建优化)
#
# ============================================================

set -e  # 遇到错误立即退出

# 设置清理陷阱
cleanup_on_error() {
    local exit_code=$?
    if [ $exit_code -ne 0 ]; then
        print_error "部署过程中发生错误 (退出码: $exit_code)"
        echo ""
        echo "错误排查建议："
        echo "  1. 检查前端是否能正常编译"
        echo "  2. 检查 Docker 服务是否正常运行"
        echo "  3. 查看日志: docker compose -f docker-compose.https.yml logs"
        echo ""
    fi
}

trap cleanup_on_error EXIT

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# 打印函数
print_header() {
    echo -e "${BLUE}============================================================${NC}"
    echo -e "${BLUE}  $1${NC}"
    echo -e "${BLUE}============================================================${NC}"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

print_info() {
    echo -e "${CYAN}ℹ️  $1${NC}"
}

# 检查依赖
check_dependencies() {
    print_header "检查系统依赖"
    
    local missing_deps=()
    
    # 检查 Docker
    if ! command -v docker &> /dev/null; then
        missing_deps+=("docker")
    else
        print_success "Docker 已安装: $(docker --version)"
    fi
    
    # 检查 Docker Compose
    if ! command -v docker compose version &> /dev/null; then
        if ! command -v docker-compose &> /dev/null; then
            missing_deps+=("docker compose")
        fi
    else
        print_success "Docker Compose 已安装: $(docker compose version)"
    fi
    
    # 检查 OpenSSL
    if ! command -v openssl &> /dev/null; then
        missing_deps+=("openssl")
    else
        print_success "OpenSSL 已安装: $(openssl version)"
    fi
    
    # 检查 pnpm (用于前端编译检查)
    if ! command -v pnpm &> /dev/null; then
        print_warning "pnpm 未安装，将跳过前端编译检查"
        print_info "建议安装 pnpm: npm install -g pnpm"
        SKIP_FRONTEND_CHECK=true
    else
        print_success "pnpm 已安装: $(pnpm --version)"
        SKIP_FRONTEND_CHECK=false
    fi
    
    if [ ${#missing_deps[@]} -ne 0 ]; then
        print_error "缺少以下依赖: ${missing_deps[*]}"
        echo ""
        echo "请先安装缺少的依赖："
        echo "  sudo apt-get update"
        echo "  sudo apt-get install -y docker.io docker-compose openssl"
        exit 1
    fi
    
    echo ""
}

# 检查前端编译
check_frontend() {
    if [ "$SKIP_FRONTEND_CHECK" = true ]; then
        print_warning "跳过前端编译检查（pnpm 未安装）"
        echo ""
        return 0
    fi
    
    print_header "检查前端编译"
    
    # 检查 Login SPA
    print_info "检查 Login SPA..."
    if [ ! -d "frontend/login" ]; then
        print_error "Login SPA 目录不存在"
        exit 1
    fi
    
    cd frontend/login
    if ! pnpm install --frozen-lockfile > /dev/null 2>&1; then
        print_error "Login SPA 依赖安装失败"
        cd ../..
        exit 1
    fi
    
    if ! pnpm build > /dev/null 2>&1; then
        print_error "Login SPA 编译失败"
        echo ""
        echo "请运行以下命令查看详细错误："
        echo "  cd frontend/login && pnpm build"
        cd ../..
        exit 1
    fi
    print_success "Login SPA 编译成功"
    cd ../..
    
    # 检查 Admin Portal
    print_info "检查 Admin Portal..."
    if [ ! -d "frontend/admin" ]; then
        print_error "Admin Portal 目录不存在"
        exit 1
    fi
    
    cd frontend/admin
    if ! pnpm install --frozen-lockfile > /dev/null 2>&1; then
        print_error "Admin Portal 依赖安装失败"
        cd ../..
        exit 1
    fi
    
    if ! pnpm build > /dev/null 2>&1; then
        print_error "Admin Portal 编译失败"
        echo ""
        echo "请运行以下命令查看详细错误："
        echo "  cd frontend/admin && pnpm build"
        cd ../..
        exit 1
    fi
    print_success "Admin Portal 编译成功"
    cd ../..
    
    print_success "所有前端项目编译通过！"
    echo ""
}

# 配置向导
configure() {
    print_header "配置向导"
    
    # 检查是否存在 .env 文件
    if [ -f ".env" ]; then
        print_warning "检测到现有的 .env 配置文件"
        echo ""
        read -p "是否使用现有配置？[Y/n] " -r
        echo
        
        if [[ ! $REPLY =~ ^[Nn]$ ]]; then
            print_info "加载现有配置..."
            # 加载现有配置
            source .env
            print_success "配置已加载"
            echo ""
            echo -e "${CYAN}当前配置:${NC}"
            echo -e "  域名/IP:         $DOMAIN"
            echo -e "  管理员用户名:    $ADMIN_USERNAME"
            echo -e "  Identity 端口:   $IDENTITY_PORT"
            echo -e "  Admin 端口:      $ADMIN_PORT"
            echo ""
            return 0
        fi
    fi
    
    # 获取本机IP
    DEFAULT_IP=$(hostname -I | awk '{print $1}')
    
    echo ""
    echo "请输入部署配置（直接回车使用默认值）："
    echo ""
    
    # 域名/IP
    read -p "域名或IP地址 [默认: $DEFAULT_IP]: " DOMAIN
    DOMAIN=${DOMAIN:-$DEFAULT_IP}
    
    # 数据库密码
    DEFAULT_DB_PASSWORD="OneID_Secure_Password_2024"
    read -sp "数据库密码 [默认: $DEFAULT_DB_PASSWORD]: " POSTGRES_PASSWORD
    echo ""
    POSTGRES_PASSWORD=${POSTGRES_PASSWORD:-$DEFAULT_DB_PASSWORD}
    
    # 管理员用户名
    read -p "管理员用户名 [默认: admin]: " ADMIN_USERNAME
    ADMIN_USERNAME=${ADMIN_USERNAME:-admin}
    
    # 管理员密码
    read -sp "管理员密码 [默认: Admin@123456]: " ADMIN_PASSWORD
    echo ""
    ADMIN_PASSWORD=${ADMIN_PASSWORD:-Admin@123456}
    
    # 管理员邮箱
    read -p "管理员邮箱 [默认: 285283010@qq.com]: " ADMIN_EMAIL
    ADMIN_EMAIL=${ADMIN_EMAIL:-285283010@qq.com}
    
    echo ""
    echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${CYAN}高级配置（可选）${NC}"
    echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo ""
    echo "端口配置（按回车使用默认值）："
    echo ""
    
    # Identity Server 端口
    read -p "Identity Server HTTPS 端口 [默认: 9443]: " IDENTITY_PORT
    IDENTITY_PORT=${IDENTITY_PORT:-9443}
    
    # Admin Portal 端口
    read -p "Admin Portal HTTPS 端口 [默认: 9444]: " ADMIN_PORT
    ADMIN_PORT=${ADMIN_PORT:-9444}
    
    # HTTP 端口（可选）
    read -p "HTTP 端口（自动重定向到HTTPS） [默认: 9080]: " HTTP_PORT
    HTTP_PORT=${HTTP_PORT:-9080}
    
    echo ""
    echo "回调地址配置（按回车使用默认值）："
    echo ""
    
    # 自动生成默认的回调地址
    DEFAULT_REDIRECT_URI="https://${DOMAIN}:${ADMIN_PORT}/callback"
    DEFAULT_LOGOUT_URI="https://${DOMAIN}:${ADMIN_PORT}"
    
    echo ""
    echo -e "${CYAN}提示: 如果使用反向代理或自定义域名，请输入实际访问地址${NC}"
    echo -e "${CYAN}多个地址用逗号分隔，例如: https://admin.company.com/callback,https://192.168.1.100:8443/callback${NC}"
    echo ""
    
    read -p "Admin Portal 回调地址 [默认: $DEFAULT_REDIRECT_URI]: " ADMIN_REDIRECT_URIS
    ADMIN_REDIRECT_URIS=${ADMIN_REDIRECT_URIS:-$DEFAULT_REDIRECT_URI}
    
    read -p "Admin Portal 登出地址 [默认: $DEFAULT_LOGOUT_URI]: " ADMIN_LOGOUT_URIS
    ADMIN_LOGOUT_URIS=${ADMIN_LOGOUT_URIS:-$DEFAULT_LOGOUT_URI}
    
    echo ""
    print_header "配置确认"
    echo ""
    echo -e "${CYAN}基础配置:${NC}"
    echo -e "  域名/IP:        $DOMAIN"
    echo -e "  数据库密码:      ******"
    echo -e "  管理员用户名:    $ADMIN_USERNAME"
    echo -e "  管理员密码:      ******"
    echo -e "  管理员邮箱:      $ADMIN_EMAIL"
    echo ""
    echo -e "${CYAN}端口配置:${NC}"
    echo -e "  Identity Server: https://${DOMAIN}:${IDENTITY_PORT}"
    echo -e "  Admin Portal:    https://${DOMAIN}:${ADMIN_PORT}"
    echo -e "  HTTP (重定向):   http://${DOMAIN}:${HTTP_PORT}"
    echo ""
    echo -e "${CYAN}OIDC 回调地址:${NC}"
    echo -e "  回调地址:        $ADMIN_REDIRECT_URIS"
    echo -e "  登出地址:        $ADMIN_LOGOUT_URIS"
    echo ""
    
    read -p "确认配置正确？[Y/n] " -r
    echo
    # 默认为 Y，只有明确输入 n 或 N 才取消
    if [[ $REPLY =~ ^[Nn]$ ]]; then
        print_warning "已取消部署"
        exit 1
    fi
    
    # 保存配置到 .env 文件
    print_info "保存配置到 .env 文件..."
    cat > .env << EOF
# OneID 生产环境配置
# 重要：请妥善保管此文件，不要提交到版本控制
# 生成时间: $(date '+%Y-%m-%d %H:%M:%S')

# 域名/IP配置
DOMAIN=$DOMAIN

# 数据库配置
POSTGRES_PASSWORD=$POSTGRES_PASSWORD

# 管理员账号配置
ADMIN_USERNAME=$ADMIN_USERNAME
ADMIN_PASSWORD=$ADMIN_PASSWORD
ADMIN_EMAIL=$ADMIN_EMAIL
ADMIN_DISPLAY_NAME="Platform Administrator"

# 端口配置
IDENTITY_PORT=$IDENTITY_PORT
ADMIN_PORT=$ADMIN_PORT
HTTP_PORT=$HTTP_PORT

# OIDC 回调地址配置
ADMIN_REDIRECT_URIS="$ADMIN_REDIRECT_URIS"
ADMIN_LOGOUT_URIS="$ADMIN_LOGOUT_URIS"

# Data Protection 应用名称（多个服务必须使用相同的名称）
DATAPROTECTION_APP_NAME="OneID"

# CORS 配置（允许的前端源，多个用逗号分隔）
# 注意：需要包含实际的访问地址和端口
IDENTITY_CORS_ALLOWED_ORIGINS="http://localhost:5173,https://$DOMAIN:$ADMIN_PORT,http://localhost:5102"
EOF
    print_success "配置已保存到 .env 文件"
    echo ""
    
    # 导出环境变量
    export DOMAIN
    export POSTGRES_PASSWORD
    export ADMIN_USERNAME
    export ADMIN_PASSWORD
    export ADMIN_EMAIL
    export IDENTITY_PORT
    export ADMIN_PORT
    export HTTP_PORT
    export ADMIN_REDIRECT_URIS
    export ADMIN_LOGOUT_URIS
    export DATAPROTECTION_APP_NAME
    export IDENTITY_CORS_ALLOWED_ORIGINS="http://localhost:5173,https://$DOMAIN:$ADMIN_PORT,http://localhost:5102"
}

# 生成SSL证书
generate_ssl() {
    print_header "生成 SSL 证书"
    
    cd nginx/ssl
    
    if [ -f "cert.pem" ] && [ -f "key.pem" ]; then
        print_warning "检测到已存在的证书文件"
        read -p "是否重新生成？[Y/n] " -r
        echo
        # 默认为 Y，只有明确输入 n 或 N 才使用现有证书
        if [[ $REPLY =~ ^[Nn]$ ]]; then
            print_info "使用现有证书"
            cd ../..
            return
        fi
    fi
    
    print_info "正在生成自签名证书..."
    
    # 生成证书
    openssl req -x509 -nodes -days 3650 \
        -newkey rsa:2048 \
        -keyout key.pem \
        -out cert.pem \
        -subj "/C=CN/ST=Beijing/L=Beijing/O=OneID/OU=IT/CN=$DOMAIN" \
        -addext "subjectAltName=DNS:$DOMAIN,DNS:localhost,IP:127.0.0.1,IP:$DOMAIN" \
        2>/dev/null
    
    # 设置权限
    chmod 600 key.pem
    chmod 644 cert.pem
    
    print_success "SSL 证书生成成功"
    
    cd ../..
    echo ""
}

# 停止旧服务
stop_old_services() {
    print_header "停止旧服务"
    
    if docker compose -f docker-compose.https.yml ps | grep -q "Up"; then
        print_info "检测到运行中的服务"
        echo ""
        print_warning "⚠️  是否删除现有数据卷（包括数据库数据）？"
        echo ""
        echo "  选择 Y: 完全重置，删除所有数据（数据库、Redis、密钥等）"
        echo "  选择 N: 保留数据，仅重启服务（推荐用于更新配置）"
        echo ""
        read -p "删除数据卷？[y/N] " -r
        echo
        
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            print_info "正在停止服务并删除数据卷..."
            docker compose -f docker-compose.https.yml down -v --remove-orphans
            print_warning "所有数据卷已删除，将使用新配置初始化数据库"
        else
            print_info "正在停止服务（保留数据卷）..."
            docker compose -f docker-compose.https.yml down --remove-orphans
            print_info "数据卷已保留，数据库密码等配置保持不变"
        fi
        print_success "旧服务已停止"
    else
        print_info "没有检测到运行中的服务"
    fi
    
    # 也检查普通版本（如果存在 docker-compose.yml）
    if [ -f "docker-compose.yml" ]; then
        if docker compose ps 2>/dev/null | grep -q "Up"; then
            print_info "检测到普通版本服务，正在停止..."
            docker compose down --remove-orphans
            print_success "普通版本服务已停止"
        fi
    fi
    
    echo ""
}

# 构建镜像
build_images() {
    print_header "构建 Docker 镜像"
    
    echo ""
    echo -e "${CYAN}构建选项:${NC}"
    echo "  1. 快速构建 (使用缓存，适合代码更新)"
    echo "  2. 完全重建 (不使用缓存，适合依赖更新)"
    echo ""
    read -p "请选择构建方式 [1/2，默认: 1]: " BUILD_OPTION
    BUILD_OPTION=${BUILD_OPTION:-1}
    
    echo ""
    
    if [ "$BUILD_OPTION" = "2" ]; then
        print_info "正在执行完全重建（不使用缓存）..."
        print_warning "这可能需要 5-10 分钟，将重新下载所有依赖"
        echo ""
        docker compose -f docker-compose.https.yml build --no-cache
    else
        print_info "正在执行快速构建（使用缓存）..."
        print_info "这通常只需要 1-2 分钟"
        echo ""
        docker compose -f docker-compose.https.yml build
    fi
    
    print_success "镜像构建完成"
    echo ""
}

# 启动服务
start_services() {
    print_header "启动服务"
    
    docker compose -f docker-compose.https.yml up -d
    
    print_success "服务启动命令已执行"
    echo ""
}

# 等待服务就绪
wait_for_services() {
    print_header "等待服务启动"
    
    echo "正在等待服务就绪..."
    echo ""
    
    # 先等待容器启动
    print_info "等待容器启动..."
    sleep 8
    
    # 检查容器状态
    print_info "检查容器状态..."
    if ! docker compose -f docker-compose.https.yml ps | grep -q "Up"; then
        print_error "容器启动失败"
        echo ""
        echo "请查看日志："
        echo "  docker compose -f docker-compose.https.yml logs"
        return 1
    fi
    print_success "所有容器已启动"
    
    # 等待 Identity Server 就绪（最多2分钟）
    print_info "等待 Identity Server 就绪..."
    local max_attempts=24
    local attempt=0
    local identity_port=${IDENTITY_PORT:-9443}
    
    while [ $attempt -lt $max_attempts ]; do
        # 检查 OIDC 配置端点
        if curl -k -s -f "https://localhost:${identity_port}/.well-known/openid-configuration" > /dev/null 2>&1; then
            print_success "Identity Server 已就绪！"
            break
        fi
        
        echo -n "."
        sleep 5
        attempt=$((attempt + 1))
    done
    
    if [ $attempt -eq $max_attempts ]; then
        echo ""
        print_warning "Identity Server 启动超时"
        print_info "服务可能仍在初始化，请稍后访问或查看日志"
        echo ""
        echo "查看日志命令："
        echo "  docker compose -f docker-compose.https.yml logs identity"
        return 1
    fi
    
    echo ""
    
    # 等待 Admin Portal 就绪
    print_info "等待 Admin Portal 就绪..."
    local admin_port=${ADMIN_PORT:-9444}
    attempt=0
    
    while [ $attempt -lt 12 ]; do
        if curl -k -s -f "https://localhost:${admin_port}/" > /dev/null 2>&1; then
            print_success "Admin Portal 已就绪！"
            echo ""
            return 0
        fi
        
        echo -n "."
        sleep 5
        attempt=$((attempt + 1))
    done
    
    echo ""
    print_warning "Admin Portal 启动超时"
    print_info "服务可能仍在初始化，请稍后访问或查看日志"
    echo ""
    echo "查看日志命令："
    echo "  docker compose -f docker-compose.https.yml logs adminapi"
    echo ""
}

# 显示部署信息
show_deployment_info() {
    print_header "部署完成"
    
    echo ""
    echo -e "${GREEN}🎉 OneID HTTPS 服务部署成功！${NC}"
    echo ""
    echo -e "${PURPLE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo ""
    echo -e "${CYAN}📍 访问地址:${NC}"
    echo ""
    echo -e "   ${YELLOW}登录界面:${NC}  https://$DOMAIN:$IDENTITY_PORT"
    
    # 显示配置的管理后台地址（从回调地址中提取）
    ADMIN_URL=$(echo "$ADMIN_REDIRECT_URIS" | cut -d',' -f1 | sed 's|/callback||')
    echo -e "   ${YELLOW}管理后台:${NC}  $ADMIN_URL"
    echo -e "   ${YELLOW}HTTP重定向:${NC} http://$DOMAIN:$HTTP_PORT (自动跳转到HTTPS)"
    
    if [[ "$ADMIN_REDIRECT_URIS" == *","* ]]; then
        echo ""
        echo -e "   ${CYAN}其他已配置的回调地址:${NC}"
        IFS=',' read -ra URIS <<< "$ADMIN_REDIRECT_URIS"
        for i in "${!URIS[@]}"; do
            if [ $i -gt 0 ]; then
                echo -e "   - $(echo "${URIS[$i]}" | sed 's|/callback||')"
            fi
        done
    fi
    echo ""
    echo -e "${PURPLE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo ""
    echo -e "${CYAN}🔐 管理员账号:${NC}"
    echo ""
    echo -e "   ${YELLOW}用户名:${NC}  $ADMIN_USERNAME"
    echo -e "   ${YELLOW}密码:${NC}    $ADMIN_PASSWORD"
    echo -e "   ${YELLOW}邮箱:${NC}    $ADMIN_EMAIL"
    echo ""
    echo -e "${PURPLE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo ""
    echo -e "${CYAN}📊 服务状态:${NC}"
    echo ""
    docker compose -f docker-compose.https.yml ps
    echo ""
    echo -e "${PURPLE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo ""
    echo -e "${CYAN}⚠️  重要提示:${NC}"
    echo ""
    echo "  1. 浏览器会显示证书不安全警告，这是正常的（自签名证书）"
    echo "  2. 点击 '高级' → '继续访问' 即可正常使用"
    echo "  3. 生产环境建议使用 Let's Encrypt 或购买正式证书"
    echo "  4. 请妥善保管管理员密码和数据库密码"
    echo "  5. 如需修改回调地址，请编辑 docker-compose.https.yml 中的环境变量："
    echo "     - ADMIN_REDIRECT_URIS"
    echo "     - ADMIN_LOGOUT_URIS"
    echo ""
    echo -e "${PURPLE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo ""
    echo -e "${CYAN}🛠️  常用命令:${NC}"
    echo ""
    echo "  # 查看日志"
    echo "  docker compose -f docker-compose.https.yml logs -f"
    echo ""
    echo "  # 重启服务"
    echo "  docker compose -f docker-compose.https.yml restart"
    echo ""
    echo "  # 停止服务"
    echo "  docker compose -f docker-compose.https.yml down"
    echo ""
    echo -e "${PURPLE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo ""
    echo -e "${GREEN}部署完成！祝使用愉快！${NC}"
    echo ""
}

# 主流程
main() {
    clear
    
    print_header "OneID HTTPS 快速部署工具"
    echo ""
    echo -e "${CYAN}欢迎使用 OneID 统一身份认证平台${NC}"
    echo -e "${CYAN}版本: 2.1 (优化版 - 添加前端检查和构建优化)${NC}"
    echo ""
    
    # 检查依赖
    check_dependencies
    
    # 配置向导
    configure
    
    # 检查前端编译
    check_frontend
    
    # 生成SSL证书
    generate_ssl
    
    # 停止旧服务
    stop_old_services
    
    # 构建镜像
    build_images
    
    # 启动服务
    start_services
    
    # 等待服务就绪
    wait_for_services
    
    # 显示部署信息
    show_deployment_info
}

# 运行主流程
main "$@"

