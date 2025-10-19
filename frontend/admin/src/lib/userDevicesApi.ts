import { apiClient } from "./apiClient";

export interface UserDevice {
  id: string;
  userId: string;
  deviceFingerprint: string;
  deviceName?: string;
  browser?: string;
  browserVersion?: string;
  operatingSystem?: string;
  osVersion?: string;
  deviceType?: string;
  screenResolution?: string;
  timeZone?: string;
  language?: string;
  platform?: string;
  firstUsedAt: string;
  lastUsedAt: string;
  lastIpAddress?: string;
  lastLocation?: string;
  isTrusted: boolean;
  isActive: boolean;
  usageCount: number;
  tenantId?: string;
}

export interface DeviceStatistics {
  totalDevices: number;
  trustedDevices: number;
  activeDevices: number;
  lastDeviceAdded?: string;
  deviceTypeCounts: DeviceTypeCount[];
}

export interface DeviceTypeCount {
  deviceType: string;
  count: number;
}

export const userDevicesApi = {
  /**
   * 获取指定用户的所有设备
   */
  getUserDevices: async (userId: string): Promise<UserDevice[]> => {
    return apiClient.get<UserDevice[]>(`/api/userdevices/user/${userId}`);
  },

  /**
   * 获取设备详情
   */
  getDevice: async (deviceId: string): Promise<UserDevice> => {
    return apiClient.get<UserDevice>(`/api/userdevices/${deviceId}`);
  },

  /**
   * 获取用户设备统计
   */
  getDeviceStatistics: async (userId: string): Promise<DeviceStatistics> => {
    return apiClient.get<DeviceStatistics>(`/api/userdevices/user/${userId}/statistics`);
  },

  /**
   * 信任/取消信任设备
   */
  trustDevice: async (deviceId: string, trusted: boolean): Promise<void> => {
    return apiClient.patch(`/api/userdevices/${deviceId}/trust`, { trusted });
  },

  /**
   * 激活/停用设备
   */
  setDeviceActive: async (deviceId: string, active: boolean): Promise<void> => {
    return apiClient.patch(`/api/userdevices/${deviceId}/active`, { active });
  },

  /**
   * 重命名设备
   */
  renameDevice: async (deviceId: string, newName: string): Promise<void> => {
    return apiClient.patch(`/api/userdevices/${deviceId}/rename`, { newName });
  },

  /**
   * 删除设备
   */
  deleteDevice: async (deviceId: string): Promise<void> => {
    return apiClient.delete(`/api/userdevices/${deviceId}`);
  },
};

