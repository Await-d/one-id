# OneID 集成指南

> 本指南提供了 OneID 与第三方应用集成的完整说明，包括多种集成模式、最佳实践和代码示例。

## 📋 目录

- [快速开始](#快速开始)
- [集成模式对比](#集成模式对比)
- [详细集成指南](#详细集成指南)
- [常见问题](#常见问题)
- [技术支持](#技术支持)

---

## 🚀 快速开始

OneID 是一个企业级身份认证和授权系统，支持 OAuth 2.1 和 OpenID Connect (OIDC) 标准协议。根据你的应用场景，可以选择不同的集成模式：

### 选择集成模式

| 场景 | 推荐模式 | 文档链接 |
|------|---------|---------|
| 企业内部系统统一认证 | 标准 OIDC 集成 | [查看详情](docs/integration/01_标准OIDC集成.md) |
| SaaS 应用自定义登录体验 | 自定义登录页集成 | [查看详情](docs/integration/02_自定义登录页集成.md) |
| 现有系统账号迁移 | 账号统一管理 | [查看详情](docs/integration/03_账号统一管理.md) |
| 后端服务直接调用 | API 直接集成 | [查看详情](docs/integration/04_API直接集成.md) |
| 多租户/SaaS 平台 | 多租户集成 | [查看详情](docs/integration/05_多租户集成.md) |

---

## 📊 集成模式对比

### 1. 标准 OIDC 集成（推荐）

**适用场景**：企业内部系统、多个业务系统共享认证、快速集成

**优点**：
- ✅ 开箱即用，无需开发登录页
- ✅ 统一的登录体验和品牌
- ✅ 支持所有认证方式（本地登录、第三方登录）
- ✅ 自动处理 Token 刷新和会话管理
- ✅ 安全性最高（遵循 OIDC 最佳实践）

**缺点**：
- ⚠️ 登录页样式由 OneID 统一管理
- ⚠️ 跳转流程（应用 → OneID → 应用）

**集成难度**：⭐ 简单（1-2 小时）

---

### 2. 自定义登录页集成

**适用场景**：SaaS 应用、需要自定义品牌的登录体验、公网应用

**优点**：
- ✅ 完全自定义 UI/UX
- ✅ 保持品牌一致性
- ✅ 灵活的登录流程
- ✅ 支持直接显示第三方登录按钮

**缺点**：
- ⚠️ 需要开发和维护登录页
- ⚠️ 需要手动处理错误和边界情况

**集成难度**：⭐⭐ 中等（0.5-1 天）

---

### 3. 账号统一管理

**适用场景**：已有用户系统需要迁移、账号打通、混合认证模式

**优点**：
- ✅ 支持现有账号和 OneID 账号共存
- ✅ 平滑迁移路径
- ✅ 灵活的账号绑定策略

**缺点**：
- ⚠️ 需要处理账号映射逻辑
- ⚠️ 数据迁移需要规划

**集成难度**：⭐⭐⭐ 复杂（1-2 天）

---

### 4. API 直接集成

**适用场景**：后端服务、微服务架构、M2M 认证、脚本/CLI 工具

**优点**：
- ✅ 无需前端交互
- ✅ 适合服务间认证
- ✅ 支持 Client Credentials 流程

**缺点**：
- ⚠️ 不适合用户登录场景
- ⚠️ 需要妥善保管客户端凭证

**集成难度**：⭐⭐ 中等（2-4 小时）

---

### 5. 多租户集成

**适用场景**：SaaS 平台、企业版应用、需要租户隔离的系统

**优点**：
- ✅ 完整的租户隔离
- ✅ 灵活的租户配置
- ✅ 支持子域名路由

**缺点**：
- ⚠️ 需要处理租户上下文
- ⚠️ 配置相对复杂

**集成难度**：⭐⭐⭐ 复杂（1-2 天）

---

## 📚 详细集成指南

### 1. [标准 OIDC 集成](docs/integration/01_标准OIDC集成.md)

最简单、最安全的集成方式，适合大多数场景。使用 OneID 提供的登录页面，支持：
- 用户名密码登录
- 第三方社交账号登录（GitHub、Google、Gitee、微信等）
- 双因素认证（MFA）
- 记住我功能

**快速开始**：
```typescript
// 安装依赖
npm install oidc-client-ts

// 配置 OIDC 客户端
const userManager = new UserManager({
  authority: 'http://localhost:5001',
  client_id: 'your-client-id',
  redirect_uri: 'http://localhost:3000/callback',
  response_type: 'code',
  scope: 'openid profile email'
});

// 发起登录
await userManager.signinRedirect();
```

[查看完整文档 →](docs/integration/01_标准OIDC集成.md)

---

### 2. [自定义登录页集成](docs/integration/02_自定义登录页集成.md)

在你自己的应用中实现登录页，同时支持本地登录和第三方快捷登录。

**核心思路**：
- 在你的登录页直接显示第三方登录按钮（GitHub、Google 等）
- 调用 OneID API 获取可用的认证提供商
- 点击后跳转到 OneID 进行认证中转
- 认证完成后返回到你的应用

**效果示例**：
```
你的应用登录页
┌─────────────────────────────────────┐
│         欢迎登录 Your App           │
├─────────────────────────────────────┤
│  用户名: [_______________]          │
│  密码:   [_______________]          │
│         [      登录      ]          │
├─────────────────────────────────────┤
│       快速登录                      │
│  [🐙 GitHub] [🔵 Google] [🟠 Gitee]│
└─────────────────────────────────────┘
```

[查看完整文档 →](docs/integration/02_自定义登录页集成.md)

---

### 3. [账号统一管理](docs/integration/03_账号统一管理.md)

如果你的应用已有用户系统，学习如何：
- 将现有账号迁移到 OneID
- 实现账号绑定和关联
- 支持混合认证模式（本地账号 + OneID 账号）
- 平滑过渡策略

**三种账号管理模式**：
1. **完全托管模式**：所有账号由 OneID 管理
2. **关联模式**：OneID 账号与本地账号关联
3. **SSO 模式**：OneID 仅用于认证，本地系统管理账号

[查看完整文档 →](docs/integration/03_账号统一管理.md)

---

### 4. [API 直接集成](docs/integration/04_API直接集成.md)

后端服务、定时任务、CLI 工具等无需用户交互的场景。

**支持的认证方式**：
- Client Credentials（服务间认证）
- Resource Owner Password Credentials（用户名密码）
- API Key 认证

**示例场景**：
- 微服务之间的相互调用
- 后台定时任务
- 管理脚本和工具
- 第三方系统集成

[查看完整文档 →](docs/integration/04_API直接集成.md)

---

### 5. [多租户集成](docs/integration/05_多租户集成.md)

SaaS 平台的多租户认证方案，实现：
- 租户隔离（数据、用户、配置）
- 子域名路由（tenant1.yourdomain.com）
- 租户级配置管理
- 跨租户用户管理

**架构示例**：
```
tenant1.app.com ──→ OneID (tenant1) ──→ tenant1 数据
tenant2.app.com ──→ OneID (tenant2) ──→ tenant2 数据
tenant3.app.com ──→ OneID (tenant3) ──→ tenant3 数据
```

[查看完整文档 →](docs/integration/05_多租户集成.md)

---

## ❓ 常见问题

### Q1: 我应该选择哪种集成模式？

**A**: 根据你的场景选择：
- **企业内部系统** → 标准 OIDC 集成
- **SaaS 应用** → 自定义登录页集成
- **已有用户系统** → 账号统一管理
- **后端服务** → API 直接集成
- **多租户平台** → 多租户集成

### Q2: 标准 OIDC 集成能否自定义登录页样式？

**A**: 可以！OneID Login Portal 支持：
- 自定义 Logo
- 自定义主题色
- 自定义背景
- 自定义文案

但如果需要完全自定义的 UI，建议使用"自定义登录页集成"模式。

### Q3: 第三方登录支持哪些平台？

**A**: OneID 当前支持：
- GitHub
- Google
- Gitee
- 微信

更多平台可通过配置 ASP.NET Core Identity 的 External Login 添加。

### Q4: 如何实现"记住我"功能？

**A**: 在标准 OIDC 集成中：
```typescript
const userManager = new UserManager({
  // ... 其他配置
  automaticSilentRenew: true, // 自动刷新 Token
  accessTokenExpiringNotificationTimeInSeconds: 60
});
```

### Q5: 如何处理 Token 过期？

**A**: OneID 支持自动 Token 刷新：
```typescript
// 监听 Token 即将过期
userManager.events.addAccessTokenExpiring(() => {
  console.log('Token expiring, renewing...');
});

// 监听 Token 更新
userManager.events.addAccessTokenExpired(() => {
  console.log('Token expired, redirecting to login...');
  userManager.signinRedirect();
});
```

### Q6: 支持单点登录（SSO）吗？

**A**: 完全支持！使用标准 OIDC 集成即可实现：
- 用户在 App A 登录后
- 访问 App B 时自动登录（无需再次输入密码）
- 在 App A 注销后，App B 也会同步注销

### Q7: 如何获取用户信息？

**A**: 解析 ID Token 或调用 UserInfo 端点：
```typescript
// 方式1：解析 ID Token
const user = await userManager.getUser();
console.log(user.profile.sub);    // 用户唯一标识
console.log(user.profile.email);  // 邮箱
console.log(user.profile.name);   // 姓名

// 方式2：调用 UserInfo API
const response = await fetch('http://localhost:5001/connect/userinfo', {
  headers: {
    'Authorization': `Bearer ${user.access_token}`
  }
});
const userInfo = await response.json();
```

### Q8: 如何处理账号迁移？

**A**: 参考 [账号统一管理文档](docs/integration/03_账号统一管理.md)，提供了三种迁移策略：
1. 一次性批量迁移
2. 渐进式迁移（用户首次登录时迁移）
3. 双系统并行（保留两套账号体系）

### Q9: 支持移动端集成吗？

**A**: 支持！使用以下库：
- **iOS**: AppAuth-iOS
- **Android**: AppAuth-Android
- **React Native**: react-native-app-auth
- **Flutter**: flutter_appauth

集成方式与 Web 端类似，遵循 OIDC 标准。

### Q10: 如何测试集成？

**A**: OneID 提供：
1. **开发环境**：本地启动 OneID 进行测试
2. **测试客户端**：预配置的测试应用
3. **API 测试工具**：Postman Collection
4. **日志查看**：Admin Portal 提供完整的审计日志

---

## 🔒 安全最佳实践

### 1. Token 安全

- ✅ 使用 HTTPS（生产环境）
- ✅ 不要在 URL 中暴露 Token
- ✅ 使用 HttpOnly Cookie 存储 Refresh Token
- ✅ 设置合理的 Token 过期时间
- ❌ 不要在前端存储 Client Secret

### 2. PKCE（Proof Key for Code Exchange）

对于公共客户端（SPA、移动应用），启用 PKCE：
```typescript
const userManager = new UserManager({
  // ... 其他配置
  code_verifier: true // 启用 PKCE
});
```

### 3. State 参数

防止 CSRF 攻击，OIDC 库会自动处理 State 参数。

### 4. 验证 ID Token

始终验证 ID Token 的签名和声明：
```typescript
// oidc-client-ts 会自动验证
const user = await userManager.signinRedirectCallback();
// user.id_token 已经过验证
```

---

## 🛠️ 开发工具

### 1. OIDC 调试工具

- [oidcdebugger.com](https://oidcdebugger.com/) - 在线 OIDC 流程测试
- [jwt.io](https://jwt.io/) - JWT Token 解析

### 2. 推荐客户端库

#### JavaScript/TypeScript
- `oidc-client-ts` - 功能最全面
- `@auth0/auth0-spa-js` - 简单易用

#### .NET
- `Microsoft.AspNetCore.Authentication.OpenIdConnect`
- `IdentityModel.OidcClient`

#### Python
- `authlib`
- `python-jose`

#### Java
- `spring-security-oauth2-client`
- `nimbus-oauth2-oidc-sdk`

### 3. OneID 管理工具

- **Admin Portal**: http://localhost:5174
  - 客户端管理
  - 用户管理
  - 审计日志
  - 系统配置

- **API Endpoints**:
  - Discovery: `/.well-known/openid-configuration`
  - Authorization: `/connect/authorize`
  - Token: `/connect/token`
  - UserInfo: `/connect/userinfo`
  - End Session: `/connect/endsession`

---

## 📞 技术支持

### 文档资源

- [OneID 主文档](README.md)
- [快速开始指南](QUICKSTART.md)
- [API 参考文档](docs/API_REFERENCE.md)
- [配置手册](docs/)

### 示例代码

查看 `examples/` 目录获取完整的集成示例：
- React SPA 示例
- Vue 3 示例
- Next.js 示例
- .NET API 示例
- Python FastAPI 示例

### 社区支持

- **GitHub Issues**: 报告 Bug 和功能请求
- **Discussions**: 技术讨论和问题解答

---

## 📝 许可证

OneID 使用 MIT 许可证。详见 [LICENSE](LICENSE) 文件。

---

## 🎯 下一步

1. 选择适合你的集成模式
2. 阅读对应的详细文档
3. 查看示例代码
4. 开始集成！

祝集成顺利！🚀

