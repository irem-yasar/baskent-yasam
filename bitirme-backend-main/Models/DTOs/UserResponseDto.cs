namespace ApiProject.Models.DTOs;

public class UserResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? StudentNo { get; set; }
    public string? Email { get; set; } // Email eklendi (select box'ta gösterilmek için)
}

