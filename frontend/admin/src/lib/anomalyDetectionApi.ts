import { apiClient } from "./apiClient";

export interface LoginHistory {
  id: string;
  userId: string;
  userName?: string;
  loginTime: string;
  ipAddress?: string;
  country?: string;
  city?: string;
  userAgent?: string;
  browser?: string;
  operatingSystem?: string;
  deviceType?: string;
  success: boolean;
  failureReason?: string;
  isAnomalous: boolean;
  anomalyReason?: string;
  riskScore: number;
  userNotified: boolean;
}

export const anomalyDetectionApi = {
  getAllAnomalousLogins: async (
    startDate?: string,
    endDate?: string,
    pageNumber = 1,
    pageSize = 50
  ): Promise<LoginHistory[]> => {
    const params = new URLSearchParams();
    if (startDate) params.append("startDate", startDate);
    if (endDate) params.append("endDate", endDate);
    params.append("pageNumber", pageNumber.toString());
    params.append("pageSize", pageSize.toString());
    
    return apiClient.get<LoginHistory[]>(`/api/anomalydetection/anomalous-logins?${params.toString()}`);
  },

  getUserAnomalousLogins: async (
    userId: string,
    startDate?: string,
    endDate?: string
  ): Promise<LoginHistory[]> => {
    const params = new URLSearchParams();
    if (startDate) params.append("startDate", startDate);
    if (endDate) params.append("endDate", endDate);
    
    return apiClient.get<LoginHistory[]>(`/api/anomalydetection/user/${userId}/anomalous-logins?${params.toString()}`);
  },

  markAsNotified: async (loginHistoryId: string): Promise<void> => {
    return apiClient.post(`/api/anomalydetection/${loginHistoryId}/mark-notified`);
  },
};

