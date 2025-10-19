import { apiClient } from "./apiClient";

// IP Access Rules
export interface IpAccessRule {
  id: number;
  name: string;
  ipAddress: string;
  ruleType: "Whitelist" | "Blacklist";
  isEnabled: boolean;
  scope: "Global" | "User" | "Role";
  targetUserId?: string;
  targetRoleName?: string;
  description?: string;
  priority: number;
  createdAt: string;
  updatedAt?: string;
  createdBy?: string;
}

export interface CreateIpAccessRuleRequest {
  name: string;
  ipAddress: string;
  ruleType: 0 | 1; // 0=Whitelist, 1=Blacklist
  isEnabled: boolean;
  scope: 0 | 1 | 2; // 0=Global, 1=User, 2=Role
  targetUserId?: string;
  targetRoleName?: string;
  description?: string;
  priority: number;
}

// Login Time Restrictions
export interface LoginTimeRestriction {
  id: number;
  name: string;
  isEnabled: boolean;
  scope: "Global" | "User" | "Role";
  targetUserId?: string;
  targetRoleName?: string;
  allowedDaysOfWeek?: string; // "1,2,3,4,5" for weekdays
  dailyStartTime?: string; // "09:00"
  dailyEndTime?: string; // "18:00"
  timeZone: string;
  description?: string;
  priority: number;
  createdAt: string;
  updatedAt?: string;
  createdBy?: string;
}

export interface CreateLoginTimeRestrictionRequest {
  name: string;
  isEnabled: boolean;
  scope: 0 | 1 | 2;
  targetUserId?: string;
  targetRoleName?: string;
  allowedDaysOfWeek?: string;
  dailyStartTime?: string;
  dailyEndTime?: string;
  timeZone?: string;
  description?: string;
  priority: number;
}

export const loginPoliciesApi = {
  // IP Access Rules
  ipRules: {
    async getAll(): Promise<IpAccessRule[]> {
      return apiClient.get<IpAccessRule[]>("/api/ipaccessrules");
    },

    async getById(id: number): Promise<IpAccessRule> {
      return apiClient.get<IpAccessRule>(`/api/ipaccessrules/${id}`);
    },

    async create(request: CreateIpAccessRuleRequest): Promise<IpAccessRule> {
      return apiClient.post<IpAccessRule>("/api/ipaccessrules", request);
    },

    async update(id: number, request: CreateIpAccessRuleRequest): Promise<IpAccessRule> {
      return apiClient.put<IpAccessRule>(`/api/ipaccessrules/${id}`, request);
    },

    async delete(id: number): Promise<void> {
      return apiClient.delete(`/api/ipaccessrules/${id}`);
    },

    async toggleEnabled(id: number): Promise<IpAccessRule> {
      return apiClient.post<IpAccessRule>(`/api/ipaccessrules/${id}/toggle`, {});
    },
  },

  // Login Time Restrictions
  timeRestrictions: {
    async getAll(): Promise<LoginTimeRestriction[]> {
      return apiClient.get<LoginTimeRestriction[]>("/api/logintimerestrictions");
    },

    async getById(id: number): Promise<LoginTimeRestriction> {
      return apiClient.get<LoginTimeRestriction>(`/api/logintimerestrictions/${id}`);
    },

    async create(request: CreateLoginTimeRestrictionRequest): Promise<LoginTimeRestriction> {
      return apiClient.post<LoginTimeRestriction>("/api/logintimerestrictions", request);
    },

    async update(id: number, request: CreateLoginTimeRestrictionRequest): Promise<LoginTimeRestriction> {
      return apiClient.put<LoginTimeRestriction>(`/api/logintimerestrictions/${id}`, request);
    },

    async delete(id: number): Promise<void> {
      return apiClient.delete(`/api/logintimerestrictions/${id}`);
    },

    async toggleEnabled(id: number): Promise<LoginTimeRestriction> {
      return apiClient.post<LoginTimeRestriction>(`/api/logintimerestrictions/${id}/toggle`, {});
    },
  },
};

