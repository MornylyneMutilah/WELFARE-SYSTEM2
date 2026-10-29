import apiClient from './client';

export interface DepartmentResponse {
  id: number;
  name: string;
  code: string;
  description: string;
}

export interface CreateDepartmentRequest {
  name: string;
  code: string;
  description: string;
}

export interface UpdateDepartmentRequest {
  name: string;
  code: string;
  description: string;
}

export const departmentsApi = {
  getAllDepartments: async (): Promise<DepartmentResponse[]> => {
    const response = await apiClient.get<DepartmentResponse[]>('/departments');
    return response.data;
  },

  getDepartmentById: async (id: number): Promise<DepartmentResponse> => {
    const response = await apiClient.get<DepartmentResponse>(`/departments/${id}`);
    return response.data;
  },

  createDepartment: async (data: CreateDepartmentRequest): Promise<DepartmentResponse> => {
    const response = await apiClient.post<DepartmentResponse>('/departments', data);
    return response.data;
  },

  updateDepartment: async (id: number, data: UpdateDepartmentRequest): Promise<DepartmentResponse> => {
    const response = await apiClient.put<DepartmentResponse>(`/departments/${id}`, data);
    return response.data;
  },
};
