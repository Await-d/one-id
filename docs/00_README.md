# 统一身份认证平台（OneID）文档总览
> 版本：v0.1（草案） · 日期：2025-10-05

本文档包包含 **产品 PRD**、**架构与技术栈**、**协议/接口**、**数据库设计**、**开发步骤**、**第三方平台接入指南**、**RP（依赖方）集成指南**、
**安全与合规**、**部署与运维**、**测试计划**、**开发路线图**、**变更与日志模板**、**UI 流程说明** 等 13 份 Markdown 文档。

## 推荐阅读顺序
1. 《01_PRD_统一身份认证平台.md》
2. 《02_架构与技术栈.md》
3. 《03_接口与协议_OIDC_SSO.md》
4. 《04_数据库设计.md》
5. 《05_开发步骤_从零到可运行.md》
6. 《06_外部平台接入_配置手册_GitHub_Gitee_Google_微信.md》
7. 《07_RP集成指南_其他系统如何使用.md》
8. 《08_安全设计与合规.md》
9. 《09_部署运维_CI_CD_监控.md》
10. 《10_测试计划_E2E清单.md》
11. 《11_开发路线图_里程碑与任务拆解.md》
12. 《12_变更记录模板与每日开发日志样例.md》
13. 《13_UI原型说明_页面流程.md》

## 目标环境（用户约束）
- 前端：**React 19** + TypeScript
- 后端：**.NET 9**（ASP.NET Core）
- 数据库：支持 **PostgreSQL / SQL Server / MySQL / SQLite**（可切换）
- 登录方式：本地账号 + 外部平台（GitHub、Gitee、Google、微信）
- 对外协议：**OIDC（OAuth 2.1 授权码 + PKCE）** 为主，SAML（可选插件）
- 单体起步，可演进 **多租户** 与 **微服务化**

## 目录结构（建议）
```
/docs                         # 文档（本包）
/frontend                     # React 19 SPA 管理台 & 登录站点
/backend
  /OneID.Identity             # ASP.NET Core Identity + OpenIddict（OIDC Provider）
  /OneID.AdminApi             # 管理 API（客户端/用户/租户等）
  /OneID.Shared               # 共享内核（领域、DTO、工具）
/deploy
  /docker
  /k8s
```

## 许可与商用提示
- OIDC 提供端建议采用 **OpenIddict**（开源许可友好）；如需 Duende IdentityServer，请关注其商业许可要求。
- 微信登录遵循微信开放平台/公众平台条款，需申请审核、备案等。

---

> 有任何条目需要细化，可在《12_变更记录模板与每日开发日志样例.md》中按模板提交变更。
