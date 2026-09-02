import { apiClient } from "@/lib/api-client";
import { User, UserRole } from "@/features/auth/types/auth.types";
import { PagedUsersResponse, UserListQuery } from "../types/user.types";

export const userService = {
  async getMe(): Promise<User> {
    return apiClient.get<User>("/api/users/me");
  },

  async getUsers(query?: UserListQuery): Promise<PagedUsersResponse> {
    const params = new URLSearchParams();
    if (query?.page) params.append("page", String(query.page));
    if (query?.pageSize) params.append("pageSize", String(query.pageSize));

    return apiClient.get<PagedUsersResponse>(
      `/api/users${params.toString() ? `?${params.toString()}` : ""}`
    );
  },

  async updateUserRole(userId: string, role: UserRole): Promise<void> {
    await apiClient.put(`/api/users/${userId}/role`, { role });
  },
};
