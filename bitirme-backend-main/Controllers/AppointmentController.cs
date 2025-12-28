using ApiProject.Models.DTOs;
using ApiProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ApiProject.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Student,AcademicStaff")]
public class AppointmentController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly ILogger<AppointmentController> _logger;

    public AppointmentController(IAppointmentService appointmentService, ILogger<AppointmentController> logger)
    {
        _appointmentService = appointmentService;
        _logger = logger;
    }

    /// <summary>
    /// Tüm randevuları getirir (sadece admin)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AppointmentResponseDto>>> GetAllAppointments()
    {
        try
        {
            var appointments = await _appointmentService.GetAllAppointmentsAsync();
            var response = appointments.Select(a => MapToDto(a)).ToList();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Randevular getirilirken hata oluştu");
            return StatusCode(500, "Randevular getirilirken bir hata oluştu");
        }
    }

    /// <summary>
    /// ID'ye göre randevu getirir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AppointmentResponseDto>> GetAppointmentById(int id)
    {
        try
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null)
                return NotFound($"ID: {id} olan randevu bulunamadı");

            // Kullanıcının kendi randevusu olduğunu kontrol et
            var userEmail = GetCurrentUserEmail();
            var userRole = GetCurrentUserRole();
            if (!string.IsNullOrEmpty(userEmail) && userRole != "Admin")
            {
                if (appointment.Student?.Email.ToLower() != userEmail.ToLower() && 
                    appointment.Teacher?.Email.ToLower() != userEmail.ToLower())
                {
                    return Forbid("Bu randevuya erişim yetkiniz yok");
                }
            }

            return Ok(MapToDto(appointment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Randevu getirilirken hata oluştu");
            return StatusCode(500, "Randevu getirilirken bir hata oluştu");
        }
    }

    /// <summary>
    /// Giriş yapmış kullanıcının randevularını getirir (öğrenci veya öğretmen)
    /// </summary>
    [HttpGet("my-appointments")]
    public async Task<ActionResult<List<AppointmentResponseDto>>> GetMyAppointments()
    {
        try
        {
            // JWT token'dan UserId'yi al (email yerine - AcademicStaff için email null olabilir)
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized("Kullanıcı bilgisi bulunamadı");

            var userRole = GetCurrentUserRole();
            List<Models.Appointment> appointments;

            // Case-insensitive role kontrolü
            if (string.Equals(userRole, "AcademicStaff", StringComparison.OrdinalIgnoreCase))
            {
                // Teacher randevularını UserId ile getir
                appointments = await _appointmentService.GetAppointmentsByTeacherIdAsync(currentUserId.Value);
                _logger.LogInformation("Hoca randevuları getiriliyor. UserId: {UserId}, Role: {Role}, Bulunan randevu sayısı: {Count}", 
                    currentUserId.Value, userRole, appointments.Count);
                
                // DEBUG: Randevuları logla
                foreach (var apt in appointments)
                {
                    _logger.LogInformation("Randevu - Id: {Id}, StudentId: {StudentId}, TeacherId: {TeacherId}, Status: {Status}", 
                        apt.Id, apt.StudentId, apt.TeacherId, apt.Status);
                }
            }
            else
            {
                // Student randevularını UserId ile getir
                appointments = await _appointmentService.GetAppointmentsByStudentIdAsync(currentUserId.Value);
                _logger.LogInformation("Öğrenci randevuları getiriliyor. UserId: {UserId}, Role: {Role}, Bulunan randevu sayısı: {Count}", 
                    currentUserId.Value, userRole, appointments.Count);
            }

            var response = appointments.Select(a => MapToDto(a)).ToList();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Randevular getirilirken hata oluştu");
            return StatusCode(500, "Randevular getirilirken bir hata oluştu");
        }
    }

    /// <summary>
    /// Yeni randevu oluşturur
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AppointmentResponseDto>> CreateAppointment([FromBody] object requestBody)
    {
        try
        {
            AppointmentCreateDto dto;
            
            // Frontend'in gönderdiği formatı kontrol et
            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var jsonDoc = System.Text.Json.JsonDocument.Parse(json);
            
            // Eğer "dto" field'ı varsa wrapper kullan
            if (jsonDoc.RootElement.TryGetProperty("dto", out var dtoElement))
            {
                dto = System.Text.Json.JsonSerializer.Deserialize<AppointmentCreateDto>(dtoElement.GetRawText()) 
                    ?? throw new ArgumentException("dto field'ı geçersiz format");
            }
            else
            {
                // Direkt DTO formatında gönderilmiş
                dto = System.Text.Json.JsonSerializer.Deserialize<AppointmentCreateDto>(json) 
                    ?? throw new ArgumentException("Request body geçersiz format");
            }

            // Öğretmen bilgisi kontrolü - daha açıklayıcı hata mesajı
            if (!dto.TeacherId.HasValue && string.IsNullOrWhiteSpace(dto.TeacherName) && string.IsNullOrWhiteSpace(dto.TeacherEmail))
            {
                return BadRequest(new { 
                    message = "Öğretmen bilgisi gereklidir. Lütfen teacherId, teacherName veya teacherEmail alanlarından birini gönderin.",
                    receivedData = new { 
                        date = dto.Date, 
                        time = dto.TimeString, 
                        subject = dto.Subject,
                        teacherId = dto.TeacherId,
                        teacherName = dto.TeacherName,
                        teacherEmail = dto.TeacherEmail
                    }
                });
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // JWT token'dan kullanıcı ID'sini al (öğrenci için)
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized("Kullanıcı bilgisi bulunamadı");

            // DEBUG: StudentId ve TeacherId'yi logla
            _logger.LogInformation("StudentId from token: {StudentId}, TeacherId from request: {TeacherId}", 
                currentUserId.Value, dto.TeacherId);

            var appointment = await _appointmentService.CreateAppointmentAsync(dto, currentUserId);
            return CreatedAtAction(nameof(GetAppointmentById), new { id = appointment.Id }, MapToDto(appointment));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Randevu oluşturulurken validasyon hatası: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Inner exception'ı logla - gerçek hatayı görmek için
            var innerException = ex.InnerException?.Message ?? ex.Message;
            var fullException = ex.ToString();
            
            _logger.LogError(ex, "Randevu oluşturulurken hata oluştu. Inner Exception: {InnerException}, Full: {FullException}", 
                innerException, fullException);
            
            // Gerçek hatayı frontend'e gönder
            return BadRequest(new { 
                message = $"Randevu oluşturulurken bir hata oluştu: {innerException}",
                innerException = innerException,
                fullException = fullException
            });
        }
    }

    /// <summary>
    /// Randevu günceller (Onayla/Reddet)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<AppointmentResponseDto>> UpdateAppointment(int id, [FromBody] AppointmentUpdateDto dto)
    {
        try
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null)
                return NotFound($"ID: {id} olan randevu bulunamadı");

            // Kullanıcının bu randevuyu güncelleme yetkisi var mı kontrol et
            var currentUserId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            
            if (currentUserId == null)
                return Unauthorized("Kullanıcı bilgisi bulunamadı");

            // Admin değilse, sadece kendi randevusunu güncelleyebilir
            if (!string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                // Hoca ise: Sadece kendi randevularını (TeacherId) güncelleyebilir
                // Öğrenci ise: Sadece kendi randevularını (StudentId) güncelleyebilir
                bool canUpdate = false;
                
                if (string.Equals(userRole, "AcademicStaff", StringComparison.OrdinalIgnoreCase))
                {
                    // Hoca sadece kendi randevularını onaylayabilir/reddedebilir
                    canUpdate = appointment.TeacherId == currentUserId.Value;
                }
                else if (string.Equals(userRole, "Student", StringComparison.OrdinalIgnoreCase))
                {
                    // Öğrenci sadece kendi randevularını güncelleyebilir (genelde iptal eder)
                    canUpdate = appointment.StudentId == currentUserId.Value;
                }

                if (!canUpdate)
                {
                    _logger.LogWarning("Yetkisiz randevu güncelleme denemesi. UserId: {UserId}, Role: {Role}, AppointmentId: {AppointmentId}, TeacherId: {TeacherId}, StudentId: {StudentId}",
                        currentUserId.Value, userRole, id, appointment.TeacherId, appointment.StudentId);
                    return Forbid("Bu randevuyu güncelleme yetkiniz yok");
                }
            }

            // Randevuyu güncelle
            var updatedAppointment = await _appointmentService.UpdateAppointmentAsync(id, dto);
            if (updatedAppointment == null)
                return NotFound($"ID: {id} olan randevu güncellenemedi");

            _logger.LogInformation("Randevu güncellendi. AppointmentId: {AppointmentId}, Status: {Status}, UserId: {UserId}, Role: {Role}",
                id, dto.Status, currentUserId.Value, userRole);

            return Ok(MapToDto(updatedAppointment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Randevu güncellenirken hata oluştu");
            return StatusCode(500, "Randevu güncellenirken bir hata oluştu");
        }
    }

    /// <summary>
    /// Randevu siler
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAppointment(int id)
    {
        try
        {
            var result = await _appointmentService.DeleteAppointmentAsync(id);
            if (!result)
                return NotFound($"ID: {id} olan randevu bulunamadı");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Randevu silinirken hata oluştu");
            return StatusCode(500, "Randevu silinirken bir hata oluştu");
        }
    }

    private AppointmentResponseDto MapToDto(Models.Appointment appointment)
    {
        return new AppointmentResponseDto
        {
            Id = appointment.Id,
            StudentId = appointment.StudentId,
            StudentName = appointment.Student?.FullName ?? "Bilinmiyor", // FullName kullan
            StudentNo = appointment.Student?.StaffId, // StaffId kullan
            TeacherId = appointment.TeacherId,
            TeacherName = appointment.Teacher?.FullName ?? "Bilinmiyor", // FullName kullan
            Date = appointment.Date,
            Time = appointment.Time,
            Subject = appointment.Subject,
            RequestReason = appointment.RequestReason,
            Status = appointment.Status.ToString(),
            RejectionReason = appointment.RejectionReason,
            CreatedAt = appointment.CreatedAt,
            UpdatedAt = appointment.UpdatedAt
        };
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
            return userId;
        return null;
    }

    private string? GetCurrentUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value;
    }

    private string? GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value;
    }

    /// <summary>
    /// Debug endpoint - JWT token'dan user bilgilerini gösterir
    /// </summary>
    [HttpGet("debug")]
    public IActionResult Debug()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var name = User.FindFirst(ClaimTypes.Name)?.Value;

        return Ok(new { 
            id, 
            email, 
            role, 
            name,
            allClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
        });
    }
}

