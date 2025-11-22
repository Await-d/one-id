import { createBrowserRouter } from "react-router-dom";
import { ProtectedLayout } from "./components/ProtectedLayout";
import { LoginPage } from "./pages/LoginPage";
import { CallbackPage } from "./pages/CallbackPage";
import { LogoutCallbackPage } from "./pages/LogoutCallbackPage";
import { LoggedOutPage } from "./pages/LoggedOutPage";
import { ClientsPage } from "./pages/ClientsPage";
import { UsersPage } from "./pages/UsersPage";
import UserImportPage from "./pages/UserImportPage";
import DashboardPage from "./pages/DashboardPage";
import { ExternalAuthProvidersPage } from "./pages/ExternalAuthProvidersPage";
import AuditLogsPage from "./pages/AuditLogsPage";
import EmailConfigPage from "./pages/EmailConfigPage";
import EmailTemplatesPage from "./pages/EmailTemplatesPage";
import RolesPage from "./pages/RolesPage";
import ScopesPage from "./pages/ScopesPage";
import UserSessionsPage from "./pages/UserSessionsPage";
import SigningKeysPage from "./pages/SigningKeysPage";
import SecurityRulesPage from "./pages/SecurityRulesPage";
import GdprPage from "./pages/GdprPage";
import TenantsPage from "./pages/TenantsPage";
import SystemConfigPage from "./pages/SystemConfigPage";
import SystemSettingsPage from "./pages/SystemSettingsPage";
import LoginPoliciesPage from "./pages/LoginPoliciesPage";
import UserBehaviorPage from "./pages/UserBehaviorPage";
import AnomalousLoginsPage from "./pages/AnomalousLoginsPage";
import UserDevicesPage from "./pages/UserDevicesPage";
import NotificationSettingsPage from "./pages/NotificationSettingsPage";
import WebhooksPage from "./pages/WebhooksPage";
import RateLimitSettingsPage from "./pages/RateLimitSettingsPage";
import ConfigurationPage from "./pages/ConfigurationPage";

export const router = createBrowserRouter([
  {
    path: "/login",
    element: <LoginPage />,
  },
  {
    path: "/callback",
    element: <CallbackPage />,
  },
  {
    path: "/logout-callback",
    element: <LogoutCallbackPage />,
  },
  {
    path: "/logged-out",
    element: <LoggedOutPage />,
  },
  {
    path: "/",
    element: <ProtectedLayout />,
    children: [
      {
        index: true,
        element: <DashboardPage />,
      },
      {
        path: "clients",
        element: <ClientsPage />,
      },
      {
        path: "users",
        element: <UsersPage />,
      },
      {
        path: "user-import",
        element: <UserImportPage />,
      },
      {
        path: "external-auth",
        element: <ExternalAuthProvidersPage />,
      },
      {
        path: "audit-logs",
        element: <AuditLogsPage />,
      },
      {
        path: "system-config",
        element: <SystemConfigPage />,
      },
      {
        path: "email-config",
        element: <EmailConfigPage />,
      },
      {
        path: "email-templates",
        element: <EmailTemplatesPage />,
      },
      {
        path: "roles",
        element: <RolesPage />,
      },
      {
        path: "scopes",
        element: <ScopesPage />,
      },
      {
        path: "signing-keys",
        element: <SigningKeysPage />,
      },
      {
        path: "security-rules",
        element: <SecurityRulesPage />,
      },
      {
        path: "gdpr",
        element: <GdprPage />,
      },
      {
        path: "tenants",
        element: <TenantsPage />,
      },
      {
        path: "system-settings",
        element: <SystemSettingsPage />,
      },
      {
        path: "rate-limit-settings",
        element: <RateLimitSettingsPage />,
      },
      {
        path: "login-policies",
        element: <LoginPoliciesPage />,
      },
      {
        path: "user-behavior",
        element: <UserBehaviorPage />,
      },
      {
        path: "anomalous-logins",
        element: <AnomalousLoginsPage />,
      },
      {
        path: "notification-settings",
        element: <NotificationSettingsPage />,
      },
      {
        path: "webhooks",
        element: <WebhooksPage />,
      },
      {
        path: "users/:userId/sessions",
        element: <UserSessionsPage />,
      },
      {
        path: "users/:userId/devices",
        element: <UserDevicesPage />,
      },
      {
        path: "configuration",
        element: <ConfigurationPage />,
      },
    ],
  },
], {
  basename: '/admin', // 设置基础路径,因为应用部署在 /admin 路径下
});
