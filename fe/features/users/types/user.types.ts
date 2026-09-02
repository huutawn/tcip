import { User, UserRole } from "@/features/auth/types/auth.types";

export interface PagedUsersResponse {
  items: User[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface UserListQuery {
  page?: number;
  pageSize?: number;
}

export interface UpdateUserRoleRequest {
  role: UserRole;
}
