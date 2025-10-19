#!/bin/bash
set -e

echo "============================================"
echo "OneID 统一认证平台启动中..."
echo "============================================"

# 等待数据库就绪
if [ -n "$ConnectionStrings__Default" ]; then
    echo "等待数据库连接..."
    sleep 5
fi

# 创建必要的目录
mkdir -p /app/data /app/logs

echo "启动 OneID.Identity (端口 5101)..."
echo "启动 OneID.AdminApi (端口 5102)..."

# 启动 Supervisor
exec /usr/bin/supervisord -c /etc/supervisor/conf.d/supervisord.conf
