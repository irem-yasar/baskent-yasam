using ApiProject.Models;
using ApiProject.Models.DTOs;
using ApiProject.Data;
using Microsoft.EntityFrameworkCore;

namespace ApiProject.Services;

public interface IAppointmentService
{
    Task<List<Appointment>> GetAllAppointmentsAsync();
    Task<Appointment?> GetAppointmentByIdAsync(int id);
    Task<Appointment> CreateAppointmentAsync(AppointmentCreateDto dto, int? currentUserId = null);
    Task<Appointment?> UpdateAppointmentAsync(int id, AppointmentUpdateDto dto);
    Task<bool> DeleteAppointmentAsync(int id);
    Task<List<Appointment>> GetAppointmentsByStudentEmailAsync(string email);
    Task<List<Appointment>> GetAppointmentsByTeacherEmailAsync(string email);
    Task<List<Appointment>> GetAppointmentsByStudentIdAsync(int studentId);
    Task<List<Appointment>> GetAppointmentsByTeacherIdAsync(int teacherId);
}

public class AppointmentService : IAppointmentService
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;

    public AppointmentService(AppDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<List<Appointment>> GetAllAppointmentsAsync()
    {
        return await _context.Appointments
            .Include(a => a.Student)
            .Include(a => a.Teacher)
            .OrderByDescending(a => a.Date)
            .ToListAsync();
    }

    public async Task<Appointment?> GetAppointmentByIdAsync(int id)
    {
        return await _context.Appointments
            .Include(a => a.Student)
            .Include(a => a.Teacher)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Appointment> CreateAppointmentAsync(AppointmentCreateDto dto, int? currentUserId = null)
    {
        // Öğrenci ID'sini belirle (dto'dan veya currentUserId'den)
        int studentId = dto.StudentId ?? currentUserId ?? throw new ArgumentException("Öğrenci ID gereklidir.");
        
        // DEBUG: StudentId ve TeacherId'yi logla
        Console.WriteLine($"StudentId from token: {studentId}");
        Console.WriteLine($"TeacherId from request: {dto.TeacherId}");
        
        var student = await _context.Users.FindAsync(studentId);
        if (student == null)
            throw new ArgumentException($"Öğrenci bulunamadı. StudentId: {studentId}");

        // Öğretmen ID'sini belirle - ID ile bul (isimle arama yapma!)
        User? teacher = null;
        
        if (dto.TeacherId.HasValue && dto.TeacherId.Value > 0)
        {
            // ✅ DOĞRU: ID ile bul, RoleId ile filtrele
            teacher = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == dto.TeacherId.Value && u.RoleId == (int)UserRole.AcademicStaff);
            
            if (teacher == null)
                throw new ArgumentException($"Öğretim elemanı bulunamadı. TeacherId: {dto.TeacherId} ile eşleşen AcademicStaff rolüne sahip kullanıcı bulunamadı.");
        }
        else
        {
            // TeacherId zorunlu - frontend'den gönderilmeli
            throw new ArgumentException("TeacherId gereklidir. Lütfen frontend'den teacherId gönderin.");
        }

        // DEBUG: Appointment oluşturulmadan önce değerleri logla
        Console.WriteLine($"Creating appointment - StudentId: {studentId}, TeacherId: {teacher.Id}");
        
        var appointment = new Appointment
        {
            StudentId = studentId,  // 🔥 KRİTİK: JWT'den alınan StudentId
            TeacherId = teacher.Id, // Frontend'den gelen TeacherId
            Date = dto.Date,
            Time = dto.Time,
            Subject = dto.Subject,
            RequestReason = dto.RequestReason ?? string.Empty, // Frontend'den gelen görüşme sebebi
            Status = AppointmentStatus.Pending,
            CreatedAt = DateTime.Now
        };

        // DEBUG: Appointment entity değerlerini logla
        Console.WriteLine($"Appointment entity - StudentId: {appointment.StudentId}, TeacherId: {appointment.TeacherId}, RequestReason: {appointment.RequestReason}");

        try
        {
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Inner exception'ı logla - gerçek hatayı görmek için
            var innerException = ex.InnerException?.Message ?? ex.Message;
            var fullException = ex.ToString();
            
            throw new Exception($"Veritabanı hatası: {innerException}. Full exception: {fullException}", ex);
        }

        // İlişkili verileri yükle (bildirim için)
        await _context.Entry(appointment)
            .Reference(a => a.Student)
            .LoadAsync();
        await _context.Entry(appointment)
            .Reference(a => a.Teacher)
            .LoadAsync();

        // Null check - Student ve Teacher yüklenmiş olmalı
        if (appointment.Student == null)
            throw new InvalidOperationException($"Öğrenci bilgisi yüklenemedi. StudentId: {appointment.StudentId}");
        if (appointment.Teacher == null)
            throw new InvalidOperationException($"Öğretmen bilgisi yüklenemedi. TeacherId: {appointment.TeacherId}");

        // Öğrenciye bildirim gönder (SignalR ile canlı bildirim)
        await _notificationService.SendNotificationAsync(
            "Randevu Talebi Oluşturuldu",
            $"Sayın {appointment.Student.FullName}, {appointment.Date:dd.MM.yyyy} tarihinde {appointment.Time:hh\\:mm} saatinde {appointment.Teacher.FullName} hocasına randevu talebiniz oluşturulmuştur. Hocanızın onayını bekliyor.",
            NotificationType.AppointmentCreated,
            appointment.Student.Email,
            appointment.Student.Id, // 🔥 KRİTİK: Öğrenci UserId
            appointment.Id
        );

        // Hocaya bildirim gönder (SignalR ile canlı bildirim)
        await _notificationService.SendNotificationAsync(
            "Yeni Randevu Talebi",
            $"Sayın {appointment.Teacher.FullName}, {appointment.Student.FullName} ({appointment.Student.StaffId ?? "N/A"}) öğrencisi {appointment.Date:dd.MM.yyyy} tarihinde {appointment.Time:hh\\:mm} saatinde randevu talebinde bulunmuştur. Konu: {appointment.Subject}",
            NotificationType.AppointmentCreated,
            appointment.Teacher.Email,
            appointment.Teacher.Id, // 🔥 KRİTİK: Öğretmen UserId
            appointment.Id
        );

        return appointment;
    }

    public async Task<Appointment?> UpdateAppointmentAsync(int id, AppointmentUpdateDto dto)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Student)
            .Include(a => a.Teacher)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
            return null;

        // Randevu bilgileri güncelleme
        if (dto.Date.HasValue)
            appointment.Date = dto.Date.Value;

        if (dto.Time.HasValue)
            appointment.Time = dto.Time.Value;

        if (!string.IsNullOrEmpty(dto.Subject))
            appointment.Subject = dto.Subject;

        // Reddetme sebebi güncelleme
        if (!string.IsNullOrEmpty(dto.RejectionReason))
        {
            appointment.RejectionReason = dto.RejectionReason;
        }

        // Durum değişikliği (Hoca onay/red işlemi)
        if (dto.Status.HasValue)
        {
            var oldStatus = appointment.Status;
            appointment.Status = dto.Status.Value;
            appointment.UpdatedAt = DateTime.Now; // Güncelleme zamanını kaydet

            // Durum değişikliğinde bildirim gönder
            var notificationType = dto.Status.Value switch
            {
                AppointmentStatus.Approved => NotificationType.AppointmentConfirmed,
                AppointmentStatus.Rejected => NotificationType.AppointmentCancelled,
                AppointmentStatus.Cancelled => NotificationType.AppointmentCancelled,
                AppointmentStatus.Completed => NotificationType.AppointmentConfirmed,
                _ => NotificationType.General
            };

            var statusMessage = dto.Status.Value switch
            {
                AppointmentStatus.Approved => "onaylanmıştır",
                AppointmentStatus.Rejected => "reddedilmiştir",
                AppointmentStatus.Cancelled => "iptal edilmiştir",
                AppointmentStatus.Completed => "tamamlanmıştır",
                _ => "güncellenmiştir"
            };

            // Bildirim mesajı oluştur
            var notificationMessage = dto.Status.Value == AppointmentStatus.Rejected && !string.IsNullOrEmpty(dto.RejectionReason)
                ? $"Sayın {appointment.Student.FullName}, {appointment.Date:dd.MM.yyyy} tarihinde {appointment.Time:hh\\:mm} saatindeki {appointment.Teacher.FullName} hocasına olan randevu talebiniz {statusMessage}. Sebep: {dto.RejectionReason}"
                : $"Sayın {appointment.Student.FullName}, {appointment.Date:dd.MM.yyyy} tarihinde {appointment.Time:hh\\:mm} saatindeki {appointment.Teacher.FullName} hocasına olan randevu talebiniz {statusMessage}.";

            // Öğrenciye bildirim (SignalR ile canlı bildirim)
            await _notificationService.SendNotificationAsync(
                $"Randevu Talebi {statusMessage}",
                notificationMessage,
                notificationType,
                appointment.Student.Email,
                appointment.Student.Id, // 🔥 KRİTİK: Öğrenci UserId
                appointment.Id
            );
        }

        await _context.SaveChangesAsync();
        return appointment;
    }

    public async Task<bool> DeleteAppointmentAsync(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment == null)
            return false;

        _context.Appointments.Remove(appointment);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Appointment>> GetAppointmentsByStudentEmailAsync(string email)
    {
        // Email'i normalize et (küçük harfe çevir, trim yap)
        var normalizedEmail = email?.ToLower().Trim() ?? string.Empty;
        
        return await _context.Appointments
            .Include(a => a.Student)
            .Include(a => a.Teacher)
            .Where(a => a.Student != null && a.Student.Email.ToLower().Trim() == normalizedEmail)
            .OrderByDescending(a => a.Date)
            .ToListAsync();
    }

    public async Task<List<Appointment>> GetAppointmentsByTeacherEmailAsync(string email)
    {
        // Email'i normalize et (küçük harfe çevir, trim yap)
        var normalizedEmail = email?.ToLower().Trim() ?? string.Empty;
        
        return await _context.Appointments
            .Include(a => a.Student)
            .Include(a => a.Teacher)
            .Where(a => a.Teacher != null && a.Teacher.Email != null && a.Teacher.Email.ToLower().Trim() == normalizedEmail)
            .OrderByDescending(a => a.Date)
            .ToListAsync();
    }

    public async Task<List<Appointment>> GetAppointmentsByStudentIdAsync(int studentId)
    {
        return await _context.Appointments
            .Include(a => a.Student)
            .Include(a => a.Teacher)
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.Date)
            .ToListAsync();
    }

    public async Task<List<Appointment>> GetAppointmentsByTeacherIdAsync(int teacherId)
    {
        return await _context.Appointments
            .Include(a => a.Student)
            .Include(a => a.Teacher)
            .Where(a => a.TeacherId == teacherId)
            .OrderByDescending(a => a.Date)
            .ToListAsync();
    }
}
