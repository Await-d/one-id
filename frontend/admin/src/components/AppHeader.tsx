import { Layout, Dropdown, Badge, Button, Space, Avatar, theme } from "antd";
import {
    UserOutlined,
    BellOutlined,
    SettingOutlined,
    LogoutOutlined,
    MenuFoldOutlined,
    MenuUnfoldOutlined,
} from "@ant-design/icons";
import { useAuth } from "../contexts/AuthContext";
import { useTranslation } from "react-i18next";
import { LanguageSwitcher } from "./LanguageSwitcher";

const { Header } = Layout;

interface AppHeaderProps {
    collapsed: boolean;
    onToggleCollapse: () => void;
}

export function AppHeader({ collapsed, onToggleCollapse }: AppHeaderProps) {
    const { user, logout } = useAuth();
    const { t } = useTranslation();
    const { token } = theme.useToken();

    const userMenuItems = [
        {
            key: "user-info",
            label: (
                <div style={{ padding: "8px 0" }}>
                    <div style={{ fontWeight: 600, fontSize: "14px", marginBottom: "4px" }}>
                        {user?.profile?.name || user?.profile?.email || t("common.user")}
                    </div>
                    <div style={{ fontSize: "12px", color: token.colorTextSecondary }}>
                        {user?.profile?.email}
                    </div>
                </div>
            ),
            disabled: true,
        },
        {
            type: "divider" as const,
        },
        {
            key: "profile",
            icon: <UserOutlined />,
            label: t("header.profile"),
        },
        {
            key: "settings",
            icon: <SettingOutlined />,
            label: t("header.settings"),
        },
        {
            type: "divider" as const,
        },
        {
            key: "logout",
            icon: <LogoutOutlined />,
            label: t("common.logout"),
            onClick: logout,
            danger: true,
        },
    ];

    const notificationItems = [
        {
            key: "1",
            label: (
                <div>
                    <div style={{ fontWeight: 500 }}>{t("header.noNotifications")}</div>
                    <div style={{ fontSize: "12px", color: token.colorTextSecondary, marginTop: "4px" }}>
                        {t("header.allCaughtUp")}
                    </div>
                </div>
            ),
        },
    ];

    return (
        <Header
            style={{
                padding: "0 24px",
                background: token.colorBgContainer,
                borderBottom: `1px solid ${token.colorBorderSecondary}`,
                display: "flex",
                alignItems: "center",
                justifyContent: "space-between",
                height: "64px",
                position: "sticky",
                top: 0,
                zIndex: 999,
                boxShadow: "0 1px 4px rgba(0,21,41,.08)",
            }}
        >
            <Space size="middle">
                <Button
                    type="text"
                    icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
                    onClick={onToggleCollapse}
                    style={{
                        fontSize: "16px",
                        width: "40px",
                        height: "40px",
                    }}
                />
                <div style={{
                    fontSize: "18px",
                    fontWeight: 600,
                    color: token.colorText,
                    display: "flex",
                    alignItems: "center",
                    gap: "12px"
                }}>
                    <span style={{
                        background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
                        WebkitBackgroundClip: "text",
                        WebkitTextFillColor: "transparent",
                        backgroundClip: "text",
                    }}>
                        OneID
                    </span>
                    <span style={{ color: token.colorTextSecondary, fontSize: "14px", fontWeight: 400 }}>
                        {t("header.adminPortal")}
                    </span>
                </div>
            </Space>

            <Space size="middle">
                <LanguageSwitcher />

                <Dropdown
                    menu={{ items: notificationItems }}
                    placement="bottomRight"
                    trigger={["click"]}
                >
                    <Badge count={0} showZero={false}>
                        <Button
                            type="text"
                            icon={<BellOutlined style={{ fontSize: "16px" }} />}
                            style={{
                                width: "40px",
                                height: "40px",
                            }}
                        />
                    </Badge>
                </Dropdown>

                <Dropdown
                    menu={{ items: userMenuItems }}
                    placement="bottomRight"
                    trigger={["click"]}
                >
                    <Button
                        type="text"
                        style={{
                            height: "40px",
                            padding: "4px 12px",
                            display: "flex",
                            alignItems: "center",
                            gap: "8px",
                        }}
                    >
                        <Avatar
                            size="small"
                            icon={<UserOutlined />}
                            style={{
                                backgroundColor: token.colorPrimary,
                            }}
                        />
                        <span style={{
                            maxWidth: "120px",
                            overflow: "hidden",
                            textOverflow: "ellipsis",
                            whiteSpace: "nowrap"
                        }}>
                            {user?.profile?.name || user?.profile?.email?.split("@")[0] || t("common.user")}
                        </span>
                    </Button>
                </Dropdown>
            </Space>
        </Header>
    );
}

