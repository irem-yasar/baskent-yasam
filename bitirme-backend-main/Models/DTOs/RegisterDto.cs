using System.ComponentModel.DataAnnotations;
using ApiProject.Models;

namespace ApiProject.Models.DTOs;

public class RegisterDto
{
    [Required(ErrorMessage = "Ad Soyad gereklidir")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta adresi gereklidir")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre gereklidir")]
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rol gereklidir")]
    public UserRole Role { get; set; } = UserRole.Student;

    [MaxLength(50)]
    public string? StudentNo { get; set; }
}

