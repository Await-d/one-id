import { UserManager, WebStorageStateStore } from "oidc-client-ts";

/**
 * OIDC配置 - Admin Portal
 * 使用Authorization Code + PKCE流程
 */

/**
 * 智能获取OIDC Authority地址
 * 开发环境：http://localhost:5101
 * 生产环境（9444端口）：使用9443端口的Identity Server
 * 其他生产环境：使用当前origin
 */
const getAuthority = (): string => {
    // 优先使用环境变量
    if (import.meta.env.VITE_OIDC_AUTHORITY && import.meta.env.VITE_OIDC_AUTHORITY.trim()) {
        return import.meta.env.VITE_OIDC_AUTHORITY;
    }

    // 开发环境
    if (import.meta.env.DEV) {
        return "http://localhost:5101";
    }

    // 生产环境 - 智能检测
    const currentOrigin = window.location.origin;
    const currentUrl = new URL(currentOrigin);

    // 如果是9444端口（Admin Portal），则Identity Server在9443端口
    if (currentUrl.port === "9444") {
        return `${currentUrl.protocol}//${currentUrl.hostname}:9443`;
    }

    // 其他情况使用当前origin（同域部署）
    return currentOrigin;
};

// Authority (OIDC服务器地址)
const authority = getAuthority();

// Client ID
const clientId = import.meta.env.VITE_OIDC_CLIENT_ID ?? "spa.admin";

// Redirect URIs - 直接使用运行时值
const redirectUri = import.meta.env.VITE_OIDC_REDIRECT_URI || `${window.location.origin}/admin/callback`;
const postLogoutRedirectUri = import.meta.env.VITE_OIDC_LOGOUT_REDIRECT_URI || `${window.location.origin}/admin/logout-callback`;

// Scopes
const scope = import.meta.env.VITE_OIDC_SCOPE ?? "openid profile email offline_access admin_api";

/**
 * User Manager 实例
 */
export const userManager = new UserManager({
    authority,
    client_id: clientId,
    redirect_uri: redirectUri,
    post_logout_redirect_uri: postLogoutRedirectUri,
    response_type: "code", // Authorization Code Flow
    scope,
    loadUserInfo: true,
    automaticSilentRenew: true, // 自动刷新token
    userStore: new WebStorageStateStore({ store: window.localStorage }),
});

/**
 * 获取当前用户
 */
export const getUser = () => userManager.getUser();

/**
 * 登录
 */
export const login = () => userManager.signinRedirect();

/**
 * 登出
 */
export const logout = () => userManager.signoutRedirect();

/**
 * 处理回调
 */
export const handleCallback = () => userManager.signinRedirectCallback();

/**
 * 获取Access Token
 */
export const getAccessToken = async (): Promise<string | null> => {
    const user = await userManager.getUser();
    return user?.access_token ?? null;
};

