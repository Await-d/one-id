# 接口与协议｜OIDC / OAuth 2.1 / SSO

## 1. OIDC 发现与 JWK
- `GET /.well-known/openid-configuration`
- `GET /.well-known/jwks.json`（包含当前与历史公钥；kid 标识）

## 2. 授权端点（/connect/authorize）
**授权码 + PKCE（推荐）**
```
GET /connect/authorize?
  response_type=code
  &client_id={client_id}
  &redirect_uri={registered_uri}
  &scope=openid profile email offline_access
  &state={random}
  &nonce={random}
  &code_challenge={S256(code_verifier)}
  &code_challenge_method=S256
```
- `state` 防 CSRF；`nonce` 绑定 ID Token。

## 3. 令牌端点（/connect/token）
**交换授权码**
```
POST /connect/token
  grant_type=authorization_code
  code=...
  redirect_uri=...
  code_verifier=...
  client_id=...    # 公共客户端可省 client_secret
```
**刷新令牌**
```
POST /connect/token
  grant_type=refresh_token
  refresh_token=...
  client_id=...
```
- 刷新令牌默认**一次性**（轮换），旧的立即失效（可启用滑动过期）。

## 4. 用户信息端点（/connect/userinfo）
- 需携带 `access_token`（`scope` 决定可见 Claim）。

## 5. 注销（/connect/endsession）
- 前通道：浏览器重定向；
- 后通道：服务端回调 RP `logout` 端点（可选）。

## 6. 令牌说明（示例）
**ID Token（JWT）**：
- 标准 Claim：`iss sub aud exp iat auth_time nonce acr amr at_hash`  
- 用户 Claim（依 scope）：`name preferred_username given_name family_name email email_verified phone_number`

**Access Token（JWT）**：
- Audience 指向 RP 资源；可包含最小化权限 Claim（或基于 scope 的约定）。

## 7. 作用域（建议）
- 标准：`openid profile email phone`、`offline_access`
- 自定义：`oneid.admin`, `oneid.read`, `oneid.write`, `tenant:{id}` 等

## 8. 安全基线
- 强制 **PKCE(S256)**；
- Access Token 短期；Refresh Token 轮换；
- JWK **按季度轮换**，kid 版本化；
- 支持 **DPoP/MTLS**（可选增强）。

## 9. 示例（React + oidc-client-ts）
```ts
import { UserManager, WebStorageStateStore } from "oidc-client-ts";

export const userManager = new UserManager({
  authority: "https://oneid.example.com",
  client_id: "spa.portal",
  redirect_uri: "https://app.example.com/callback",
  post_logout_redirect_uri: "https://app.example.com",
  response_type: "code",
  scope: "openid profile email offline_access",
  automaticSilentRenew: true,
  loadUserInfo: true,
  userStore: new WebStorageStateStore({ store: window.sessionStorage })
});
```

## 10. 内省与撤销（可选）
- `/connect/introspect`：验证 Reference Token 或 JTIs；
- `/connect/revocation`：手动撤销刷新令牌或客户端凭据。

## 11. SAML 2.0（可选）
- 作为 IdP 支持 SAML SP；采用 Sustainsys.Saml2。  
- 建议仅在确需兼容传统系统时启用；主推 OIDC。
