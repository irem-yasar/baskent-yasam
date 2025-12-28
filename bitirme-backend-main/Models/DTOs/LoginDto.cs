using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ApiProject.Models.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "Kullanıcı adı veya e-posta gereklidir")]
    [JsonPropertyName("usernameOrEmail")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre gereklidir")]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string? Role { get; set; } // Frontend'den gelen rol (opsiyonel ama kontrol için kullanılacak)
}

