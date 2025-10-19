import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { Spin, message } from "antd";
import { useTranslation } from "react-i18next";
import { handleCallback } from "../lib/oidcConfig";

/**
 * OIDC回调页面
 * 处理授权码并获取token
 */
export function CallbackPage() {
    const { t } = useTranslation();
    const navigate = useNavigate();

    useEffect(() => {
        const processCallback = async () => {
            try {
                // 处理OIDC回调
                await handleCallback();

                // 成功后重定向到首页
                message.success(t("auth.loginSuccess"));
                navigate("/");
            } catch (error) {
                console.error("Login callback failed:", error);
                message.error(t("auth.loginFailed"));
                navigate("/login");
            }
        };

        processCallback();
    }, [navigate, t]);

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
            <p>{t("auth.processingLogin")}</p>
        </div>
    );
}

