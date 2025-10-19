import { apiClient } from "./apiClient";

export interface SecurityRule {
    id: string;
    ruleType: string;
    ruleValue: string;
    description?: string;
    isEnabled: boolean;
    createdAt: string;
    updatedAt: string;
    tenantId?: string;
}

export interface CreateSecurityRuleRequest {
    ruleType: string;
    ruleValue: string;
    description?: string;
}

export interface UpdateSecurityRuleRequest {
    ruleValue: string;
    description?: string;
}

export interface ToggleRuleRequest {
    isEnabled: boolean;
}

export interface TestIpRequest {
    ipAddress: string;
}

export interface IpTestResult {
    ipAddress: string;
    isAllowed: boolean;
    testedAt: string;
}

export const securityRulesApi = {
    async getAll(includeDisabled: boolean = false): Promise<SecurityRule[]> {
        return await apiClient.get<SecurityRule[]>(`/api/securityrules?includeDisabled=${includeDisabled}`);
    },

    async getById(id: string): Promise<SecurityRule> {
        return await apiClient.get<SecurityRule>(`/api/securityrules/${id}`);
    },

    async create(request: CreateSecurityRuleRequest): Promise<SecurityRule> {
        return await apiClient.post<SecurityRule>("/api/securityrules", request);
    },

    async update(id: string, request: UpdateSecurityRuleRequest): Promise<SecurityRule> {
        return await apiClient.put<SecurityRule>(`/api/securityrules/${id}`, request);
    },

    async toggle(id: string, isEnabled: boolean): Promise<SecurityRule> {
        return await apiClient.post<SecurityRule>(`/api/securityrules/${id}/toggle`, { isEnabled });
    },

    async delete(id: string): Promise<void> {
        await apiClient.delete(`/api/securityrules/${id}`);
    },

    async testIp(ipAddress: string): Promise<IpTestResult> {
        return await apiClient.post<IpTestResult>("/api/securityrules/test-ip", { ipAddress });
    },
};

