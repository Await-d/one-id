import { apiClient, API_BASE_URL } from "./apiClient";
import { userManager } from "./oidcConfig";

export interface UserDataExport {
    userId: string;
    exportedAt: string;
    format: string;
    purpose: string;
    data: any;
}

export const gdprApi = {
    async exportUserData(userId: string): Promise<Blob> {
        const user = await userManager.getUser();
        const accessToken = user?.access_token;

        const response = await fetch(`${API_BASE_URL}/api/gdpr/users/${userId}/export`, {
            method: "GET",
            headers: {
                Authorization: `Bearer ${accessToken}`,
            },
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.message || "Export failed");
        }

        return await response.blob();
    },

    async deleteUserData(userId: string, softDelete: boolean = false): Promise<any> {
        return await apiClient.delete(`/api/gdpr/users/${userId}?softDelete=${softDelete}`);
    },
};

