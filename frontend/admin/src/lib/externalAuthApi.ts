import { apiClient } from "./apiClient";
import type {
  ExternalAuthProvider,
  CreateExternalAuthProviderPayload,
  UpdateExternalAuthProviderPayload,
} from "../types/externalAuth";

export const externalAuthApi = {
  async list(): Promise<ExternalAuthProvider[]> {
    return await apiClient.get<ExternalAuthProvider[]>("/api/externalauthproviders");

  },

  async getById(id: string): Promise<ExternalAuthProvider> {
    return await apiClient.get<ExternalAuthProvider>(`/api/externalauthproviders/${id}`);

  },

  async create(payload: CreateExternalAuthProviderPayload): Promise<ExternalAuthProvider> {
    return await apiClient.post<ExternalAuthProvider>("/api/externalauthproviders", payload);

  },

  async update(id: string, payload: UpdateExternalAuthProviderPayload): Promise<ExternalAuthProvider> {
    return await apiClient.put<ExternalAuthProvider>(`/api/externalauthproviders/${id}`, payload);

  },

  async remove(id: string): Promise<void> {
    await apiClient.delete(`/api/externalauthproviders/${id}`);
  },

  async toggle(id: string, enabled: boolean): Promise<ExternalAuthProvider> {
    return await apiClient.post<ExternalAuthProvider>(
      `/api/externalauthproviders/${id}/toggle`,
      { enabled }
    );

  },
};
