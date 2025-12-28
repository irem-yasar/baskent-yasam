using ApiProject.Models.DTOs;
using ApiProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ApiProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ScheduleController : ControllerBase
    {
        private readonly ScheduleService _scheduleService;
        private readonly ILogger<ScheduleController> _logger;

        public ScheduleController(ScheduleService scheduleService, ILogger<ScheduleController> logger)
        {
            _scheduleService = scheduleService;
            _logger = logger;
        }

        // JWT token'dan UserId'yi al
        private int? GetCurrentUserId()
        {
            // Önce "UserId" claim'ini dene (yeni token'larda var)
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                return userId;
            
            // Eğer yoksa ClaimTypes.NameIdentifier'ı kullan (eski token'lar için)
            userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out userId))
                return userId;
            
            return null;
        }

        // JWT token'dan Role'ü al
        private string? GetCurrentUserRole()
        {
            return User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        }

        // GET: api/Schedule/my-schedule
        [HttpGet("my-schedule")]
        public async Task<ActionResult<List<ScheduleResponseDto>>> GetMySchedule()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized("Kullanıcı bilgisi bulunamadı");

                var userRole = GetCurrentUserRole();
                if (string.IsNullOrEmpty(userRole) || !string.Equals(userRole, "AcademicStaff", StringComparison.OrdinalIgnoreCase))
                    return Forbid("Sadece öğretim elemanları ders programı görüntüleyebilir");

                var schedules = await _scheduleService.GetScheduleByTeacherIdAsync(currentUserId.Value);

                var response = schedules.Select(s => new ScheduleResponseDto
                {
                    Id = s.Id,
                    TeacherId = s.TeacherId,
                    Day = s.Day,
                    TimeSlot = s.TimeSlot
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ders programı getirilirken hata oluştu");
                return StatusCode(500, "Ders programı getirilirken bir hata oluştu");
            }
        }

        // POST: api/Schedule
        [HttpPost]
        public async Task<ActionResult<List<ScheduleResponseDto>>> SaveSchedule([FromBody] ScheduleUpdateDto dto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                    return Unauthorized("Kullanıcı bilgisi bulunamadı");

                var userRole = GetCurrentUserRole();
                if (string.IsNullOrEmpty(userRole) || !string.Equals(userRole, "AcademicStaff", StringComparison.OrdinalIgnoreCase))
                    return Forbid("Sadece öğretim elemanları ders programı kaydedebilir");

                if (dto.Slots == null || dto.Slots.Count == 0)
                    return BadRequest("En az bir zaman dilimi seçilmelidir");

                var schedules = await _scheduleService.SaveOrUpdateScheduleAsync(currentUserId.Value, dto.Slots);

                var response = schedules.Select(s => new ScheduleResponseDto
                {
                    Id = s.Id,
                    TeacherId = s.TeacherId,
                    Day = s.Day,
                    TimeSlot = s.TimeSlot
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ders programı kaydedilirken hata oluştu");
                _logger.LogError(ex, "Inner exception: {InnerException}", ex.InnerException?.Message);
                _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
                
                // Daha detaylı hata mesajı döndür
                var errorMessage = ex.Message;
                var innerException = ex.InnerException?.Message;
                
                // Kullanıcıya daha anlaşılır mesaj göster
                var userFriendlyMessage = errorMessage;
                if (innerException != null && !errorMessage.Contains(innerException))
                {
                    userFriendlyMessage = $"{errorMessage}. Detay: {innerException}";
                }
                
                return StatusCode(500, new { 
                    message = userFriendlyMessage,
                    error = innerException ?? errorMessage
                });
            }
        }
    }
}

