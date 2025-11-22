import { useEffect, useState } from "react";
import { Outlet, useNavigate, useLocation, Link } from "react-router-dom";
import { Layout, Spin, Breadcrumb, theme } from "antd";
import { HomeOutlined } from "@ant-design/icons";
import { useAuth } from "../contexts/AuthContext";
import { useTranslation } from "react-i18next";
import { AppHeader } from "./AppHeader";
import { AppSidebar } from "./AppSidebar";

const { Content } = Layout;

/**
 * 受保护的布局组件
 * 需要认证才能访问
 */
export function ProtectedLayout() {
    const { isLoading, isAuthenticated } = useAuth();
    const navigate = useNavigate();
    const location = useLocation();
    const { t } = useTranslation();
    const { token } = theme.useToken();
    const [collapsed, setCollapsed] = useState(false);

    useEffect(() => {
        // 如果未加载完成，等待
        if (isLoading) return;

        // 检查是否正在登出过程中
        const isLoggingOut = sessionStorage.getItem("isLoggingOut") === "true";
        if (isLoggingOut) {
            console.log("Logout in progress, skipping auto-login");
            return;
        }

        // 如果未认证，重定向到登录页
        if (!isAuthenticated) {
            navigate("/login");
        }
    }, [isLoading, isAuthenticated, navigate]);

    // 加载中显示loading
    if (isLoading) {
        return (
            <div
                style={{
                    display: "flex",
                    justifyContent: "center",
                    alignItems: "center",
                    height: "100vh",
                    background: token.colorBgLayout,
                }}
            >
                <Spin size="large" tip={t("common.loading")}>
                    <div style={{ height: 100 }} />
                </Spin>
            </div>
        );
    }

    // 检查是否正在登出过程中
    const isLoggingOut = sessionStorage.getItem("isLoggingOut") === "true";
    if (isLoggingOut) {
        return (
            <div
                style={{
                    display: "flex",
                    justifyContent: "center",
                    alignItems: "center",
                    height: "100vh",
                    background: token.colorBgLayout,
                }}
            >
                <Spin size="large" tip={t("logout.processing")}>
                    <div style={{ height: 100 }} />
                </Spin>
            </div>
        );
    }

    // 未认证显示空白（会重定向）
    if (!isAuthenticated) {
        return null;
    }

    // 根据路径生成面包屑
    const getBreadcrumbs = () => {
        const pathSnippets = location.pathname.split("/").filter((i) => i);

        const breadcrumbItems = [
            {
                title: (
                    <Link to="/">
                        <HomeOutlined style={{ marginRight: "4px" }} />
                        {t("breadcrumb.home")}
                    </Link>
                ),
            },
        ];

        // 路径映射
        const pathMap: Record<string, string> = {
            clients: t("menu.clients"),
            users: t("menu.users"),
            "user-import": t("userImport.title"),
            roles: t("menu.roles"),
            scopes: t("menu.scopes"),
            "external-auth": t("menu.externalAuth"),
            "email-config": t("menu.emailConfig"),
            "email-templates": t("emailTemplates.title"),
            "audit-logs": t("menu.auditLogs"),
            "system-config": t("menu.systemConfig"),
            "system-settings": t("menu.systemSettings"),
            "rate-limit-settings": t("rateLimitSettings.title"),
            "login-policies": t("loginPolicies.title"),
            "user-behavior": t("userBehavior.title"),
            "anomalous-logins": t("anomalousLogins.title"),
            "notification-settings": t("notificationSettings.title"),
            "webhooks": t("webhooks.title"),
            "signing-keys": t("menu.signingKeys"),
            "security-rules": t("menu.securityRules"),
            "configuration": t("configuration.title"),
            "devices": t("userDevices.title"),
        };

        pathSnippets.forEach((snippet, index) => {
            const url = `/${pathSnippets.slice(0, index + 1).join("/")}`;
            const isLast = index === pathSnippets.length - 1;

            breadcrumbItems.push({
                title: isLast ? (
                    <span>{pathMap[snippet] || snippet}</span>
                ) : (
                    <Link to={url}>{pathMap[snippet] || snippet}</Link>
                ),
            });
        });

        return breadcrumbItems;
    };

    return (
        <Layout style={{ minHeight: "100vh" }}>
            <AppSidebar collapsed={collapsed} />
            <Layout>
                <AppHeader collapsed={collapsed} onToggleCollapse={() => setCollapsed(!collapsed)} />
                <Content
                    style={{
                        margin: "16px",
                        minHeight: "calc(100vh - 64px - 32px)",
                    }}
                >
                    {/* 面包屑 */}
                    {location.pathname !== "/" && (
                        <Breadcrumb
                            items={getBreadcrumbs()}
                            style={{
                                marginBottom: "16px",
                                padding: "12px 16px",
                                background: token.colorBgContainer,
                                borderRadius: "8px",
                                border: `1px solid ${token.colorBorderSecondary}`,
                            }}
                        />
                    )}

                    {/* 页面内容 */}
                    <div
                        style={{
                            padding: "24px",
                            background: token.colorBgContainer,
                            borderRadius: "8px",
                            minHeight: "calc(100vh - 64px - 32px - 48px)",
                            border: `1px solid ${token.colorBorderSecondary}`,
                        }}
                    >
                        <Outlet />
                    </div>
                </Content>
            </Layout>
        </Layout>
    );
}
