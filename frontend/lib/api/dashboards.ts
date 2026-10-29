import apiClient from './client';

export const dashboardsApi = {
  employee: async () => (await apiClient.get('/dashboards/employee')).data,
  hr: async () => (await apiClient.get('/dashboards/hr')).data,
  committee: async () => (await apiClient.get('/dashboards/committee')).data,
  management: async () => (await apiClient.get('/dashboards/management')).data,
  admin: async () => (await apiClient.get('/dashboards/admin')).data,
};
