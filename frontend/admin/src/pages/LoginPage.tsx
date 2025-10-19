import { useEffect, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { Spin } from "antd";
import { useTranslation } from "react-i18next";
import { useAuth } from "../contexts/AuthContext";

/**
 * 登录页面
 * 自动重定向到OIDC授权端点
 */
export function LoginPage() {
    const { t } = useTranslation();
    const { login } = useAuth();
    const navigate = useNavigate();
    const hasTriggeredLogin = useRef(false);

    useEffect(() => {
        // 检查是否正在登出过程中
        const isLoggingOut = sessionStorage.getItem("isLoggingOut") === "true";
        if (isLoggingOut) {
            console.log("Logout in progress, redirecting to logged-out page");
            navigate("/logged-out", { replace: true });
            return;
        }

        // 确保只触发一次OIDC登录流程
        if (!hasTriggeredLogin.current) {
            hasTriggeredLogin.current = true;
            login();
        }
    }, [login, navigate]);

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
            <p>{t("auth.redirectingToLogin")}</p>
        </div>
    );
}

