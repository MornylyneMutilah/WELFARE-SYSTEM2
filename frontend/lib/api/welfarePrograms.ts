import apiClient from './client';

export interface WelfareProgramResponse {
  id: number;
  name: string;
  description: string;
  category: string;
  maxAmount: number;
  minServicePeriod: number;
  requiredDocuments: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateWelfareProgramRequest {
  name: string;
  description: string;
  category: string;
  maxAmount: number;
  minServicePeriod: number;
  requiredDocuments: string;
}

export interface UpdateWelfareProgramRequest {
  name: string;
  description: string;
  category: string;
  maxAmount: number;
  minServicePeriod: number;
  requiredDocuments: string;
  isActive: boolean;
}

export const welfareProgramsApi = {
  getAllPrograms: async (): Promise<WelfareProgramResponse[]> => {
    const response = await apiClient.get<WelfareProgramResponse[]>('/welfareprograms');
    return response.data;
  },

  getActivePrograms: async (): Promise<WelfareProgramResponse[]> => {
    const response = await apiClient.get<WelfareProgramResponse[]>('/welfareprograms/active');
    return response.data;
  },

  getProgramById: async (id: number): Promise<WelfareProgramResponse> => {
    const response = await apiClient.get<WelfareProgramResponse>(`/welfareprograms/${id}`);
    return response.data;
  },

  createProgram: async (data: CreateWelfareProgramRequest): Promise<WelfareProgramResponse> => {
    const response = await apiClient.post<WelfareProgramResponse>('/welfareprograms', data);
    return response.data;
  },

  updateProgram: async (id: number, data: UpdateWelfareProgramRequest): Promise<WelfareProgramResponse> => {
    const response = await apiClient.put<WelfareProgramResponse>(`/welfareprograms/${id}`, data);
    return response.data;
  },

  deactivateProgram: async (id: number): Promise<void> => {
    await apiClient.delete(`/welfareprograms/${id}`);
  },
};
