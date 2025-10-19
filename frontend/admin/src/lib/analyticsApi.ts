import { apiClient } from "./apiClient";

export interface DashboardStatistics {
  totalUsers: number;
  activeUsers24h: number;
  totalLogins: number;
  successfulLogins: number;
  failedLogins: number;
  loginSuccessRate: number;
  totalApiCalls: number;
  totalErrors: number;
  errorRate: number;
  activeSessions: number;
}

export interface LoginTrend {
  date: string;
  successfulLogins: number;
  failedLogins: number;
  totalLogins: number;
}

export interface ApiCallStatistic {
  action: string;
  callCount: number;
  successCount: number;
  failureCount: number;
  successRate: number;
}

export const analyticsApi = {
  async getDashboardStatistics(
    startDate?: Date,
    endDate?: Date
  ): Promise<DashboardStatistics> {
    const params = new URLSearchParams();
    if (startDate) {
      params.append("startDate", startDate.toISOString());
    }
    if (endDate) {
      params.append("endDate", endDate.toISOString());
    }

    const query = params.toString();
    const url = query ? `/api/analytics/dashboard?${query}` : "/api/analytics/dashboard";
    
    return apiClient.get<DashboardStatistics>(url);
  },

  async getLoginTrends(days: number = 7): Promise<LoginTrend[]> {
    return apiClient.get<LoginTrend[]>(`/api/analytics/login-trends?days=${days}`);
  },

  async getApiCallStatistics(topCount: number = 10): Promise<ApiCallStatistic[]> {
    return apiClient.get<ApiCallStatistic[]>(`/api/analytics/api-calls?topCount=${topCount}`);
  },
};

