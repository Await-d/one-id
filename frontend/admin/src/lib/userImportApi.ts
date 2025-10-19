import { apiClient } from "./apiClient";

export interface UserImportResult {
  totalRows: number;
  successCount: number;
  failureCount: number;
  errors: UserImportError[];
  createdUserIds: string[];
}

export interface UserImportError {
  rowNumber: number;
  userName: string;
  email: string;
  errorMessage: string;
}

export interface ImportInstructions {
  requiredColumns: string[];
  optionalColumns: string[];
  defaultRole: string;
  passwordRequirements: string;
  maxFileSize: string;
  supportedFormats: string[];
  notes: string[];
}

export const userImportApi = {
  async uploadCsv(file: File, defaultRole?: string): Promise<UserImportResult> {
    const formData = new FormData();
    formData.append("file", file);
    if (defaultRole) {
      formData.append("defaultRole", defaultRole);
    }

    // apiClient 会自动处理 FormData
    return apiClient.post<UserImportResult>("/api/userimport/upload", formData);
  },

  async downloadSample(): Promise<Blob> {
    // 对于下载文件，我们需要获取原始响应
    const url = `/api/userimport/sample`;
    const response = await fetch(
      `${import.meta.env.VITE_API_URL || (import.meta.env.DEV ? "http://localhost:5102" : window.location.origin)}${url}`,
      {
        method: "GET",
        credentials: "include",
      }
    );

    if (!response.ok) {
      throw new Error("Failed to download sample");
    }

    return response.blob();
  },

  async getInstructions(): Promise<ImportInstructions> {
    return apiClient.get<ImportInstructions>("/api/userimport/instructions");
  },
};

