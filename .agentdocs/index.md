# OneID .agentdocs 知识库

## 项目概览
- 企业级 OIDC/OAuth2.1 身份认证平台
- 技术栈：ASP.NET Core 9 + React 18 + TypeScript + PostgreSQL
- 三个核心模块：OneID.Identity、OneID.AdminApi、OneID.Shared
- 前端：frontend/admin（管理后台）、frontend/login（登录 SPA）

## Architecture Decisions
- [2026-03-20] 测试策略：后端用 xUnit + WebApplicationFactory（SQLite in-memory），前端待引入 Vitest + RTL — 现有后端测试模式稳定，前端测试基础设施尚未建立
- [2026-03-20] 错误处理：admin 前端用 Ant Design message.error()，login 前端部分仍用 alert()，需统一

## Coding Conventions
- 后端：PascalCase 类名/接口，camelCase 局部变量，async 方法加 Async 后缀
- 前端：React 组件 PascalCase（ClientList.tsx），共享逻辑放 src/shared/，页面放 src/pages/
- 测试命名：`MethodName_State_Expectation`

## Known Pitfalls
- SQLite in-memory 并发测试 → 使用 Guid.NewGuid() 作为数据库名，每个测试类独立实例
- AuthContext.tsx handleAccessTokenExpiring 目前只打日志，未实际调用 signinSilent() — 用户 token 过期后无法静默刷新

## Active Workflows
- [260320-技术债务修复计划](workflow/260320-技术债务修复计划.md)

## Global Important Memory
- 核心功能完成度 ~99%，主要技术债务在测试覆盖和前端代码质量
- 后端 30 个 AdminApi Controller 中仅 5 个有测试；前端 0 个 *.test.tsx 文件
- 生产环境有 6 处 console.log 残留，4 处 alert() 未替换
