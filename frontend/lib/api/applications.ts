import apiClient from './client';

export interface ApplicationResponse {
  id: number;
  userId: number;
  userName: string;
  welfareProgramId: number;
  programName: string;
  status: number;
  statusName: string;
  requestedAmount: number;
  reason: string;
  isEligible: boolean;
  anomalyFlags: string;
  ruleEvaluationResult: string;
  submittedAt: string;
  updatedAt: string;
}

export interface CreateApplicationRequest {
  welfareProgramId: number;
  requestedAmount: number;
  reason: string;
}

export interface UpdateApplicationRequest {
  requestedAmount: number;
  reason: string;
}

export interface StatusUpdateRequest {
  status: number;
  comments: string;
}

export const applicationsApi = {
  getApplications: async (): Promise<ApplicationResponse[]> => {
    const response = await apiClient.get<ApplicationResponse[]>('/applications');
    return response.data;
  },

  getAllApplications: async (): Promise<ApplicationResponse[]> => {
    const response = await apiClient.get<ApplicationResponse[]>('/applications/all');
    return response.data;
  },

  getApplicationById: async (id: number): Promise<ApplicationResponse> => {
    const response = await apiClient.get<ApplicationResponse>(`/applications/${id}`);
    return response.data;
  },

  createApplication: async (data: CreateApplicationRequest): Promise<ApplicationResponse> => {
    const response = await apiClient.post<ApplicationResponse>('/applications', data);
    return response.data;
  },

  updateApplication: async (id: number, data: UpdateApplicationRequest): Promise<ApplicationResponse> => {
    const response = await apiClient.put<ApplicationResponse>(`/applications/${id}`, data);
    return response.data;
  },

  submitApplication: async (id: number): Promise<ApplicationResponse> => {
    const response = await apiClient.post<ApplicationResponse>(`/applications/${id}/submit`);
    return response.data;
  },

  cancelApplication: async (id: number): Promise<void> => {
    await apiClient.delete(`/applications/${id}`);
  },

  updateApplicationStatus: async (id: number, data: StatusUpdateRequest): Promise<ApplicationResponse> => {
    const response = await apiClient.put<ApplicationResponse>(`/applications/${id}/status`, data);
    return response.data;
  },
};
