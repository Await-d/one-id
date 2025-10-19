# OneID Backend 指南

## 解决方案结构
- `OneID.Identity`：OIDC Provider，含播种与数据库配置
- `OneID.AdminApi`：管理 API，提供客户端查询等能力
- `OneID.Shared`：共享领域模型、DbContext 与应用契约
- `tests/`：单元与集成测试
  - `OneID.Identity.Tests`：播种逻辑测试（SQLite 内存）
  - `OneID.AdminApi.Tests`：Clients 端点集成测试（WebApplicationFactory）

## 常用命令
> 当前沙箱缺少 `dotnet` CLI，需在具备 SDK 的环境或 Compose 容器内执行。

```bash
# 进入后端目录
cd backend

# 还原工具与依赖
dotnet tool restore
dotnet restore

# 运行所有测试
dotnet test

# 仅运行 AdminApi 测试
dotnet test tests/OneID.AdminApi.Tests/OneID.AdminApi.Tests.csproj
```

### 使用 Docker SDK 镜像运行测试
若本地未安装 .NET，可运行仓库提供的脚本：

```bash
./scripts/run-backend-tests.sh
```

脚本会拉取 `mcr.microsoft.com/dotnet/sdk:9.0-preview` 镜像，在容器内执行 `dotnet tool restore && dotnet test`。

## 测试说明
- AdminApi 测试使用内存 SQLite 替换默认数据库，并注入测试认证方案；
- 若需调试，可在 `AdminApiFactory` 中调整种子数据或扩展更多端点校验；
- 所有版本统一通过 `Directory.Packages.props` 管理，新增包时请先更新该文件以符合 DRY。

## API 速览
- `GET /api/clients`：返回现有 OIDC 客户端列表。
- `POST /api/clients`：创建新客户端，Body 参考 `CreateClientRequest`（需提供 `clientId`、`displayName`、`redirectUri` 等字段）。
- `DELETE /api/clients/{clientId}`：删除指定客户端，若不存在返回 404。
- `PUT /api/clients/{clientId}`：更新客户端展示信息、回调地址及作用域（请求体参考 `UpdateClientRequest`）。
- `PUT /api/clients/{clientId}/scopes`：覆盖客户端作用域列表（请求体 `UpdateClientScopesRequest`）。
- `GET /api/auditlogs`：分页查询审计日志，支持 `page` / `pageSize`、时间、类别、结果筛选。
- `GET /api/auditlogs/categories`：按字母升序返回数据库中存在的日志类别，供前端筛选器使用。
- `GET /api/auditlogs/export`：导出审计日志 CSV，遵循与列表相同的过滤参数。

## 外部认证配置
- `Seed:ExternalAuth:Providers` 支持在初始化阶段预置 GitHub / Google / Gitee / WeChat 等外部登录；若未填写 `ClientId` 或 `ClientSecret` 将自动跳过，避免脏数据。
- 运行时会按数据库中的 `ExternalAuthProvider` 动态注册认证方案，可通过 `AdditionalConfig` 指定 WeChat `region`、`agentId` 等附加参数。
- 生产环境需同步更新 `appsettings.Production.example.json` 中的 `ExternalAuth` 段落，并确保密钥只保存在密钥管理服务（Vault、KeyVault 等）。

## 客户端 Redirect 校验
- 配置保存在数据库表 `ClientValidationSettings` 中，应用启动时若表为空会按环境变量初始化：
  - `CLIENT_VALIDATION_ALLOWED_SCHEMES`（默认 `https,http`）
  - `CLIENT_VALIDATION_ALLOW_HTTP_LOOPBACK`（默认 `true`）
  - `CLIENT_VALIDATION_ALLOWED_HOSTS`（逗号分隔，默认空表示不限）
- 校验逻辑仅允许在配置范围内的 Scheme/Host，且禁止包含 `#fragment`；失败时返回 `400` + `ValidationProblemDetails`，字段为 `RedirectUri` 或 `PostLogoutRedirectUri`。

## CORS 设置
- 配置存储在 `CorsSettings` 表中，可通过 Admin API (`/api/cors-settings`) 或管理台“CORS 配置”页面调整。
- Docker 环境可通过以下变量初始化：
  - `IDENTITY_CORS_ALLOWED_ORIGINS`（逗号分隔，为空时默认允许本机调试）
  - `IDENTITY_CORS_ALLOW_ANY_ORIGIN`（布尔值，默认 `false`，当为 `true` 时忽略列表并允许任意来源）
