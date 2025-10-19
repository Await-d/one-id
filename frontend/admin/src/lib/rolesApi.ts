import apiClient from "./apiClient";

export interface Role {
    id: string;
    name: string;
    description: string;
    userCount: number;
}

export interface RoleFormData {
    name: string;
    description: string;
}

export interface UserRole {
    userId: string;
    userName: string;
    email: string;
    displayName?: string;
}

export interface UserAddToRolePayload {
    userId: string;
}

export const rolesApi = {
    async getAll(): Promise<Role[]> {
        return await apiClient.get<Role[]>("/api/roles");
    },

    async getById(id: string): Promise<Role> {
        return await apiClient.get<Role>(`/api/roles/${id}`);
    },

    async create(payload: RoleFormData): Promise<Role> {
        return await apiClient.post<Role>("/api/roles", payload);
    },

    async update(id: string, payload: RoleFormData): Promise<Role> {
        return await apiClient.put<Role>(`/api/roles/${id}`, payload);
    },

    async delete(id: string): Promise<void> {
        await apiClient.delete(`/api/roles/${id}`);
    },

    async getUsersInRole(roleId: string): Promise<UserRole[]> {
        return await apiClient.get<UserRole[]>(`/api/roles/${roleId}/users`);
    },

    async addUserToRole(roleId: string, userId: string): Promise<void> {
        await apiClient.post(`/api/roles/${roleId}/users/${userId}`);
    },

    async removeUserFromRole(roleId: string, userId: string): Promise<void> {
        await apiClient.delete(`/api/roles/${roleId}/users/${userId}`);
    },
};

