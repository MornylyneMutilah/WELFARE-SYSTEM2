import apiClient from './client';

export interface UserResponse {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  employeeId: string;
  departmentId: number;
  departmentName: string;
  roleId: number;
  roleName: string;
  isActive: boolean;
  hireDate: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateUserRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  employeeId: string;
  departmentId: number;
  roleId: number;
  hireDate: string;
  reason: string;
}

export interface UpdateUserRequest {
  email: string;
  firstName: string;
  lastName: string;
  employeeId: string;
  departmentId: number;
  roleId: number;
  isActive: boolean;
  hireDate: string;
  reason: string;
  expectedUpdatedAt?: string;
}

export interface UserOptions {
  roles: { id: number; name: string }[];
  departments: { id: number; name: string }[];
}
export interface UserAudit {
  id: number; action: string; timestamp: string; actorName: string; details: string;
}

export const usersApi = {
  getOptions: async (): Promise<UserOptions> => (await apiClient.get<UserOptions>('/users/options')).data,
  getAudit: async (id: number): Promise<UserAudit[]> => (await apiClient.get<UserAudit[]>(`/users/${id}/audit`)).data,
  setStatus: async (id: number, data: { isActive: boolean; reason: string; expectedUpdatedAt: string }): Promise<UserResponse> =>
    (await apiClient.patch<UserResponse>(`/users/${id}/status`, data)).data,
  getCurrentUser: async (): Promise<UserResponse> => {
    const response = await apiClient.get<UserResponse>('/users/me');
    return response.data;
  },

  getAllUsers: async (): Promise<UserResponse[]> => {
    const response = await apiClient.get<UserResponse[]>('/users');
    return response.data;
  },

  getUserById: async (id: number): Promise<UserResponse> => {
    const response = await apiClient.get<UserResponse>(`/users/${id}`);
    return response.data;
  },

  updateCurrentUser: async (data: {
    email: string;
    firstName: string;
    lastName: string;
  }): Promise<UserResponse> => {
    const response = await apiClient.put<UserResponse>('/users/me', data);
    return response.data;
  },

  createUser: async (data: CreateUserRequest): Promise<UserResponse> => {
    const response = await apiClient.post<UserResponse>('/users', data);
    return response.data;
  },

  updateUser: async (id: number, data: UpdateUserRequest): Promise<UserResponse> => {
    const response = await apiClient.put<UserResponse>(`/users/${id}`, data);
    return response.data;
  },

  deactivateUser: async (id: number, reason: string, expectedUpdatedAt: string): Promise<void> => {
    await apiClient.patch(`/users/${id}/status`, { isActive: false, reason, expectedUpdatedAt });
  },
};
