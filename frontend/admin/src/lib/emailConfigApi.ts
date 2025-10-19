import apiClient from "./apiClient";

export interface EmailConfiguration {
    id: number;
    tenantId?: string;
    provider: string;
    fromEmail: string;
    fromName: string;
    smtpHost?: string;
    smtpPort?: number;
    smtpUseSsl: boolean;
    smtpUsername?: string;
    hasSmtpPassword: boolean;
    hasSendGridApiKey: boolean;
    isEnabled: boolean;
    createdAt: string;
    updatedAt?: string;
}

export interface EmailConfigFormData {
    tenantId?: string;
    provider: string;
    fromEmail: string;
    fromName: string;
    smtpHost?: string;
    smtpPort?: number;
    smtpUseSsl: boolean;
    smtpUsername?: string;
    smtpPassword?: string;
    sendGridApiKey?: string;
    isEnabled: boolean;
}

export interface TestEmailRequest {
    to: string;
    subject?: string;
    body?: string;
}

export const emailConfigApi = {
    async getAll(): Promise<EmailConfiguration[]> {
        return await apiClient.get<EmailConfiguration[]>("/api/emailconfiguration");
    },

    async getById(id: number): Promise<EmailConfiguration> {
        return await apiClient.get<EmailConfiguration>(`/api/emailconfiguration/${id}`);
    },

    async getActive(): Promise<EmailConfiguration> {
        return await apiClient.get<EmailConfiguration>("/api/emailconfiguration/active");
    },

    async create(payload: EmailConfigFormData): Promise<EmailConfiguration> {
        return await apiClient.post<EmailConfiguration>("/api/emailconfiguration", payload);
    },

    async update(id: number, payload: EmailConfigFormData): Promise<EmailConfiguration> {
        return await apiClient.put<EmailConfiguration>(`/api/emailconfiguration/${id}`, payload);
    },

    async delete(id: number): Promise<void> {
        await apiClient.delete(`/api/emailconfiguration/${id}`);
    },

    async test(id: number, request: TestEmailRequest): Promise<void> {
        await apiClient.post(`/api/emailconfiguration/${id}/test`, request);
    },
};

