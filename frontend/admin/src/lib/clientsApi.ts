import { apiClient } from "./apiClient";
import type {
  ClientSummary,
  CreateClientPayload,
  UpdateClientPayload,
  UpdateClientScopesPayload,
} from "../types/clients";

export const clientsApi = {
  async list(): Promise<ClientSummary[]> {
    return await apiClient.get<ClientSummary[]>("/api/clients");
  },

  async create(payload: CreateClientPayload): Promise<ClientSummary> {
    return await apiClient.post<ClientSummary>("/api/clients", payload);
  },

  async update(clientId: string, payload: UpdateClientPayload): Promise<ClientSummary> {
    return await apiClient.put<ClientSummary>(`/api/clients/${clientId}`, payload);
  },

  async updateScopes(clientId: string, payload: UpdateClientScopesPayload): Promise<ClientSummary> {
    return await apiClient.put<ClientSummary>(
      `/api/clients/${clientId}/scopes`,
      payload,
    );
  },

  async remove(clientId: string): Promise<void> {
    await apiClient.delete(`/api/clients/${clientId}`);
  },
};
