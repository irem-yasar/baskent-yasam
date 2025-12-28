using ApiProject.Data;
using ApiProject.Models;
using ApiProject.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ApiProject.Services
{
    public class ScheduleService
    {
        private readonly AppDbContext _context;

        public ScheduleService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Schedule>> GetScheduleByTeacherIdAsync(int teacherId)
        {
            var schedules = await _context.Schedules
                .Where(s => s.TeacherId == teacherId)
                .ToListAsync();
            
            // DayOfWeek integer'ını Day string'ine çevir (helper property sayesinde otomatik)
            // Helper property zaten getter'da çeviriyor, ekstra işlem gerekmez
            return schedules;
        }

        public async Task<List<Schedule>> SaveOrUpdateScheduleAsync(int teacherId, List<ScheduleDto> slots)
        {
            try
            {
                // Mevcut schedule'ları sil
                var existingSchedules = await _context.Schedules
                    .Where(s => s.TeacherId == teacherId)
                    .ToListAsync();

                if (existingSchedules.Any())
                {
                    _context.Schedules.RemoveRange(existingSchedules);
                    await _context.SaveChangesAsync(); // Önce silme işlemini kaydet
                }

                // Yeni schedule'ları ekle
                var newSchedules = slots.Select(slot => 
                {
                    var schedule = new Schedule
                    {
                        TeacherId = teacherId,
                        CourseName = string.Empty // Veritabanında NOT NULL, boş string gönder
                    };
                    // Day string'ini integer'a çevir
                    schedule.Day = slot.Day; // Helper property otomatik olarak DayOfWeek'e çevirir
                    // TimeSlot string'ini TimeSpan'e çevir
                    schedule.TimeSlot = slot.TimeSlot; // Helper property otomatik olarak StartTime'e çevirir
                    return schedule;
                }).ToList();

                if (newSchedules.Any())
                {
                    _context.Schedules.AddRange(newSchedules);
                    await _context.SaveChangesAsync();
                }

                return newSchedules;
            }
            catch (Exception ex)
            {
                // Inner exception'ı logla
                var innerException = ex.InnerException?.Message ?? ex.Message;
                var fullException = ex.ToString();
                
                Console.WriteLine($"ScheduleService SaveOrUpdateScheduleAsync hatası: {innerException}");
                Console.WriteLine($"Full exception: {fullException}");
                
                // PostgreSQL hatalarını daha anlaşılır hale getir
                if (innerException.Contains("duplicate key") || innerException.Contains("unique constraint"))
                {
                    throw new Exception($"Veritabanı unique constraint hatası: {innerException}", ex);
                }
                
                if (innerException.Contains("null") || innerException.Contains("NOT NULL"))
                {
                    throw new Exception($"Veritabanı null constraint hatası: {innerException}", ex);
                }
                
                // Foreign key constraint hatası
                if (innerException.Contains("foreign key") || innerException.Contains("23503"))
                {
                    if (innerException.Contains("instructor_id") || innerException.Contains("teacher_id"))
                    {
                        throw new Exception(
                            $"Foreign key constraint hatası: instructor_id için users tablosunda kayıt bulunamadı. " +
                            $"TeacherId: {teacherId}. " +
                            $"Detay: {innerException}", ex);
                    }
                    throw new Exception($"Foreign key constraint hatası: {innerException}", ex);
                }
                
                // Table not found hatası
                if (innerException.Contains("relation") && innerException.Contains("does not exist"))
                {
                    throw new Exception(
                        $"Tablo bulunamadı: instructor_schedule tablosu veritabanında yok. " +
                        $"Lütfen create_schedules_table.sql script'ini çalıştırın. " +
                        $"Detay: {innerException}", ex);
                }
                
                // Diğer hatalar için detaylı mesaj
                throw new Exception($"Ders programı kaydedilirken hata oluştu: {innerException}. Full: {fullException}", ex);
            }
        }
    }
}

