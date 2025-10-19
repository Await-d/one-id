import { apiClient } from "./apiClient";

export interface NotificationSettings {
  anomalousLogin: AnomalousLoginSettings;
  newDevice: NewDeviceSettings;
  passwordChanged: PasswordChangedSettings;
  accountLocked: AccountLockedSettings;
  mfaEnabled: MfaEnabledSettings;
}

export interface AnomalousLoginSettings {
  enabled: boolean;
  riskScoreThreshold: number;
}

export interface NewDeviceSettings {
  enabled: boolean;
}

export interface PasswordChangedSettings {
  enabled: boolean;
}

export interface AccountLockedSettings {
  enabled: boolean;
}

export interface MfaEnabledSettings {
  enabled: boolean;
}

export const notificationSettingsApi = {
  getSettings: async (): Promise<NotificationSettings> => {
    return apiClient.get<NotificationSettings>("/api/notificationsettings");
  },

  updateAnomalousLoginSettings: async (settings: AnomalousLoginSettings): Promise<void> => {
    await apiClient.put("/api/notificationsettings/anomalous-login", settings);
  },

  updateNewDeviceSettings: async (settings: NewDeviceSettings): Promise<void> => {
    await apiClient.put("/api/notificationsettings/new-device", settings);
  },

  updatePasswordChangedSettings: async (settings: PasswordChangedSettings): Promise<void> => {
    await apiClient.put("/api/notificationsettings/password-changed", settings);
  },

  updateAccountLockedSettings: async (settings: AccountLockedSettings): Promise<void> => {
    await apiClient.put("/api/notificationsettings/account-locked", settings);
  },

  updateMfaEnabledSettings: async (settings: MfaEnabledSettings): Promise<void> => {
    await apiClient.put("/api/notificationsettings/mfa-enabled", settings);
  },

  updateAllSettings: async (settings: NotificationSettings): Promise<void> => {
    await apiClient.put("/api/notificationsettings", settings);
  },
};

