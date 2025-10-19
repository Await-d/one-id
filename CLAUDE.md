# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

OneID is an enterprise-grade unified identity authentication platform based on OIDC (OpenID Connect) protocol. It provides secure Single Sign-On (SSO) services using **Authorization Code Flow + PKCE** and refresh token support.

**Important**: The system uses a **single-container deployment** where the React SPA is served from the .NET backend's `wwwroot` directory, eliminating CORS issues and simplifying deployment.

## Technology Stack

### Backend
- **.NET 9** - ASP.NET Core Minimal APIs
- **OpenIddict 5.8** - OIDC Provider implementation
- **ASP.NET Core Identity** - User management
- **Entity Framework Core 9** - ORM with support for PostgreSQL, MySQL, SQLite, SQL Server
- **PostgreSQL 16** - Primary database (production)
- **Redis 7** - Caching layer
- **Serilog** - Structured logging

### Frontend
- **React 18** (with React 19 migration in progress)
- **TypeScript**
- **oidc-client-ts** - OIDC client library
- **TanStack Query** - Data fetching and state management
- **Vite** - Build tool
- **Tailwind CSS v4** - Styling
- **pnpm 10.17.1** - Package manager

### Project Structure
```
backend/
  ├── OneID.Identity/      # OIDC Server (main application)
  ├── OneID.AdminApi/      # Admin API (future)
  ├── OneID.Shared/        # Shared domain models and infrastructure
  └── tests/               # xUnit tests

frontend/
  ├── login/               # Login/authentication SPA (production)
  ├── admin/               # Admin portal (in development)
  └── vite-project/        # (deprecated/experimental)
```

## Common Commands

### Docker Compose (Recommended for Development)

```bash
# Start all services (PostgreSQL + Redis + Backend)
docker compose -f docker-compose.dev.yml up -d

# View backend logs
docker compose -f docker-compose.dev.yml logs -f identity

# Restart backend after code changes
docker compose -f docker-compose.dev.yml restart identity

# Stop all services
docker compose -f docker-compose.dev.yml down

# Stop and remove volumes (clean slate)
docker compose -f docker-compose.dev.yml down -v
```

### Backend Development

```bash
cd backend

# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run Identity server
dotnet run --project OneID.Identity/OneID.Identity.csproj

# Run tests
dotnet test

# Run specific test project
dotnet test tests/OneID.Identity.Tests/OneID.Identity.Tests.csproj

# Database migrations (requires dotnet-ef tool)
dotnet tool restore
dotnet ef migrations add <MigrationName> --project OneID.Identity
dotnet ef database update --project OneID.Identity
```

### Frontend Development

```bash
cd frontend/login

# Install dependencies (uses pnpm)
pnpm install

# Start dev server (requires backend running on localhost:5101)
pnpm dev

# Build for production
pnpm build

# Preview production build
pnpm preview
```

### Production Build

```bash
# Build complete application (frontend + backend in single container)
docker compose build

# Start production stack
docker compose up -d

# View logs
docker compose logs -f oneid
```

## Architecture Principles

### Single-Container Design
The production deployment packages the React SPA inside the .NET backend container:
1. Frontend is built during Docker image creation
2. Built assets are placed in `backend/OneID.Identity/wwwroot`
3. ASP.NET Core serves both static files and API endpoints
4. All requests share the same origin (no CORS configuration needed)
5. OIDC authentication flow occurs within the same domain

### OIDC Endpoints
- **Discovery**: `/.well-known/openid-configuration`
- **JWKS**: `/.well-known/jwks`
- **Authorization**: `/connect/authorize`
- **Token**: `/connect/token`
- **UserInfo**: `/connect/userinfo`
- **EndSession**: `/connect/endsession`
- **Introspection**: `/connect/introspect`
- **Revocation**: `/connect/revocation`

### Database Strategy
- Uses **Central Package Management** (`Directory.Packages.props`)
- Supports multiple database providers via abstraction in `OneID.Shared`
- Provider selected via `Persistence__Provider` configuration
- Migrations are in `OneID.Identity/Migrations/` for PostgreSQL
- Database is auto-seeded on first run with admin user and OIDC client

### Configuration
Environment variables are used for runtime configuration:
- `ConnectionStrings__Default` - Database connection string
- `Persistence__Provider` - Database provider (Postgres/MySQL/SQLite/SqlServer)
- `Seed__Admin__*` - Admin user configuration
- `Seed__Oidc__*` - OIDC client configuration
- See `.env.example` for complete list

## Development Workflow

### Adding Database Migrations
1. Ensure `dotnet-ef` tool is installed: `dotnet tool restore`
2. Make changes to domain models in `OneID.Shared/Domain/`
3. Create migration: `cd backend && dotnet ef migrations add <Name> --project OneID.Identity`
4. Review generated migration files
5. Apply migration: `dotnet ef database update --project OneID.Identity`

### Testing OIDC Flow
1. Start backend (Docker Compose or `dotnet run`)
2. Start frontend dev server: `cd frontend/login && pnpm dev`
3. Navigate to `http://localhost:5173`
4. Login with admin credentials (from `.env`)
5. Frontend automatically uses OIDC Authorization Code + PKCE flow

### Debugging
- **Backend**: Attach debugger to `OneID.Identity` process (port 5101 in dev)
- **Frontend**: Use browser DevTools
- **OIDC Issues**: Check `/.well-known/openid-configuration` and browser Network tab
- **Database**: Connect to PostgreSQL on `localhost:15432` (dev) or `localhost:5432` (via docker exec)

### Frontend Environment Configuration
- Development uses `.env.development` with `VITE_OIDC_AUTHORITY=http://localhost:5101`
- Production build creates empty authority in `.env.production` (uses `window.location.origin`)
- This allows the same build to work in any deployment environment

## Security Considerations

⚠️ **Development-only settings** (must be removed for production):
- `DisableTransportSecurityRequirement()` - Allows HTTP instead of HTTPS
- `DisableAccessTokenEncryption()` - Access tokens sent in plaintext
- Development certificates used for signing (not secure)

Production deployments should:
- Use HTTPS (via reverse proxy like nginx/Traefik)
- Generate proper signing/encryption certificates
- Use strong database passwords
- Enable rate limiting (already configured)
- Restrict CORS origins

## 三大原则 (Three Core Principles)

When working on this codebase, always follow:

1. **🚫 No Mock Solutions** - Use real data, real system metrics, real container operations
2. **🚫 No Simplified Solutions** - Implement complete error handling, performance optimization, security validation
3. **🚫 No Temporary Solutions** - All code must be production-grade, maintainable, and extensible

## Implemented Features

### ✅ Core Authentication
- OIDC Authorization Code + PKCE flow
- Refresh token support
- Local account login (username/password)
- Session management
- JWT token validation

### ✅ External Login Providers
- **GitHub** OAuth integration
- **Google** OAuth integration
- **Gitee** OAuth integration
- Automatic account creation on first external login
- Email-based account linking
- Configuration via `ExternalAuth` section in appsettings

### ✅ Account Management
- External account binding/unbinding
- Multiple external accounts per user
- User profile page with linked accounts
- `/profile` page for managing external logins

### ✅ Admin API - User Management
- List all users with pagination
- Get user by ID or email
- Create new users
- Update user information (display name, email confirmation, lockout)
- Delete users
- Change user passwords
- Unlock locked accounts
- View external login associations

### ✅ Admin Portal
- **Clients Management** - OIDC client CRUD operations
- **Users Management** - Full user administration UI
- Ant Design components for professional UI
- Real-time data updates with TanStack Query

### ✅ Audit Logging
- Comprehensive audit trail for all operations
- Track user actions, IP addresses, user agents
- Query logs by date range, category, user, success status
- Categories: Authentication, Authorization, User, Client, Configuration, Security
- Audit logs API endpoint for admin portal
- Admin portal UI for viewing audit logs with filtering

### ✅ Multi-Factor Authentication (MFA/TOTP)
- **TOTP-based 2FA** using authenticator apps (Google Authenticator, Authy, etc.)
- **Complete login flow integration** - MFA verification required after password authentication
- QR code generation for easy setup
- Recovery codes (10 codes, single-use)
- Encrypted storage of TOTP secrets and recovery codes using ASP.NET Core Data Protection
- MFA management UI in user profile
- Enable/disable MFA with password confirmation
- Regenerate recovery codes
- Custom TOTP token provider for ASP.NET Core Identity
- Two-factor authentication page with TOTP and recovery code support
- Automatic redirect to 2FA page when user has MFA enabled
- Uses **Otp.NET** for TOTP generation and **QRCoder** for QR code images

### ✅ User Registration
- Public user registration with username, email, password
- Password strength validation (min 8 chars, requires lowercase and digit)
- Email uniqueness validation
- Optional display name field
- Audit logging for registration attempts
- Registration page with form validation

### ✅ Password Reset & Recovery
- **Forgot password flow** with email-based token delivery
- Time-limited, single-use password reset tokens via ASP.NET Core Identity
- Security-conscious design (no user enumeration - always returns success)
- **Reset password** page with token validation from URL
- **Change password** endpoint for authenticated users (requires current password)
- Email service abstraction with development logging implementation
- Production-ready interface for SendGrid/SES/SMTP integration
- Comprehensive audit logging for all password operations
- Account lockout protection during password reset attempts

### ✅ External Auth Provider Management (Database-backed)
- **Dynamic provider configuration** stored in database instead of config files
- Create, update, delete, enable/disable OAuth providers via admin UI
- Encrypted client secrets using Data Protection
- Support for GitHub, Google, Gitee (extensible)
- Multi-tenant ready with TenantId field
- Providers loaded from database at application startup

### ✅ API Key Management
- **Secure API key generation** using cryptographic random bytes
- SHA256 hashing for secure storage (keys stored as hashes, never plaintext)
- API key authentication handler for Bearer token authentication
- Create, list, and revoke API keys per user
- Key prefix display (e.g., "ak_12345...") for identification
- Optional expiration dates for time-limited access
- Track last used timestamp for security auditing
- Scopes/permissions support (future-ready for fine-grained access control)
- Multi-tenant support with TenantId field
- Complete audit logging for all API key operations
- User-friendly management UI in profile page
- One-time display of full API key on creation (security best practice)
- Automatic detection of expired and revoked keys

### ✅ Email Service & Configuration (Database-backed)
- **Production-grade email service** with multiple providers
- Email configuration **stored in database** (not config files)
- Three provider types:
  - **None**: Development logging only
  - **SMTP**: Universal SMTP support via MailKit
  - **SendGrid**: Cloud email via SendGrid API
- Encrypted storage of credentials using Data Protection
- Beautiful HTML email templates with responsive design:
  - Password reset emails
  - Email confirmation/verification
  - Welcome emails
  - MFA enabled notifications
- Dynamic configuration via Admin Portal (`/email-config`)
- Support for multiple configurations per tenant
- Test email functionality from admin UI

### ✅ Email Verification
- **Required email verification** for new user registrations
- Automatic email confirmation token generation
- Time-limited, single-use confirmation tokens
- Resend confirmation email functionality
- Welcome email sent after successful verification
- Prevents login until email is confirmed
- Security-conscious design (no user enumeration)
- Dedicated UI pages:
  - `/confirm-email` - Email confirmation page
  - `/resend-confirmation` - Resend confirmation email page

### ✅ Role & Permission Management (RBAC)
- **Complete role-based access control** system
- Full CRUD operations for roles
- User-role assignment and management
- Query users by role
- Query roles by user
- Role descriptions and metadata
- User count tracking per role
- Admin Portal UI at `/roles`:
  - Create, edit, delete roles
  - View users in each role
  - Assign/remove users from roles
  - Beautiful Ant Design interface

### ✅ Swagger/OpenAPI Documentation
- **Comprehensive API documentation** for both services
- Identity Server API docs: `http://localhost:5101/swagger`
- Admin API docs: `http://localhost:5102/swagger`
- JWT Bearer and API Key authentication support
- Auto-generated from XML comments
- Interactive API testing interface
- Schema definitions and examples

### ✅ Internationalization (i18n)
- **Complete multi-language support** for Chinese (zh) and English (en)
- **Frontend (Login SPA)**:
  - react-i18next integration
  - Language switcher component
  - Automatic language detection from browser
  - Translation files: `frontend/login/src/i18n/locales/{zh|en}.json`
- **Frontend (Admin Portal)**:
  - react-i18next + Ant Design internationalization
  - Language switcher component
  - Translation files: `frontend/admin/src/i18n/locales/{zh|en}.json`
- **Backend API**:
  - .NET Resource Files (.resx) for error messages
  - LocalizationService for multi-language support
  - Automatic language detection from Accept-Language header
  - Resource files: `backend/OneID.Shared/Resources/ErrorMessages.{resx|zh.resx}`
- **Email Templates**:
  - Multi-language email templates (EmailTemplatesI18n)
  - Password reset, email confirmation, welcome, and MFA emails
  - Culture-aware template selection
- **Easy extensibility** to add new languages

### ✅ Unit & Integration Tests
- **Comprehensive test coverage** using xUnit
- Test projects:
  - `OneID.Identity.Tests` - Identity server tests
  - `OneID.AdminApi.Tests` - Admin API tests
- Test categories:
  - Email service tests (configuration, sending, encryption)
  - Role management tests (CRUD, assignments)
  - Account controller tests (registration, verification)
  - API integration tests (endpoints, authentication)
- In-memory database for test isolation
- Code coverage reporting with coverlet
- Automated test scripts:
  - `run-tests.sh` (Linux/macOS)
  - `run-tests.ps1` (Windows/PowerShell)

## API Endpoints

### Identity Server (OneID.Identity)
**Account:**
- `POST /api/account/login` - Local account login (username/password, returns MFA requirement)
- `POST /api/account/login-2fa` - Two-factor authentication verification (TOTP or recovery code)
- `POST /api/account/register` - User registration
- `POST /api/account/logout` - Logout
- `GET /api/account/me` - Get current user info
- `POST /api/account/forgot-password` - Request password reset email
- `POST /api/account/reset-password` - Reset password with token
- `POST /api/account/change-password` - Change password (authenticated users)
- `POST /api/account/confirm-email` - Confirm email with token
- `POST /api/account/resend-confirmation` - Resend email confirmation

**External Auth:**
- `GET /api/externalauth/providers` - List available external providers
- `POST /api/externalauth/challenge/{provider}` - Initiate external login
- `GET /api/externalauth/callback` - External login callback
- `GET /api/externalauth/logins` - Get user's external logins
- `POST /api/externalauth/link/{provider}` - Link external account
- `DELETE /api/externalauth/unlink/{provider}/{providerKey}` - Unlink external account

**MFA:**
- `GET /api/mfa/status` - Get MFA status for current user
- `POST /api/mfa/enable` - Start MFA setup (returns secret & QR code)
- `POST /api/mfa/verify` - Verify TOTP code and enable MFA
- `POST /api/mfa/disable` - Disable MFA
- `POST /api/mfa/validate-recovery-code` - Use recovery code
- `POST /api/mfa/regenerate-recovery-codes` - Generate new recovery codes
- `GET /api/mfa/qrcode` - Get QR code image for current secret

**API Keys:**
- `GET /api/apikeys` - List all API keys for current user
- `POST /api/apikeys` - Create new API key (returns key only once)
- `POST /api/apikeys/{id}/revoke` - Revoke API key

### Admin API (OneID.AdminApi)
**Clients:**
- `GET /api/clients` - List all OIDC clients
- `POST /api/clients` - Create new client
- `PUT /api/clients/{clientId}` - Update client
- `DELETE /api/clients/{clientId}` - Delete client
- `PUT /api/clients/{clientId}/scopes` - Update client scopes

**Users:**
- `GET /api/users` - List all users
- `GET /api/users/{userId}` - Get user by ID
- `GET /api/users/by-email/{email}` - Get user by email
- `POST /api/users` - Create new user
- `PUT /api/users/{userId}` - Update user
- `DELETE /api/users/{userId}` - Delete user
- `POST /api/users/{userId}/change-password` - Change user password
- `POST /api/users/{userId}/unlock` - Unlock user account

**Audit Logs:**
- `GET /api/auditlogs` - Query audit logs (with filters)
- `GET /api/auditlogs/categories` - Get available categories

**External Auth Providers:**
- `GET /api/externalauthproviders` - List all providers
- `POST /api/externalauthproviders` - Create new provider
- `PUT /api/externalauthproviders/{id}` - Update provider
- `DELETE /api/externalauthproviders/{id}` - Delete provider

**Roles:**
- `GET /api/roles` - List all roles
- `GET /api/roles/{id}` - Get role by ID
- `POST /api/roles` - Create new role
- `PUT /api/roles/{id}` - Update role
- `DELETE /api/roles/{id}` - Delete role
- `GET /api/roles/{id}/users` - Get users in role
- `POST /api/roles/{roleId}/users/{userId}` - Add user to role
- `DELETE /api/roles/{roleId}/users/{userId}` - Remove user from role
- `GET /api/roles/users/{userId}` - Get roles for a user

**Email Configuration:**
- `GET /api/emailconfiguration` - List all email configurations
- `GET /api/emailconfiguration/active` - Get active email configuration
- `GET /api/emailconfiguration/{id}` - Get email configuration by ID
- `POST /api/emailconfiguration` - Create email configuration
- `PUT /api/emailconfiguration/{id}` - Update email configuration
- `DELETE /api/emailconfiguration/{id}` - Delete email configuration
- `POST /api/emailconfiguration/{id}/test` - Test email configuration

## Configuration

### External Authentication Setup

Add to `appsettings.json` or environment variables:

```json
{
  "ExternalAuth": {
    "GitHub": {
      "Enabled": true,
      "ClientId": "your-github-client-id",
      "ClientSecret": "your-github-client-secret"
    },
    "Google": {
      "Enabled": true,
      "ClientId": "your-google-client-id.apps.googleusercontent.com",
      "ClientSecret": "your-google-client-secret"
    },
    "Gitee": {
      "Enabled": true,
      "ClientId": "your-gitee-client-id",
      "ClientSecret": "your-gitee-client-secret"
    }
  }
}
```

**Required NuGet packages** (already added):
- `Microsoft.AspNetCore.Authentication.Google`
- `Microsoft.AspNetCore.Authentication.GitHub`
- `AspNet.Security.OAuth.Gitee`
- `Otp.NET` - TOTP generation and validation
- `QRCoder` - QR code image generation

### Frontend Routes

**Login SPA** (`frontend/login`):
- `/` - Home page
- `/login` - Login page with username/password and external providers
- `/two-factor` - Two-factor authentication verification page
- `/register` - User registration page
- `/forgot-password` - Request password reset
- `/reset-password` - Reset password with token
- `/confirm-email` - Email confirmation page
- `/resend-confirmation` - Resend email confirmation
- `/signin` - OIDC signin redirect
- `/callback` - OIDC callback handler
- `/profile` - User profile and external account management
- `/mfa-setup` - MFA setup and management
- `/api-keys` - API key management

**Admin Portal** (`frontend/admin`):
- `/` - Clients management
- `/users` - Users management
- `/roles` - Role management
- `/external-auth` - External auth providers management
- `/email-config` - Email configuration management
- `/audit-logs` - Audit logs viewer with filtering

## Key Files

- `backend/OneID.Identity/Program.cs` - Application startup and OIDC configuration
- `backend/OneID.Identity/Controllers/ExternalAuthController.cs` - External login flow
- `backend/OneID.Identity/Controllers/AccountController.cs` - Local login, 2FA verification, registration, password reset
- `backend/OneID.Identity/Controllers/MfaController.cs` - MFA/TOTP management
- `backend/OneID.Identity/Controllers/ApiKeysController.cs` - API key management
- `backend/OneID.Identity/Services/MfaService.cs` - TOTP and recovery code generation
- `backend/OneID.Identity/Services/TotpTokenProvider.cs` - Custom ASP.NET Core Identity token provider
- `backend/OneID.Identity/Services/ApiKeyService.cs` - API key generation, validation, and management
- `backend/OneID.Shared/Infrastructure/EmailService.cs` - Email service implementations (SMTP, SendGrid, Logging)
- `backend/OneID.Shared/Infrastructure/DatabaseEmailService.cs` - Database-backed email service
- `backend/OneID.Shared/Infrastructure/EmailTemplates.cs` - HTML email templates
- `backend/OneID.Shared/Infrastructure/EmailTemplatesI18n.cs` - Multi-language email templates
- `backend/OneID.Shared/Infrastructure/EmailOptions.cs` - Email configuration options
- `backend/OneID.Shared/Infrastructure/EmailServiceExtensions.cs` - Email service registration
- `backend/OneID.Shared/Infrastructure/LocalizationService.cs` - Multi-language localization service
- `backend/OneID.Shared/Resources/ErrorMessages.resx` - English error messages
- `backend/OneID.Shared/Resources/ErrorMessages.zh.resx` - Chinese error messages
- `backend/OneID.Shared/Domain/EmailConfiguration.cs` - Email configuration entity
- `frontend/login/src/i18n/config.ts` - i18n configuration for Login SPA
- `frontend/login/src/i18n/locales/zh.json` - Chinese translations for Login SPA
- `frontend/login/src/i18n/locales/en.json` - English translations for Login SPA
- `frontend/login/src/components/LanguageSwitcher.tsx` - Language switcher for Login SPA
- `frontend/admin/src/i18n/config.ts` - i18n configuration for Admin Portal
- `frontend/admin/src/i18n/locales/zh.json` - Chinese translations for Admin Portal
- `frontend/admin/src/i18n/locales/en.json` - English translations for Admin Portal
- `frontend/admin/src/components/LanguageSwitcher.tsx` - Language switcher for Admin Portal
- `backend/OneID.Identity/Authentication/ApiKeyAuthenticationHandler.cs` - API key authentication handler
- `backend/OneID.Identity/Extensions/SwaggerExtensions.cs` - Swagger/OpenAPI configuration for Identity
- `backend/OneID.AdminApi/Extensions/SwaggerExtensions.cs` - Swagger/OpenAPI configuration for Admin API
- `backend/OneID.Shared/Domain/` - User, role, audit log, external auth provider, and API key models
- `backend/OneID.Shared/Infrastructure/AuditLogService.cs` - Audit logging service
- `backend/OneID.Shared/Data/AppDbContext.cs` - EF Core DbContext with entity configurations
- `backend/OneID.Identity/Seed/DatabaseSeeder.cs` - Initial data seeding
- `backend/OneID.Identity/Extensions/DynamicExternalAuthExtensions.cs` - Dynamic OAuth provider loading
- `backend/OneID.AdminApi/Services/UserQueryService.cs` - User query operations
- `backend/OneID.AdminApi/Services/UserCommandService.cs` - User command operations
- `backend/OneID.AdminApi/Services/ExternalAuthProviderQueryService.cs` - Provider queries
- `backend/OneID.AdminApi/Services/ExternalAuthProviderCommandService.cs` - Provider commands
- `backend/OneID.AdminApi/Controllers/UsersController.cs` - User management API
- `backend/OneID.AdminApi/Controllers/RolesController.cs` - Role management API
- `backend/OneID.AdminApi/Controllers/EmailConfigurationController.cs` - Email configuration API
- `backend/OneID.AdminApi/Controllers/AuditLogsController.cs` - Audit log query API
- `backend/OneID.AdminApi/Controllers/ExternalAuthProvidersController.cs` - Provider management API
- `backend/OneID.AdminApi/Services/RoleService.cs` - Role management business logic
- `frontend/login/src/pages/Login.tsx` - Login page with username/password and external providers
- `frontend/login/src/pages/TwoFactor.tsx` - Two-factor authentication verification page
- `frontend/login/src/pages/Register.tsx` - User registration page
- `frontend/login/src/pages/ForgotPassword.tsx` - Forgot password page
- `frontend/login/src/pages/ResetPassword.tsx` - Reset password page
- `frontend/login/src/pages/Profile.tsx` - User profile and account linking
- `frontend/login/src/pages/MfaSetup.tsx` - MFA setup and management UI
- `frontend/login/src/pages/ApiKeys.tsx` - API key management UI
- `frontend/login/src/pages/ConfirmEmail.tsx` - Email confirmation page
- `frontend/login/src/pages/ResendConfirmation.tsx` - Resend confirmation email page
- `frontend/admin/src/pages/UsersPage.tsx` - User management UI
- `frontend/admin/src/pages/RolesPage.tsx` - Role management UI
- `frontend/admin/src/pages/EmailConfigPage.tsx` - Email configuration UI
- `frontend/admin/src/pages/ExternalAuthProvidersPage.tsx` - External auth providers UI
- `frontend/admin/src/pages/AuditLogsPage.tsx` - Audit logs viewer
- `backend/tests/OneID.Identity.Tests/` - Identity server unit and integration tests
- `backend/tests/OneID.AdminApi.Tests/` - Admin API unit and integration tests
- `backend/tests/run-tests.sh` - Test runner script (Linux/macOS)
- `backend/tests/run-tests.ps1` - Test runner script (Windows)
- `Dockerfile` - Multi-stage build (backend + frontend)
- `docker-compose.yml` - Production deployment
- `docker-compose.dev.yml` - Development environment
