# API 直接集成指南

> 本文档介绍如何在后端服务、定时任务、CLI 工具等无需用户交互的场景中直接调用 OneID API。

## 📋 目录

- [概述](#概述)
- [认证方式](#认证方式)
- [集成步骤](#集成步骤)
- [平台示例](#平台示例)
- [最佳实践](#最佳实践)

---

## 🎯 概述

### 什么是 API 直接集成？

API 直接集成适用于以下场景：

- 🤖 后端服务之间的相互调用
- ⏰ 定时任务和批处理作业
- 🛠️ 命令行工具和脚本
- 📊 数据同步和导入导出
- 🔌 第三方系统集成

这些场景的共同特点是：**没有用户交互界面**。

### 与标准 OIDC 集成的区别

| 特性 | 标准 OIDC | API 直接集成 |
|------|----------|------------|
| 用户交互 | ✅ 需要 | ❌ 不需要 |
| 浏览器 | ✅ 需要 | ❌ 不需要 |
| 授权流程 | Authorization Code | Client Credentials / Password |
| 使用场景 | Web/Mobile 应用 | 后端服务 |
| Token 类型 | Access + ID Token | Access Token |

---

## 🔐 认证方式

### 方式 1: Client Credentials（推荐）

**适用场景**：服务间认证（Machine-to-Machine）

**原理**：使用 Client ID 和 Client Secret 直接获取 Access Token

**安全性**：⭐⭐⭐⭐⭐（最安全）

**示例**：

```bash
curl -X POST http://localhost:5001/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials" \
  -d "client_id=my-service" \
  -d "client_secret=your-secret" \
  -d "scope=api"
```

**响应**：

```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "scope": "api"
}
```

---

### 方式 2: Resource Owner Password Credentials

**适用场景**：受信任的应用（如官方 CLI 工具）

**原理**：使用用户的用户名和密码换取 Token

**安全性**：⭐⭐⭐（需谨慎使用）

**示例**：

```bash
curl -X POST http://localhost:5001/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "username=admin" \
  -d "password=Admin@123" \
  -d "client_id=cli-tool" \
  -d "scope=openid profile email api"
```

**⚠️ 安全警告**：
- 仅在完全受信任的客户端中使用
- 不要在公共客户端中使用
- OAuth 2.1 已废弃此流程

---

### 方式 3: API Key（自定义实现）

**适用场景**：简单的 API 访问

**原理**：预先生成的长期有效 Token

**示例**：

```bash
curl http://localhost:5002/api/users \
  -H "X-API-Key: your-api-key-here"
```

**实现**（OneID Admin API 已支持）：

1. 在 Admin Portal 创建 API Key
2. 记录 Key 值（仅显示一次）
3. 在请求中使用 `X-API-Key` Header

---

## 🚀 集成步骤

### 步骤 1: 注册服务客户端

在 OneID Admin Portal 中创建客户端：

| 字段 | 值 | 说明 |
|------|-----|------|
| Client ID | `backend-service` | 服务标识 |
| Client Type | `Confidential` | 机密客户端 |
| Grant Types | `client_credentials` | 授权类型 |
| Scopes | `api` | 权限范围 |
| Require Client Secret | ✅ 是 | 必须 |

保存后获取 `Client Secret`，妥善保管。

---

### 步骤 2: 实现 Token 获取

#### Node.js/TypeScript 示例

```typescript
// src/lib/oneIdClient.ts

interface TokenResponse {
  access_token: string;
  token_type: string;
  expires_in: number;
  scope: string;
}

export class OneIdClient {
  private baseUrl: string;
  private clientId: string;
  private clientSecret: string;
  private cachedToken: string | null = null;
  private tokenExpiresAt: number = 0;

  constructor(baseUrl: string, clientId: string, clientSecret: string) {
    this.baseUrl = baseUrl;
    this.clientId = clientId;
    this.clientSecret = clientSecret;
  }

  /**
   * 获取 Access Token（自动缓存和刷新）
   */
  async getAccessToken(): Promise<string> {
    // 如果 Token 还有效，直接返回
    if (this.cachedToken && Date.now() < this.tokenExpiresAt - 60000) {
      return this.cachedToken;
    }

    // 获取新 Token
    const params = new URLSearchParams({
      grant_type: 'client_credentials',
      client_id: this.clientId,
      client_secret: this.clientSecret,
      scope: 'api',
    });

    const response = await fetch(`${this.baseUrl}/connect/token`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
      },
      body: params.toString(),
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(`Failed to get token: ${error.error_description}`);
    }

    const tokenResponse: TokenResponse = await response.json();

    // 缓存 Token
    this.cachedToken = tokenResponse.access_token;
    this.tokenExpiresAt = Date.now() + tokenResponse.expires_in * 1000;

    return this.cachedToken;
  }

  /**
   * 调用受保护的 API
   */
  async callApi<T>(method: string, path: string, data?: any): Promise<T> {
    const token = await this.getAccessToken();

    const options: RequestInit = {
      method,
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    };

    if (data && (method === 'POST' || method === 'PUT' || method === 'PATCH')) {
      options.body = JSON.stringify(data);
    }

    const response = await fetch(`${this.baseUrl}${path}`, options);

    if (!response.ok) {
      throw new Error(`API call failed: ${response.statusText}`);
    }

    return response.json();
  }

  /**
   * 获取用户列表
   */
  async getUsers(page: number = 1, pageSize: number = 10) {
    return this.callApi('GET', `/api/users?page=${page}&pageSize=${pageSize}`);
  }

  /**
   * 创建用户
   */
  async createUser(userData: any) {
    return this.callApi('POST', '/api/users', userData);
  }

  /**
   * 更新用户
   */
  async updateUser(userId: string, userData: any) {
    return this.callApi('PUT', `/api/users/${userId}`, userData);
  }

  /**
   * 删除用户
   */
  async deleteUser(userId: string) {
    return this.callApi('DELETE', `/api/users/${userId}`);
  }
}

// 使用示例
const oneId = new OneIdClient(
  'http://localhost:5001',
  process.env.ONEID_CLIENT_ID!,
  process.env.ONEID_CLIENT_SECRET!
);

// 获取用户列表
const users = await oneId.getUsers(1, 20);
console.log('用户列表:', users);

// 创建用户
const newUser = await oneId.createUser({
  email: 'newuser@example.com',
  username: 'newuser',
  password: 'SecurePassword123!',
});
console.log('创建成功:', newUser);
```

---

### 步骤 3: 配置环境变量

```bash
# .env
ONEID_BASE_URL=http://localhost:5001
ONEID_CLIENT_ID=backend-service
ONEID_CLIENT_SECRET=your-secret-here
```

```typescript
// src/config.ts
import { config } from 'dotenv';

config();

export const oneIdConfig = {
  baseUrl: process.env.ONEID_BASE_URL!,
  clientId: process.env.ONEID_CLIENT_ID!,
  clientSecret: process.env.ONEID_CLIENT_SECRET!,
};
```

---

### 步骤 4: 实现错误处理和重试

```typescript
// src/lib/oneIdClient.ts

export class OneIdClient {
  // ... 其他代码 ...

  /**
   * 带重试的 API 调用
   */
  async callApiWithRetry<T>(
    method: string,
    path: string,
    data?: any,
    maxRetries: number = 3
  ): Promise<T> {
    let lastError: Error | null = null;

    for (let attempt = 1; attempt <= maxRetries; attempt++) {
      try {
        return await this.callApi<T>(method, path, data);
      } catch (error: any) {
        lastError = error;
        
        console.error(`API 调用失败 (尝试 ${attempt}/${maxRetries}):`, error.message);
        
        // 如果是 401 错误，清除缓存的 Token
        if (error.message.includes('401')) {
          this.cachedToken = null;
          this.tokenExpiresAt = 0;
        }
        
        // 如果不是最后一次尝试，等待后重试
        if (attempt < maxRetries) {
          const delay = Math.pow(2, attempt) * 1000; // 指数退避
          console.log(`等待 ${delay}ms 后重试...`);
          await new Promise(resolve => setTimeout(resolve, delay));
        }
      }
    }

    throw lastError!;
  }
}
```

---

## 🌐 平台示例

### Python 示例

```python
# oneid_client.py

import requests
import time
from typing import Optional, Dict, Any

class OneIdClient:
    def __init__(self, base_url: str, client_id: str, client_secret: str):
        self.base_url = base_url
        self.client_id = client_id
        self.client_secret = client_secret
        self.cached_token: Optional[str] = None
        self.token_expires_at: float = 0

    def get_access_token(self) -> str:
        """获取 Access Token（自动缓存）"""
        # 如果 Token 还有效，直接返回
        if self.cached_token and time.time() < self.token_expires_at - 60:
            return self.cached_token

        # 获取新 Token
        response = requests.post(
            f'{self.base_url}/connect/token',
            data={
                'grant_type': 'client_credentials',
                'client_id': self.client_id,
                'client_secret': self.client_secret,
                'scope': 'api',
            }
        )

        response.raise_for_status()
        token_data = response.json()

        # 缓存 Token
        self.cached_token = token_data['access_token']
        self.token_expires_at = time.time() + token_data['expires_in']

        return self.cached_token

    def call_api(self, method: str, path: str, data: Optional[Dict] = None) -> Any:
        """调用受保护的 API"""
        token = self.get_access_token()

        headers = {
            'Authorization': f'Bearer {token}',
            'Content-Type': 'application/json',
        }

        response = requests.request(
            method=method,
            url=f'{self.base_url}{path}',
            headers=headers,
            json=data
        )

        response.raise_for_status()
        return response.json()

    def get_users(self, page: int = 1, page_size: int = 10):
        """获取用户列表"""
        return self.call_api('GET', f'/api/users?page={page}&pageSize={page_size}')

    def create_user(self, user_data: Dict):
        """创建用户"""
        return self.call_api('POST', '/api/users', user_data)

# 使用示例
import os

oneid = OneIdClient(
    base_url=os.getenv('ONEID_BASE_URL'),
    client_id=os.getenv('ONEID_CLIENT_ID'),
    client_secret=os.getenv('ONEID_CLIENT_SECRET')
)

# 获取用户列表
users = oneid.get_users(page=1, page_size=20)
print('用户列表:', users)

# 创建用户
new_user = oneid.create_user({
    'email': 'newuser@example.com',
    'username': 'newuser',
    'password': 'SecurePassword123!'
})
print('创建成功:', new_user)
```

---

### .NET 示例

```csharp
// OneIdClient.cs

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class OneIdClient
{
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private string? _cachedToken;
    private DateTime _tokenExpiresAt;

    public OneIdClient(string baseUrl, string clientId, string clientSecret)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _clientId = clientId;
        _clientSecret = clientSecret;
    }

    /// <summary>
    /// 获取 Access Token（自动缓存）
    /// </summary>
    public async Task<string> GetAccessTokenAsync()
    {
        // 如果 Token 还有效，直接返回
        if (_cachedToken != null && DateTime.Now < _tokenExpiresAt.AddMinutes(-1))
        {
            return _cachedToken;
        }

        // 获取新 Token
        var requestData = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", _clientId },
            { "client_secret", _clientSecret },
            { "scope", "api" }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/connect/token")
        {
            Content = new FormUrlEncodedContent(requestData)
        };

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();

        // 缓存 Token
        _cachedToken = tokenResponse.AccessToken;
        _tokenExpiresAt = DateTime.Now.AddSeconds(tokenResponse.ExpiresIn);

        return _cachedToken;
    }

    /// <summary>
    /// 调用受保护的 API
    /// </summary>
    public async Task<T> CallApiAsync<T>(HttpMethod method, string path, object? data = null)
    {
        var token = await GetAccessTokenAsync();

        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (data != null)
        {
            var json = JsonSerializer.Serialize(data);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>();
    }

    /// <summary>
    /// 获取用户列表
    /// </summary>
    public async Task<UserListResponse> GetUsersAsync(int page = 1, int pageSize = 10)
    {
        return await CallApiAsync<UserListResponse>(
            HttpMethod.Get,
            $"/api/users?page={page}&pageSize={pageSize}"
        );
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    public async Task<User> CreateUserAsync(CreateUserDto userData)
    {
        return await CallApiAsync<User>(HttpMethod.Post, "/api/users", userData);
    }
}

// 数据模型
public class TokenResponse
{
    public string AccessToken { get; set; }
    public string TokenType { get; set; }
    public int ExpiresIn { get; set; }
}

// 使用示例
var oneId = new OneIdClient(
    baseUrl: "http://localhost:5001",
    clientId: Environment.GetEnvironmentVariable("ONEID_CLIENT_ID"),
    clientSecret: Environment.GetEnvironmentVariable("ONEID_CLIENT_SECRET")
);

// 获取用户列表
var users = await oneId.GetUsersAsync(page: 1, pageSize: 20);
Console.WriteLine($"找到 {users.TotalCount} 个用户");

// 创建用户
var newUser = await oneId.CreateUserAsync(new CreateUserDto
{
    Email = "newuser@example.com",
    Username = "newuser",
    Password = "SecurePassword123!"
});
Console.WriteLine($"创建成功: {newUser.Email}");
```

---

### Java 示例

```java
// OneIdClient.java

import com.fasterxml.jackson.databind.ObjectMapper;
import okhttp3.*;

import java.io.IOException;
import java.time.Instant;
import java.util.HashMap;
import java.util.Map;

public class OneIdClient {
    private final OkHttpClient httpClient;
    private final String baseUrl;
    private final String clientId;
    private final String clientSecret;
    private final ObjectMapper objectMapper;
    
    private String cachedToken;
    private Instant tokenExpiresAt;

    public OneIdClient(String baseUrl, String clientId, String clientSecret) {
        this.httpClient = new OkHttpClient();
        this.baseUrl = baseUrl;
        this.clientId = clientId;
        this.clientSecret = clientSecret;
        this.objectMapper = new ObjectMapper();
    }

    /**
     * 获取 Access Token（自动缓存）
     */
    public String getAccessToken() throws IOException {
        // 如果 Token 还有效，直接返回
        if (cachedToken != null && Instant.now().isBefore(tokenExpiresAt.minusSeconds(60))) {
            return cachedToken;
        }

        // 获取新 Token
        FormBody requestBody = new FormBody.Builder()
                .add("grant_type", "client_credentials")
                .add("client_id", clientId)
                .add("client_secret", clientSecret)
                .add("scope", "api")
                .build();

        Request request = new Request.Builder()
                .url(baseUrl + "/connect/token")
                .post(requestBody)
                .build();

        try (Response response = httpClient.newCall(request).execute()) {
            if (!response.isSuccessful()) {
                throw new IOException("Failed to get token: " + response);
            }

            TokenResponse tokenResponse = objectMapper.readValue(
                    response.body().string(),
                    TokenResponse.class
            );

            // 缓存 Token
            this.cachedToken = tokenResponse.getAccessToken();
            this.tokenExpiresAt = Instant.now().plusSeconds(tokenResponse.getExpiresIn());

            return this.cachedToken;
        }
    }

    /**
     * 调用受保护的 API
     */
    public <T> T callApi(String method, String path, Object data, Class<T> responseType) throws IOException {
        String token = getAccessToken();

        Request.Builder requestBuilder = new Request.Builder()
                .url(baseUrl + path)
                .header("Authorization", "Bearer " + token);

        if (data != null) {
            String json = objectMapper.writeValueAsString(data);
            RequestBody body = RequestBody.create(json, MediaType.parse("application/json"));
            
            if ("POST".equals(method)) {
                requestBuilder.post(body);
            } else if ("PUT".equals(method)) {
                requestBuilder.put(body);
            }
        } else {
            requestBuilder.method(method, null);
        }

        try (Response response = httpClient.newCall(requestBuilder.build()).execute()) {
            if (!response.isSuccessful()) {
                throw new IOException("API call failed: " + response);
            }

            return objectMapper.readValue(response.body().string(), responseType);
        }
    }

    /**
     * 获取用户列表
     */
    public UserListResponse getUsers(int page, int pageSize) throws IOException {
        return callApi("GET", "/api/users?page=" + page + "&pageSize=" + pageSize, 
                      null, UserListResponse.class);
    }

    /**
     * 创建用户
     */
    public User createUser(CreateUserDto userData) throws IOException {
        return callApi("POST", "/api/users", userData, User.class);
    }
}

// 使用示例
OneIdClient oneId = new OneIdClient(
    "http://localhost:5001",
    System.getenv("ONEID_CLIENT_ID"),
    System.getenv("ONEID_CLIENT_SECRET")
);

// 获取用户列表
UserListResponse users = oneId.getUsers(1, 20);
System.out.println("找到 " + users.getTotalCount() + " 个用户");

// 创建用户
CreateUserDto newUserData = new CreateUserDto();
newUserData.setEmail("newuser@example.com");
newUserData.setUsername("newuser");
newUserData.setPassword("SecurePassword123!");

User newUser = oneId.createUser(newUserData);
System.out.println("创建成功: " + newUser.getEmail());
```

---

## ✅ 最佳实践

### 1. Token 缓存

```typescript
// ✅ 好的做法：缓存 Token，避免频繁请求
class OneIdClient {
  private cachedToken: string | null = null;
  private tokenExpiresAt: number = 0;

  async getAccessToken(): Promise<string> {
    // 在过期前 1 分钟刷新
    if (this.cachedToken && Date.now() < this.tokenExpiresAt - 60000) {
      return this.cachedToken;
    }
    
    // 获取新 Token
    // ...
  }
}

// ❌ 坏的做法：每次都获取新 Token
async function callApi() {
  const token = await getNewToken(); // 浪费性能
  // ...
}
```

### 2. 错误处理

```typescript
// ✅ 好的做法：详细的错误处理
try {
  const users = await oneId.getUsers();
} catch (error) {
  if (error.message.includes('401')) {
    console.error('认证失败，请检查凭据');
  } else if (error.message.includes('403')) {
    console.error('权限不足');
  } else if (error.message.includes('429')) {
    console.error('请求过于频繁，稍后重试');
  } else {
    console.error('未知错误:', error);
  }
}
```

### 3. 安全存储凭据

```typescript
// ✅ 好的做法：使用环境变量
const client = new OneIdClient(
  process.env.ONEID_BASE_URL!,
  process.env.ONEID_CLIENT_ID!,
  process.env.ONEID_CLIENT_SECRET!
);

// ❌ 坏的做法：硬编码凭据
const client = new OneIdClient(
  'http://localhost:5001',
  'my-service',
  'super-secret-123' // 不要这样做！
);
```

### 4. 超时和重试

```typescript
// 设置合理的超时
const httpClient = new HttpClient({
  timeout: 30000, // 30 秒
  retry: {
    limit: 3,
    methods: ['GET', 'PUT', 'HEAD', 'DELETE', 'OPTIONS', 'TRACE'],
    statusCodes: [408, 413, 429, 500, 502, 503, 504],
  },
});
```

### 5. 日志记录

```typescript
// 记录重要操作
async function createUser(userData: any) {
  console.log('[OneID] 创建用户:', userData.email);
  
  try {
    const user = await oneId.createUser(userData);
    console.log('[OneID] 用户创建成功:', user.id);
    return user;
  } catch (error) {
    console.error('[OneID] 用户创建失败:', error);
    throw error;
  }
}
```

---

## 🔧 故障排除

### 问题 1: 获取 Token 失败

**错误**：`invalid_client`

**原因**：Client ID 或 Secret 不正确

**解决**：
1. 检查 Admin Portal 中的客户端配置
2. 确认 Client Secret 没有过期
3. 验证环境变量是否正确加载

### 问题 2: API 调用返回 403

**错误**：`insufficient_scope`

**原因**：Token 的 Scope 不足

**解决**：
1. 在获取 Token 时请求正确的 Scope
2. 在 Admin Portal 中为客户端授予相应权限

### 问题 3: Token 频繁失效

**原因**：没有正确缓存 Token

**解决**：实现 Token 缓存机制（见上文示例）

---

## 📚 相关资源

- [OAuth 2.0 Client Credentials](https://oauth.net/2/grant-types/client-credentials/)
- [标准 OIDC 集成](./集成指南_01_标准OIDC集成.md)
- [OneID Admin API 文档](./docs/API_REFERENCE.md)

---

## 🎯 总结

**API 直接集成适合**：

- ✅ 后端服务间调用
- ✅ 定时任务和批处理
- ✅ CLI 工具和脚本
- ✅ 数据同步

**关键要点**：

1. ✅ 使用 Client Credentials 授权（最安全）
2. ✅ 妥善保管 Client Secret
3. ✅ 实现 Token 缓存
4. ✅ 处理错误和重试
5. ✅ 记录详细日志

Happy Coding! 🚀

