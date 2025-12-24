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

        // Kullanıcı adı (Name) veya Email ile arama yap
        var user = await _context.Users
            .FirstOrDefaultAsync(u => 
                u.Email.ToLower().Trim() == usernameOrEmail || 
                u.Name.ToLower().Trim() == usernameOrEmail);

        if (user == null)
            return null;

        // BCrypt ile şifre kontrolü
        if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            return null;

        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Name = user.Name,
            Role = user.Role.ToString()
        };
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        // E-posta kontrolü
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == registerDto.Email.ToLower());

        if (existingUser != null)
            throw new InvalidOperationException("Bu e-posta adresi zaten kullanılıyor.");

        // Şifreyi BCrypt ile hashle
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

        var user = new User
        {
            Name = registerDto.Name,
            Email = registerDto.Email.ToLower(),
            PasswordHash = passwordHash,
            Role = registerDto.Role,
            StudentNo = registerDto.StudentNo
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Name = user.Name,
            Role = user.Role.ToString()
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

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
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

