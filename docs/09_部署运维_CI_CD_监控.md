# 部署运维｜CI/CD 与监控

## 1. 容器化
- 后端：`Dockerfile` 基于 `mcr.microsoft.com/dotnet/aspnet:9.0`；
- 前端：Vite 构建后静态资源挂载到 Nginx。

## 2. Compose（生产示意）
```yaml
services:
  oneid:
    image: oneid/identity:1.0.0
    env_file: .env
    ports: ["443:443"]
    depends_on: [db, redis]
  db:
    image: postgres:16
  redis:
    image: redis:7
  otel-collector:
    image: otel/opentelemetry-collector
```

## 3. Kubernetes（要点）
- Ingress + TLS；HPA 按 CPU/延迟扩缩；
- ConfigMap/Secret 区分配置与密钥；
- PodSecurity/NetworkPolicy 加固。

## 4. CI/CD（示例流程）
- 代码扫描（SAST/依赖漏洞）→ 构建 → 单元/集成测试 → 制品签名 → 镜像推送 → 部署 → 冒烟 → 通知。

## 5. 可观测性
- OpenTelemetry：采集 ASP.NET Core/EF/HTTP Client；
- 指标：授权成功率、失败率、P95 延迟、外部平台错误占比；
- 告警：速率异常、密钥切换、登录失败激增。

## 6. 灰度与回滚
- Blue/Green、Canary；
- 令牌签名密钥的灰度轮换与回滚策略。

