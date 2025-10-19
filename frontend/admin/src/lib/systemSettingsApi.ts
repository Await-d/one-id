import apiClient from "./apiClient";

export interface SystemSetting {
  id: number;
  key: string;
  value: string;
  group: string;
  valueType: string;
  displayName?: string;
  description?: string;
  defaultValue?: string;
  isSensitive: boolean;
  isReadOnly: boolean;
  sortOrder: number;
  validationRules?: string;
  allowedValues?: string;
  createdAt: string;
  updatedAt?: string;
  lastModifiedBy?: string;
}

export interface CreateSystemSettingRequest {
  key: string;
  value: string;
  group: string;
  valueType: string;
  displayName?: string;
  description?: string;
  defaultValue?: string;
  isSensitive: boolean;
  isReadOnly: boolean;
  sortOrder: number;
  validationRules?: string;
  allowedValues?: string;
}

export interface UpdateSystemSettingRequest {
  value: string;
  displayName?: string;
  description?: string;
}

export interface UpdateValueRequest {
  value: string;
}

const API_BASE = "/api/SystemSettings";

export const systemSettingsApi = {
  // 获取所有设置
  async getAll(group?: string): Promise<SystemSetting[]> {
    const params = group ? { group } : undefined;
    return await apiClient.get<SystemSetting[]>(API_BASE, params);
  },

  // 根据 ID 获取设置
  async getById(id: number): Promise<SystemSetting> {
    return await apiClient.get<SystemSetting>(`${API_BASE}/${id}`);
  },

  // 根据键获取设置
  async getByKey(key: string): Promise<SystemSetting> {
    return await apiClient.get<SystemSetting>(`${API_BASE}/key/${encodeURIComponent(key)}`);
  },

  // 获取分组设置（键值对）
  async getByGroup(group: string): Promise<Record<string, string>> {
    return await apiClient.get<Record<string, string>>(`${API_BASE}/group/${encodeURIComponent(group)}`);
  },

  // 创建设置
  async create(data: CreateSystemSettingRequest): Promise<SystemSetting> {
    return await apiClient.post<SystemSetting>(API_BASE, data);
  },

  // 更新设置
  async update(id: number, data: UpdateSystemSettingRequest): Promise<SystemSetting> {
    return await apiClient.put<SystemSetting>(`${API_BASE}/${id}`, data);
  },

  // 更新设置值（简化接口）
  async updateValue(key: string, value: string): Promise<void> {
    await apiClient.patch(`${API_BASE}/${encodeURIComponent(key)}/value`, { value });
  },

  // 删除设置
  async delete(id: number): Promise<void> {
    await apiClient.delete(`${API_BASE}/${id}`);
  },

  // 重置为默认值
  async reset(key: string): Promise<void> {
    await apiClient.post(`${API_BASE}/${encodeURIComponent(key)}/reset`);
  },

  // 重置分组所有设置
  async resetGroup(group: string): Promise<void> {
    await apiClient.post(`${API_BASE}/group/${encodeURIComponent(group)}/reset`);
  },

  // 确保默认设置已初始化
  async ensureDefaults(): Promise<void> {
    await apiClient.post(`${API_BASE}/ensure-defaults`);
  },
};

