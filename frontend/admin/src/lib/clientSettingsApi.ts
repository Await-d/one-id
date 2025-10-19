import { apiClient } from "./apiClient";

export interface ClientValidationSettings {
  allowedSchemes: string[];
  allowHttpOnLoopback: boolean;
  allowedHosts: string[];
  updatedAt: string;
}

export interface UpdateClientValidationSettingsPayload {
  allowedSchemes: string[];
  allowHttpOnLoopback: boolean;
  allowedHosts: string[];
}

export const clientSettingsApi = {
  async getValidation(): Promise<ClientValidationSettings> {
    return apiClient.get<ClientValidationSettings>("/api/client-settings/validation");
  },

  async updateValidation(payload: UpdateClientValidationSettingsPayload): Promise<ClientValidationSettings> {
    return apiClient.put<ClientValidationSettings>("/api/client-settings/validation", payload);
  },
};
