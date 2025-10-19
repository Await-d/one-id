/**
 * Form Utilities - 表单提交工具类
 * 用于处理需要浏览器完整重定向的表单提交场景（如OIDC授权流程）
 */

/**
 * 创建并提交一个隐藏表单
 * 适用于需要浏览器自动处理重定向的场景（如OAuth/OIDC）
 * 
 * @param action - 表单提交的目标URL
 * @param params - 表单参数
 * @param method - HTTP方法，默认为POST
 */
export function submitForm(
    action: string,
    params: Record<string, string>,
    method: "GET" | "POST" = "POST"
): void {
    const form = document.createElement("form");
    form.method = method;
    form.action = action;

    // 添加所有参数作为隐藏input
    Object.entries(params).forEach(([key, value]) => {
        const input = document.createElement("input");
        input.type = "hidden";
        input.name = key;
        input.value = value;
        form.appendChild(input);
    });

    // 添加到DOM并提交
    document.body.appendChild(form);
    form.submit();
}

/**
 * 从URL的search params构建参数对象
 * 
 * @param url - URL对象或字符串
 * @returns 参数对象
 */
export function extractSearchParams(url: URL | string): Record<string, string> {
    const urlObj = typeof url === "string" ? new URL(url, window.location.origin) : url;
    const params: Record<string, string> = {};
    
    urlObj.searchParams.forEach((value, key) => {
        params[key] = value;
    });
    
    return params;
}

/**
 * 重定向到URL并带上错误参数
 * 
 * @param redirectUri - 重定向目标URL
 * @param error - 错误代码
 * @param errorDescription - 错误描述（可选）
 */
export function redirectWithError(
    redirectUri: string,
    error: string,
    errorDescription?: string
): void {
    const url = new URL(redirectUri);
    url.searchParams.set("error", error);
    
    if (errorDescription) {
        url.searchParams.set("error_description", errorDescription);
    }
    
    window.location.href = url.toString();
}

