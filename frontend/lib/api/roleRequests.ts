import apiClient from './client';

export interface RoleAccessRequest {
  id: number;
  userId: number;
  userEmail: string;
  userName: string;
  requestedRole: string;
  status: string;
  createdAt: string;
}

export const roleRequestsApi = {
  list: async (): Promise<RoleAccessRequest[]> => {
    const response = await apiClient.get<RoleAccessRequest[]>('/role-requests');
    return response.data;
  },
  decide: async (id: number, approve: boolean): Promise<void> => {
    await apiClient.post(`/role-requests/${id}/decision`, { approve });
  },
};
