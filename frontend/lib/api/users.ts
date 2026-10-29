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
}

export interface UpdateUserRequest {
  email: string;
  firstName: string;
  lastName: string;
  employeeId: string;
  departmentId: number;
  roleId: number;
  isActive: boolean;
}

export const usersApi = {
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

  deactivateUser: async (id: number): Promise<void> => {
    await apiClient.delete(`/users/${id}`);
  },
};
