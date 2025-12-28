import apiClient from '../api/axios';

export interface Notification {
  id: number;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  createdAt: string;
  readAt?: string | null;
  appointmentId?: number | null;
}

export interface ApiError {
  message: string;
  status?: number;
}

// Kullanıcının bildirimlerini getir
export const getNotifications = async (): Promise<Notification[]> => {
  try {
    const response = await apiClient.get<Notification[]>('/Notification');
    return response.data;
  } catch (error: any) {
    throw {
      message: error.response?.data?.message || 'Bildirimler yüklenirken bir hata oluştu',
      status: error.response?.status,
    } as ApiError;
  }
};

// Okunmamış bildirimleri getir
export const getUnreadNotifications = async (): Promise<Notification[]> => {
  try {
    const response = await apiClient.get<Notification[]>('/Notification/unread');
    return response.data;
  } catch (error: any) {
    throw {
      message: error.response?.data?.message || 'Okunmamış bildirimler yüklenirken bir hata oluştu',
      status: error.response?.status,
    } as ApiError;
  }
};

// Bildirimi okundu olarak işaretle
export const markAsRead = async (notificationId: number): Promise<Notification> => {
  try {
    const response = await apiClient.put<Notification>(`/Notification/${notificationId}/read`);
    return response.data;
  } catch (error: any) {
    throw {
      message: error.response?.data?.message || 'Bildirim okundu olarak işaretlenirken bir hata oluştu',
      status: error.response?.status,
    } as ApiError;
  }
};

