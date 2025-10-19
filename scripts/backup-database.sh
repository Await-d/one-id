#!/bin/bash

#######################################################
# OneID 数据库备份脚本
#######################################################
# 功能：
# - 自动备份 PostgreSQL 数据库
# - 支持压缩和时间戳
# - 自动清理旧备份
# - 支持本地和 Docker 环境
#######################################################

set -e

# 配置
BACKUP_DIR="${BACKUP_DIR:-./backups}"
RETENTION_DAYS="${RETENTION_DAYS:-30}"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
DB_NAME="${DB_NAME:-oneid}"
DB_USER="${DB_USER:-oneid}"
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
USE_DOCKER="${USE_DOCKER:-false}"
DOCKER_CONTAINER="${DOCKER_CONTAINER:-oneid-postgres-prod}"

# 颜色输出
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}OneID 数据库备份工具${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# 创建备份目录
mkdir -p "$BACKUP_DIR"

# 备份文件名
BACKUP_FILE="$BACKUP_DIR/oneid_backup_$TIMESTAMP.sql"
BACKUP_FILE_GZ="$BACKUP_FILE.gz"

echo -e "${YELLOW}📦 开始备份数据库...${NC}"
echo "数据库: $DB_NAME"
echo "时间戳: $TIMESTAMP"
echo "备份文件: $BACKUP_FILE_GZ"
echo ""

# 执行备份
if [ "$USE_DOCKER" = "true" ]; then
    echo -e "${YELLOW}🐳 使用 Docker 容器备份...${NC}"
    docker exec "$DOCKER_CONTAINER" pg_dump -U "$DB_USER" "$DB_NAME" > "$BACKUP_FILE"
else
    echo -e "${YELLOW}💻 使用本地 PostgreSQL 备份...${NC}"
    PGPASSWORD="$DB_PASSWORD" pg_dump -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" "$DB_NAME" > "$BACKUP_FILE"
fi

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ 数据库导出成功${NC}"
    
    # 压缩备份文件
    echo -e "${YELLOW}📦 压缩备份文件...${NC}"
    gzip "$BACKUP_FILE"
    
    if [ $? -eq 0 ]; then
        BACKUP_SIZE=$(du -h "$BACKUP_FILE_GZ" | cut -f1)
        echo -e "${GREEN}✓ 压缩完成，文件大小: $BACKUP_SIZE${NC}"
    else
        echo -e "${RED}✗ 压缩失败${NC}"
        exit 1
    fi
else
    echo -e "${RED}✗ 数据库导出失败${NC}"
    exit 1
fi

# 清理旧备份
echo ""
echo -e "${YELLOW}🧹 清理 $RETENTION_DAYS 天前的备份...${NC}"
OLD_BACKUPS=$(find "$BACKUP_DIR" -name "oneid_backup_*.sql.gz" -type f -mtime +"$RETENTION_DAYS" 2>/dev/null || true)

if [ -n "$OLD_BACKUPS" ]; then
    echo "$OLD_BACKUPS" | while read -r file; do
        echo "删除: $file"
        rm -f "$file"
    done
    echo -e "${GREEN}✓ 清理完成${NC}"
else
    echo "没有需要清理的旧备份"
fi

# 显示当前备份列表
echo ""
echo -e "${YELLOW}📋 当前备份列表：${NC}"
ls -lh "$BACKUP_DIR"/oneid_backup_*.sql.gz 2>/dev/null || echo "无备份文件"

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}✓ 备份完成！${NC}"
echo -e "${GREEN}========================================${NC}"
echo "备份文件: $BACKUP_FILE_GZ"
echo ""

# 显示恢复命令
echo -e "${YELLOW}📖 恢复命令：${NC}"
if [ "$USE_DOCKER" = "true" ]; then
    echo "  gunzip -c $BACKUP_FILE_GZ | docker exec -i $DOCKER_CONTAINER psql -U $DB_USER $DB_NAME"
else
    echo "  gunzip -c $BACKUP_FILE_GZ | PGPASSWORD=\$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER $DB_NAME"
fi
echo ""

exit 0

