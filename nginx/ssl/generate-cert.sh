#!/bin/bash

# ============================================================
# OneID SSL 自签名证书生成脚本
# ============================================================
# 
# 用途：生成自签名SSL证书用于测试和开发环境
# 生产环境建议使用 Let's Encrypt 或购买正式证书
#
# 使用方法：
#   chmod +x generate-cert.sh
#   ./generate-cert.sh
#
# ============================================================

set -e

echo "============================================================"
echo "  OneID SSL 证书生成工具"
echo "============================================================"
echo ""

# 默认配置
DEFAULT_DOMAIN="192.168.123.9"
DEFAULT_DAYS=3650
CERT_FILE="cert.pem"
KEY_FILE="key.pem"

# 获取域名/IP
read -p "请输入域名或IP地址 [默认: $DEFAULT_DOMAIN]: " DOMAIN
DOMAIN=${DOMAIN:-$DEFAULT_DOMAIN}

# 获取有效期
read -p "证书有效期（天）[默认: $DEFAULT_DAYS]: " DAYS
DAYS=${DAYS:-$DEFAULT_DAYS}

echo ""
echo "生成配置："
echo "  域名/IP: $DOMAIN"
echo "  有效期: $DAYS 天"
echo "  证书文件: $CERT_FILE"
echo "  密钥文件: $KEY_FILE"
echo ""

# 确认
read -p "确认生成证书？(y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "已取消"
    exit 1
fi

echo ""
echo "正在生成SSL证书..."

# 生成私钥和自签名证书
openssl req -x509 -nodes -days "$DAYS" \
    -newkey rsa:2048 \
    -keyout "$KEY_FILE" \
    -out "$CERT_FILE" \
    -subj "/C=CN/ST=Beijing/L=Beijing/O=OneID/OU=IT/CN=$DOMAIN" \
    -addext "subjectAltName=DNS:$DOMAIN,DNS:localhost,IP:127.0.0.1,IP:$DOMAIN"

# 设置权限
chmod 600 "$KEY_FILE"
chmod 644 "$CERT_FILE"

echo ""
echo "✅ SSL 证书生成成功！"
echo ""
echo "文件位置："
echo "  证书: $(pwd)/$CERT_FILE"
echo "  私钥: $(pwd)/$KEY_FILE"
echo ""
echo "证书信息："
openssl x509 -in "$CERT_FILE" -text -noout | grep -A 2 "Subject:"
echo ""
echo "有效期："
openssl x509 -in "$CERT_FILE" -text -noout | grep -A 2 "Validity"
echo ""
echo "============================================================"
echo "  ⚠️  注意事项"
echo "============================================================"
echo ""
echo "1. 这是自签名证书，浏览器会显示安全警告，这是正常的"
echo "2. 生产环境建议使用 Let's Encrypt 或购买正式证书"
echo "3. 如需信任此证书，请将 $CERT_FILE 导入系统信任列表"
echo ""
echo "下一步："
echo "  docker compose -f docker-compose.https.yml up -d"
echo ""
echo "访问地址："
echo "  登录界面: https://$DOMAIN:9443"
echo "  管理后台: https://$DOMAIN:9444"
echo ""
echo "============================================================"

