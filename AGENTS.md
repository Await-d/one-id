# Repository Guidelines

## Project Structure & Module Organization
- `backend/OneID.sln` ties together `OneID.Identity`, `OneID.AdminApi`, and `OneID.Shared`, all written in ASP.NET Core 9. Shared contracts and DbContext live in `OneID.Shared` to keep APIs lean.
- Automated tests reside in `backend/tests`, with xUnit projects mirroring production modules. Scripts (`run-tests.sh`, `run-tests.ps1`) provide container-friendly execution.
- Frontend portals live under `frontend/`: `admin/` (management SPA) and `login/` (OIDC login SPA), both built with Vite, React 18+, and TypeScript. Each folder contains its own `package.json`, `src/`, and `dist/` outputs.
- Long-form documentation is grouped in `docs/`, covering architecture, security, deployment, and change-log templates.

## Build, Test, and Development Commands
- `cd backend && dotnet tool restore && dotnet restore` — prime the .NET solution and EF tooling.
- `cd backend && dotnet test` — run the complete xUnit suite against in-memory SQLite fixtures.
- `./backend/tests/run-tests.sh` — execute backend tests inside the official .NET SDK container when the host lacks the SDK.
- `cd frontend/admin && pnpm install && pnpm dev -- --port 5174` — start the admin console locally; adjust `.env` values for API/OIDC endpoints.
- `cd frontend/login && pnpm install && pnpm dev` — boot the login SPA; use `pnpm --filter oneid-*- build` for production bundles.

## Coding Style & Naming Conventions
- Follow standard C# conventions: PascalCase for classes/interfaces, camelCase for locals/fields, async suffixes for Tasks. Keep files focused on a single responsibility to stay within SRP.
- TypeScript adopts ESLint + Prettier defaults with 2-space indentation; React components live under `src/features/*` with file names in PascalCase (`ClientList.tsx`). Extract shared hooks/utilities into `src/shared/` to avoid duplication.
- Centralize package versions via `Directory.Packages.props` and prefer dependency injection over direct instantiation to uphold SOLID.

## Testing Guidelines
- Backend: use xUnit for unit/integration tests; place fixtures under `backend/tests/<Project>.Tests`. Name tests following `MethodName_State_Expectation` and keep Arrange/Act/Assert blocks explicit.
- Frontend: add React Testing Library tests alongside components (`*.test.tsx`). Target critical flows (client CRUD, OIDC redirect handling) and aim for coverage that exercises API adapters and hooks.
- Run tests in CI-ready environments (container scripts or Node 18+). Avoid relying on external IdP services; mock OIDC responses where needed.

## Commit & Pull Request Guidelines
- This distribution ships without `.git` history; align new work to Conventional Commits (`feat:`, `fix:`, `chore:`) for clarity and changelog automation. Keep messages under 72 characters and describe the problem before the solution in the body.
- Open pull requests with a concise summary, linked task/issue, testing evidence (`dotnet test`, `pnpm test`), and screenshots for UI-facing changes. Highlight backward-compatibility considerations and configuration impacts.
- Before requesting review, ensure formatters (e.g., `dotnet format`, `pnpm lint`) pass locally, secrets remain out of source, and `.env` examples (`env.example`) reflect any new settings.

## Security & Configuration Tips
- Never commit real secrets; base new configuration keys on `env.example` and document defaults in `docs/`.
- For HTTPS, use the provided `docker-compose.https.yml` and `deploy-https.sh` scripts; update `nginx/` configs when exposing new routes.
- When integrating external IdPs, validate scopes and redirect URIs against the references in `docs/06_外部平台接入_配置手册_GitHub_Gitee_Google_微信.md` to prevent misconfiguration.
