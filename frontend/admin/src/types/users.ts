export interface UserSummary {
  id: string;
  userName: string;
  email: string;
  emailConfirmed: boolean;
  displayName?: string;
  isExternal: boolean;
  lockoutEnabled: boolean;
  lockoutEnd?: string;
  accessFailedCount: number;
  roles: string[];
  externalLogins: ExternalLoginInfo[];
}

export interface ExternalLoginInfo {
  loginProvider: string;
  providerKey: string;
  providerDisplayName?: string;
}

export interface CreateUserPayload {
  userName: string;
  email: string;
  password: string;
  displayName?: string;
  emailConfirmed?: boolean;
}

export interface UpdateUserPayload {
  displayName?: string;
  emailConfirmed?: boolean;
  lockoutEnabled?: boolean;
}

export interface ChangePasswordPayload {
  newPassword: string;
}
