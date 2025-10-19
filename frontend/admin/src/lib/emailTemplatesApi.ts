import { apiClient } from "./apiClient";

export interface EmailTemplate {
  id: string;
  templateKey: string;
  name: string;
  description?: string;
  subject: string;
  htmlBody: string;
  textBody?: string;
  language: string;
  isActive: boolean;
  isDefault: boolean;
  availableVariables?: string;
  lastModifiedBy?: string;
  updatedAt?: string;
  createdAt: string;
}

export const emailTemplatesApi = {
  /**
   * 获取所有邮件模板
   */
  getAllTemplates: async (): Promise<EmailTemplate[]> => {
    return apiClient.get<EmailTemplate[]>("/api/emailtemplates");
  },

  /**
   * 按语言获取模板
   */
  getTemplatesByLanguage: async (language: string): Promise<EmailTemplate[]> => {
    return apiClient.get<EmailTemplate[]>(`/api/emailtemplates/language/${language}`);
  },

  /**
   * 根据ID获取模板
   */
  getTemplate: async (id: string): Promise<EmailTemplate> => {
    return apiClient.get<EmailTemplate>(`/api/emailtemplates/${id}`);
  },

  /**
   * 创建新模板
   */
  createTemplate: async (template: Partial<EmailTemplate>): Promise<EmailTemplate> => {
    return apiClient.post<EmailTemplate>("/api/emailtemplates", template);
  },

  /**
   * 更新模板
   */
  updateTemplate: async (id: string, template: Partial<EmailTemplate>): Promise<EmailTemplate> => {
    return apiClient.put<EmailTemplate>(`/api/emailtemplates/${id}`, { ...template, id });
  },

  /**
   * 删除模板
   */
  deleteTemplate: async (id: string): Promise<void> => {
    return apiClient.delete(`/api/emailtemplates/${id}`);
  },

  /**
   * 复制模板到新语言
   */
  duplicateTemplate: async (id: string, newLanguage: string): Promise<EmailTemplate> => {
    return apiClient.post<EmailTemplate>(`/api/emailtemplates/${id}/duplicate`, {
      newLanguage,
    });
  },

  /**
   * 提取模板中的变量
   */
  extractVariables: async (template: string): Promise<string[]> => {
    return apiClient.post<string[]>("/api/emailtemplates/extract-variables", {
      template,
    });
  },

  /**
   * 确保默认模板存在
   */
  ensureDefaults: async (): Promise<void> => {
    return apiClient.post("/api/emailtemplates/ensure-defaults");
  },
};

