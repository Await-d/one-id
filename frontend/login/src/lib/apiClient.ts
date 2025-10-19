/**
 * API Client - 统一的API请求帮助类
 * 自动处理开发环境和生产环境的API地址
 */

// 获取API基础URL
// 开发环境：使用localhost:5101
// 生产环境：使用当前域名（前端和后端同源部署）
const getApiBaseUrl = (): string => {
    const envApiUrl = import.meta.env.VITE_API_BASE_URL;

    // 如果环境变量中配置了API地址，使用配置的地址
    if (envApiUrl && envApiUrl.trim()) {
        return envApiUrl;
    }

    // 开发环境使用localhost:5101
    if (import.meta.env.DEV) {
        return "http://localhost:5101";
    }

    // 生产环境使用当前域名（前后端同源）
    return window.location.origin;
};

export const API_BASE_URL = getApiBaseUrl();

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

    // 默认配置
    const config: RequestInit = {
        credentials: "include", // 包含cookies
        headers: {
            "Content-Type": "application/json",
            ...fetchOptions.headers,
        },
        ...fetchOptions,
    };

    const response = await fetch(url, config);

    // 如果是401，可能需要重新登录
    if (response.status === 401) {
        // 可以在这里添加重定向到登录页的逻辑
        console.warn("Unauthorized request, please login");
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
        return request<T>(endpoint, {
            method: "POST",
            body: body ? JSON.stringify(body) : undefined,
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

