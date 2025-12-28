using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ApiProject.Models.DTOs;

public class RegisterDto
{
    // Frontend hem username hem email gönderebilir, ikisi de optional
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Şifre gereklidir")]
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rol gereklidir")]
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty; // "Öğrenci" veya "Akademik Personel"
}

