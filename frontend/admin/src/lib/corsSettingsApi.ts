import { apiClient } from "./apiClient";

export interface CorsSettings {
  allowedOrigins: string[];
  allowAnyOrigin: boolean;
  updatedAt: string;
}

export interface UpdateCorsSettingsPayload {
  allowedOrigins: string[];
  allowAnyOrigin: boolean;
}

export const corsSettingsApi = {
  async get(): Promise<CorsSettings> {
    return apiClient.get<CorsSettings>("/api/cors-settings");
  },

  async update(payload: UpdateCorsSettingsPayload): Promise<CorsSettings> {
    return apiClient.put<CorsSettings>("/api/cors-settings", payload);
  },
};
