/**
 * API Client - 统一的API请求帮助类
 * 自动处理开发环境和生产环境的API地址
 * 自动添加JWT Bearer Token认证
 */

import { userManager } from "./oidcConfig";

// 获取API基础URL
// 开发环境：使用localhost:5102
// 生产环境：智能检测 Admin API 地址
const getApiBaseUrl = (): string => {
    const envApiUrl = import.meta.env.VITE_API_URL;

    // 如果环境变量中配置了API地址，使用配置的地址
    if (envApiUrl && envApiUrl.trim()) {
        return envApiUrl;
    }

    // 开发环境使用localhost:5102
    if (import.meta.env.DEV) {
        return "http://localhost:5102";
    }

    // 生产环境智能检测
    const currentUrl = new URL(window.location.origin);

    // 如果当前是 10230 端口（Identity Server），Admin API 在 10231 端口
    if (currentUrl.port === "10230") {
        return `${currentUrl.protocol}//${currentUrl.hostname}:10231`;
    }

    // 如果通过域名访问（如 https://auth.awitk.cn），Admin API 通过 /api 路径访问
    // 注意：Admin Portal 在 /admin 路径，但 Admin API 在 /api 路径
    return `${window.location.origin}/api`;
};

export const API_BASE_URL = getApiBaseUrl();

/**
 * 获取Access Token
 */
const getAccessToken = async (): Promise<string | null> => {
    try {
        const user = await userManager.getUser();
        return user?.access_token ?? null;
    } catch (error) {
        console.error("Failed to get access token:", error);
        return null;
    }
};

/**
 * 通用请求方法
 */
interface RequestOptions extends RequestInit {
    params?: Record<string, string | number | boolean>;
}

/**
 * 构建完整的URL（包含query参数）
 */
const buildUrl = (endpoint: string, params?: Record<string, string | number | boolean>): string => {
    // 确保endpoint以/开头
    const path = endpoint.startsWith("/") ? endpoint : `/${endpoint}`;
    const url = new URL(path, API_BASE_URL);

    if (params) {
        Object.entries(params).forEach(([key, value]) => {
            url.searchParams.append(key, String(value));
        });
    }

    return url.toString();
};

/**
 * 通用fetch包装器
 */
const request = async <T = any>(
    endpoint: string,
    options: RequestOptions = {}
): Promise<T> => {
    const { params, ...fetchOptions } = options;

    const url = buildUrl(endpoint, params);

    // 获取Access Token
    const accessToken = await getAccessToken();

    // 检查是否是 FormData（不需要设置 Content-Type）
    const isFormData = fetchOptions.body instanceof FormData;
    
    // 默认配置
    const config: RequestInit = {
        credentials: "include", // 包含cookies
        headers: {
            // FormData 会自动设置正确的 Content-Type（包含 boundary）
            ...(isFormData ? {} : { "Content-Type": "application/json" }),
            ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
            ...fetchOptions.headers,
        },
        ...fetchOptions,
    };

    const response = await fetch(url, config);

    // 如果是401，清除用户信息（让ProtectedLayout处理重定向）
    if (response.status === 401) {
        console.warn("Unauthorized request, clearing session");
        // 清除用户会话，ProtectedLayout会自动检测并重定向到登录页
        await userManager.removeUser();
        throw new Error("Unauthorized");
    }

    // 如果响应不成功，抛出错误
    if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`API Error ${response.status}: ${errorText}`);
    }

    // 尝试解析JSON
    const contentType = response.headers.get("content-type");
    if (contentType && contentType.includes("application/json")) {
        return response.json();
    }

    // 如果不是JSON，返回文本
    return response.text() as any;
};

/**
 * API Client 对象
 */
export const apiClient = {
    /**
     * GET 请求
     */
    get: <T = any>(endpoint: string, params?: Record<string, string | number | boolean>) => {
        return request<T>(endpoint, { method: "GET", params });
    },

    /**
     * POST 请求
     */
    post: <T = any>(endpoint: string, body?: any, options?: RequestOptions) => {
        // 如果 body 是 FormData，直接使用，不做 JSON.stringify
        const isFormData = body instanceof FormData;
        
        return request<T>(endpoint, {
            method: "POST",
            body: isFormData ? body : (body ? JSON.stringify(body) : undefined),
            headers: isFormData ? {} : undefined, // FormData 不需要手动设置 Content-Type
            ...options,
        });
    },

    /**
     * PUT 请求
     */
    put: <T = any>(endpoint: string, body?: any, options?: RequestOptions) => {
        return request<T>(endpoint, {
            method: "PUT",
            body: body ? JSON.stringify(body) : undefined,
            ...options,
        });
    },

    /**
     * DELETE 请求
     */
    delete: <T = any>(endpoint: string, options?: RequestOptions) => {
        return request<T>(endpoint, { method: "DELETE", ...options });
    },

    /**
     * PATCH 请求
     */
    patch: <T = any>(endpoint: string, body?: any, options?: RequestOptions) => {
        return request<T>(endpoint, {
            method: "PATCH",
            body: body ? JSON.stringify(body) : undefined,
            ...options,
        });
    },
};

/**
 * 导出常用的fetch方法（兼容旧代码）
 */
export const apiFetch = async (endpoint: string, options?: RequestInit) => {
    const url = buildUrl(endpoint);
    return fetch(url, {
        credentials: "include",
        ...options,
    });
};

export default apiClient;

