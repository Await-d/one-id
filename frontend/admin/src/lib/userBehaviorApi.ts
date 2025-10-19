import { apiClient } from "./apiClient";

export interface UserBehaviorReport {
  deviceTypes: Record<string, number>;
  browsers: Record<string, number>;
  operatingSystems: Record<string, number>;
  countries: Record<string, number>;
  topBrowserVersions: Record<string, number>;
  totalRequests: number;
  uniqueUsers: number;
  startDate: string;
  endDate: string;
}

export const userBehaviorApi = {
  /**
   * 获取设备类型统计
   */
  getDeviceStatistics: async (startDate?: string, endDate?: string): Promise<Record<string, number>> => {
    const params = new URLSearchParams();
    if (startDate) params.append("startDate", startDate);
    if (endDate) params.append("endDate", endDate);
    
    return apiClient.get<Record<string, number>>(`/api/userbehavior/devices?${params.toString()}`);
  },

  /**
   * 获取浏览器统计
   */
  getBrowserStatistics: async (startDate?: string, endDate?: string): Promise<Record<string, number>> => {
    const params = new URLSearchParams();
    if (startDate) params.append("startDate", startDate);
    if (endDate) params.append("endDate", endDate);
    
    return apiClient.get<Record<string, number>>(`/api/userbehavior/browsers?${params.toString()}`);
  },

  /**
   * 获取操作系统统计
   */
  getOperatingSystemStatistics: async (startDate?: string, endDate?: string): Promise<Record<string, number>> => {
    const params = new URLSearchParams();
    if (startDate) params.append("startDate", startDate);
    if (endDate) params.append("endDate", endDate);
    
    return apiClient.get<Record<string, number>>(`/api/userbehavior/operating-systems?${params.toString()}`);
  },

  /**
   * 获取地理位置统计
   */
  getGeographicStatistics: async (startDate?: string, endDate?: string): Promise<Record<string, number>> => {
    const params = new URLSearchParams();
    if (startDate) params.append("startDate", startDate);
    if (endDate) params.append("endDate", endDate);
    
    return apiClient.get<Record<string, number>>(`/api/userbehavior/geographic?${params.toString()}`);
  },

  /**
   * 获取综合行为分析报告
   */
  getBehaviorReport: async (startDate?: string, endDate?: string): Promise<UserBehaviorReport> => {
    const params = new URLSearchParams();
    if (startDate) params.append("startDate", startDate);
    if (endDate) params.append("endDate", endDate);
    
    return apiClient.get<UserBehaviorReport>(`/api/userbehavior/report?${params.toString()}`);
  },
};

