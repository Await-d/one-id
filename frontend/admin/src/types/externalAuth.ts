export interface ExternalAuthProvider {
  id: string;
  providerType: string;
  name: string;
  displayName: string;
  enabled: boolean;
  clientId: string;
  callbackPath: string;
  scopes: string[];
  additionalConfig: Record<string, string>;
  displayOrder: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateExternalAuthProviderPayload {
  providerType: string;
  name: string;
  displayName: string;
  clientId: string;
  clientSecret: string;
  callbackPath?: string;
  scopes?: string[];
  additionalConfig?: Record<string, string>;
  displayOrder?: number;
}

export interface UpdateExternalAuthProviderPayload {
  displayName?: string;
  clientId?: string;
  clientSecret?: string;
  callbackPath?: string;
  enabled?: boolean;
  scopes?: string[];
  additionalConfig?: Record<string, string>;
  displayOrder?: number;
}
