import { useState } from "react";
import { Layout, Menu, Input, theme } from "antd";
import {
    ApiOutlined,
    UserOutlined,
    SafetyCertificateOutlined,
    GlobalOutlined,
    MailOutlined,
    AuditOutlined,
    SearchOutlined,
    AppstoreOutlined,
    SettingOutlined,
    SecurityScanOutlined,
    KeyOutlined,
    CloudServerOutlined,
    SafetyOutlined,
    DashboardOutlined,
    LockOutlined,
    BellOutlined,
} from "@ant-design/icons";
import { useNavigate, useLocation } from "react-router-dom";
import { useTranslation } from "react-i18next";
import type { MenuProps } from "antd";

const { Sider } = Layout;

interface AppSidebarProps {
    collapsed: boolean;
}

type MenuItem = Required<MenuProps>["items"][number];

export function AppSidebar({ collapsed }: AppSidebarProps) {
    const navigate = useNavigate();
    const location = useLocation();
    const { t } = useTranslation();
    const { token } = theme.useToken();
    const [searchText, setSearchText] = useState("");

    const allMenuItems: MenuItem[] = [
        {
            key: "management",
            label: t("menu.management"),
            type: "group",
            children: [
                {
                    key: "/",
                    icon: <DashboardOutlined />,
                    label: t("dashboard.title"),
                    onClick: () => navigate("/"),
                },
                {
                    key: "/clients",
                    icon: <ApiOutlined />,
                    label: t("menu.clients"),
                    onClick: () => navigate("/clients"),
                },
                {
                    key: "/users",
                    icon: <UserOutlined />,
                    label: t("menu.users"),
                    onClick: () => navigate("/users"),
                },
                {
                    key: "/user-import",
                    icon: <UserOutlined />,
                    label: t("userImport.title"),
                    onClick: () => navigate("/user-import"),
                },
                {
                    key: "/roles",
                    icon: <SafetyCertificateOutlined />,
                    label: t("menu.roles"),
                    onClick: () => navigate("/roles"),
                },
                {
                    key: "/scopes",
                    icon: <KeyOutlined />,
                    label: t("menu.scopes"),
                    onClick: () => navigate("/scopes"),
                },
            ],
        },
        {
            key: "auth",
            label: t("menu.authentication"),
            type: "group",
            children: [
                {
                    key: "/external-auth",
                    icon: <GlobalOutlined />,
                    label: t("menu.externalAuth"),
                    onClick: () => navigate("/external-auth"),
                },
                {
                    key: "/signing-keys",
                    icon: <SafetyCertificateOutlined />,
                    label: t("menu.signingKeys"),
                    onClick: () => navigate("/signing-keys"),
                },
                {
                    key: "/security-rules",
                    icon: <SecurityScanOutlined />,
                    label: t("menu.securityRules"),
                    onClick: () => navigate("/security-rules"),
                },
                {
                    key: "/login-policies",
                    icon: <SafetyOutlined />,
                    label: t("loginPolicies.title"),
                    onClick: () => navigate("/login-policies"),
                },
            ],
        },
        {
            key: "system",
            label: t("menu.systemSettings"),
            type: "group",
            children: [
                {
                    key: "/tenants",
                    icon: <CloudServerOutlined />,
                    label: t("menu.tenants"),
                    onClick: () => navigate("/tenants"),
                },
                {
                    key: "/gdpr",
                    icon: <SafetyOutlined />,
                    label: t("menu.gdpr"),
                    onClick: () => navigate("/gdpr"),
                },
                {
                    key: "/email-config",
                    icon: <MailOutlined />,
                    label: t("menu.emailConfig"),
                    onClick: () => navigate("/email-config"),
                },
                {
                    key: "/email-templates",
                    icon: <MailOutlined />,
                    label: t("emailTemplates.title"),
                    onClick: () => navigate("/email-templates"),
                },
                {
                    key: "/audit-logs",
                    icon: <AuditOutlined />,
                    label: t("menu.auditLogs"),
                    onClick: () => navigate("/audit-logs"),
                },
                {
                    key: "/user-behavior",
                    icon: <GlobalOutlined />,
                    label: t("userBehavior.title"),
                    onClick: () => navigate("/user-behavior"),
                },
                {
                    key: "/anomalous-logins",
                    icon: <SafetyOutlined />,
                    label: t("anomalousLogins.title"),
                    onClick: () => navigate("/anomalous-logins"),
                },
                {
                    key: "/notification-settings",
                    icon: <BellOutlined />,
                    label: t("notificationSettings.title"),
                    onClick: () => navigate("/notification-settings"),
                },
                {
                    key: "/webhooks",
                    icon: <ApiOutlined />,
                    label: t("webhooks.title"),
                    onClick: () => navigate("/webhooks"),
                },
                {
                    key: "/login-policies",
                    icon: <LockOutlined />,
                    label: t("loginPolicies.title"),
                    onClick: () => navigate("/login-policies"),
                },
                {
                    key: "/client-validation",
                    icon: <SecurityScanOutlined />,
                    label: t("menu.clientValidation"),
                    onClick: () => navigate("/client-validation"),
                },
                {
                    key: "/cors-settings",
                    icon: <SecurityScanOutlined />,
                    label: t("menu.corsSettings"),
                    onClick: () => navigate("/cors-settings"),
                },
                {
                    key: "/system-settings",
                    icon: <SettingOutlined />,
                    label: t("menu.systemSettings"),
                    onClick: () => navigate("/system-settings"),
                },
            ],
        },
    ];

    // 根据搜索文本过滤菜单项
    const filterMenuItems = (items: MenuItem[], search: string): MenuItem[] => {
        if (!search) return items;

        const searchLower = search.toLowerCase();
        return items
            .map((item) => {
                if (!item || typeof item !== "object" || !("key" in item)) return null;

                // 如果是分组
                if ("children" in item && Array.isArray(item.children)) {
                    const filteredChildren = item.children.filter((child) => {
                        if (!child || typeof child !== "object" || !("label" in child)) return false;
                        const label = String(child.label || "").toLowerCase();
                        return label.includes(searchLower);
                    });

                    if (filteredChildren.length > 0) {
                        return { ...item, children: filteredChildren };
                    }
                    return null;
                }

                // 如果是普通菜单项
                if ("label" in item) {
                    const label = String(item.label || "").toLowerCase();
                    return label.includes(searchLower) ? item : null;
                }

                return null;
            })
            .filter((item): item is NonNullable<typeof item> => item !== null) as MenuItem[];
    };

    const menuItems = filterMenuItems(allMenuItems, searchText);

    // 获取当前选中的菜单项
    const getSelectedKey = () => {
        const path = location.pathname;
        if (path === "/") return ["/"];

        // 查找匹配的菜单项
        for (const group of allMenuItems) {
            if (group && typeof group === "object" && "children" in group && Array.isArray(group.children)) {
                for (const item of group.children) {
                    if (item && typeof item === "object" && "key" in item) {
                        const key = String(item.key);
                        if (key !== "/" && path.startsWith(key)) {
                            return [key];
                        }
                    }
                }
            }
        }

        return [path];
    };

    return (
        <Sider
            trigger={null}
            collapsible
            collapsed={collapsed}
            width={240}
            collapsedWidth={80}
            style={{
                overflow: "auto",
                height: "100vh",
                position: "sticky",
                top: 0,
                left: 0,
                background: token.colorBgContainer,
                borderRight: `1px solid ${token.colorBorderSecondary}`,
            }}
        >
            {/* Logo */}
            <div
                style={{
                    height: "64px",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    borderBottom: `1px solid ${token.colorBorderSecondary}`,
                    padding: collapsed ? "0 24px" : "0 20px",
                    transition: "all 0.2s",
                }}
            >
                {collapsed ? (
                    <AppstoreOutlined style={{ fontSize: "24px", color: token.colorPrimary }} />
                ) : (
                    <div style={{
                        display: "flex",
                        alignItems: "center",
                        gap: "12px",
                        width: "100%"
                    }}>
                        <AppstoreOutlined style={{ fontSize: "24px", color: token.colorPrimary }} />
                        <span style={{
                            fontSize: "16px",
                            fontWeight: 600,
                            background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
                            WebkitBackgroundClip: "text",
                            WebkitTextFillColor: "transparent",
                            backgroundClip: "text",
                        }}>
                            OneID
                        </span>
                    </div>
                )}
            </div>

            {/* 搜索框 */}
            {!collapsed && (
                <div style={{ padding: "16px 16px 8px" }}>
                    <Input
                        placeholder={t("menu.searchMenu")}
                        prefix={<SearchOutlined />}
                        value={searchText}
                        onChange={(e) => setSearchText(e.target.value)}
                        allowClear
                        style={{ borderRadius: "6px" }}
                    />
                </div>
            )}

            {/* 菜单 */}
            <Menu
                mode="inline"
                selectedKeys={getSelectedKey()}
                items={menuItems}
                style={{
                    borderRight: 0,
                    padding: collapsed ? "8px 8px" : "8px 12px",
                }}
            />
        </Sider>
    );
}
