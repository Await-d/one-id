# ============================================
# OneID 统一构建 Dockerfile
# 单容器运行 Identity + AdminApi 两个服务
# ============================================

# ==================== 后端构建阶段 ====================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS backend-build
WORKDIR /src

# 复制后端项目文件和中央包管理文件
COPY backend/Directory.Packages.props ./
COPY backend/OneID.Shared/OneID.Shared.csproj OneID.Shared/
COPY backend/OneID.Identity/OneID.Identity.csproj OneID.Identity/
COPY backend/OneID.AdminApi/OneID.AdminApi.csproj OneID.AdminApi/

# 恢复后端依赖
RUN dotnet restore OneID.Identity/OneID.Identity.csproj && \
    dotnet restore OneID.AdminApi/OneID.AdminApi.csproj

# 复制所有后端代码
COPY backend/ ./

# 编译 Identity 和 AdminApi
RUN dotnet publish OneID.Identity/OneID.Identity.csproj -c Release -o /app/identity --no-restore && \
    dotnet publish OneID.AdminApi/OneID.AdminApi.csproj -c Release -o /app/admin --no-restore

# ==================== 前端 Login 构建阶段 ====================
FROM node:20-alpine AS login-build
WORKDIR /src

# 安装 pnpm
RUN npm install -g pnpm@10.17.1

# 复制前端 package.json
COPY frontend/login/package.json frontend/login/pnpm-lock.yaml ./

# 安装前端依赖
RUN pnpm install --frozen-lockfile

# 复制前端代码
COPY frontend/login/ ./

# 创建生产环境配置
RUN echo "VITE_OIDC_CLIENT_ID=spa.portal" > .env.production && \
    echo "VITE_OIDC_SCOPE=openid profile email offline_access" >> .env.production

# 构建前端
ENV NODE_OPTIONS="--max-old-space-size=1096"
RUN pnpm build

# ==================== 前端 Admin 构建阶段 ====================
FROM node:20-alpine AS admin-build
WORKDIR /src

# 安装 pnpm
RUN npm install -g pnpm@10.17.1

# 复制 Admin Portal package.json
COPY frontend/admin/package.json frontend/admin/pnpm-lock.yaml ./

# 安装依赖
RUN pnpm install --frozen-lockfile

# 复制代码
COPY frontend/admin/ ./

# 创建生产环境配置
RUN echo "VITE_OIDC_AUTHORITY=" > .env.production && \
    echo "VITE_OIDC_CLIENT_ID=spa.admin" >> .env.production && \
    echo "VITE_OIDC_REDIRECT_URI=" >> .env.production && \
    echo "VITE_OIDC_POST_LOGOUT_REDIRECT_URI=" >> .env.production && \
    echo "VITE_ADMIN_API_URL=" >> .env.production

# 构建 Admin Portal
ENV NODE_OPTIONS="--max-old-space-size=1096"
RUN pnpm build

# ==================== 运行时镜像 ====================
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# 安装必要工具
RUN apt-get update && \
    apt-get install -y curl supervisor && \
    rm -rf /var/lib/apt/lists/*

# 复制后端应用
COPY --from=backend-build /app/identity ./identity
COPY --from=backend-build /app/admin ./admin

# 复制前端构建产物到 Identity 的 wwwroot
COPY --from=login-build /src/dist ./identity/wwwroot
COPY --from=admin-build /src/dist ./identity/wwwroot/admin

# 创建必要的目录
RUN mkdir -p /app/data /app/logs

# 复制 Supervisor 配置
COPY docker/supervisord.conf /etc/supervisor/conf.d/supervisord.conf

# 复制启动脚本
COPY docker/start.sh /app/start.sh
RUN chmod +x /app/start.sh

# 暴露端口
EXPOSE 80

# 设置默认环境变量（可通过 docker run -e 覆盖）
ENV ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__Default="Host=postgres;Port=5432;Database=oneid;Username=oneid;Password=oneid_password" \
    Persistence__Provider=Postgres

# 健康检查
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:5101/health || exit 1

# 使用 Supervisor 启动所有服务
CMD ["/usr/bin/supervisord", "-c", "/etc/supervisor/conf.d/supervisord.conf"]
