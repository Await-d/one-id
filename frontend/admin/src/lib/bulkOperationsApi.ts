import { apiClient } from "./apiClient";

export interface BulkOperationResult {
  success: boolean;
  totalCount: number;
  successCount: number;
  failureCount: number;
  succeededUserIds: string[];
  errors: BulkOperationError[];
  message: string;
}

export interface BulkOperationError {
  userId: string;
  userIdentifier: string;
  errorMessage: string;
}

export interface AssignRolesRequest {
  userIds: string[];
  roleNames: string[];
}

export interface RemoveRolesRequest {
  userIds: string[];
  roleNames: string[];
}

export interface LockUsersRequest {
  userIds: string[];
  lockoutEndUtc?: string; // ISO 8601 格式
}

export interface ResetPasswordsRequest {
  userIds: string[];
  sendEmail: boolean;
}

export const bulkOperationsApi = {
  /**
   * 批量分配角色
   */
  async assignRoles(request: AssignRolesRequest): Promise<BulkOperationResult> {
    return apiClient.post<BulkOperationResult>("/api/bulkoperations/assign-roles", request);
  },

  /**
   * 批量撤销角色
   */
  async removeRoles(request: RemoveRolesRequest): Promise<BulkOperationResult> {
    return apiClient.post<BulkOperationResult>("/api/bulkoperations/remove-roles", request);
  },

  /**
   * 批量启用用户
   */
  async enableUsers(userIds: string[]): Promise<BulkOperationResult> {
    return apiClient.post<BulkOperationResult>("/api/bulkoperations/enable-users", { userIds });
  },

  /**
   * 批量禁用用户
   */
  async disableUsers(userIds: string[]): Promise<BulkOperationResult> {
    return apiClient.post<BulkOperationResult>("/api/bulkoperations/disable-users", { userIds });
  },

  /**
   * 批量锁定用户
   */
  async lockUsers(request: LockUsersRequest): Promise<BulkOperationResult> {
    return apiClient.post<BulkOperationResult>("/api/bulkoperations/lock-users", request);
  },

  /**
   * 批量解锁用户
   */
  async unlockUsers(userIds: string[]): Promise<BulkOperationResult> {
    return apiClient.post<BulkOperationResult>("/api/bulkoperations/unlock-users", { userIds });
  },

  /**
   * 批量撤销会话
   */
  async revokeSessions(userIds: string[]): Promise<BulkOperationResult> {
    return apiClient.post<BulkOperationResult>("/api/bulkoperations/revoke-sessions", { userIds });
  },

  /**
   * 批量重置密码
   */
  async resetPasswords(request: ResetPasswordsRequest): Promise<BulkOperationResult> {
    return apiClient.post<BulkOperationResult>("/api/bulkoperations/reset-passwords", request);
  },

  /**
   * 批量删除用户
   */
  async deleteUsers(userIds: string[]): Promise<BulkOperationResult> {
    return apiClient.post<BulkOperationResult>("/api/bulkoperations/delete-users", { userIds });
  },
};

