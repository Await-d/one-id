import apiClient from "./apiClient";

export interface AuditLog {
    id: number;
    userId?: string;
    userName?: string;
    category: string;
    action: string;
    entityType?: string;
    entityId?: string;
    details?: string;
    ipAddress?: string;
    userAgent?: string;
    success: boolean;
    errorMessage?: string;
    createdAt: string;
}

export interface AuditLogFilters {
    startDate?: string;
    endDate?: string;
    category?: string;
    userId?: string;
    success?: boolean;
    keyword?: string;
    page?: number;
    pageSize?: number;
}

export interface AuditLogsResponse {
    logs: AuditLog[];
    total: number;
}

export const auditLogsApi = {
    async getAll(filters?: AuditLogFilters): Promise<AuditLogsResponse> {
        const params: Record<string, string | number> = {};

        if (filters?.startDate) params.startDate = filters.startDate;
        if (filters?.endDate) params.endDate = filters.endDate;
        if (filters?.category) params.category = filters.category;
        if (filters?.userId) params.userId = filters.userId;
        if (filters?.success !== undefined) params.success = String(filters.success);
        if (filters?.keyword?.trim()) params.keyword = filters.keyword.trim();
        if (filters?.page) params.page = filters.page;
        if (filters?.pageSize) params.pageSize = filters.pageSize;

        return await apiClient.get<AuditLogsResponse>("/api/auditlogs", params);
    },

    async getCategories(): Promise<string[]> {
        return await apiClient.get<string[]>("/api/auditlogs/categories");
    },
};

