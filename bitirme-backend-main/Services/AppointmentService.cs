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
        var student = await _context.Users.FindAsync(studentId);
        if (student == null)
            throw new ArgumentException($"Öğrenci bulunamadı. StudentId: {studentId}");

        // Öğretmen ID'sini belirle (dto'dan, adından veya email'inden)
        User? teacher = null;
        if (dto.TeacherId.HasValue && dto.TeacherId.Value > 0)
        {
            teacher = await _context.Users.FindAsync(dto.TeacherId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(dto.TeacherName))
        {
            var teacherNameLower = dto.TeacherName.ToLower().Trim();
            // Önce tam eşleşme dene
            teacher = await _context.Users
                .FirstOrDefaultAsync(u => u.Name.ToLower().Trim() == teacherNameLower && u.Role == UserRole.Teacher);
            
            // Tam eşleşme yoksa, isim içinde arama yap (partial match)
            if (teacher == null)
            {
                teacher = await _context.Users
                    .FirstOrDefaultAsync(u => 
                        u.Name.ToLower().Contains(teacherNameLower) && 
                        u.Role == UserRole.Teacher);
            }
            
            // Hala bulunamadıysa, ilk kelimeyi eşleştir (örn: "Mehmet Dikmen" -> "Mehmet")
            if (teacher == null)
            {
                var firstWord = teacherNameLower.Split(' ').FirstOrDefault();
                if (!string.IsNullOrEmpty(firstWord))
                {
                    teacher = await _context.Users
                        .FirstOrDefaultAsync(u => 
                            u.Name.ToLower().Trim().StartsWith(firstWord) && 
                            u.Role == UserRole.Teacher);
                }
            }
            
            // Hala bulunamadıysa, ters yönde arama yap
            if (teacher == null)
            {
                var allTeachers = await _context.Users
                    .Where(u => u.Role == UserRole.Teacher)
                    .ToListAsync();
                
                teacher = allTeachers.FirstOrDefault(u => 
                    teacherNameLower.Contains(u.Name.ToLower().Trim()) || 
                    u.Name.ToLower().Trim().Contains(teacherNameLower));
            }
        }
        else if (!string.IsNullOrWhiteSpace(dto.TeacherEmail))
        {
            teacher = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower().Trim() == dto.TeacherEmail.ToLower().Trim() && u.Role == UserRole.Teacher);
        }

        if (teacher == null)
        {
            var errorMsg = "Öğretmen bulunamadı. ";
            if (dto.TeacherId.HasValue)
                errorMsg += $"TeacherId: {dto.TeacherId} ile eşleşen öğretmen bulunamadı. ";
            if (!string.IsNullOrWhiteSpace(dto.TeacherName))
                errorMsg += $"TeacherName: '{dto.TeacherName}' ile eşleşen öğretmen bulunamadı. ";
            if (!string.IsNullOrWhiteSpace(dto.TeacherEmail))
                errorMsg += $"TeacherEmail: '{dto.TeacherEmail}' ile eşleşen öğretmen bulunamadı. ";
            errorMsg += "Lütfen öğretim elemanı adını kontrol edin.";
            throw new ArgumentException(errorMsg);
        }

        if (teacher.Role != UserRole.Teacher)
            throw new ArgumentException($"Belirtilen kullanıcı öğretmen değil. UserId: {teacher.Id}");

        var appointment = new Appointment
        {
            StudentId = studentId,
            TeacherId = teacher.Id,
            Date = dto.Date,
            Time = dto.Time,
            Subject = dto.Subject,
            RequestReason = dto.RequestReason ?? string.Empty, // Frontend'den gelen görüşme sebebi
            Status = AppointmentStatus.Pending,
            CreatedAt = DateTime.Now
        };

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
            $"Sayın {appointment.Student.Name}, {appointment.Date:dd.MM.yyyy} tarihinde {appointment.Time:hh\\:mm} saatinde {appointment.Teacher.Name} hocasına randevu talebiniz oluşturulmuştur. Hocanızın onayını bekliyor.",
            NotificationType.AppointmentCreated,
            appointment.Student.Email,
            appointment.Student.Id, // 🔥 KRİTİK: Öğrenci UserId
            appointment.Id
        );

        // Hocaya bildirim gönder (SignalR ile canlı bildirim)
        await _notificationService.SendNotificationAsync(
            "Yeni Randevu Talebi",
            $"Sayın {appointment.Teacher.Name}, {appointment.Student.Name} ({appointment.Student.StudentNo ?? "N/A"}) öğrencisi {appointment.Date:dd.MM.yyyy} tarihinde {appointment.Time:hh\\:mm} saatinde randevu talebinde bulunmuştur. Konu: {appointment.Subject}",
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

        // Durum değişikliği (Hoca onay/red işlemi)
        if (dto.Status.HasValue)
        {
            var oldStatus = appointment.Status;
            appointment.Status = dto.Status.Value;

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

            // Öğrenciye bildirim (SignalR ile canlı bildirim)
            await _notificationService.SendNotificationAsync(
                $"Randevu Talebi {statusMessage}",
                $"Sayın {appointment.Student.Name}, {appointment.Date:dd.MM.yyyy} tarihinde {appointment.Time:hh\\:mm} saatindeki {appointment.Teacher.Name} hocasına olan randevu talebiniz {statusMessage}.",
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
            .Where(a => a.Teacher != null && a.Teacher.Email.ToLower().Trim() == normalizedEmail)
            .OrderByDescending(a => a.Date)
            .ToListAsync();
    }
}
