import apiClient from './client';

export interface Anomaly {
  id: number;
  anomalyType: string;
  severity: string;
  description: string;
  applicationId?: number;
  userId?: number;
  status: string;
  detectedAt: string;
  resolvedAt?: string;
  resolvedBy?: number;
  resolutionNotes?: string;
  anomalyScore?: number;
  detectionDetails?: string;
}

export interface AnomalySummary {
  severityCounts: Record<string, number>;
  typeCounts: Record<string, number>;
  totalAnomalies: number;
}

export interface ResolveRequest {
  resolvedBy: number;
  notes: string;
}

export interface DismissRequest {
  resolvedBy: number;
  notes: string;
}

export const anomaliesApi = {
  getAll: async (params?: { severity?: string; status?: string }): Promise<Anomaly[]> => {
    const queryParams = new URLSearchParams();
    if (params?.severity) queryParams.append('severity', params.severity);
    if (params?.status) queryParams.append('status', params.status);

    const response = await apiClient.get<Anomaly[]>(`/anomalies?${queryParams.toString()}`);
    return response.data;
  },

  getById: async (id: number): Promise<Anomaly> => {
    const response = await apiClient.get<Anomaly>(`/anomalies/${id}`);
    return response.data;
  },

  getByApplicationId: async (applicationId: number): Promise<Anomaly[]> => {
    const response = await apiClient.get<Anomaly[]>(`/anomalies/application/${applicationId}`);
    return response.data;
  },

  getByUserId: async (userId: number): Promise<Anomaly[]> => {
    const response = await apiClient.get<Anomaly[]>(`/anomalies/user/${userId}`);
    return response.data;
  },

  getByDepartmentId: async (departmentId: number): Promise<Anomaly[]> => {
    const response = await apiClient.get<Anomaly[]>(`/anomalies/department/${departmentId}`);
    return response.data;
  },

  getSummary: async (): Promise<AnomalySummary> => {
    const response = await apiClient.get<AnomalySummary>('/anomalies/stats/summary');
    return response.data;
  },

  resolve: async (id: number, data: ResolveRequest): Promise<void> => {
    await apiClient.post(`/anomalies/${id}/resolve`, data);
  },

  investigate: async (id: number): Promise<void> => {
    await apiClient.post(`/anomalies/${id}/investigate`);
  },

  dismiss: async (id: number, data: DismissRequest): Promise<void> => {
    await apiClient.post(`/anomalies/${id}/dismiss`, data);
  },

  detectForApplication: async (applicationId: number): Promise<Anomaly[]> => {
    const response = await apiClient.post<Anomaly[]>(`/anomalies/detect/application/${applicationId}`);
    return response.data;
  },
};
