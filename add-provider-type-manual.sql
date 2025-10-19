-- 手动添加 ProviderType 字段到 ExternalAuthProvider 表
-- 执行此脚本以支持多个相同类型的提供商

-- 1. 添加 ProviderType 列
ALTER TABLE "ExternalAuthProvider" 
ADD COLUMN IF NOT EXISTS "ProviderType" text NOT NULL DEFAULT '';

-- 2. 为现有数据设置 ProviderType = Name（向后兼容）
UPDATE "ExternalAuthProvider" 
SET "ProviderType" = "Name"
WHERE "ProviderType" = '' OR "ProviderType" IS NULL;

-- 3. 验证数据
SELECT "Id", "ProviderType", "Name", "DisplayName", "Enabled" 
FROM "ExternalAuthProvider"
ORDER BY "DisplayOrder";

