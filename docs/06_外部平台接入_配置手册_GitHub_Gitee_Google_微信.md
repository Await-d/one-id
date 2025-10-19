# 外部平台接入｜配置手册

> 所有外部平台统一走 **OAuth 2.0 / OIDC** 适配层（Provider Adapter）。以下为高层步骤，细节以各平台官方最新文档为准。

## 1. GitHub
- 创建 OAuth App：设置回调 `https://<issuer>/external/signin/github`
- 授权端点：`https://github.com/login/oauth/authorize`
- 令牌端点：`https://github.com/login/oauth/access_token`
- 用户 API：`GET https://api.github.com/user`（如需 email：`/user/emails`）
- 建议 Scope：`read:user user:email`

## 2. Gitee
- 授权端点：`https://gitee.com/oauth/authorize`
- 令牌端点：`https://gitee.com/oauth/token`
- 用户 API：`GET https://gitee.com/api/v5/user`

## 3. Google（OIDC）
- 使用 **Google Cloud Console** 创建 OAuth Client；
- 直接通过 `/.well-known/openid-configuration` 发现端点；
- 建议 Scope：`openid profile email`；使用 ID Token 解 `sub`。

## 4. 微信
> 分场景：**网站扫码（PC）** vs **公众号/移动应用**。注意 `openid`/`unionid` 差异。

- 网站扫码：
  - 授权端点：`https://open.weixin.qq.com/connect/qrconnect`
  - 令牌端点：`https://api.weixin.qq.com/sns/oauth2/access_token`
  - 用户信息：`https://api.weixin.qq.com/sns/userinfo`
- 公众号网页：`https://open.weixin.qq.com/connect/oauth2/authorize`
- 移动应用：使用 SDK；回调至统一 `/external/signin/wechat`

**特别注意**
- 微信可能返回 `openid`（每应用唯一）与 `unionid`（同主体下唯一），跨应用统一账号建议优先使用 `unionid`。

## 5. 平台侧统一回调与绑定流程
1) 外部授权成功回调到 `/external/signin/{provider}`；  
2) 服务端以授权码交换外部 Access Token；  
3) 读取外部用户资料，生成 `ExternalLogin`（`Provider`+`ProviderKey`）；  
4) 匹配已绑定用户 → 直接登录；否则：
   - 若存在同邮箱用户 → 提醒合并与绑定；
   - 否则创建新用户（可要求补充手机号/昵称）。

## 6. 配置项（示例）
```json
{
  "Authentication": {
    "GitHub": { "ClientId": "", "ClientSecret": "" },
    "Gitee": { "ClientId": "", "ClientSecret": "" },
    "Google": { "ClientId": "", "ClientSecret": "" },
    "WeChat": { "AppId": "", "AppSecret": "", "Scope": "snsapi_login" }
  }
}
```

## 7. 错误与重试
- 网络波动：重试与超时；
- 用户取消：返回登录页并记录 Audit；
- 平台封禁：提示具体错误码与支持渠道。

