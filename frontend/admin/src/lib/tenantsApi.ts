import { apiClient } from "./apiClient";

export interface Tenant {
    id: string;
    name: string;
    displayName: string;
    domain: string;
    isActive: boolean;
    createdAt: string;
    updatedAt?: string;
}

export interface CreateTenantRequest {
    name: string;
    displayName: string;
    domain: string;
}

export interface UpdateTenantRequest {
    displayName: string;
    domain: string;
    isActive: boolean;
}

export const tenantsApi = {
    /**
     * 获取所有租户列表
     */
    getAllTenants: async (): Promise<Tenant[]> => {
        return apiClient.get<Tenant[]>("/api/tenants");
    },

    /**
     * 根据ID获取租户
     */
    getTenantById: async (id: string): Promise<Tenant> => {
        return apiClient.get<Tenant>(`/api/tenants/${id}`);
    },

    /**
     * 创建新租户
     */
    createTenant: async (request: CreateTenantRequest): Promise<Tenant> => {
        return apiClient.post<Tenant>("/api/tenants", request);
    },

    /**
     * 更新租户
     */
    updateTenant: async (id: string, request: UpdateTenantRequest): Promise<Tenant> => {
        return apiClient.put<Tenant>(`/api/tenants/${id}`, request);
    },

    /**
     * 删除租户
     */
    deleteTenant: async (id: string): Promise<void> => {
        return apiClient.delete(`/api/tenants/${id}`);
    },

    /**
     * 切换租户状态
     */
    toggleTenantStatus: async (id: string): Promise<{ isActive: boolean }> => {
        return apiClient.post<{ isActive: boolean }>(`/api/tenants/${id}/toggle`);
    },
};

