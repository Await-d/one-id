# 标准 OIDC 集成指南

> 本文档详细介绍如何使用标准 OpenID Connect (OIDC) 协议将你的应用与 OneID 集成。

## 📋 目录

- [概述](#概述)
- [前置条件](#前置条件)
- [集成步骤](#集成步骤)
- [平台特定示例](#平台特定示例)
- [高级配置](#高级配置)
- [故障排除](#故障排除)

---

## 🎯 概述

### 什么是标准 OIDC 集成？

标准 OIDC 集成是最简单、最安全的集成方式。你的应用将用户重定向到 OneID 的登录页面，用户完成认证后，OneID 将用户重定向回你的应用，并携带身份信息。

### 用户流程

```
1. 用户访问你的应用
   ↓
2. 点击"登录"按钮
   ↓
3. 跳转到 OneID 登录页
   ↓
4. 用户选择登录方式：
   - 用户名密码登录
   - GitHub 登录
   - Google 登录
   - Gitee 登录
   - 微信登录
   ↓
5. 完成认证（可能需要双因素认证）
   ↓
6. 跳转回你的应用（携带授权码）
   ↓
7. 你的应用用授权码换取 Token
   ↓
8. 用户登录成功！
```

### OneID 登录页效果

```
┌─────────────────────────────────────┐
│         OneID 登录                  │
├─────────────────────────────────────┤
│  用户名: [_______________]          │
│  密码:   [_______________]          │
│         [      登录      ]          │
│                                     │
│  [    忘记密码？    ]               │
├─────────────────────────────────────┤
│       或使用以下方式继续            │
│                                     │
│  ┌───────────────────────────────┐ │
│  │ 🐙  GitHub                    │ │
│  └───────────────────────────────┘ │
│  ┌───────────────────────────────┐ │
│  │ 🔵  Google                    │ │
│  └───────────────────────────────┘ │
│  ┌───────────────────────────────┐ │
│  │ 🟠  Gitee                     │ │
│  └───────────────────────────────┘ │
│  ┌───────────────────────────────┐ │
│  │ 💬  微信                      │ │
│  └───────────────────────────────┘ │
└─────────────────────────────────────┘
```

### 优势

- ✅ **开箱即用**：无需开发登录页
- ✅ **安全性高**：遵循 OIDC 最佳实践
- ✅ **统一体验**：所有应用共享同一登录页
- ✅ **自动更新**：支持的认证方式由 OneID 统一管理
- ✅ **双因素认证**：自动支持 MFA
- ✅ **单点登录**：一次登录，多处使用

---

## 📋 前置条件

### 1. OneID 服务已启动

```bash
# 启动 OneID
cd backend
docker-compose up -d

# 验证服务
curl http://localhost:5001/.well-known/openid-configuration
```

### 2. 注册客户端应用

在 OneID Admin Portal 注册你的应用：

1. 访问 http://localhost:5174
2. 登录管理后台（默认账号：admin / Admin@123）
3. 进入"客户端管理"
4. 点击"创建客户端"

**配置示例**：

| 字段 | 值 | 说明 |
|------|-----|------|
| Client ID | `my-app` | 客户端唯一标识 |
| Client Name | `My Application` | 显示名称 |
| Client Type | `Web Application` | 应用类型 |
| Grant Types | `Authorization Code` | 授权类型 |
| Redirect URIs | `http://localhost:3000/callback` | 回调地址 |
| Post Logout Redirect URIs | `http://localhost:3000` | 登出后跳转地址 |
| Scopes | `openid profile email` | 请求的权限范围 |
| Require PKCE | ✅ 是 | 公共客户端必须启用 |
| Require Client Secret | ✅ 是（服务端应用）<br>❌ 否（SPA/移动应用） | 是否需要密钥 |

保存后，系统会生成 `Client Secret`（如果需要），请妥善保管。

---

## 🚀 集成步骤

### 步骤 1: 安装 OIDC 客户端库

根据你的技术栈选择：

#### JavaScript/TypeScript (React/Vue/Angular)

```bash
npm install oidc-client-ts
# 或
yarn add oidc-client-ts
```

#### .NET

```bash
dotnet add package Microsoft.AspNetCore.Authentication.OpenIdConnect
```

#### Python

```bash
pip install authlib
```

#### Java

```xml
<!-- pom.xml -->
<dependency>
    <groupId>org.springframework.boot</groupId>
    <artifactId>spring-boot-starter-oauth2-client</artifactId>
</dependency>
```

---

### 步骤 2: 配置 OIDC 客户端

#### JavaScript/TypeScript 示例

```typescript
// src/auth/oidcConfig.ts
import { UserManager, WebStorageStateStore } from 'oidc-client-ts';

const oidcConfig = {
  // OneID 服务地址
  authority: 'http://localhost:5001',
  
  // 客户端配置
  client_id: 'my-app',
  client_secret: 'your-client-secret', // 仅服务端应用需要
  
  // 回调地址（必须在 OneID 中注册）
  redirect_uri: 'http://localhost:3000/callback',
  post_logout_redirect_uri: 'http://localhost:3000',
  
  // 授权流程
  response_type: 'code', // 授权码模式
  scope: 'openid profile email',
  
  // 自动刷新 Token
  automaticSilentRenew: true,
  silent_redirect_uri: 'http://localhost:3000/silent-renew',
  
  // Token 存储
  userStore: new WebStorageStateStore({ store: window.localStorage }),
  
  // 高级配置
  loadUserInfo: true, // 自动加载用户信息
  monitorSession: true, // 监控会话状态
  checkSessionIntervalInSeconds: 10,
  
  // PKCE（公共客户端必须启用）
  code_challenge_method: 'S256',
};

export const userManager = new UserManager(oidcConfig);

// 事件监听
userManager.events.addAccessTokenExpiring(() => {
  console.log('Token 即将过期，自动续期...');
});

userManager.events.addAccessTokenExpired(() => {
  console.log('Token 已过期，重定向到登录页...');
  userManager.signinRedirect();
});

userManager.events.addUserLoaded((user) => {
  console.log('用户信息已加载:', user.profile);
});

userManager.events.addUserUnloaded(() => {
  console.log('用户已注销');
});
```

---

### 步骤 3: 实现登录功能

#### React 示例

```typescript
// src/components/LoginButton.tsx
import React from 'react';
import { userManager } from '../auth/oidcConfig';

export function LoginButton() {
  const handleLogin = async () => {
    try {
      // 保存当前页面路径，登录后返回
      sessionStorage.setItem('preLoginPath', window.location.pathname);
      
      // 发起登录（跳转到 OneID）
      await userManager.signinRedirect({
        state: { returnUrl: window.location.pathname }
      });
    } catch (error) {
      console.error('登录失败:', error);
    }
  };

  return (
    <button
      onClick={handleLogin}
      className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700"
    >
      登录
    </button>
  );
}
```

```typescript
// src/pages/CallbackPage.tsx
import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { userManager } from '../auth/oidcConfig';

export function CallbackPage() {
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const handleCallback = async () => {
      try {
        // 处理回调，获取用户信息
        const user = await userManager.signinRedirectCallback();
        
        console.log('登录成功！用户信息:', user.profile);
        console.log('Access Token:', user.access_token);
        console.log('ID Token:', user.id_token);
        
        // 跳转到登录前的页面
        const returnUrl = user.state?.returnUrl || '/';
        navigate(returnUrl);
      } catch (err) {
        console.error('登录回调处理失败:', err);
        setError(err.message);
      }
    };

    handleCallback();
  }, [navigate]);

  if (error) {
    return (
      <div className="error">
        <h2>登录失败</h2>
        <p>{error}</p>
        <button onClick={() => navigate('/')}>返回首页</button>
      </div>
    );
  }

  return (
    <div className="loading">
      <p>正在登录，请稍候...</p>
    </div>
  );
}
```

---

### 步骤 4: 实现登出功能

```typescript
// src/components/LogoutButton.tsx
import React from 'react';
import { userManager } from '../auth/oidcConfig';

export function LogoutButton() {
  const handleLogout = async () => {
    try {
      // 从 OneID 注销（会清除 OneID 的会话）
      await userManager.signoutRedirect();
      
      // 或者仅从本地注销（不影响其他应用）
      // await userManager.removeUser();
      // window.location.href = '/';
    } catch (error) {
      console.error('登出失败:', error);
    }
  };

  return (
    <button
      onClick={handleLogout}
      className="text-gray-600 hover:text-gray-900"
    >
      登出
    </button>
  );
}
```

---

### 步骤 5: 保护受限路由

```typescript
// src/components/ProtectedRoute.tsx
import React, { useEffect, useState } from 'react';
import { Navigate } from 'react-router-dom';
import { userManager } from '../auth/oidcConfig';

interface ProtectedRouteProps {
  children: React.ReactNode;
}

export function ProtectedRoute({ children }: ProtectedRouteProps) {
  const [isAuthenticated, setIsAuthenticated] = useState<boolean | null>(null);

  useEffect(() => {
    const checkAuth = async () => {
      try {
        const user = await userManager.getUser();
        setIsAuthenticated(user !== null && !user.expired);
      } catch (error) {
        console.error('检查认证状态失败:', error);
        setIsAuthenticated(false);
      }
    };

    checkAuth();
  }, []);

  // 加载中
  if (isAuthenticated === null) {
    return <div>Loading...</div>;
  }

  // 未登录，跳转到登录页
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  // 已登录，显示内容
  return <>{children}</>;
}
```

**使用示例**：

```typescript
// src/App.tsx
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { HomePage } from './pages/HomePage';
import { DashboardPage } from './pages/DashboardPage';
import { CallbackPage } from './pages/CallbackPage';
import { ProtectedRoute } from './components/ProtectedRoute';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/callback" element={<CallbackPage />} />
        
        {/* 受保护的路由 */}
        <Route
          path="/dashboard"
          element={
            <ProtectedRoute>
              <DashboardPage />
            </ProtectedRoute>
          }
        />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
```

---

### 步骤 6: 获取和使用用户信息

```typescript
// src/hooks/useAuth.ts
import { useState, useEffect } from 'react';
import { User } from 'oidc-client-ts';
import { userManager } from '../auth/oidcConfig';

export function useAuth() {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // 获取当前用户
    const loadUser = async () => {
      try {
        const currentUser = await userManager.getUser();
        setUser(currentUser);
      } catch (error) {
        console.error('加载用户失败:', error);
      } finally {
        setLoading(false);
      }
    };

    loadUser();

    // 监听用户变化
    const handleUserLoaded = (user: User) => setUser(user);
    const handleUserUnloaded = () => setUser(null);

    userManager.events.addUserLoaded(handleUserLoaded);
    userManager.events.addUserUnloaded(handleUserUnloaded);

    return () => {
      userManager.events.removeUserLoaded(handleUserLoaded);
      userManager.events.removeUserUnloaded(handleUserUnloaded);
    };
  }, []);

  return {
    user,
    loading,
    isAuthenticated: user !== null && !user.expired,
    profile: user?.profile,
    accessToken: user?.access_token,
    login: () => userManager.signinRedirect(),
    logout: () => userManager.signoutRedirect(),
  };
}
```

**使用示例**：

```typescript
// src/pages/DashboardPage.tsx
import React from 'react';
import { useAuth } from '../hooks/useAuth';

export function DashboardPage() {
  const { user, profile, loading, logout } = useAuth();

  if (loading) {
    return <div>Loading...</div>;
  }

  return (
    <div className="dashboard">
      <header>
        <h1>欢迎，{profile.name || profile.email}！</h1>
        <button onClick={logout}>登出</button>
      </header>

      <div className="user-info">
        <h2>用户信息</h2>
        <dl>
          <dt>用户 ID (sub):</dt>
          <dd>{profile.sub}</dd>
          
          <dt>邮箱:</dt>
          <dd>{profile.email}</dd>
          
          <dt>邮箱已验证:</dt>
          <dd>{profile.email_verified ? '是' : '否'}</dd>
          
          <dt>姓名:</dt>
          <dd>{profile.name}</dd>
          
          <dt>用户名:</dt>
          <dd>{profile.preferred_username}</dd>
        </dl>
      </div>

      <div className="token-info">
        <h2>Token 信息</h2>
        <dl>
          <dt>Access Token 过期时间:</dt>
          <dd>{new Date(user.expires_at * 1000).toLocaleString()}</dd>
          
          <dt>Token 类型:</dt>
          <dd>{user.token_type}</dd>
          
          <dt>Scopes:</dt>
          <dd>{user.scope}</dd>
        </dl>
      </div>
    </div>
  );
}
```

---

### 步骤 7: 调用受保护的 API

```typescript
// src/lib/apiClient.ts
import { userManager } from '../auth/oidcConfig';

export class ApiClient {
  private baseUrl: string;

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
  }

  private async getAccessToken(): Promise<string | null> {
    const user = await userManager.getUser();
    return user?.access_token || null;
  }

  async get<T>(path: string): Promise<T> {
    const token = await this.getAccessToken();
    
    const response = await fetch(`${this.baseUrl}${path}`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      if (response.status === 401) {
        // Token 无效或过期，重新登录
        await userManager.signinRedirect();
      }
      throw new Error(`API 请求失败: ${response.statusText}`);
    }

    return response.json();
  }

  async post<T>(path: string, data: any): Promise<T> {
    const token = await this.getAccessToken();
    
    const response = await fetch(`${this.baseUrl}${path}`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(data),
    });

    if (!response.ok) {
      if (response.status === 401) {
        await userManager.signinRedirect();
      }
      throw new Error(`API 请求失败: ${response.statusText}`);
    }

    return response.json();
  }
}

// 导出单例
export const apiClient = new ApiClient('http://localhost:5000');
```

**使用示例**：

```typescript
// src/pages/DataPage.tsx
import React, { useEffect, useState } from 'react';
import { apiClient } from '../lib/apiClient';

interface DataItem {
  id: string;
  name: string;
}

export function DataPage() {
  const [data, setData] = useState<DataItem[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const loadData = async () => {
      try {
        // 调用受保护的 API（自动携带 Bearer Token）
        const items = await apiClient.get<DataItem[]>('/api/data');
        setData(items);
      } catch (error) {
        console.error('加载数据失败:', error);
      } finally {
        setLoading(false);
      }
    };

    loadData();
  }, []);

  if (loading) {
    return <div>Loading...</div>;
  }

  return (
    <div>
      <h1>数据列表</h1>
      <ul>
        {data.map(item => (
          <li key={item.id}>{item.name}</li>
        ))}
      </ul>
    </div>
  );
}
```

---

## 🌐 平台特定示例

### .NET 示例

```csharp
// Program.cs
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie()
.AddOpenIdConnect(options =>
{
    options.Authority = "http://localhost:5001";
    options.ClientId = "dotnet-app";
    options.ClientSecret = "your-client-secret";
    options.ResponseType = "code";
    
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    
    // 本地开发禁用 HTTPS 验证
    options.RequireHttpsMetadata = false;
    
    options.CallbackPath = "/signin-oidc";
    options.SignedOutCallbackPath = "/signout-callback-oidc";
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// 登录端点
app.MapGet("/login", async (HttpContext context) =>
{
    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme,
        new AuthenticationProperties { RedirectUri = "/" });
});

// 登出端点
app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
});

// 受保护的端点
app.MapGet("/profile", (HttpContext context) =>
{
    var user = context.User;
    return new
    {
        Sub = user.FindFirst("sub")?.Value,
        Name = user.FindFirst("name")?.Value,
        Email = user.FindFirst("email")?.Value,
    };
}).RequireAuthorization();

app.Run();
```

---

### Python (FastAPI) 示例

```python
# main.py
from fastapi import FastAPI, Depends, Request
from fastapi.responses import RedirectResponse
from authlib.integrations.starlette_client import OAuth
from starlette.middleware.sessions import SessionMiddleware

app = FastAPI()

# 添加会话中间件
app.add_middleware(SessionMiddleware, secret_key="your-secret-key")

# 配置 OAuth
oauth = OAuth()
oauth.register(
    name='oneid',
    client_id='python-app',
    client_secret='your-client-secret',
    server_metadata_url='http://localhost:5001/.well-known/openid-configuration',
    client_kwargs={
        'scope': 'openid profile email'
    }
)

@app.get('/login')
async def login(request: Request):
    redirect_uri = request.url_for('auth_callback')
    return await oauth.oneid.authorize_redirect(request, redirect_uri)

@app.get('/callback')
async def auth_callback(request: Request):
    token = await oauth.oneid.authorize_access_token(request)
    user_info = token.get('userinfo')
    
    # 保存用户信息到会话
    request.session['user'] = user_info
    
    return RedirectResponse(url='/')

@app.get('/logout')
async def logout(request: Request):
    request.session.clear()
    return RedirectResponse(url='http://localhost:5001/connect/endsession')

@app.get('/profile')
async def profile(request: Request):
    user = request.session.get('user')
    if not user:
        return RedirectResponse(url='/login')
    return user
```

---

## ⚙️ 高级配置

### 1. 指定登录方式（直接跳转到第三方登录）

```typescript
// 直接跳转到 GitHub 登录
await userManager.signinRedirect({
  extraQueryParams: {
    acr_values: 'idp:GitHub', // 或 'idp:Google', 'idp:Gitee', 'idp:Wechat'
  }
});
```

### 2. 多语言支持

```typescript
await userManager.signinRedirect({
  extraQueryParams: {
    ui_locales: 'zh-CN', // 或 'en-US'
  }
});
```

### 3. 请求额外的权限范围

```typescript
const userManager = new UserManager({
  // ... 其他配置
  scope: 'openid profile email phone address roles', // 请求更多 Scopes
});
```

### 4. 自定义登录提示

```typescript
// 总是显示登录页（即使用户已登录）
await userManager.signinRedirect({
  extraQueryParams: {
    prompt: 'login',
  }
});

// 总是显示同意页
await userManager.signinRedirect({
  extraQueryParams: {
    prompt: 'consent',
  }
});
```

---

## 🔧 故障排除

### 问题 1: 无法跳转到 OneID 登录页

**排查**：
1. 检查 OneID 服务是否启动
2. 检查浏览器控制台错误
3. 检查 CORS 配置

**解决**：确保 OneID 允许你的应用域名：

```csharp
// OneID.Identity/Program.cs
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

### 问题 2: 回调后报错 "invalid_grant"

**原因**：Redirect URI 不匹配

**解决**：确认 Redirect URI 完全匹配（包括协议、域名、端口、路径）

### 问题 3: Token 无法刷新

**解决**：确保配置了 `silent_redirect_uri` 并创建对应页面

---

## 📚 相关资源

- [OpenID Connect 规范](https://openid.net/specs/openid-connect-core-1_0.html)
- [oidc-client-ts 文档](https://github.com/authts/oidc-client-ts)
- [自定义登录页集成](./集成指南_02_自定义登录页集成.md)

---

**集成完成后，你的应用将支持**：

- ✅ 用户名密码登录
- ✅ GitHub/Google/Gitee/微信快捷登录
- ✅ 双因素认证
- ✅ 单点登录（SSO）
- ✅ 自动 Token 刷新
- ✅ 安全的会话管理

Happy Coding! 🚀

