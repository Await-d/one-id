import { apiClient } from "./apiClient";

export interface SigningKey {
  id: string;
  type: string;
  use: string;
  algorithm: string;
  publicKey: string;
  version: number;
  isActive: boolean;
  createdAt: string;
  activatedAt?: string;
  expiresAt: string;
  revokedAt?: string;
  notes?: string;
  tenantId?: string;
}

export interface GenerateRsaKeyRequest {
  keySize: number;
  validityDays: number;
  notes?: string;
}

export interface GenerateEcdsaKeyRequest {
  curve: string;
  validityDays: number;
  notes?: string;
}

export interface RevokeKeyRequest {
  reason?: string;
}

export interface CleanupResult {
  deletedCount: number;
  retentionDays: number;
  timestamp: string;
}

export interface RotationStatus {
  type: string;
  shouldRotate: boolean;
  activeKey?: SigningKey;
  warningDays: number;
  timestamp: string;
}

export const signingKeysApi = {
  async getAll(includeRevoked: boolean = false): Promise<SigningKey[]> {
    return await apiClient.get<SigningKey[]>(`/api/signingkeys?includeRevoked=${includeRevoked}`);
  },

  async getById(id: string): Promise<SigningKey> {
    return await apiClient.get<SigningKey>(`/api/signingkeys/${id}`);
  },

  async getActive(type: string = "RSA"): Promise<SigningKey> {
    return await apiClient.get<SigningKey>(`/api/signingkeys/active?type=${type}`);
  },

  async generateRsa(request: GenerateRsaKeyRequest): Promise<SigningKey> {
    return await apiClient.post<SigningKey>("/api/signingkeys/rsa", request);
  },

  async generateEcdsa(request: GenerateEcdsaKeyRequest): Promise<SigningKey> {
    return await apiClient.post<SigningKey>("/api/signingkeys/ecdsa", request);
  },

  async activate(id: string): Promise<SigningKey> {
    return await apiClient.post<SigningKey>(`/api/signingkeys/${id}/activate`, {});
  },

  async revoke(id: string, request?: RevokeKeyRequest): Promise<SigningKey> {
    return await apiClient.post<SigningKey>(`/api/signingkeys/${id}/revoke`, request || {});
  },

  async delete(id: string): Promise<void> {
    await apiClient.delete(`/api/signingkeys/${id}`);
  },

  async cleanup(retentionDays: number = 30): Promise<CleanupResult> {
    return await apiClient.post<CleanupResult>(`/api/signingkeys/cleanup?retentionDays=${retentionDays}`, {});
  },

  async getRotationStatus(type: string = "RSA", warningDays: number = 30): Promise<RotationStatus> {
    return await apiClient.get<RotationStatus>(`/api/signingkeys/rotation-status?type=${type}&warningDays=${warningDays}`);
  },
};

