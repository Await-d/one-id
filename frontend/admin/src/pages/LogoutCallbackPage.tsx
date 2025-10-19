import { useEffect, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { Spin } from "antd";
import { useTranslation } from "react-i18next";
import { userManager } from "../lib/oidcConfig";

/**
 * 登出回调页面
 * 处理Identity Server登出后的重定向
 */
export function LogoutCallbackPage() {
    const { t } = useTranslation();
    const navigate = useNavigate();
    const hasProcessed = useRef(false);

    useEffect(() => {
        // 确保只处理一次
        if (hasProcessed.current) return;
        hasProcessed.current = true;

        const handleLogoutCallback = async () => {
            try {
                // 设置标记，防止自动登录
                sessionStorage.setItem("isLoggingOut", "true");

                // 处理登出回调
                await userManager.signoutRedirectCallback();
                console.log("Logout callback processed successfully");

                // 清除所有本地用户数据
                await userManager.removeUser();

                // 清除登出标记
                sessionStorage.removeItem("isLoggingOut");

                // 使用 navigate 而不是 window.location.href
                navigate("/logged-out", { replace: true });
            } catch (error) {
                console.error("Logout callback failed:", error);

                // 即使失败，也清除本地数据并重定向
                await userManager.removeUser();
                sessionStorage.removeItem("isLoggingOut");
                navigate("/logged-out", { replace: true });
            }
        };

        handleLogoutCallback();
    }, [navigate]);

    return (
        <div
            style={{
                display: "flex",
                justifyContent: "center",
                alignItems: "center",
                height: "100vh",
                flexDirection: "column",
                gap: "20px",
            }}
        >
            <Spin size="large" />
            <p>{t("auth.loggingOut")}</p>
        </div>
    );
}


