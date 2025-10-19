import { apiClient } from "./apiClient";

export interface Webhook {
  id: string;
  name: string;
  url: string;
  description?: string;
  events: string[];
  secret?: string;
  isActive: boolean;
  maxRetries: number;
  timeoutSeconds: number;
  customHeaders?: string;
  lastTriggeredAt?: string;
  lastSuccessAt?: string;
  lastFailureAt?: string;
  failureCount: number;
  totalTriggers: number;
  successCount: number;
  createdAt: string;
  updatedAt: string;
  tenantId?: string;
}

export interface CreateWebhookDto {
  name: string;
  url: string;
  description?: string;
  events: string[];
  secret?: string;
  isActive: boolean;
  maxRetries: number;
  timeoutSeconds: number;
  customHeaders?: string;
  tenantId?: string;
}

export interface UpdateWebhookDto {
  name: string;
  url: string;
  description?: string;
  events: string[];
  secret?: string;
  isActive: boolean;
  maxRetries: number;
  timeoutSeconds: number;
  customHeaders?: string;
}

export interface WebhookLog {
  id: string;
  webhookId: string;
  eventType: string;
  payload: string;
  url: string;
  statusCode: number;
  response?: string;
  success: boolean;
  errorMessage?: string;
  retryCount: number;
  durationMs: number;
  createdAt: string;
}

export interface WebhookTestResult {
  success: boolean;
  statusCode: number;
  response?: string;
  errorMessage?: string;
  durationMs: number;
}

export interface EventType {
  value: string;
  label: string;
}

export const webhooksApi = {
  // 获取所有Webhook
  getAllWebhooks: async (): Promise<Webhook[]> => {
    return apiClient.get<Webhook[]>("/api/webhooks");
  },

  // 获取Webhook详情
  getWebhook: async (id: string): Promise<Webhook> => {
    return apiClient.get<Webhook>(`/api/webhooks/${id}`);
  },

  // 创建Webhook
  createWebhook: async (dto: CreateWebhookDto): Promise<Webhook> => {
    return apiClient.post<Webhook>("/api/webhooks", dto);
  },

  // 更新Webhook
  updateWebhook: async (id: string, dto: UpdateWebhookDto): Promise<Webhook> => {
    return apiClient.put<Webhook>(`/api/webhooks/${id}`, dto);
  },

  // 删除Webhook
  deleteWebhook: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/webhooks/${id}`);
  },

  // 测试Webhook
  testWebhook: async (id: string): Promise<WebhookTestResult> => {
    return apiClient.post<WebhookTestResult>(`/api/webhooks/${id}/test`, {});
  },

  // 获取Webhook日志
  getWebhookLogs: async (
    id: string,
    pageNumber: number = 1,
    pageSize: number = 50
  ): Promise<WebhookLog[]> => {
    return apiClient.get<WebhookLog[]>(
      `/api/webhooks/${id}/logs?pageNumber=${pageNumber}&pageSize=${pageSize}`
    );
  },

  // 重试Webhook日志
  retryWebhookLog: async (logId: string): Promise<void> => {
    await apiClient.post(`/api/webhooks/logs/${logId}/retry`, {});
  },

  // 获取事件类型
  getEventTypes: async (): Promise<EventType[]> => {
    return apiClient.get<EventType[]>("/api/webhooks/event-types");
  },
};

