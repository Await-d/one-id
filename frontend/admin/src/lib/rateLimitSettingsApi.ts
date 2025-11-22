import apiClient from "./apiClient";

export interface RateLimitSetting {
  id: number;
  limiterName: string;
  displayName: string;
  description?: string;
  enabled: boolean;
  permitLimit: number;
  windowSeconds: number;
  queueLimit: number;
  sortOrder: number;
  createdAt: string;
  updatedAt: string;
  lastModifiedBy?: string;
}

export interface CreateRateLimitSettingRequest {
  limiterName: string;
  displayName: string;
  description?: string;
  enabled: boolean;
  permitLimit: number;
  windowSeconds: number;
  queueLimit?: number;
  sortOrder?: number;
}

export interface UpdateRateLimitSettingRequest {
  id: number;
  displayName: string;
  description?: string;
  enabled: boolean;
  permitLimit: number;
  windowSeconds: number;
  queueLimit?: number;
  sortOrder?: number;
}

class RateLimitSettingsApi {
  private baseUrl = "/api/ratelimitsettings";

  async getAll(): Promise<RateLimitSetting[]> {
    const response = await apiClient.get(this.baseUrl);
    return response.data;
  }

  async getEnabled(): Promise<RateLimitSetting[]> {
    const response = await apiClient.get(`${this.baseUrl}/enabled`);
    return response.data;
  }

  async getById(id: number): Promise<RateLimitSetting> {
    const response = await apiClient.get(`${this.baseUrl}/${id}`);
    return response.data;
  }

  async create(data: CreateRateLimitSettingRequest): Promise<RateLimitSetting> {
    const response = await apiClient.post(this.baseUrl, data);
    return response.data;
  }

  async update(data: UpdateRateLimitSettingRequest): Promise<RateLimitSetting> {
    const response = await apiClient.put(`${this.baseUrl}/${data.id}`, data);
    return response.data;
  }

  async delete(id: number): Promise<void> {
    await apiClient.delete(`${this.baseUrl}/${id}`);
  }

  async ensureDefaults(): Promise<void> {
    await apiClient.post(`${this.baseUrl}/ensure-defaults`);
  }
}

export const rateLimitSettingsApi = new RateLimitSettingsApi();
