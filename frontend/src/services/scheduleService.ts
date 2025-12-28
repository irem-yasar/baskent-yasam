import apiClient from '../api/axios';

export interface ScheduleSlot {
  day: string; // "Pzt", "Sal", "Çar", "Per", "Cum"
  timeSlot: string; // "09.00-09.50", "10.00-10.50", etc.
}

export interface ScheduleResponse {
  id: number;
  teacherId: number;
  day: string;
  timeSlot: string;
}

export interface ApiError {
  message: string;
  status?: number;
}

export const getMySchedule = async (): Promise<ScheduleResponse[]> => {
  try {
    const response = await apiClient.get<ScheduleResponse[]>('/Schedule/my-schedule');
    return response.data;
  } catch (error: any) {
    console.error("Get schedule error:", error.response?.data);
    throw {
      message: error.response?.data?.message || 'Ders programı yüklenirken bir hata oluştu',
      status: error.response?.status,
    } as ApiError;
  }
};

export const saveSchedule = async (slots: ScheduleSlot[]): Promise<ScheduleResponse[]> => {
  try {
    const response = await apiClient.post<ScheduleResponse[]>('/Schedule', {
      slots: slots
    });
    return response.data;
  } catch (error: any) {
    console.error("Save schedule error:", error.response?.data);
    throw {
      message: error.response?.data?.message || 'Ders programı kaydedilirken bir hata oluştu',
      status: error.response?.status,
    } as ApiError;
  }
};

