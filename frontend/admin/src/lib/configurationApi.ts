import { apiClient } from './apiClient';

// 配置状态响应
export interface ConfigurationStatus {
  rateLimit: {
    version: number;
    lastUpdated: string;
    settingsCount: number;
    enabledCount: number;
  };
  cors: {
    version: number;
    lastUpdated: string;
    allowAnyOrigin: boolean;
    originsCount: number;
  };
  externalAuth: {
    version: number;
    lastUpdated: string;
    providersCount: number;
  };
}

// 刷新响应
export interface ReloadResponse {
  message: string;
  reloadedAt?: string;
  version?: number;
  lastUpdated?: string;
  settingsCount?: number;
  allowAnyOrigin?: boolean;
  originsCount?: number;
  providersCount?: number;
  providers?: Array<{ name: string; displayName: string; providerType: string }>;
}

// 重启响应
export interface RestartResponse {
  message?: string;
  warning?: string;
  note?: string;
  triggeredBy?: string;
  triggeredAt?: string;
  requesterUserId?: string;
}

// 应用信息
export interface AppInfo {
  environment: string;
  machineName: string;
  processId: number;
  startTime: string;
  uptime: string;
  dotNetVersion: string;
  osDescription: string;
}

// 获取配置状态
export const getConfigurationStatus = async (): Promise<ConfigurationStatus> => {
  const response = await apiClient.get('/api/configuration/status');
  return response.data;
};

// 刷新所有配置
export const reloadAllConfigurations = async (): Promise<ReloadResponse> => {
  const response = await apiClient.post('/api/configuration/reload');
  return response.data;
};

// 刷新速率限制配置
export const reloadRateLimitConfiguration = async (): Promise<ReloadResponse> => {
  const response = await apiClient.post('/api/configuration/reload/ratelimit');
  return response.data;
};

// 刷新 CORS 配置
export const reloadCorsConfiguration = async (): Promise<ReloadResponse> => {
  const response = await apiClient.post('/api/configuration/reload/cors');
  return response.data;
};

// 刷新外部认证配置
export const reloadExternalAuthConfiguration = async (): Promise<ReloadResponse> => {
  const response = await apiClient.post('/api/configuration/reload/externalauth');
  return response.data;
};

// 获取应用信息
export const getAppInfo = async (): Promise<AppInfo> => {
  const response = await apiClient.get('/api/admin/info');
  return response.data;
};

// 触发应用重启
export const restartApplication = async (confirm: boolean = false): Promise<RestartResponse> => {
  const response = await apiClient.post(`/api/admin/restart?confirm=${confirm}`);
  return response.data;
};
