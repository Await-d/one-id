FROM mcr.microsoft.com/dotnet/sdk:9.0 AS backend-build
WORKDIR /src

# 复制后端项目文件和中央包管理文件
COPY backend/Directory.Packages.props ./
COPY backend/OneID.Shared/OneID.Shared.csproj OneID.Shared/
COPY backend/OneID.Identity/OneID.Identity.csproj OneID.Identity/
COPY backend/OneID.AdminApi/OneID.AdminApi.csproj OneID.AdminApi/

# 恢复后端依赖（只恢复生产项目，不包含测试项目）
RUN dotnet restore OneID.Identity/OneID.Identity.csproj && \
    dotnet restore OneID.AdminApi/OneID.AdminApi.csproj

# 复制所有后端代码
COPY backend/ ./

# 编译后端
RUN dotnet publish OneID.Identity/OneID.Identity.csproj -c Release -o /app/publish --no-restore

# 前端构建阶段
FROM node:20-alpine AS frontend-build
WORKDIR /src

# 安装pnpm
RUN npm install -g pnpm@10.17.1

# 复制前端package.json
COPY frontend/login/package.json frontend/login/pnpm-lock.yaml ./

# 安装前端依赖
RUN pnpm install --frozen-lockfile

# 复制前端代码
COPY frontend/login/ ./

# 创建生产环境配置（不设置authority/redirect_uri，让代码使用window.location.origin的fallback）
RUN echo "VITE_OIDC_CLIENT_ID=spa.portal" > .env.production && \
    echo "VITE_OIDC_SCOPE=openid profile email offline_access" >> .env.production

# 构建前端
ENV NODE_OPTIONS="--max-old-space-size=1096"
RUN pnpm build

# 运行时镜像
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# 安装 curl 用于健康检查
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# 复制后端应用
COPY --from=backend-build /app/publish ./

# 复制数据库初始化脚本
COPY backend/OneID.Identity/init-db.sql ./

# 复制前端构建产物到wwwroot
COPY --from=frontend-build /src/dist ./wwwroot

# 暴露端口
EXPOSE 80

# 设置环境变量
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://0.0.0.0:80 \
    Logging__Console__DisableColors=true

# 启动应用
ENTRYPOINT ["dotnet", "OneID.Identity.dll"]
