export type ClientSummary = {
  clientId: string;
  displayName: string;
  clientType: string;
  redirectUris: string[];
  postLogoutRedirectUris: string[];
  scopes: string[];
};

export type CreateClientPayload = {
  clientId: string;
  displayName: string;
  redirectUri: string;
  postLogoutRedirectUri?: string;
  scopes?: string[];
  clientType?: string;
  clientSecret?: string;
};

export type UpdateClientPayload = {
  displayName: string;
  redirectUri: string;
  postLogoutRedirectUri?: string;
  scopes?: string[];
  clientType?: string;
  clientSecret?: string;
};

export type UpdateClientScopesPayload = {
  scopes: string[];
};
