import apiClient from './client';

export interface DocumentResponse {
  id: number;
  applicationId: number;
  fileName: string;
  fileType: string;
  fileSize: number;
  filePath: string;
  uploadedAt: string;
}

export const documentsApi = {
  getDocumentById: async (id: number): Promise<DocumentResponse> => {
    const response = await apiClient.get<DocumentResponse>(`/documents/${id}`);
    return response.data;
  },

  getDocumentsByApplicationId: async (applicationId: number): Promise<DocumentResponse[]> => {
    const response = await apiClient.get<DocumentResponse[]>(`/documents/application/${applicationId}`);
    return response.data;
  },

  uploadDocument: async (applicationId: number, file: File): Promise<DocumentResponse> => {
    const formData = new FormData();
    formData.append('file', file);
    
    const response = await apiClient.post<DocumentResponse>(`/documents/application/${applicationId}`, formData);
    return response.data;
  },

  deleteDocument: async (id: number): Promise<void> => {
    await apiClient.delete(`/documents/${id}`);
  },
};
