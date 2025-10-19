import { Result, Button } from "antd";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";

/**
 * 退出登录成功页面
 * 显示退出成功信息，用户需要手动点击按钮才能重新登录
 */
export function LoggedOutPage() {
    const navigate = useNavigate();
    const { t } = useTranslation();

    const handleLogin = () => {
        navigate("/login");
    };

    return (
        <div
            style={{
                display: "flex",
                justifyContent: "center",
                alignItems: "center",
                height: "100vh",
                background: "#f0f2f5",
            }}
        >
            <Result
                status="success"
                title={t("logout.success")}
                subTitle={t("logout.message")}
                extra={[
                    <Button type="primary" key="login" onClick={handleLogin}>
                        {t("logout.relogin")}
                    </Button>,
                ]}
            />
        </div>
    );
}

