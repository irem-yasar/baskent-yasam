import apiClient from '../api/axios';

// Frontend'den gönderilen format
export interface AppointmentRequest {
  teacherId: number; // ✅ ID ile gönder
  course: string;
  reason: string;
  date: string;
  time: string;
  note?: string;
}

// Backend'in beklediği format - direkt root seviyesinde
// Backend artık time'ı string formatında ("HH:mm") kabul ediyor
// studentId göndermemize gerek yok, JWT token'dan otomatik alınıyor
// teacherName, teacherId veya teacherEmail gönderebiliriz
export interface BackendAppointmentRequest {
  teacherName?: string; // Öğretmen adı ile
  teacherId?: number; // Öğretmen ID ile
  teacherEmail?: string; // Öğretmen email ile
  date: string; // ISO datetime string: "2025-12-24T17:55:50.911Z"
  time: string; // TimeSpan string formatı: "HH:mm" (örn: "14:30")
  subject: string;
  requestReason?: string; // Görüşme sebebi (diğer seçeneğinde yazılan metin buraya gider)
}

// Backend'den gelen response formatı
export interface BackendAppointmentResponse {
  id: number;
  studentId: number;
  studentName: string;
  studentNo?: string | null;
  teacherId: number;
  teacherName: string;
  date: string; // ISO date string
  time: string; // TimeSpan string (HH:mm:ss)
  subject: string;
  requestReason: string; // Görüşme sebebi (diğer seçeneğinde yazılan metin burada)
  status: string; // "Pending", "Approved", "Rejected", etc.
  rejectionReason?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

// Frontend'in kullandığı format (backward compatibility)
export interface Appointment {
  id: string;
  studentId: string;
  instructorId: string; // teacherId'den map edilecek
  course: string; // subject'ten map edilecek
  reason: string; // requestReason'dan map edilecek (diğer seçeneğinde yazılan metin burada)
  date: string;
  time: string;
  status: 'pending' | 'approved' | 'rejected';
  rejectionReason?: string | null; // Reddetme sebebi
  createdAt: string;
}

export interface ApiError {
  message: string;
  status?: number;
}

// Randevu oluştur
export const createAppointment = async (
  appointment: AppointmentRequest
): Promise<Appointment> => {
  try {
    // Backend endpoint'i /Appointment (tekil, büyük A ile başlıyor)
    // Backend'in beklediği formata dönüştür
    
    // Date ve time'ı birleştir (ISO format)
    let isoDateTime = '';
    if (appointment.date && appointment.time) {
      // Tarih formatını düzelt (DD.MM.YYYY -> YYYY-MM-DD)
      const dateParts = appointment.date.split('.');
      if (dateParts.length === 3) {
        const [day, month, year] = dateParts;
        const isoDate = `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`;
        isoDateTime = `${isoDate}T${appointment.time}:00.000Z`;
      } else {
        // Eğer zaten YYYY-MM-DD formatındaysa
        isoDateTime = `${appointment.date}T${appointment.time}:00.000Z`;
      }
    }

    // Backend artık time'ı string formatında ("HH:mm") kabul ediyor
    // Time'ı direkt string olarak gönder (örn: "14:30")
    const timeString = appointment.time; // Zaten "HH:mm" formatında

    // Course değerini kontrol et
    if (!appointment.course || appointment.course.trim() === '') {
      throw {
        message: 'Ders alanı boş olamaz',
        status: 400,
      } as ApiError;
    }

    // TeacherId kontrolü
    if (!appointment.teacherId || appointment.teacherId <= 0) {
      throw {
        message: 'Öğretim elemanı seçilmelidir',
        status: 400,
      } as ApiError;
    }

    // Backend'in beklediği format - direkt root seviyesinde
    // studentId göndermemize gerek yok, JWT token'dan otomatik alınıyor
    // ✅ teacherId gönderiyoruz (formdan seçilen teacherId)
    const backendRequest: BackendAppointmentRequest = {
      teacherId: appointment.teacherId, // ✅ ID ile gönder
      date: isoDateTime,
      time: timeString, // String format: "HH:mm" (örn: "14:30")
      subject: appointment.course.trim(), // course -> subject, boşlukları temizle
      requestReason: appointment.reason || '', // Görüşme sebebi (diğer seçeneğinde yazılan metin buraya gider)
    };

    console.log('Teacher ID:', appointment.teacherId);
    console.log('Backend request body:', JSON.stringify(backendRequest, null, 2)); // Debug için - detaylı göster

    const response = await apiClient.post<Appointment>('/Appointment', backendRequest);
    return response.data;
  } catch (error: any) {
    console.error('Create appointment error:', error.response?.data); // Debug için
    
    // Backend validation hatalarını parse et
    let errorMessage = error.response?.data?.title || error.response?.data?.message || 'Randevu oluşturulurken bir hata oluştu';
    
    // 500 hatası için daha detaylı mesaj
    if (error.response?.status === 500) {
      const detailMessage = error.response?.data?.message || error.response?.data?.detail || '';
      if (detailMessage) {
        errorMessage = `Randevu oluşturulurken bir hata oluştu: ${detailMessage}`;
      } else {
        errorMessage = 'Randevu oluşturulurken bir hata oluştu. Lütfen öğretim elemanı adını kontrol edin.';
      }
    } else if (error.response?.data?.errors) {
      // Validation hatalarını birleştir
      const validationErrors: string[] = [];
      Object.keys(error.response.data.errors).forEach((key) => {
        const fieldErrors = error.response.data.errors[key];
        if (Array.isArray(fieldErrors)) {
          fieldErrors.forEach((err: string) => {
            validationErrors.push(`${key}: ${err}`);
          });
        }
      });
      
      if (validationErrors.length > 0) {
        errorMessage = `Validation hataları:\n${validationErrors.join('\n')}`;
      }
    } else if (error.response?.data?.message) {
      errorMessage = error.response.data.message;
    }
    
    throw {
      message: errorMessage,
      status: error.response?.status,
    } as ApiError;
  }
};

// Öğrencinin randevularını getir
export const getStudentAppointments = async (): Promise<Appointment[]> => {
  try {
    // Backend'de my-appointments endpoint'i var
    const response = await apiClient.get<BackendAppointmentResponse[]>('/Appointment/my-appointments');
    
    // Backend response'u frontend formatına dönüştür
    const appointments: Appointment[] = response.data.map(apt => ({
      id: apt.id.toString(),
      studentId: apt.studentId.toString(),
      instructorId: apt.teacherId.toString(), // teacherId → instructorId
      course: apt.subject, // subject → course
      reason: apt.requestReason, // requestReason → reason (diğer seçeneğinde yazılan metin burada)
      date: apt.date,
      time: apt.time.split(':').slice(0, 2).join(':'), // "HH:mm:ss" → "HH:mm"
      status: apt.status.toLowerCase() as 'pending' | 'approved' | 'rejected',
      rejectionReason: apt.rejectionReason, // Reddetme sebebi
      createdAt: apt.createdAt
    }));
    
    return appointments;
  } catch (error: any) {
    throw {
      message: error.response?.data?.message || 'Randevular yüklenirken bir hata oluştu',
      status: error.response?.status,
    } as ApiError;
  }
};

// Öğretim elemanının randevularını getir
export const getInstructorAppointments = async (): Promise<Appointment[]> => {
  try {
    // Backend'de my-appointments endpoint'i var (hem öğrenci hem öğretim elemanı için)
    const response = await apiClient.get<BackendAppointmentResponse[]>('/Appointment/my-appointments');
    
    // DEBUG: Backend'den gelen response'u logla
    console.log("Backend appointments response:", response.data);
    
    // Backend response'u frontend formatına dönüştür
    const appointments: Appointment[] = response.data.map(apt => ({
      id: apt.id.toString(),
      studentId: apt.studentId.toString(),
      instructorId: apt.teacherId.toString(), // teacherId → instructorId
      course: apt.subject, // subject → course
      reason: apt.requestReason, // requestReason → reason (diğer seçeneğinde yazılan metin burada)
      date: apt.date,
      time: apt.time.split(':').slice(0, 2).join(':'), // "HH:mm:ss" → "HH:mm"
      status: apt.status.toLowerCase() as 'pending' | 'approved' | 'rejected',
      rejectionReason: apt.rejectionReason, // Reddetme sebebi
      createdAt: apt.createdAt
    }));
    
    console.log("Mapped appointments:", appointments);
    
    return appointments;
  } catch (error: any) {
    console.error("Get instructor appointments error:", error.response?.data);
    throw {
      message: error.response?.data?.message || 'Randevular yüklenirken bir hata oluştu',
      status: error.response?.status,
    } as ApiError;
  }
};

// Randevu durumunu güncelle (onayla/reddet)
export const updateAppointmentStatus = async (
  appointmentId: string,
  status: 'approved' | 'rejected',
  appointment?: Appointment, // Mevcut appointment bilgileri (opsiyonel)
  rejectionReason?: string
): Promise<Appointment> => {
  try {
    // Backend'de PUT /api/Appointment/{id} kullanılıyor
    // Backend AppointmentStatus enum bekliyor: "Pending", "Approved", "Rejected", "Cancelled", "Completed"
    // Backend artık TimeString ve StatusString kullanıyor (string formatında)
    
    // Sadece status ve rejectionReason gönder (diğer alanlar opsiyonel ve değiştirilmiyor)
    const updateData: any = {
      status: status === 'approved' ? 'Approved' : 'Rejected' // String olarak gönder
    };
    
    // Reddetme sebebi varsa ekle
    if (status === 'rejected' && rejectionReason) {
      updateData.rejectionReason = rejectionReason;
    }
    
    console.log('Updating appointment:', { appointmentId, updateData });
    
    // Backend'den BackendAppointmentResponse dönecek, frontend formatına çevir
    const response = await apiClient.put<BackendAppointmentResponse>(`/Appointment/${appointmentId}`, updateData);
    
    // Backend response'u frontend formatına dönüştür
    const apt = response.data;
    const mappedAppointment: Appointment = {
      id: apt.id.toString(),
      studentId: apt.studentId.toString(),
      instructorId: apt.teacherId.toString(),
      course: apt.subject,
      reason: apt.requestReason,
      date: apt.date,
      time: apt.time.split(':').slice(0, 2).join(':'),
      status: apt.status.toLowerCase() as 'pending' | 'approved' | 'rejected',
      createdAt: apt.createdAt
    };
    
    return mappedAppointment;
  } catch (error: any) {
    console.error('Update appointment error:', error.response?.data);
    throw {
      message: error.response?.data?.message || 'Randevu durumu güncellenirken bir hata oluştu',
      status: error.response?.status,
    } as ApiError;
  }
};

// Randevu sil
export const deleteAppointment = async (appointmentId: string): Promise<void> => {
  try {
    await apiClient.delete(`/Appointment/${appointmentId}`);
  } catch (error: any) {
    throw {
      message: error.response?.data?.message || 'Randevu silinirken bir hata oluştu',
      status: error.response?.status,
    } as ApiError;
  }
};

