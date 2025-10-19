import React, { createContext, useContext, useEffect, useState, useCallback, ReactNode } from "react";
import { User } from "oidc-client-ts";
import { userManager, getUser } from "../lib/oidcConfig";

interface AuthContextType {
    user: User | null;
    isLoading: boolean;
    isAuthenticated: boolean;
    login: () => void;
    logout: () => void;
    getAccessToken: () => Promise<string | null>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

interface AuthProviderProps {
    children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
    const [user, setUser] = useState<User | null>(null);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        // 初始化：尝试从存储中加载用户
        const initAuth = async () => {
            try {
                const storedUser = await getUser();
                setUser(storedUser);
            } catch (error) {
                console.error("Failed to load user:", error);
            } finally {
                setIsLoading(false);
            }
        };

        initAuth();

        // 监听用户状态变化
        const handleUserLoaded = (loadedUser: User) => {
            setUser(loadedUser);
        };

        const handleUserUnloaded = () => {
            setUser(null);
        };

        const handleAccessTokenExpiring = () => {
            console.log("Access token expiring, attempting silent renew...");
        };

        const handleAccessTokenExpired = () => {
            console.log("Access token expired");
            setUser(null);
        };

        userManager.events.addUserLoaded(handleUserLoaded);
        userManager.events.addUserUnloaded(handleUserUnloaded);
        userManager.events.addAccessTokenExpiring(handleAccessTokenExpiring);
        userManager.events.addAccessTokenExpired(handleAccessTokenExpired);

        return () => {
            userManager.events.removeUserLoaded(handleUserLoaded);
            userManager.events.removeUserUnloaded(handleUserUnloaded);
            userManager.events.removeAccessTokenExpiring(handleAccessTokenExpiring);
            userManager.events.removeAccessTokenExpired(handleAccessTokenExpired);
        };
    }, []);

    const login = useCallback(() => {
        userManager.signinRedirect();
    }, []);

    const logout = useCallback(async () => {
        try {
            // 设置登出标记，防止自动触发登录
            sessionStorage.setItem("isLoggingOut", "true");

            // 获取当前用户的id_token，用于登出时的身份验证
            const currentUser = await userManager.getUser();
            if (currentUser) {
                // 传递id_token_hint参数，确保Identity Server正确识别并清除会话
                await userManager.signoutRedirect({
                    id_token_hint: currentUser.id_token,
                });
            } else {
                // 如果没有用户信息，先清除本地存储
                await userManager.removeUser();
                sessionStorage.removeItem("isLoggingOut");
                window.location.href = "/logged-out";
            }
        } catch (error) {
            console.error("Logout failed:", error);
            // 即使登出失败，也清除本地用户信息
            await userManager.removeUser();
            sessionStorage.removeItem("isLoggingOut");
            window.location.href = "/logged-out";
        }
    }, []);

    const getAccessToken = useCallback(async (): Promise<string | null> => {
        const currentUser = await userManager.getUser();
        return currentUser?.access_token ?? null;
    }, []);

    const value: AuthContextType = {
        user,
        isLoading,
        isAuthenticated: !!user && !user.expired,
        login,
        logout,
        getAccessToken,
    };

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

/**
 * Hook to access auth context
 */
export const useAuth = (): AuthContextType => {
    const context = useContext(AuthContext);
    if (!context) {
        throw new Error("useAuth must be used within an AuthProvider");
    }
    return context;
};

