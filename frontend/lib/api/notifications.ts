import apiClient from './client';

export interface NotificationResponse {
  id: number;
  userId: number;
  title: string;
  message: string;
  type: string; // Info, Success, Warning, Error
  entityId?: string;
  entityType?: string;
  isRead: boolean;
  createdAt: string;
}

export interface NotificationListResponse {
  notifications: NotificationResponse[];
  totalCount: number;
  unreadCount: number;
}

export const notificationsApi = {
  getUserNotifications: async (): Promise<NotificationListResponse> => {
    const response = await apiClient.get<NotificationListResponse>('/notifications');
    return response.data;
  },

  getUnreadNotifications: async (): Promise<NotificationListResponse> => {
    const response = await apiClient.get<NotificationListResponse>('/notifications/unread');
    return response.data;
  },

  getUnreadCount: async (): Promise<{ count: number }> => {
    const response = await apiClient.get<{ count: number }>('/notifications/unread-count');
    return response.data;
  },

  markAsRead: async (id: number): Promise<NotificationResponse> => {
    const response = await apiClient.put<NotificationResponse>(`/notifications/${id}/read`, { isRead: true });
    return response.data;
  },

  markAllAsRead: async (): Promise<{ message: string }> => {
    const response = await apiClient.put<{ message: string }>('/notifications/read-all');
    return response.data;
  },
};
