using ApiProject.Data;
using ApiProject.Models;
using ApiProject.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

namespace ApiProject.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(LoginDto loginDto);
    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
    string GenerateJwtToken(User user);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
    {
        var usernameOrEmail = loginDto.UsernameOrEmail?.ToLower().Trim() ?? string.Empty;
        
        if (string.IsNullOrEmpty(usernameOrEmail))
            return null;

        // Login mantığı: CHECK constraint'e göre
        // Student → email ile login
        // AcademicStaff → staff_id ile login
        // Ayrıca FullName ile de login yapılabilir
        var user = await _context.Users
            .FirstOrDefaultAsync(u => 
                (u.Email != null && u.Email.ToLower().Trim() == usernameOrEmail) || 
                (u.StaffId != null && u.StaffId.ToLower().Trim() == usernameOrEmail) ||
                u.FullName.ToLower().Trim() == usernameOrEmail);

        if (user == null)
            return null;

        // BCrypt ile şifre kontrolü
        if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            return null;

        // 🔐 ROL KONTROLÜ: Kullanıcının gerçek rolü ile seçilen rolü karşılaştır
        if (!string.IsNullOrWhiteSpace(loginDto.Role))
        {
            // Kullanıcının gerçek rolü
            var userActualRole = user.RoleId switch
            {
                0 => "Student",
                1 => "AcademicStaff",
                2 => "Staff",
                3 => "Admin",
                _ => "Unknown"
            };

            // Frontend'den gelen rol (normalize et)
            var selectedRole = loginDto.Role.Trim();
            var normalizedSelectedRole = selectedRole.ToLower() switch
            {
                "student" or "öğrenci" => "Student",
                "instructor" or "academicstaff" or "teacher" or "akademik personel" => "AcademicStaff",
                _ => selectedRole // Değişiklik yapma, olduğu gibi bırak
            };

            // Rol eşleşmiyorsa hata döndür
            if (!string.Equals(userActualRole, normalizedSelectedRole, StringComparison.OrdinalIgnoreCase))
            {
                var roleTurkish = userActualRole == "Student" ? "Öğrenci" : "Akademik Personel";
                throw new UnauthorizedAccessException(
                    $"Seçtiğiniz rol ile hesabınızın rolü eşleşmiyor. " +
                    $"Hesabınız '{roleTurkish}' rolüne sahip. " +
                    $"Lütfen doğru rolü seçerek tekrar deneyin."
                );
            }
        }

        var token = GenerateJwtToken(user);

        // RoleId'den string role name'e çevir
        var roleName = user.RoleId switch
        {
            0 => "Student",
            1 => "AcademicStaff",
            2 => "Staff",
            3 => "Admin",
            _ => "Unknown"
        };

        return new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Name = user.FullName,
            Role = roleName
        };
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        // ✅ GÜVENLİ FİNAL KONTROL - Frontend'den gelen verileri kontrol et
        
        // 1. Username veya Email kontrolü
        var username = (registerDto.Username ?? registerDto.Email)?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(username))
            throw new InvalidOperationException("Email veya kullanıcı adı zorunlu");

        // 2. Şifre kontrolü
        if (string.IsNullOrWhiteSpace(registerDto.Password))
            throw new InvalidOperationException("Şifre zorunlu");

        if (registerDto.Password.Length < 6)
            throw new InvalidOperationException("Şifre en az 6 karakter olmalıdır");

        // 3. Rol kontrolü
        var roleString = registerDto.Role?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(roleString))
            throw new InvalidOperationException("Rol zorunlu");

        // Kullanıcı adı kontrolü (FullName veya Email ile kontrol et)
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => 
                (u.FullName != null && u.FullName.ToLower().Trim() == username.ToLower()) ||
                (u.Email != null && u.Email.ToLower().Trim() == username.ToLower()));

        if (existingUser != null)
            throw new InvalidOperationException("Bu kullanıcı adı zaten kullanılıyor.");

        // Role mapping: Hem Türkçe hem İngilizce kabul et
        // Teacher, Instructor gibi yaygın varyasyonları da kabul et (geçici çözüm)
        int roleId = roleString switch
        {
            "Student" or "Öğrenci" => 0,
            "AcademicStaff" or "Akademik Personel" or "Teacher" or "Instructor" => 1,
            _ => throw new InvalidOperationException(
                $"Geçersiz rol: '{roleString}'. Geçerli roller: Student, Öğrenci, AcademicStaff, Akademik Personel, Teacher, Instructor"
            )
        };

        // Şifreyi BCrypt ile hashle
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

        // Veritabanı şemasına göre: email kolonu NOT NULL olabilir
        // Bu durumda hem Student hem AcademicStaff için email gerekli
        // CHECK constraint muhtemelen başka bir şey kontrol ediyor (ör: email veya staff_id'den biri olmalı)
        string? email = null;
        string? staffId = null;

        if (roleId == 0) // Student
        {
            // Student için email gerekli
            email = username.Contains("@") ? username : $"{username}@system.local";
            
            // Email uzunluk kontrolü (DB'de 120 karakter limiti var)
            if (email.Length > 120)
                throw new InvalidOperationException($"Email adresi çok uzun (maksimum 120 karakter). Mevcut: {email.Length}");

            // Email unique kontrolü
            if (await _context.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower()))
                throw new InvalidOperationException("Bu email adresi zaten kayıtlı.");

            staffId = null; // Student için staff_id null
        }
        else if (roleId == 1) // AcademicStaff
        {
            // AcademicStaff için hem email hem staff_id gerekli (email NOT NULL olduğu için)
            // Eğer username email formatında ise direkt kullan, değilse @system.local ekle
            email = username.Contains("@") ? username : $"{username}@system.local";
            
            // Email uzunluk kontrolü
            if (email.Length > 120)
                throw new InvalidOperationException($"Email adresi çok uzun (maksimum 120 karakter). Mevcut: {email.Length}");

            // Email unique kontrolü
            if (await _context.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower()))
                throw new InvalidOperationException("Bu email adresi zaten kayıtlı.");

            // AcademicStaff için staff_id gerekli
            // Eğer username email formatında ise (örn: mdikmen@baskent.edu.tr), @ öncesini al
            staffId = username.Contains("@") 
                ? username.Split('@')[0]  // mdikmen@baskent.edu.tr → mdikmen
                : username;                // mdikmen → mdikmen
            
            // StaffId unique kontrolü
            if (await _context.Users.AnyAsync(u => u.StaffId != null && u.StaffId.ToLower() == staffId.ToLower()))
                throw new InvalidOperationException("Bu staff_id zaten kayıtlı.");
        }

        // DEBUG: SaveChanges öncesi kontrol
        Console.WriteLine($"EMAIL: {email ?? "NULL"}");
        Console.WriteLine($"STAFF_ID: {staffId ?? "NULL"}");
        Console.WriteLine($"ROLE_ID: {roleId}");

        // User entity'yi veritabanı şemasına göre oluştur
        // ❗ ÖNEMLİ: email NOT NULL olduğu için her zaman değer olmalı
        // staffId null olabilir (Student için), empty string OLMAZ!
        var user = new User
        {
            FullName = username,
            Email = email ?? throw new InvalidOperationException("Email zorunlu"), // NOT NULL olduğu için
            PasswordHash = passwordHash,
            RoleId = roleId,
            StaffId = staffId     // null olabilir (Student için), empty string OLMAZ!
        };

        // DB'ye kaydet - Inner exception'ı görmek için try-catch
        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Inner exception'ı al (PostgreSQL hatası burada)
            var innerException = ex.InnerException?.Message ?? ex.Message;
            var fullException = ex.ToString();
            
            // PostgreSQL hatalarını daha anlaşılır hale getir
            if (innerException.Contains("duplicate key") || innerException.Contains("unique constraint"))
            {
                if (innerException.Contains("email"))
                    throw new InvalidOperationException("Bu email adresi zaten kayıtlı.");
                if (innerException.Contains("staff_id"))
                    throw new InvalidOperationException("Bu staff_id zaten kayıtlı.");
                throw new InvalidOperationException($"Veritabanı unique constraint hatası: {innerException}");
            }
            
            if (innerException.Contains("null") || innerException.Contains("NOT NULL"))
            {
                throw new InvalidOperationException($"Veritabanı null constraint hatası: {innerException}");
            }
            
            // Foreign key constraint hatası
            if (innerException.Contains("foreign key") || innerException.Contains("23503"))
            {
                if (innerException.Contains("role_id"))
                {
                    throw new InvalidOperationException(
                        "Foreign key constraint hatası: role_id için roles tablosunda kayıt bulunamadı. " +
                        "Lütfen roles tablosuna id=0 (Student) ve id=1 (AcademicStaff) kayıtlarını ekleyin. " +
                        $"Detay: {innerException}");
                }
                throw new InvalidOperationException($"Foreign key constraint hatası: {innerException}");
            }
            
            // Diğer hatalar için detaylı mesaj
            throw new InvalidOperationException($"Veritabanı hatası: {innerException}. Full: {fullException}");
        }

        // User'ı tekrar oku (EF Core'un SQL üretimini test et)
        var savedUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == user.Id);
        
        if (savedUser == null)
            throw new InvalidOperationException("Kullanıcı kaydedildi ama tekrar okunamadı. EF Core mapping hatası olabilir.");

        var token = GenerateJwtToken(savedUser);

        // JWT'ye string role eklemek için role name'i belirle
        var roleName = roleId switch
        {
            0 => "Student",
            1 => "AcademicStaff",
            _ => "Unknown"
        };

        return new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Name = user.FullName,
            Role = roleName
        };
    }

    public string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey bulunamadı.");
        var issuer = jwtSettings["Issuer"] ?? "ApiProject";
        var audience = jwtSettings["Audience"] ?? "ApiProjectUsers";
        var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "1440"); // Varsayılan 24 saat

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // RoleId'den string role name'e çevir
        var roleName = user.RoleId switch
        {
            0 => "Student",
            1 => "AcademicStaff",
            2 => "Staff",
            3 => "Admin",
            _ => "Unknown"
        };

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("UserId", user.Id.ToString()), // Backend'de GetCurrentUserId() için
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, roleName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

