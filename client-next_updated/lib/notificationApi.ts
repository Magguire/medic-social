import apiClient from './apiClient';

export type InAppNotification = {
  id: string;
  type: string;
  title: string;
  message: string;
  actionUrl?: string;
  entityType?: string;
  entityId?: string;
  createdAt: string;
  readAt?: string | null;
};

export const notificationApi = {
  list: (unreadOnly = false) => apiClient.get<{ items: InAppNotification[]; unreadCount: number }>(`/api/notifications?unreadOnly=${unreadOnly}`),
  markRead: (id: string) => apiClient.post(`/api/notifications/${id}/read`),
  markAllRead: () => apiClient.post('/api/notifications/read-all'),
};
