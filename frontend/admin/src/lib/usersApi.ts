import { apiClient } from "./apiClient";
import type {
  UserSummary,
  CreateUserPayload,
  UpdateUserPayload,
  ChangePasswordPayload,
} from "../types/users";

export const usersApi = {
  async list(): Promise<UserSummary[]> {
    return await apiClient.get<UserSummary[]>("/api/users");

  },

  async getById(userId: string): Promise<UserSummary> {
    return await apiClient.get<UserSummary>(`/api/users/${userId}`);

  },

  async create(payload: CreateUserPayload): Promise<UserSummary> {
    return await apiClient.post<UserSummary>("/api/users", payload);

  },

  async update(userId: string, payload: UpdateUserPayload): Promise<UserSummary> {
    return await apiClient.put<UserSummary>(`/api/users/${userId}`, payload);

  },

  async remove(userId: string): Promise<void> {
    await apiClient.delete(`/api/users/${userId}`);
  },

  async changePassword(userId: string, payload: ChangePasswordPayload): Promise<void> {
    await apiClient.post(`/api/users/${userId}/change-password`, payload);
  },

  async unlock(userId: string): Promise<void> {
    await apiClient.post(`/api/users/${userId}/unlock`);
  },
};
